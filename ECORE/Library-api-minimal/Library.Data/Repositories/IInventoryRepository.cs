using Library.Data.Entities;

namespace Library.Data;

//builder.Services.AddScoped<Interface, ConcreteClass>()
public interface IInventoryRepository
{
    // For now we'll leave this blank
    Task<IReadOnlyList<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku);
    Task<InventoryItem> AddInventoryItemAsync(string sku, string name, decimal price, int quantity);
    Task<bool> RemoveBySkuAsync(string sku);
}