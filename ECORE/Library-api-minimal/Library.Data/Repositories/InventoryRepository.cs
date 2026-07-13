using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Data;

public class InventoryRepository : IInventoryRepository
{
    //Our repo class need a db context, we can ask for a dbcontext from ASP.NET DI Container
    private readonly IDbContextFactory<LibraryDbContext> _factory;
    public InventoryRepository(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory = factory;
    }
    //Lets make some CRUD
    //Actually pretty simple to do - because we dont have to concern ourselves
    //ALL we write is DB access stuff
    //Lets write some read method
    //get all inventory Items
    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Inventory.Include(i => i.Product).ToListAsync();
    }

    //Get an item by its SKU
    public async Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Inventory.Include(i => i.Product).FirstOrDefaultAsync(i => i.Product.Sku == sku);
    }

    // Lets do a simple add
    public async Task<InventoryItem> AddInventoryItemsAsync(string sku, string name, decimal price, int quantity)
    {
        await using var db = await _factory.CreateDbContextAsync();
        InventoryItem newItem = new InventoryItem
        {
            Product = new Product{Sku = sku, Name = name, Price = price},
            CurrentStock = quantity
        };
        db.Inventory.Add(newItem);
        await db.SaveChangesAsync();
        return newItem;
    }
    //Lets do a remove
    public async Task<bool> RemoveBySkuAsync(String sku)
    {
        await using var db = await _factory.CreateDbContextAsync();
        InventoryItem? itemToRemove = await db.Inventory.Include(i => i.Product)
                                                .FirstOrDefaultAsync(i => i.Product.Sku == sku);
        if(itemToRemove is null)
        {
            return false;
        }
        db.Products.Remove(itemToRemove.Product); //Thi Should cascade
        await db.SaveChangesAsync();
        return true;
    }
}