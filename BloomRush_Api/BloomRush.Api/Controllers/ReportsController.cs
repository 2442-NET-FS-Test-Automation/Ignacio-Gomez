using BloomRush.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Controllers;

[ApiController]
[Route("reports")]
public class ReportsController : ControllerBase
{
    private readonly BloomRushDbContext _context;

    public ReportsController(BloomRushDbContext context)
    {
        _context = context;
    }

    [HttpGet("order-status")]
    public async Task<IActionResult> GetOrderStatusReport(CancellationToken ct)
    {
        var report = await _context.Orders
            .AsNoTracking()
            .GroupBy(order => order.Status)
            .Select(group => new
            {
                status = group.Key.ToString(),
                orderCount = group.Count(),
                totalUnits = group
                    .SelectMany(order => order.Lines)
                    .Sum(line => line.Quantity)
            })
            .OrderBy(row => row.status)
            .ToListAsync(ct);

        return Ok(report);
    }
}
