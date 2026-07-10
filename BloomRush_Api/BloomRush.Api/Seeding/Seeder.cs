using BloomRush.Data;
using BloomRush.Data.Entities;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Seeding;

public interface ISeeder
{
    SeedResult RestoreBaseline();
    IReadOnlyList<int> SeedOrders(int n, bool expedited);
}

public class Seeder : ISeeder
{
    private static readonly string[] Skus =
    [
        "ROSE-RED-12",
        "LILY-WHITE-06",
        "SUNFLOWER-10",
        "ORCHID-PINK-01",
        "TULIP-MIX-20"
    ];

    private readonly IDbContextFactory<BloomRushDbContext> _factory;

    public Seeder(IDbContextFactory<BloomRushDbContext> factory)
    {
        _factory = factory;
    }

    public SeedResult RestoreBaseline()
    {
        using var db = _factory.CreateDbContext();

        var eventsDeleted = db.FulfillmentEvents.Count();
        var linesDeleted = db.OrderLines.Count();
        var ordersDeleted = db.Orders.Count();

        if (eventsDeleted > 0)
        {
            db.FulfillmentEvents.RemoveRange(db.FulfillmentEvents.ToList());
        }

        if (linesDeleted > 0)
        {
            db.OrderLines.RemoveRange(db.OrderLines.ToList());
        }

        if (ordersDeleted > 0)
        {
            db.Orders.RemoveRange(db.Orders.ToList());
        }

        foreach (var baselineCustomer in BloomRushBaselineData.Customers)
        {
            var customer = db.Customers
                .Where(c => c.Email == baselineCustomer.Email)
                .FirstOrDefault();

            if (customer == null)
            {
                db.Customers.Add(new Customer
                {
                    Name = baselineCustomer.Name,
                    Email = baselineCustomer.Email
                });
            }
            else
            {
                customer.Name = baselineCustomer.Name;
            }
        }

        foreach (var baselineProduct in BloomRushBaselineData.Products)
        {
            var product = db.Products
                .Where(p => p.Sku == baselineProduct.Sku)
                .FirstOrDefault();

            if (product == null)
            {
                db.Products.Add(new Product
                {
                    Sku = baselineProduct.Sku,
                    Name = baselineProduct.Name,
                    Price = baselineProduct.Price
                });
            }
            else
            {
                product.Name = baselineProduct.Name;
                product.Price = baselineProduct.Price;
            }
        }

        db.SaveChanges();

        foreach (var baselineProduct in BloomRushBaselineData.Products)
        {
            var productId = db.Products
                .Where(p => p.Sku == baselineProduct.Sku)
                .Select(p => p.Id)
                .First();

            var inventory = db.InventoryItems
                .Where(i => i.ProductId == productId)
                .FirstOrDefault();

            if (inventory == null)
            {
                db.InventoryItems.Add(new InventoryItem
                {
                    ProductId = productId,
                    QuantityOnHand = baselineProduct.BaselineStock
                });
            }
            else
            {
                inventory.QuantityOnHand = baselineProduct.BaselineStock;
            }
        }

        db.SaveChanges();

        return new SeedResult(
            eventsDeleted,
            linesDeleted,
            ordersDeleted,
            BloomRushBaselineData.Customers.Count,
            BloomRushBaselineData.Products.Count,
            BloomRushBaselineData.Products.Count,
            BloomRushBaselineData.TotalBaselineStock);
    }

    public IReadOnlyList<int> SeedOrders(int n, bool expedited)
    {
        if (n <= 0)
        {
            return [];
        }

        using var db = _factory.CreateDbContext();

        var customerIds = db.Customers
            .OrderBy(customer => customer.Id)
            .Select(customer => customer.Id)
            .ToList();

        var productIdsBySku = db.Products
            .Where(product => Skus.Contains(product.Sku))
            .ToDictionary(product => product.Sku, product => product.Id);

        if (customerIds.Count == 0 || productIdsBySku.Count != Skus.Length)
        {
            return [];
        }

        var ids = new List<int>(n);

        for (var i = 0; i < n; i++)
        {
            var sku = Skus[i % Skus.Length];

            var order = new Order
            {
                CustomerId = customerIds[i % customerIds.Count],
                Priority = expedited ? Priority.Expedited : Priority.Standard,
                Status = Status.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    new OrderLine
                    {
                        ProductId = productIdsBySku[sku],
                        Quantity = 1
                    }
                }
            };

            db.Orders.Add(order);
            db.SaveChanges();
            ids.Add(order.Id);
        }

        return ids;
    }
}

public record SeedResult(
    int EventsDeleted,
    int LinesDeleted,
    int OrdersDeleted,
    int BaselineCustomers,
    int BaselineProducts,
    int BaselineInventoryItems,
    int BaselineStock);
