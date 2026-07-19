namespace BloomRush.Data;

using System.Reflection.Metadata;
using BloomRush.Data.Entities;
using Microsoft.EntityFrameworkCore;

// DbContext is the bridge between C# objects and SQL Server tables.
// Program.cs, Seeder.cs, and FulfillmentService.cs create/use this context
// whenever they need to query or save data.
public class BloomRushDbContext : DbContext
{
    public BloomRushDbContext(DbContextOptions<BloomRushDbContext> options)
        : base(options)
    {
    }

    // Each DbSet represents a table that EF Core can create and query.
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<FulfillmentEvent> FulfillmentEvents => Set<FulfillmentEvent>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // OnModelCreating configures table rules that are not obvious from the entity classes.
        modelBuilder.Entity<User>()
            .HasIndex(p => p.Username)
            .IsUnique();

        // Customer.Email must be unique to avoid duplicated customers by email.
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        // Product.Sku must be unique because it identifies each flower product.
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        // Price needs fixed precision to store money correctly in SQL Server.
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(10,2)");

        // Product 1:1 InventoryItem.
        // The FK lives in InventoryItems.ProductId and points to Products.Id.
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Inventory)
            .WithOne(i => i.Product)
            .HasForeignKey<InventoryItem>(i => i.ProductId);

        // RowVersion is used as a concurrency token to detect inventory changes.
        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.RowVersion)
            .IsRowVersion();

        // Non-unique index to search orders by status faster.
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Status);

        // Baseline customers inserted by EF migrations.
        // These are the fixed customers used by Swagger demos and resetbaseline.
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Ana Flores", Email = "ana@bloomrush.test" },
            new Customer { Id = 2, Name = "Marco Rivera", Email = "marco@bloomrush.test" },
            new Customer { Id = 3, Name = "Sofia Luna", Email = "sofia@bloomrush.test" });

        // Baseline products inserted by EF migrations.
        // Product Ids are fixed so InventoryItems can point to them below.
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Sku = "ROSE-RED-12", Name = "Red Roses Bouquet", Price = 49.99m },
            new Product { Id = 2, Sku = "LILY-WHITE-06", Name = "White Lilies", Price = 39.99m },
            new Product { Id = 3, Sku = "SUNFLOWER-10", Name = "Sunflower Bundle", Price = 29.99m },
            new Product { Id = 4, Sku = "ORCHID-PINK-01", Name = "Pink Orchid", Price = 59.99m },
            new Product { Id = 5, Sku = "TULIP-MIX-20", Name = "Mixed Tulips", Price = 34.99m });

        // Baseline inventory inserted by EF migrations.
        // QuantityOnHand is the starting stock restored by resetbaseline too.
        modelBuilder.Entity<InventoryItem>().HasData(
            new InventoryItem { Id = 1, ProductId = 1, QuantityOnHand = 100 },
            new InventoryItem { Id = 2, ProductId = 2, QuantityOnHand = 100 },
            new InventoryItem { Id = 3, ProductId = 3, QuantityOnHand = 100 },
            new InventoryItem { Id = 4, ProductId = 4, QuantityOnHand = 100 },
            new InventoryItem { Id = 5, ProductId = 5, QuantityOnHand = 100 });
    }
}
