namespace BloomRush.Data;

// Fixed baseline data for the project.
// This class does not talk to SQL Server by itself.
// BloomRushDbContext uses it for migration-time seed data.
// Seeder.RestoreBaseline() also reads it when /seed resets the database.
public static class BloomRushBaselineData
{
    // Customers that should always exist after migration or /seed.
    public static IReadOnlyList<BaselineCustomer> Customers { get; } =
    [
        new(1, "Ana Flores", "ana@bloomrush.test"),
        new(2, "Marco Rivera", "marco@bloomrush.test"),
        new(3, "Sofia Luna", "sofia@bloomrush.test")
    ];

    // Products that should always exist after migration or /seed.
    // The last number is BaselineStock: the stock quantity restored by /seed.
    public static IReadOnlyList<BaselineProduct> Products { get; } =
    [
        new(1, "ROSE-RED-12", "Red Roses Bouquet", 49.99m, 1),
        new(2, "LILY-WHITE-06", "White Lilies", 39.99m, 8),
        new(3, "SUNFLOWER-10", "Sunflower Bundle", 29.99m, 12),
        new(4, "ORCHID-PINK-01", "Pink Orchid", 59.99m, 5),
        new(5, "TULIP-MIX-20", "Mixed Tulips", 34.99m, 15)
    ];

    // Used by /seed response so Swagger can show total stock after reset.
    public static int TotalBaselineStock => Products.Sum(product => product.BaselineStock);
}

// Lightweight data shape for baseline customers.
public record BaselineCustomer(int Id, string Name, string Email);

// Lightweight data shape for baseline products.
public record BaselineProduct(int Id, string Sku, string Name, decimal Price, int BaselineStock);
