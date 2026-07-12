using BloomRush.Data;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Fulfillment;

public interface IOrderDiagnosticsService
{
    // This method is called CheckOneOrderAsync.
    // It works async, and when it finishes it returns an order stock check result or null.
    Task<OrderStockCheckResult?> CheckOneOrderAsync(BloomRushDbContext db, int id, CancellationToken ct);

    Task<IReadOnlyList<OrderStockCheckResult>> CheckManyOrdersSequentialAsync(BloomRushDbContext db, IReadOnlyList<int> orderIds, CancellationToken ct);
}

public class OrderDiagnosticsService : IOrderDiagnosticsService
{
    public async Task<OrderStockCheckResult?> CheckOneOrderAsync(BloomRushDbContext db, int id, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(p => p.Lines)
                .ThenInclude(line => line.Product)
            .Where(p => p.Id == id)
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

            var lineResult = new OrderStockCheckLineResult(
                line.ProductId,
                line.Product.Sku,
                line.Product.Name,
                line.Quantity,
                quantityOnHand,
                missingUnits
            );

            lineResults.Add(lineResult);
        }

        var totalMissingUnits = lineResults.Sum(line => line.MissingUnits);

        return new OrderStockCheckResult
        (
            order.Id,
            order.Status.ToString(),
            totalMissingUnits == 0,
            order.Lines.Count,
            totalMissingUnits,
            lineResults
        );
    }
    public async Task<IReadOnlyList<OrderStockCheckResult>> CheckManyOrdersSequentialAsync(BloomRushDbContext db, IReadOnlyList<int> orderIds, CancellationToken ct)
    {
        List<OrderStockCheckResult> results = new();

        foreach (int orderId in orderIds)
        {
            var check = await CheckOneOrderAsync(db, orderId, ct);

            if (check != null)
            {
                results.Add(check);
            }
        }

        return results;
    }
}

public record OrderStockCheckResult(
    int OrderId,
    string Status,
    bool CanFulfill,
    int LineCount,
    int TotalMissingUnits,
    IReadOnlyList<OrderStockCheckLineResult> Lines);

public record OrderStockCheckLineResult(
    int ProductId,
    string Sku,
    string ProductName,
    int QuantityRequested,
    int QuantityOnHand,
    int MissingUnits);
