using AutoMapper;
using Library.ControllerApi.DTOs;
using Library.ControllerApi.Services;
using Library.Data;
using Library.Data.Entities;
using Microsoft.AspNetCore.Mvc; // ControllerBase lives here
using Microsoft.Extensions.Caching.Memory;


[ApiController] // This annotation tells ASP.NET to map this controller during app.MapControllers()
[Route("api/[controller]")] // Pretty sure this will be localhost:5051/api/Inventory as the route base
public class InventoryController : ControllerBase
{
    private const string InventoryCacheKey = "inventory:all";

    // This will be removed tomorrow for sure
    private readonly IInventoryService _service;
    private readonly IMapper _mapper; // automapper object
    private readonly IMemoryCache _cache;
    private readonly ISupplierClient _supplier;

    public InventoryController(IInventoryService service, IMapper mapper, IMemoryCache cache, ISupplierClient supplier)
    {
        _service = service;
        _mapper = mapper;
        _cache = cache;
        _supplier = supplier;
    }

    // Lets write our first GET endpoint
    [HttpGet] // ActionResult just represents possible HTTP response actions
    [ResponseCache(Duration = 30)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> Get()
    {
        // Server-side cache: keeps the mapped DTOs in memory for 30 seconds.
        var dtos = await _cache.GetOrCreateAsync("inventory:all", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            var items = await _service.AllAsync();
            return _mapper.Map<List<InventoryDto>>(items);
        });
        return Ok(dtos);
    }


    // localhost:5137/api/Inventory/{sku} - sku is passed in by the user
    // We can add routing info right on the annotation
    [HttpGet("{sku}")] // I can parameterize the route itself
    public async Task<ActionResult<InventoryDto>> GetBySku(string sku)
    {
        var mappedItem = await _cache.GetOrCreateAsync<InventoryDto?>(SkuCacheKey(sku), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

            var item = await _service.BySkuAsync(sku);
            return item is null ? null : _mapper.Map<InventoryDto>(item);
        });

        if (mappedItem is null)
        {
            return NotFound(); // 404 not found
        }
        else
        {
            return Ok(mappedItem);
        }
    }

    [HttpPost]
    public async Task<ActionResult<InventoryDto>> Create(InventoryCreateDto newInv)
    {
        var created = await _service.AddAsync(newInv);
        var response = _mapper.Map<InventoryDto>(created);

        _cache.Remove(InventoryCacheKey);
        _cache.Remove(SkuCacheKey(response.Sku));

        // CreatedAt (201) works a little differently from our other response ActionResults
        // Created at needs to know how to find the newly created resource - so we tell it
        // Use the GetBySku controller method (literally the one above) and use the information
        // in response to build the URI string
        return CreatedAtAction(nameof(GetBySku), new { sku = response.Sku }, response);
    }

    [HttpDelete("{sku}")]
    public async Task<ActionResult> Delete(string sku)
    {
        bool isDeleted = await _service.RemoveAsync(sku);

        if (isDeleted)
        {
            _cache.Remove(InventoryCacheKey);
            _cache.Remove(SkuCacheKey(sku));

            return NoContent(); // 204 - No content - it WAS there, not anymore
        }
        else
        {
            return NotFound(); // 404 - couldn't delete it because your sku was wrong
        }
    }

    //New GET that uses that supplierClient to call an outside API
    //localHost:5173/api/inventory
    [HttpGet("{sku}/supplier-price")]
    public async Task<IActionResult> GetSupplierPrice(string sku)
    {
        var price = await _supplier.GetListPriceAsync(sku);
        if (price is null)
        {
            return NotFound();
        }
        return Ok(new
        {
            sku,
            supplierClient = price
        });
    }

    private static string SkuCacheKey(string sku) => $"inventory:sku:{sku}";
}
