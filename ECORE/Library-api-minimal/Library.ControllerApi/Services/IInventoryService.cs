

using Library.ControllerApi.DTOs;
using Library.Data.Entities;

namespace Library.ControllerApi.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItem>> AllAsync();
    Task<InventoryItem?> BySkuAsync(string sku);
     public Task<InventoryItem> AddAsync(InventoryCreateDto dto);
     public Task<bool> RemoveAsync(string sku);
}