using BloomRush.Data;
using BloomRush.Data.Entities;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Seeding;

// This interface is the contract that Program.cs depends on.
// Program.cs does not need to know every detail of Seeder; it only needs these methods.
public interface ISeeder
{
    SeedResult RestoreBaseline();
    IReadOnlyList<int> SeedOrders(int n, bool expedited);
}

// Seeder owns the test-data logic.
// Program.cs calls this class from /seed and /orders/seed.
// This keeps the endpoints small and moves database setup logic out of Program.cs.
public class Seeder : ISeeder
{
    // These are the product SKUs used when /orders/seed creates sample orders.
    // The SKUs must match the products in BloomRushBaselineData.
    private static readonly string[] Skus =
    [
        "ROSE-RED-12",
        "LILY-WHITE-06",
        "SUNFLOWER-10",
        "ORCHID-PINK-01",
        "TULIP-MIX-20"
    ];

    private readonly IDbContextFactory<BloomRushDbContext> _factory;

    // ASP.NET creates Seeder and sends in the DbContext factory because Program.cs registered:
    // builder.Services.AddScoped<ISeeder, Seeder>();
    public Seeder(IDbContextFactory<BloomRushDbContext> factory)
    {
        _factory = factory;
    }

    // Called by POST /seed in Program.cs.
    // Goal:
    // 1. Delete old order workflow data.
    // 2. Restore baseline customers/products/inventory.
    // 3. Return counts so the endpoint can show what happened.
    public SeedResult RestoreBaseline()
    {
        // Create one DbContext for this whole seed/reset operation.
        // The DbContext is the object EF Core uses to query and save SQL Server data.
        using var db = _factory.CreateDbContext();

        // Count existing workflow rows before deleting them.
        // These numbers are sent back to Program.cs inside SeedResult.
        var eventsDeleted = db.FulfillmentEvents.Count();
        var linesDeleted = db.OrderLines.Count();
        var ordersDeleted = db.Orders.Count();

        // Delete child tables first.
        // FulfillmentEvents and OrderLines point to Orders, so they must go before Orders.
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

        // BloomRushBaselineData lives in the Data project.
        // Seeder reads that fixed list and makes sure the database has those customers.
        foreach (var baselineCustomer in BloomRushBaselineData.Customers)
        {
            var customer = db.Customers
                .Where(c => c.Email == baselineCustomer.Email)
                .FirstOrDefault();

            if (customer == null)
            {
                // New Customer object is created in memory first.
                // db.Customers.Add marks it as "insert this row" for the next SaveChanges().
                db.Customers.Add(new Customer
                {
                    Name = baselineCustomer.Name,
                    Email = baselineCustomer.Email
                });
            }
            else
            {
                // If the customer already exists, update the name so baseline stays consistent.
                customer.Name = baselineCustomer.Name;
            }
        }

        // Same idea for products:
        // find by SKU, create if missing, update if already there.
        foreach (var baselineProduct in BloomRushBaselineData.Products)
        {
            var product = db.Products
                .Where(p => p.Sku == baselineProduct.Sku)
                .FirstOrDefault();

            if (product == null)
            {
                // New Product object is created here.
                // SQL Server gets the row only when db.SaveChanges() runs below.
                db.Products.Add(new Product
                {
                    Sku = baselineProduct.Sku,
                    Name = baselineProduct.Name,
                    Price = baselineProduct.Price
                });
            }
            else
            {
                // Keep product name/price aligned with the baseline list.
                product.Name = baselineProduct.Name;
                product.Price = baselineProduct.Price;
            }
        }

        // Save customers and products before inventory.
        // Inventory needs ProductId, so products must exist in the database first.
        db.SaveChanges();

        // Inventory belongs to products.
        // This loop finds each product by SKU, then resets or creates its stock row.
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
                // New InventoryItem object links stock to one ProductId.
                db.InventoryItems.Add(new InventoryItem
                {
                    ProductId = productId,
                    QuantityOnHand = baselineProduct.BaselineStock
                });
            }
            else
            {
                // Existing inventory row: reset stock back to the baseline amount.
                inventory.QuantityOnHand = baselineProduct.BaselineStock;
            }
        }

        // This sends all inventory INSERT/UPDATE statements to SQL Server.
        db.SaveChanges();

        // SeedResult is not a database row.
        // It is a small response object sent back to Program.cs so /seed can return JSON.
        return new SeedResult(
            eventsDeleted,
            linesDeleted,
            ordersDeleted,
            BloomRushBaselineData.Customers.Count,
            BloomRushBaselineData.Products.Count,
            BloomRushBaselineData.Products.Count,
            BloomRushBaselineData.TotalBaselineStock);
    }

    // Called by POST /orders/seed in Program.cs.
    // n decides how many orders to create.
    // expedited decides whether the orders use Priority.Expedited or Priority.Standard.
    // This method returns the IDs of the created orders back to Program.cs.
    public IReadOnlyList<int> SeedOrders(int n, bool expedited)
    {
        if (n <= 0)
        {
            return [];
        }

        using var db = _factory.CreateDbContext();

        // We need customer IDs because each Order must belong to one Customer.
        // The endpoint should run /seed first so these customers exist.
        var customerIds = db.Customers
            .OrderBy(customer => customer.Id)
            .Select(customer => customer.Id)
            .ToList();

        // Build a dictionary: SKU -> ProductId.
        var productIdsBySku = db.Products
            .Where(product => Skus.Contains(product.Sku))
            .ToDictionary(product => product.Sku, product => product.Id);

        // If baseline data is missing, return an empty list.
        // Program.cs turns this into a BadRequest telling the user to run /seed first.
        if (customerIds.Count == 0 || productIdsBySku.Count != Skus.Length)
        {
            return [];
        }

        // This list is what we send back to Program.cs after creating the orders.
        var ids = new List<int>(n);

        for (var i = 0; i < n; i++)
        {
            // Rotate through the baseline SKUs so seeded orders are not all the same product.
            var sku = Skus[i % Skus.Length];

            // New Order object:
            // - CustomerId connects it to Customers.
            // - Priority and Status describe the workflow.
            // - Lines creates the child OrderLine rows.
            var order = new Order
            {
                CustomerId = customerIds[i % customerIds.Count],
                Priority = expedited ? Priority.Expedited : Priority.Standard,
                Status = Status.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    // New OrderLine object:
                    // This says "this order wants this product and this quantity".
                    // FulfillmentService later reads these lines to decide stock decrement.
                    new OrderLine
                    {
                        ProductId = productIdsBySku[sku],
                        Quantity = 1
                    }
                }
            };

            // Add marks the Order and its OrderLine as pending inserts.
            db.Orders.Add(order);

            // SaveChanges inserts the rows and lets SQL Server generate order.Id.
            db.SaveChanges();

            // The generated Id is returned to Program.cs, then shown by Swagger.
            ids.Add(order.Id);
        }

        return ids;
    }
}

// SeedResult is a response model for /seed.
// It is created by Seeder.RestoreBaseline() and sent back through Program.cs.
public record SeedResult(
    int EventsDeleted,
    int LinesDeleted,
    int OrdersDeleted,
    int BaselineCustomers,
    int BaselineProducts,
    int BaselineInventoryItems,
    int BaselineStock);
