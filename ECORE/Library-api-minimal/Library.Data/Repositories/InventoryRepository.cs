using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Data;

// This class will hold my DB access logic. ALL it is concerned with is looking into the database

public class InventoryRepository : IInventoryRepository
{
    // Our repo class needs a db context - we can ask for a dbcontext from ASP.NET DI Container
    // same pattern we've been using since day 1 of the minmal API
    private readonly IDbContextFactory<LibraryDbContext> _factory;

    // Still taking arguments in from ASP.NET during runtime. 
    public InventoryRepository(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory = factory;
    }

    // Lets make some CRUD
    // Actually pretty simple to do - because we don't have to concern ourselves with business logic checks etc.
    // All we write is DB access stuff

    // Let's write some Read methods
    // Get all inventoryItems
    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync()
    {   
        // Ask for db context
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Inventory.Include(i => i.Product).ToListAsync(); // use it to access db
    }

    // Get an item by it's SKU
    public async Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Inventory.Include(i => i.Product).FirstOrDefaultAsync(i => i.Product.Sku == sku);
    }

    // Lets do a simple add 
    // Get in the habit of sending back the newly created objects
    public async Task<InventoryItem> AddInventoryItemAsync(string sku, string name, decimal price, int quantity)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // Creating our new item - and product
        InventoryItem newItem = new InventoryItem
        {
            Product = new Product { Sku = sku, Name = name, Price = price},
            CurrentStock = quantity
        };

        db.Inventory.Add(newItem);
        await db.SaveChangesAsync();

        return newItem; // because newItem is an object tracked by EF Core - EF will grab the PK for us
    }    

    // lets do a remove
    public async Task<bool> RemoveBySkuAsync(string sku)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // First find the thing we want out of the database - grab it
        InventoryItem? itemToRemove = await db.Inventory.Include(i => i.Product)
                                            .FirstOrDefaultAsync(i => i.Product.Sku == sku);

        // Don't assume the search criteria produced a result - check for a null
        // If it's null we failed to remove it - because it didn't exist
        if (itemToRemove is null)
        {
            return false;
        }
        // Telling EF we want to remove this object from DB
        db.Products.Remove(itemToRemove.Product); // This SHOULD Cascade based on how we setup our models

        await db.SaveChangesAsync();
        return true;

    }

}