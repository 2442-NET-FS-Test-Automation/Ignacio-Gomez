// This class will hold the business logic/db retry logic for fulfilling transactions
using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Concurrent;

namespace Library.Api.Fulfillment;

// ASP.NET's builder (DI container) NEEDS us to provide 2 things when we register a service
// An interface and a concrete implementation. These can both go in the same file.
public interface IFulfillmentService
{
    public Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
    public Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct);
    public int ResolveProductId(string sku);
}

// Im going to stick everything about order fulfillment in this file
// Requests are either Fulfilled or Backordered - no other results possible
public enum FulfillmentResult { Fulfilled, Backordered } 

// Also going to make a record for the result of a Burst (many orders at the same time)
// records are lightweight custom types that allow for comparison with == 
public record BurstResult(int Fulfilled, int Backordered);

public class FulfillmentService : IFulfillmentService
{
    // ASP.NET manages the creation (and destruction) of all our dependencies across our app
    // If we need a DbContext or DbContextFactory or Logger or any other dependency 
    // we DO NOT instantiate one here, we ask for one via the Constructor
    private readonly IDbContextFactory<LibraryDbContext> _factory; // holds my factory
    private readonly BurstPlanner _planner; //holds my BurstPlanner object
    private readonly ConcurrentDictionary<string, int> _skuToProductId;

    // The factory in the constructor arguments list comes from the ASP.NET DI Container
    public FulfillmentService(IDbContextFactory<LibraryDbContext> factory, BurstPlanner planner)
    {
        _factory = factory;
        _planner = planner;

        // Storing skus and their product Id's so we can do O(1) lookup if we need it later
        using var db = _factory.CreateDbContext();
        _skuToProductId = new ConcurrentDictionary<string, int>(
            db.Products.ToDictionary(p => p.Sku, p => p.Id)
        );

    }

    // Method to resolve Skus to productIds using that dictionary
    public int ResolveProductId(string sku)
    {
        try { return _skuToProductId[sku]; }
        catch (KeyNotFoundException) { throw new UnknownSkuException(sku); }
    }


