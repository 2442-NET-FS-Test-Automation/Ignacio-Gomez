using BloomRush.Data;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Fulfillment;

// Contract that Program.cs uses. This lets the endpoint depend on an interface
// instead of depending directly on the concrete class.
public interface IFulfillmentService
{
    Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct);
}

// High-level result categories for one fulfillment attempt.
public enum FulfillmentOutcome
{
    Fulfilled,
    Backordered,
    AlreadyTerminal,
    NotFound,
    NotReady
}

public record FulfillmentResult(
    int OrderId,
    FulfillmentOutcome Outcome,
    string Message);

// This class will hold the business logic for fulfilling one order.
// For now it only loads and validates the order; inventory changes come next.
public class FulfillmentService : IFulfillmentService
{
    private readonly IDbContextFactory<BloomRushDbContext> _factory;

    // ASP.NET injects the DbContext factory. We use it to create a fresh DbContext
    // inside each fulfillment operation.
    public FulfillmentService(IDbContextFactory<BloomRushDbContext> factory)
    {
        _factory = factory;
    }

    // Main entry point for fulfilling one order.
    public async Task<FulfillmentResult> FulfillOneAsync(int orderId, CancellationToken ct)
    {
        // Each fulfillment run owns its own DbContext.
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Load the order and its lines because inventory checks need the requested products.
        var order = await db.Orders
            .Include(order => order.Lines)
            .Where(order => order.Id == orderId)
            .FirstOrDefaultAsync(ct);

        // If the order does not exist, the endpoint should return NotFound later.
        if (order == null)
        {
            return new FulfillmentResult(
                orderId,
                FulfillmentOutcome.NotFound,
                $"Order {orderId} was not found.");
        }

        // Fulfillment should only run on pending orders.
        if (order.Status != Status.Pending)
        {
            return new FulfillmentResult(
                order.Id,
                FulfillmentOutcome.AlreadyTerminal,
                $"Order {order.Id} is already {order.Status}.");
        }

        // Temporary stopping point: next step will check inventory and update status/events.
        return new FulfillmentResult(
            order.Id,
            FulfillmentOutcome.NotReady,
            "Fulfillment service is wired. Inventory decrement comes next.");
    }
}
