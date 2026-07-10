using BloomRush.Data;
using BloomRush.Data.Entities;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BloomRush.Api.Fulfillment;

// Contract that Program.cs uses.
// Program.cs calls IFulfillmentService from POST /orders/{orderId}/fulfill.
// The concrete class is FulfillmentService because Program.cs registered it in DI.
public interface IFulfillmentService
{
    Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
    Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillBurstAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct);
}

// Fulfillment only has two business outcomes:
// either the order is completed, or it cannot be completed because stock is missing.
public enum FulfillmentResult
{
    // The order was completed and inventory was reduced.
    Fulfilled,

    // The order could not be completed because there was not enough stock.
    Backordered
}

// This class will hold the business logic for fulfilling one order.
// This first real version handles one order: check stock, update inventory,
// update order status, and write a fulfillment event.
public class FulfillmentService : IFulfillmentService
{
    // Bounded retry: if another request changes the same inventory row first,
    // we reload and try again, but only this many times.
    private const int MaxConcurrencyAttempts = 3;

    private readonly IDbContextFactory<BloomRushDbContext> _factory;

    // ASP.NET injects the DbContext factory. We use it to create a fresh DbContext
    // inside each fulfillment operation.
    public FulfillmentService(IDbContextFactory<BloomRushDbContext> factory)
    {
        _factory = factory;
    }

    // Main entry point for fulfilling one order.
    // Input from Program.cs: orderId.
    // Output back to Program.cs: FulfillmentResult.Fulfilled or FulfillmentResult.Backordered.
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            try
            {
                // Each attempt loads fresh data from SQL Server.
                // If a previous attempt lost a RowVersion race, this reloads the new stock.
                return await TryFulfillOneAttemptAsync(orderId, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request updated an InventoryItem between our read and SaveChanges.
                // We do not keep using this DbContext because it has stale RowVersion values.
                // The next loop attempt creates a new DbContext and re-checks stock.
                if (attempt == MaxConcurrencyAttempts)
                {
                    return await MarkBackorderedAfterConcurrencyAsync(orderId, ct);
                }
            }
        }

