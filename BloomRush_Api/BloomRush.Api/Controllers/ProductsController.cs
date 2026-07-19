using  BloomRush.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Controllers;


[ApiController] 
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly BloomRushDbContext _context;

    public ProductsController(BloomRushDbContext context)
        {
            _context = context;
        }
    
    [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var results = await _context.Products
            .AsNoTracking()
            .Select(products => new
            {
                Id = products.Id,
                Sku = products.Sku,
                Name = products.Name,
                Price = products.Price
            }).ToListAsync();
        
            if (results.Count == 0)
            {
                return NotFound();
            }
            return Ok(results);
        }

        [HttpGet("{sku}")]
        public async Task<IActionResult> GetProductsBySku(string sku)
        {
            var results = await _context.Products
            .AsNoTracking()
            .Where(products => products.Sku == sku)
            .Select(products => new
            {
                Id = products.Id,
                Sku = products.Sku,
                Name = products.Name,
                Price = products.Price
            }).FirstOrDefaultAsync();
        
            if (results == null)
            {
                return NotFound();
            }
            return Ok(results);
        }

        [HttpGet("/top-products")]
        public async Task<IActionResult> GetTopProducts(CancellationToken ct)
        {
            var report = await _context .OrderLines
            .AsNoTracking()
            .Join(
                _context .Products,
                line => line.ProductId,
                product => product.Id,
                (line, product) => new
                {
                    line.OrderId,
                    line.Quantity,
                    productId = product.Id,
                    sku = product.Sku,
                    name = product.Name
                })
            .GroupBy(row => new { row.productId, row.sku, row.name })
            .Select(group => new
            {
                group.Key.productId,
                group.Key.sku,
                group.Key.name,
                totalQuantity = group.Sum(row => row.Quantity),
                orderCount = group.Select(row => row.OrderId).Distinct().Count()
            })
            .OrderByDescending(row => row.totalQuantity)
            .ToListAsync(ct);

            var ranked = report
            .Select((row, index) => new
            {
                rank = index + 1,
                row.productId,
                row.sku,
                row.name,
                row.totalQuantity,
                row.orderCount
            })
            .ToList();

            return Ok(ranked);
        }

        [HttpGet("price-over/{minPrice}")]
        public async Task<IActionResult> PriceOver(decimal minPrice ,CancellationToken ct)
        {
            var productOrder = await _context.Products
            .Where(p => p.Price > minPrice)
            .OrderByDescending(p => p.Price)
            .Select(p => new
            {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Price = p.Price 
            }).ToListAsync(ct);
    
            if(productOrder.Count() == 0)
            {
                return NotFound();
            }
            return Ok(productOrder);
        }

}