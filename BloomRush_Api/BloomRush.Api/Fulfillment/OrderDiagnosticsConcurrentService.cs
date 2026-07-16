using BloomRush.Data;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Fulfillment;

public interface IOrderDiagnosticsConcurrentService
{
    Task<OrderStockCheckResult?> CheckOneOrderAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<OrderStockCheckResult>> CheckManyOrdersConcurrentAsync(IReadOnlyList<int> orderIds, CancellationToken ct);
}

public class OrderDiagnosticsConcurrentService : IOrderDiagnosticsConcurrentService
{
    private readonly IServiceScopeFactory _scopes;

    public OrderDiagnosticsConcurrentService(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<OrderStockCheckResult?> CheckOneOrderAsync(int id, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BloomRushDbContext>();

        var order = await db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
                .ThenInclude(line => line.Product)
            .Where(order => order.Id == id)
            .FirstOrDefaultAsync(ct);

        if (order == null)
        {
            return null;
        }

        var productIds = order.Lines
            .Select(line => line.ProductId)
            .ToList();

        var inventoryByProductId = await db.InventoryItems
            .AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(
                item => item.ProductId,
                item => item.QuantityOnHand,
                ct);

        var lineResults = new List<OrderStockCheckLineResult>();

        foreach (var line in order.Lines)
        {
            var quantityOnHand = inventoryByProductId.GetValueOrDefault(line.ProductId, 0);
            var missingUnits = Math.Max(0, line.Quantity - quantityOnHand);

            lineResults.Add(new OrderStockCheckLineResult(
                line.ProductId,
                line.Product.Sku,
                line.Product.Name,
                line.Quantity,
                quantityOnHand,
                missingUnits));
        }

        var totalMissingUnits = lineResults.Sum(line => line.MissingUnits);

        return new OrderStockCheckResult(
            order.Id,
            order.Status.ToString(),
            totalMissingUnits == 0,
            order.Lines.Count,
            totalMissingUnits,
            lineResults);
    }

    public async Task<IReadOnlyList<OrderStockCheckResult>> CheckManyOrdersConcurrentAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        var tasks = orderIds.Select(orderId => CheckOneOrderAsync(orderId, ct));

        var checks = await Task.WhenAll(tasks);

        return checks
            .Where(check => check != null)
            .Select(check => check!)
            .ToList();
    }
}