        // If every retry lost the concurrency race, stop retrying and backorder.
        // This keeps the burst from spinning forever.
        return await MarkBackorderedAfterConcurrencyAsync(orderId, ct);
    }

    // Called by POST /orders/burst through a background Task.Run.
    // This method does not contain separate fulfillment rules.
    public async Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillBurstAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        var tasks = orderIds.Select(async orderId =>
        {
            //Calling FulfillOneAsync
            var result = await FulfillOneAsync(orderId, ct);

            // Structured Serilog event:
            // OrderId and Result are separate properties in the console log.
            Log.Information(
                "Burst fulfillment completed for order {OrderId} with result {Result}",
                orderId,
                result);

            return new BurstFulfillmentItemResult(orderId, result);
        });

        return await Task.WhenAll(tasks);
    }

    private async Task<FulfillmentResult> TryFulfillOneAttemptAsync(int orderId, CancellationToken ct)
    {
        // Each fulfillment run owns its own DbContext.
        // This matters later when burst runs many fulfillments at the same time.
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Load the order and its lines because inventory checks need the requested products.
        var order = await db.Orders
            .Include(order => order.Lines)
            .Where(order => order.Id == orderId)
            .FirstAsync(ct);

        // Safety guards in case another endpoint or future code calls this service directly.
        // In the normal current flow, Program.cs already stops non-pending orders before this method.
        // This prevents us from decrementing inventory twice for the same order.
        if (order.Status == Status.Fulfilled)
        {
            return FulfillmentResult.Fulfilled;
        }

        if (order.Status != Status.Pending)
        {
            return FulfillmentResult.Backordered;
        }

        // These three changes must land together:
        // inventory decrement + order status + fulfillment event.
        // If SaveChanges fails, the transaction prevents a half-finished order.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Collect the ProductIds from the order lines so we can load only the inventory rows
        var productIds = order.Lines
            .Select(line => line.ProductId)
            .ToList();

        // InventoryItems contains the stock for each product.
        // These rows are tracked by EF Core, so changing QuantityOnHand below becomes an UPDATE.
        var inventoryItems = await db.InventoryItems
            .Where(item => productIds.Contains(item.ProductId))
            .ToListAsync(ct);

        // The order can be fulfilled only if every requested line has enough stock.
        // If even one product is missing or too low, the whole order becomes Backordered.
        var hasEnoughStock = true;

        foreach (var line in order.Lines)
        {
            var inventory = inventoryItems
                .FirstOrDefault(item => item.ProductId == line.ProductId);

            if (inventory == null || inventory.QuantityOnHand < line.Quantity)
            {
                hasEnoughStock = false;
                break;
            }
        }

        // We use one timestamp for the order and the audit event so they match.
        var completedAtUtc = DateTime.UtcNow;

        // Not enough stock: do not subtract anything. Just mark the order Backordered
        // and write an event explaining what happened.
        if (!hasEnoughStock)
        {
            // Update the Order entity.
            // This will become an UPDATE Orders SET Status = Backordered...
            order.Status = Status.Backordered;
            order.CompletedAtUtc = completedAtUtc;

            // FulfillmentEvents is the audit trail for the order.
            // It records "what happened" but does not change the order by itself.
            db.FulfillmentEvents.Add(new FulfillmentEvent
            {
                OrderId = order.Id,
                Type = FulfillmentEventType.Backordered,
                Message = "Order backordered because one or more products did not have enough stock.",
                TimestampUtc = completedAtUtc
            });

            // SaveChanges sends the UPDATE/INSERT statements to SQL Server.
            // Sent here:
            // - UPDATE Orders
            // - INSERT FulfillmentEvents
            await db.SaveChangesAsync(ct);

            // Commit makes the transaction final.
            await transaction.CommitAsync(ct);

            return FulfillmentResult.Backordered;
        }

        // Enough stock: subtract each requested quantity from its inventory row.
        foreach (var line in order.Lines)
        {
            // Match the order line product to its inventory row.
            // Example: line.ProductId 5 -> InventoryItems row for TULIP-MIX-20.
            var inventory = inventoryItems.First(item => item.ProductId == line.ProductId);

            // This is the actual stock decrement.
            // EF Core remembers the changed QuantityOnHand and saves it on SaveChangesAsync.
            inventory.QuantityOnHand -= line.Quantity;
        }

        // The order is now complete.
        // This will become an UPDATE to the Orders table.
        order.Status = Status.Fulfilled;
        order.CompletedAtUtc = completedAtUtc;

        // Audit event: this lets us prove later that this order was fulfilled.
        db.FulfillmentEvents.Add(new FulfillmentEvent
        {
            OrderId = order.Id,
            Type = FulfillmentEventType.Fulfilled,
            Message = "Order fulfilled and inventory decremented.",
            TimestampUtc = completedAtUtc
        });

        // Save inventory decrement + order status + event together.
        // Sent here:
        // - UPDATE InventoryItems
        // - UPDATE Orders
        // - INSERT FulfillmentEvents
        await db.SaveChangesAsync(ct);

        // Finalize the transaction.
        await transaction.CommitAsync(ct);

        return FulfillmentResult.Fulfilled;
    }

    private async Task<FulfillmentResult> MarkBackorderedAfterConcurrencyAsync(
        int orderId,
        CancellationToken ct)
    {
        // Final fallback after too many RowVersion conflicts.
        // Use a fresh DbContext so we do not touch stale tracked entities.
        await using var db = await _factory.CreateDbContextAsync(ct);

        var order = await db.Orders
            .Where(order => order.Id == orderId)
            .FirstAsync(ct);

        // If another worker already fulfilled this order while we were retrying,
        // report Fulfilled and do not overwrite the status.
        if (order.Status == Status.Fulfilled)
        {
            return FulfillmentResult.Fulfilled;
        }

        // If it is already finished in another non-pending state, treat it as Backordered.
        if (order.Status != Status.Pending)
        {
            return FulfillmentResult.Backordered;
        }

        var completedAtUtc = DateTime.UtcNow;

        order.Status = Status.Backordered;
        order.CompletedAtUtc = completedAtUtc;

        db.FulfillmentEvents.Add(new FulfillmentEvent
        {
            OrderId = order.Id,
            Type = FulfillmentEventType.Backordered,
            Message = "Order backordered after repeated inventory concurrency conflicts.",
            TimestampUtc = completedAtUtc
        });

        await db.SaveChangesAsync(ct);

        return FulfillmentResult.Backordered;
    }
}

// Small return shape for FulfillBurstAsync.
// The endpoint does not wait for this, but it is useful for logs/tests/future use.
public record BurstFulfillmentItemResult(int OrderId, FulfillmentResult Result);