    // This method is going to handle fulfillment - its gonna be a bit long. Which is why we didn't 
    // just write all of this in Program.cs
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        // First - we need a db context 
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Lets grab our order from the database
        // FLow for this - a customer places an order. It hits the order table - we are now fulfilling that order
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId, ct); // LINQ with Async

        // Lets create that dictionary with the productId Key and the OrderId value
        // yay for LINQ/Collections namespace
        var requested = order.Lines.ToDictionary(l => l.ProductId, l => l.Quantity);

        // creating a flag for "can i continue fulfilling this order"
        bool canFulfill = true; 

        foreach (OrderLine line in order.Lines)
        {
            // First - grab the current inventory from the db for that product
            InventoryItem inv = await db.Inventory.FirstAsync(i => i.ProductId == line.ProductId, ct);

            // Next - check if we can meet the order 
            if (inv.CurrentStock < line.Quantity)
            {
                canFulfill = false;
                break; 
            }

            inv.CurrentStock -= line.Quantity; // This write to the InventoryItem table is guarded by RowVersion
        }  

        // assuming we broke out of the foreach and cannot fulfill the order
        if (!canFulfill) // checking for canFulfill == false
        {
            // We can't fulfill this order, its now Backordered
            order.Status = Status.Backordered;

            // Create a new fulfillment event record for this transaction, setting it to backordered.
            db.FulfillmentEvents.Add(new FulfillmentEvent { OrderId = orderId, Type = "Backorder" });

            await db.SaveChangesAsync(ct); 

            // Log the transaction, using the Serilog structured logging syntax
            Log.Warning("Backordered {OrderId}: insufficient stock", orderId);
            
            return FulfillmentResult.Backordered; 
        }

        // If we make it here, we CAN fulfill that order
        order.Status = Status.Fulfilled;
        order.CompletedUtc = DateTime.UtcNow;
        db.FulfillmentEvents.Add(new FulfillmentEvent { OrderId = orderId, Type = "Fulfilled"});

        // Adding our retry save method
        if (!await SaveWithRetryAsync(db, requested, ct)) // if we enter this if - we lost enough times
        {// that stock dropped this order was backordered
            db.ChangeTracker.Clear(); // clear change tracker
            Order staleOrder = await db.Orders.FirstAsync(o => o.Id == orderId, ct); //grab stale order from db
            staleOrder.Status = Status.Backordered; // set its status to backordered
            Log.Warning("Backordered order {OrderId} after concurrency retry", orderId);
            return FulfillmentResult.Backordered;
        }
        
        Log.Information("Fulfilled order: {OrderId}, {LineCount} lines", orderId, order.Lines.Count);
        return FulfillmentResult.Fulfilled;
    }

    // Lets break the logic for saving with retry (via RowVersion) into its own method
    // just to help keep things straight. IReadOnlyDictionary just makes any dict we pass in readonly
    private static async Task<bool> SaveWithRetryAsync(
        LibraryDbContext db, IReadOnlyDictionary<int, int> requestedByProductId, CancellationToken ct)
    {
        
        // This is that RowVersion Change Tracker entry retry from yesterday
        // NEW: Loop forever until we run out of stock
        while (true)
        {
            
            // Our loop as written never exits - it does increment attempt for us.
            // If we retry and fail x amount of times - we will throw an exception manually
            try
            {
                // The DbContext inside this method came from FulfillOneAsync - if it has changes 
                // staged to it - we can save them here. Its the same object.
                await db.SaveChangesAsync(ct);
                return true;
            }
            // We can tell our try catch how many times to handle this exception for us
            // After 3 attempts - we won't enter the catch. It bubbles up to wherever this method 
            // was called
            catch (DbUpdateConcurrencyException ex) 
            {   
                Log.Warning("Attempty retry");
                
                // Retry logic - remember that Change Tracker stuff?
                // entry is an EF Core Change tracker entry
                foreach (var entry in ex.Entries)
                {
                    
                    var current = await entry.GetDatabaseValuesAsync(); // grab the current database values

                    // Is some other user deleted the entry out from under us... we can't save
                    // return false
                    if (current is null) return false;

                    // Set the OriginalValues bucket on the entry to what they currently are
                    entry.OriginalValues.SetValues(current);

                    if (entry.Entity is InventoryItem inv)
                    {
                        // Grab the current totals for that item's stock
                        int freshValue = current.GetValue<int>(nameof(InventoryItem.CurrentStock));
                        //Dictionary lookup against the dict we passed in
                        int desiredAmount = requestedByProductId[inv.ProductId];

                        // Re-check on the fresh stock - don't blindly trust it
                        if (freshValue < desiredAmount) return false; // this is now our exit condition
                        inv.CurrentStock = freshValue - desiredAmount;
                    }
                }
            }
        }
    }

    public async Task<BurstResult> FulfillBurstAsync(IEnumerable<int> orderIds, CancellationToken ct)
    {   
        // Grabbing all my orderIds
        List<int> idList = orderIds.ToList();

        List<Order> orders; // place to store my orders
        
        // Calling on a dbcontext that we discard after we're done
        await using (var db = await _factory.CreateDbContextAsync(ct))
        {   // Look in our db, grab every order that appears in our idList
            orders = await db.Orders.Where(o => idList.Contains(o.Id)).ToListAsync();
        }

        // Calling on our planning logic inside BurstPlanner
        // planned contains our expedited/priority first order
        var planned = _planner.OrderByPriority(orders);
        
        // we are just going to piggyback off of FulfilOneAsync - no need to rewrite logic we can just call it again
        var tasks = planned.Select(id => FulfillOneAsync(id, ct)); // each call will get its own dbContext

        // Await here until all tasks in the collection are complete
        var results = await Task.WhenAll(tasks);

        return new BurstResult(
            Fulfilled: results.Count(r => r == FulfillmentResult.Fulfilled),
            Backordered: results.Count(r => r == FulfillmentResult.Backordered)
        );

    }

}