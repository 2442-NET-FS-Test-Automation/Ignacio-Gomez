using BloomRush.Data;
using BloomRush.Data.Entities;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BloomRush.Api.Seeding;

// This interface is the contract that Program.cs depends on.
// Program.cs does not need to know every detail of Seeder; it only needs these methods.
public interface ISeeder
{
    Task<SeedResult> RestoreBaselineAsync(CancellationToken ct);
    Task<IReadOnlyList<int>> SeedOrdersAsync(int n, CancellationToken ct);
}

// Seeder owns the test-data logic.
// Program.cs calls this class from /resetbaseline and /orders/seed.
// This keeps the endpoints small and moves database setup logic out of Program.cs.
public class Seeder : ISeeder
{
    private readonly BloomRushDbContext _db;

    // ASP.NET creates Seeder and sends in the normal scoped DbContext.
    // builder.Services.AddScoped<ISeeder, Seeder>();
    public Seeder(BloomRushDbContext db)
    {
        _db = db;
    }

    // Called by POST /resetbaseline in Program.cs.
    // Goal:
    // 1. Delete old order workflow data.
    // 2. Restore baseline customers/products/inventory.
    // 3. Return counts so the endpoint can show what happened.
    public async Task<SeedResult> RestoreBaselineAsync(CancellationToken ct)
    {
        // Read the baseline values from BloomRushDbContext.HasData().
        // This keeps the seed values in one place: the Fluent API.
        var baselineCustomers = GetBaselineCustomers();
        var baselineProducts = GetBaselineProducts();
        var baselineInventory = GetBaselineInventory();

        // Count existing workflow rows before deleting them.
        // These numbers are sent back to Program.cs inside SeedResult.
        var eventsDeleted = await _db.FulfillmentEvents.CountAsync(ct);
        var linesDeleted = await _db.OrderLines.CountAsync(ct);
        var ordersDeleted = await _db.Orders.CountAsync(ct);

        // Delete child tables first.
        // FulfillmentEvents and OrderLines point to Orders, so they must go before Orders.
        if (eventsDeleted > 0)
        {
            _db.FulfillmentEvents.RemoveRange(await _db.FulfillmentEvents.ToListAsync(ct));
        }

        if (linesDeleted > 0)
        {
            _db.OrderLines.RemoveRange(await _db.OrderLines.ToListAsync(ct));
        }

        if (ordersDeleted > 0)
        {
            _db.Orders.RemoveRange(await _db.Orders.ToListAsync(ct));
        }

        // Make sure baseline customers exist and have the expected names.
        foreach (var baselineCustomer in baselineCustomers)
        {
            var customer = await _db.Customers
                .Where(c => c.Email == baselineCustomer.Email)
                .FirstOrDefaultAsync(ct);

            if (customer == null)
            {
                // New Customer object is created in memory first.
                // db.Customers.Add marks it as "insert this row" for the next SaveChanges().
                _db.Customers.Add(new Customer
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
        foreach (var baselineProduct in baselineProducts)
        {
            var product = await _db.Products
                .Where(p => p.Sku == baselineProduct.Sku)
                .FirstOrDefaultAsync(ct);

            if (product == null)
            {
                // New Product object is created here.
                // SQL Server gets the row only when db.SaveChanges() runs below.
                _db.Products.Add(new Product
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
        await _db.SaveChangesAsync(ct);

        // Inventory belongs to products.
        // This loop finds each product by SKU, then resets or creates its stock row.
        foreach (var baselineInventoryItem in baselineInventory)
        {
            var productSku = baselineProducts
                .Where(product => product.Id == baselineInventoryItem.ProductId)
                .Select(product => product.Sku)
                .First();

            var productId = await _db.Products
                .Where(p => p.Sku == productSku)
                .Select(p => p.Id)
                .FirstAsync(ct);

            var inventory = await _db.InventoryItems
                .Where(i => i.ProductId == productId)
                .FirstOrDefaultAsync(ct);

            if (inventory == null)
            {
                // New InventoryItem object links stock to one ProductId.
                _db.InventoryItems.Add(new InventoryItem
                {
                    ProductId = productId,
                    QuantityOnHand = baselineInventoryItem.QuantityOnHand
                });
            }
            else
            {
                // Existing inventory row: reset stock back to the baseline amount.
                inventory.QuantityOnHand = baselineInventoryItem.QuantityOnHand;
            }
        }

        // This sends all inventory INSERT/UPDATE statements to SQL Server.
        await _db.SaveChangesAsync(ct);

        // Record call: SeedResult is created here.
        // It is not a database row; it is only the response data for /resetbaseline.
        return new SeedResult(
            eventsDeleted,
            linesDeleted,
            ordersDeleted,
            baselineCustomers.Count,
            baselineProducts.Count,
            baselineInventory.Count,
            baselineInventory.Sum(item => item.QuantityOnHand));
    }

    // Called by POST /orders/seed in Program.cs.
    // n decides how many orders to create.
    // Seeded orders use Standard priority to keep the demo focused on fulfillment.
    // This method returns the IDs of the created orders back to Program.cs.
    public async Task<IReadOnlyList<int>> SeedOrdersAsync(int n, CancellationToken ct)
    {
        if (n <= 0)
        {
            return [];
        }

        // We need customer IDs because each Order must belong to one Customer.
        // The endpoint should run /resetbaseline first so these customers exist.
        var customerIds = await _db.Customers
            .OrderBy(customer => customer.Id)
            .Select(customer => customer.Id)
            .ToListAsync(ct);

        // Use the same product SKUs declared in BloomRushDbContext.HasData().
        var skus = GetBaselineProducts()
            .Select(product => product.Sku)
            .ToList();

        // Build a dictionary: SKU -> ProductId.
        var productIdsBySku = await _db.Products
            .Where(product => skus.Contains(product.Sku))
            .ToDictionaryAsync(product => product.Sku, product => product.Id, ct);

        // If baseline data is missing, return an empty list.
        // Program.cs turns this into a BadRequest telling the user to run /resetbaseline first.
        if (customerIds.Count == 0 || productIdsBySku.Count != skus.Count)
        {
            return [];
        }

        // This list is what we send back to Program.cs after creating the orders.
        var ids = new List<int>(n);

        for (var i = 0; i < n; i++)
        {
            // Rotate through the baseline SKUs so seeded orders are not all the same product.
            var sku = skus[i % skus.Count];

            // New Order object:
            // - CustomerId connects it to Customers.
            // - Priority and Status describe the workflow.
            // - Lines creates the child OrderLine rows.
            var order = new Order
            {
                CustomerId = customerIds[i % customerIds.Count],
                Priority = Priority.Standard,
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
            _db.Orders.Add(order);

            // SaveChanges inserts the rows and lets SQL Server generate order.Id.
            await _db.SaveChangesAsync(ct);

            // The generated Id is returned to Program.cs, then shown by Swagger.
            ids.Add(order.Id);
        }

        return ids;
    }

    private IReadOnlyList<(int Id, string Name, string Email)> GetBaselineCustomers()
    {
        return GetSeedData<Customer>()
            .Select(row => (
                Id: (int)row[nameof(Customer.Id)]!,
                Name: (string)row[nameof(Customer.Name)]!,
                Email: (string)row[nameof(Customer.Email)]!))
            .ToList();
    }

    private IReadOnlyList<(int Id, string Sku, string Name, decimal Price)> GetBaselineProducts()
    {
        return GetSeedData<Product>()
            .Select(row => (
                Id: (int)row[nameof(Product.Id)]!,
                Sku: (string)row[nameof(Product.Sku)]!,
                Name: (string)row[nameof(Product.Name)]!,
                Price: (decimal)row[nameof(Product.Price)]!))
            .ToList();
    }

    private IReadOnlyList<(int ProductId, int QuantityOnHand)> GetBaselineInventory()
    {
        return GetSeedData<InventoryItem>()
            .Select(row => (
                ProductId: (int)row[nameof(InventoryItem.ProductId)]!,
                QuantityOnHand: (int)row[nameof(InventoryItem.QuantityOnHand)]!))
            .ToList();
    }

    private IReadOnlyList<IDictionary<string, object?>> GetSeedData<TEntity>()
    {
        // HasData is stored in EF Core's design-time model, not in the optimized runtime model.
        var designTimeModel = _db.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(TEntity));

        return entityType?.GetSeedData().ToList() ?? [];
    }
}

// Record used as the response model for /resetbaseline.
// It is created by Seeder.RestoreBaselineAsync() and sent back through Program.cs.
public record SeedResult(
    int EventsDeleted,
    int LinesDeleted,
    int OrdersDeleted,
    int BaselineCustomers,
    int BaselineProducts,
    int BaselineInventoryItems,
    int BaselineStock);
