using Library.Data.Entities;
namespace Library.Data;
public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryItem?> GetInventoryItemBySkuAsync(string sku);
    Task<InventoryItem> AddInventoryItemsAsync(string sku, string name, decimal price, int quantity);
    Task<bool> RemoveBySkuAsync(String sku);
}