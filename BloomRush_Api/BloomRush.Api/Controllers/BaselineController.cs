using BloomRush.Api.Seeding;
using Microsoft.AspNetCore.Mvc;

namespace BloomRush.Api.Controllers;

[ApiController]
public class BaselineController : ControllerBase
{
    private readonly ISeeder _seeder;

    public BaselineController(ISeeder seeder)
    {
        _seeder = seeder;
    }

    [HttpPost("/resetbaseline")]
    public async Task<IActionResult> ResetBaseline(CancellationToken ct)
    {
        var result = await _seeder.RestoreBaselineAsync(ct);

        return Ok(new
        {
            message = "Baseline restored",
            eventsDeleted = result.EventsDeleted,
            linesDeleted = result.LinesDeleted,
            ordersDeleted = result.OrdersDeleted,
            baselineCustomers = result.BaselineCustomers,
            baselineProducts = result.BaselineProducts,
            baselineInventoryItems = result.BaselineInventoryItems,
            baselineStock = result.BaselineStock
        });
    }
}
