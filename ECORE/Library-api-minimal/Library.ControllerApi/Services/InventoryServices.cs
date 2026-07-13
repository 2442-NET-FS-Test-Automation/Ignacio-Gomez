using Library.ControllerApi.DTOs;
using Library.Data;
using Library.Data.Entities;

namespace Library.ControllerApi.Services;

public class InventoryService : IInventoryService
{
    // Our Inventory Service is what will call repo layer methods, so it
    // gets that dependecy. Not the controller layer
    private readonly IInventoryRepository _repo;

    public InventoryService(IInventoryRepository repo)
    {
        _repo = repo;
    }
    public Task<IReadOnlyList<InventoryItem>> AllAsync()
    {
        return _repo.GetAllAsync();
    }

    public Task<InventoryItem?> BySkuAsync(string sku)
    {
        return _repo.GetInventoryItemBySkuAsync(sku);
    }

    public Task<InventoryItem> AddAsync(InventoryDto dto)
    {
        return _repo.AddInventoryItemsAsync(dto.Sku, dto.Name, dto.Price, dto.CurrentStock);
    }

    public Task<bool> RemoveAsync(string sku)
    {
        return _repo.RemoveBySkuAsync(sku);
    }

}
