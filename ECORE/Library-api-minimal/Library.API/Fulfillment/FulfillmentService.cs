// This class will hold the businees logic/db retry logic for fulfilling transactions
using Library.Data;
using Library.Data.Entities;
using Library.Date.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Library.API.Fulfillment;

// ASP.NET builder (DI container) NEED us to provide 2 things when we register a service
// An interface and concrete implementation. These can both go in the same life.

public interface IFulfillmentService
{
    public Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
    public Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct);
}

// Im going to stick everything about order fulfillment in this life
// Request are either fulfilled or backordered

public enum FulfillmentResult { Fulfilled, Backordered }

// Also going to make a record for the result of a burst
// recods are ligthweight custom types that allow for comparison with == 
public record BurstResult(int Fulfilled, int Backordered);
public class FulfillmentService : IFulfillmentService 
{
    //ASP.NET manages the creation and destruction of all ourt dependencies acroos our app
    //If we need a DBcontext
    private readonly IDbContextFactory<LibraryDbContext> _factory;
    private readonly BurstPlanner _planner;
    public FulfillmentService(IDbContextFactory<LibraryDbContext> factory, BurstPlanner planner)
    {
        _factory = factory;
        _planner = planner;
    }

    //This method is going to handle fulfiment - its gone be a bit long. Which
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        // First - we need a dbcontext
        await using var db = await _factory.CreateDbContextAsync(ct);
        // Lets grab our order from databse
        // Flow for this - a customer places an order. It hits the order table - we are fulfilling
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId, ct);
        //Creating a flag for "can i continue fulfilling this order"
        //
        var requested = order.Lines.ToDictionary(l => l.ProductId, l => l.OrderId);

        

        bool canFulfill = true;
        foreach(OrderLines line in order.Lines)
        {
            //First - grab the current inventory from the db for that product
            InventoryItem inv = await db.Inventory.FirstAsync(i => i.ProductId == line.ProductId, ct);
            // Next - check if we can meet the order 
            if (inv.CurrentStock < line.Quantity)
            {
                canFulfill = false;
                break;
            }
            inv.CurrentStock -= line.Quantity;

        }
        //assuming we broke out of the foreach and cannot fulfill the order
        if (!canFulfill)
        {
            order.Status = Status.Backordered;
            db.FulfillmentEvents.Add(new FulfillmentEvent { OrderId = orderId, Type = "Backorder"});
            await db.SaveChangesAsync(ct);
            Log.Warning("Backorderd {OrderId}:insufficient stock", orderId);
            return FulfillmentResult.Backordered;
        }

        order.Status = Status.Fulfilled;
        order.CompletedUtc = DateTime.UtcNow;
        db.FulfillmentEvents.Add(new FulfillmentEvent {OrderId = orderId, Type = "Fulfilled"});

        if(!await SaveWithRetryAsync(db, requested, ct))
        {
            db.ChangeTracker.Clear();
            Order staleOrder = await db.Orders.FirstAsync(o => o.Id == orderId, ct);
            staleOrder.Status = Status.Backordered;
            Log.Warning("Backordered order{OrderId} after concurency  retry", orderId);
            return FulfillmentResult.Backordered;
        }

        await db.SaveChangesAsync(ct);
        Log.Information("Fulfilled order: {OrderId}, {LineCount} lines", orderId, order.Lines.Count);
        return FulfillmentResult.Fulfilled;
    }
    // Lets break the logic for saving with retry (via rowversion) into its own method
    private static async Task<bool> SaveWithRetryAsync(
        LibraryDbContext db, IReadOnlyDictionary<int, int> requestedByProductId, CancellationToken ct)
    {
        //NEW: loop forever until we out of stock
        while(true)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                //Retry logic
                foreach(var entry in ex.Entries)
                {
                    var current = await entry.GetDatabaseValuesAsync();
                    if (current is null) return false;
                    entry.OriginalValues.SetValues(current);
                    if(entry.Entity is InventoryItem inv)
                    {
                        // Grab the current for that item stock
                        int freshValue = current.GetValue<int>(nameof(InventoryItem.CurrentStock));
                        //Dictionary lookup against the dict 
                        int desiredAmount = requestedByProductId[inv.ProductId];
                        if(freshValue < desiredAmount) return false;
                        inv.CurrentStock = freshValue - desiredAmount;

                    }
                }
            }
        }
    }

    public async Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct)
    {
        var idList = orderIds.ToList();
        List<Order> orders; 
        await using (var db = await _factory.CreateDbContextAsync(ct))
        {
            orders = await db.Orders.Where(o => idList.Contains(o.Id)).ToListAsync();
        }

        var planned = _planner.OrderByPriority(orders);


        //We are just going to piggyback off of fulfilloneasync -no need to rewrite logic we can just call it agina
        var tasks = planned.Select(id => FulfillOneAsync(id, ct));
        var results = await Task.WhenAll(tasks);
        return new BurstResult(
            Fulfilled: results.Count(r => r == FulfillmentResult.Fulfilled),
            Backordered: results.Count(r => r == FulfillmentResult.Backordered)
        );
    }
}