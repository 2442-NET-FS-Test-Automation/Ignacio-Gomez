using AutoMapper;
using Library.ControllerApi.DTOs;
using Library.ControllerApi.Services;
using Library.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]//This annotation tells ASP.NET to map this controller during app
[Route("api/[controller]")]

public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    private readonly IMapper _mapper;
    public InventoryController(IInventoryService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> Get()
    {   
        var items = await _service.AllAsync();
        
        var mappedItems = _mapper.Map<List<InventoryDto>>(items);
        return Ok(mappedItems);
        // var items = await _repo.GetAllAsync();
        // var response = items.Select(ToDto).ToList();

        // return Ok(response);
    }

    [HttpGet("{sku}")]
    public async Task<ActionResult<InventoryDto>> GetBySku(string sku)
    {
        var item = await _service.BySkuAsync(sku);

        if (item is null)
        {
            return NotFound();
        }
        else
        {
            var mappedItem = _mapper.Map<InventoryDto>(item);
            return Ok(mappedItem);
        }
        // var item = await _repo.GetInventoryItemBySkuAsync(sku);

        // if (item is null)
        // {
        //     return NotFound();
        // }

        // return Ok(ToDto(item));
    }

    private static InventoryDto ToDto(Library.Data.Entities.InventoryItem item)
    {
        return new InventoryDto(
            item.Product.Sku,
            item.Product.Name,
            item.CurrentStock);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryDto>> Create(InventoryCreateDto newInv)
    {
        var created = await _service.AddAsync(newInv);
        var response = _mapper.Map<InventoryDto>(created);
        return CreatedAtAction(nameof(GetBySku), new {sku = response.Sku}, response);
    }

    [HttpDelete("{sku}")]
    public async Task<ActionResult> Delete(string sku)
    {
        bool isDeleted = await _service.RemoveAsync(sku);
        if (isDeleted)
        {
            return NoContent();
        }
        else
        {
            return NotFound();
        }
    }
}
