namespace BloomRush.Data;

using BloomRush.Data.Entities;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
