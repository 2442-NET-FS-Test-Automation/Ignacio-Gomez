using BloomRush.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Controllers;

[ApiController]
[Route("inventory")]
public class InventoryController : ControllerBase
{
    private readonly BloomRushDbContext _context;

    public InventoryController(BloomRushDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory(CancellationToken ct)
    {
        var inventory = await _context.InventoryItems
            .AsNoTracking()
            .Include(item => item.Product)
            .OrderBy(item => item.Product.Sku)
            .Select(item => new
            {
                item.Id,
                item.ProductId,
                sku = item.Product.Sku,
                name = item.Product.Name,
                item.QuantityOnHand
            })
            .ToListAsync(ct);

        return Ok(inventory);
    }
}
