using Microsoft.EntityFrameworkCore;
using Library.Data.Entities;

namespace Library.Data;

// All of the code that does the actual SQL generation, creating a connection to by database
// doing CRUD, updating the DB based on changes to my models - All of that lives in class
public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options){}
    // We need to tell our DbContext what c# classes we are tracking as entities
    // Reminder - these entities become our tables
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();

    //If I want to do things like deeper configurations options or data seeding
    //I can override a method we inherited from DbContext
    //Called OnModelCreating() - this is called when EF Core creates a migration
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Inside of here using something called Fluent API. EF Core lets you do congig
        // In 3 ways. Convention < Data annotations < Fluent API
        b.Entity<Product>(e =>
        {
            // Lets set an index while we're here, the one thing to make this worth
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Price).HasColumnType("Decimal(10,2)");
            //Setting the relationship 
            e.HasOne(p => p.Inventory)
                .WithOne(i => i.Product)
                .HasForeignKey<InventoryItem>(i => i.ProductId);

        });

        // After you've configured your entities
        // We can use OnModelCreating to seed data
        b.Entity<Product>().HasData(
            new Product { Id = 1, Sku = "BK-001", Name = "Clean Code", Price = 32.00m},
            new Product { Id = 2, Sku = "BK-002", Name = "The Pragmatic Programmer", Price = 38.00m},
            new Product { Id = 3, Sku = "BK-003", Name = "Refactoring", Price = 45.00m}
        );
        b.Entity<InventoryItem>().HasData(
            new InventoryItem {Id = 1, ProductId = 1, CurrentStock = 4},
            new InventoryItem {Id = 2, ProductId = 2, CurrentStock = 3},
            new InventoryItem {Id = 3, ProductId = 3, CurrentStock = 8}
        );
    }
}