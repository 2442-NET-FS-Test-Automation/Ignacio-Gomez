using BloomRush.Data;
using BloomRush.Data.Entities;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BloomRush.Api.Fulfillment;

public interface IFulfillmentService
{
    Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);

    Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManySequentialAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct);

    Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManyConcurrentAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct);

    Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillBurstAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct);
}

public enum FulfillmentResult
{
    Fulfilled,
    Backordered
}

// This service has the main fulfillment logic:
// check stock, update inventory, update order status, and save an audit event.
public class FulfillmentService : IFulfillmentService
{
    // If two requests touch the same inventory row, EF may throw a concurrency error.
    // We try again a few times with fresh data before backordering the order.
    private const int MaxAttempts = 3;

    private readonly IDbContextFactory<BloomRushDbContext> _factory;

    public FulfillmentService(IDbContextFactory<BloomRushDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                // Fulfillment creates its own DbContext from the factory.
                // This is safer for burst/concurrent work than sharing one scoped DbContext.
                await using var db = await _factory.CreateDbContextAsync(ct);

                // Load the order with its lines because each line asks for product stock.
                var order = await db.Orders
                    .Include(order => order.Lines)
                    .FirstAsync(order => order.Id == orderId, ct);

                // Do not process the same order twice.
                if (order.Status == Status.Fulfilled)
                {
                    return FulfillmentResult.Fulfilled;
                }

                if (order.Status != Status.Pending)
                {
                    return FulfillmentResult.Backordered;
                }

                // Inventory changes, order status, and fulfillment event should save together.
                await using var transaction = await db.Database.BeginTransactionAsync(ct);

                var productIds = order.Lines
                    .Select(line => line.ProductId)
                    .ToList();

                // Load only the inventory rows needed by this order.
                var inventoryItems = await db.InventoryItems
                    .Where(item => productIds.Contains(item.ProductId))
                    .ToListAsync(ct);

                bool canFulfill = true;

                // Check stock first. If one product is missing or too low, the whole order backorders.
                foreach (var line in order.Lines)
                {
                    var inventory = inventoryItems
                        .FirstOrDefault(item => item.ProductId == line.ProductId);

                    if (inventory == null || inventory.QuantityOnHand < line.Quantity)
                    {
                        canFulfill = false;
                        break;
                    }
                }

                var now = DateTime.UtcNow;

                if (canFulfill)
                {
                    // Enough stock: subtract the requested quantity from each inventory row.
                    foreach (var line in order.Lines)
                    {
                        var inventory = inventoryItems
                            .First(item => item.ProductId == line.ProductId);

                        inventory.QuantityOnHand -= line.Quantity;
                    }

                    order.Status = Status.Fulfilled;
                    order.CompletedAtUtc = now;

                    db.FulfillmentEvents.Add(new FulfillmentEvent
                    {
                        OrderId = order.Id,
                        Type = FulfillmentEventType.Fulfilled,
                        Message = "Order fulfilled.",
                        TimestampUtc = now
                    });

                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return FulfillmentResult.Fulfilled;
                }

                // Not enough stock: do not subtract inventory, only mark the order as Backordered.
                order.Status = Status.Backordered;
                order.CompletedAtUtc = now;

                db.FulfillmentEvents.Add(new FulfillmentEvent
                {
                    OrderId = order.Id,
                    Type = FulfillmentEventType.Backordered,
                    Message = "Order backordered because there was not enough stock.",
                    TimestampUtc = now
                });

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return FulfillmentResult.Backordered;
            }
            catch (DbUpdateConcurrencyException)
            {
                // RowVersion conflict: another request saved stock first.
                // The next loop creates a new DbContext and reads fresh data.
                Log.Warning(
                    "Stock changed while fulfilling order {OrderId}. Attempt {Attempt} of {MaxAttempts}",
                    orderId,
                    attempt,
                    MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    return await BackorderOrderAsync(
                        orderId,
                        "Order backordered after repeated stock conflicts.",
                        ct);
                }
            }
        }

        return await BackorderOrderAsync(
            orderId,
            "Order backordered after repeated stock conflicts.",
            ct);
    }

    public async Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManySequentialAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        var results = new List<BurstFulfillmentItemResult>();

        foreach (var orderId in orderIds)
        {
            var result = await FulfillOneAsync(orderId, ct);

            Log.Information(
                "Sequential fulfillment completed for order {OrderId} with result {Result}",
                orderId,
                result);

            // Record call: create a small response item with the order id and result.
            results.Add(new BurstFulfillmentItemResult(orderId, result));
        }

        return results;
    }

    public async Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillBurstAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        return await FulfillManyConcurrentAsync(orderIds, ct);
    }

    public async Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManyConcurrentAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        //Here lunch all the task and wait them.
        var tasks = orderIds.Select(async orderId =>
        {
            var result = await FulfillOneAsync(orderId, ct);

            Log.Information(
                "Concurrent fulfillment completed for order {OrderId} with result {Result}",
                orderId,
                result);

            // Record call: each task returns one BurstFulfillmentItemResult.
            return new BurstFulfillmentItemResult(orderId, result);
        });

        return await Task.WhenAll(tasks);
    }

    private async Task<FulfillmentResult> BackorderOrderAsync(
        int orderId,
        string message,
        CancellationToken ct)
    {
        // Used only after repeated concurrency conflicts.
        // It keeps the final fallback simple and avoids leaving the order Pending forever.
        await using var db = await _factory.CreateDbContextAsync(ct);

        var order = await db.Orders
            .FirstAsync(order => order.Id == orderId, ct);

        if (order.Status == Status.Fulfilled)
        {
            return FulfillmentResult.Fulfilled;
        }

        if (order.Status != Status.Pending)
        {
            return FulfillmentResult.Backordered;
        }

        var now = DateTime.UtcNow;

        order.Status = Status.Backordered;
        order.CompletedAtUtc = now;

        db.FulfillmentEvents.Add(new FulfillmentEvent
        {
            OrderId = order.Id,
            Type = FulfillmentEventType.Backordered,
            Message = message,
            TimestampUtc = now
        });

        await db.SaveChangesAsync(ct);

        return FulfillmentResult.Backordered;
    }
}

// Record used as a simple return shape for burst/sequential fulfillment results.
public record BurstFulfillmentItemResult(int OrderId, FulfillmentResult Result);
