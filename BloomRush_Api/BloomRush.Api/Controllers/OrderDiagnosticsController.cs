using System.Diagnostics;
using BloomRush.Api.Fulfillment;
using BloomRush.Api.Seeding;
using BloomRush.Data;
using Microsoft.AspNetCore.Mvc;

namespace BloomRush.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrderDiagnosticsController : ControllerBase
{
    private readonly BloomRushDbContext _db;
    private readonly IOrderDiagnosticsService _diagnostics;
    private readonly IOrderDiagnosticsConcurrentService _concurrentDiagnostics;
    private readonly ISeeder _seeder;

    public OrderDiagnosticsController(
        BloomRushDbContext db,
        IOrderDiagnosticsService diagnostics,
        IOrderDiagnosticsConcurrentService concurrentDiagnostics,
        ISeeder seeder)
    {
        _db = db;
        _diagnostics = diagnostics;
        _concurrentDiagnostics = concurrentDiagnostics;
        _seeder = seeder;
    }

    [HttpGet("{id:int}/stock-check-normal")]
    public async Task<ActionResult<OrderStockCheckResult>> CheckOneNormal(
        int id,
        CancellationToken ct)
    {
        var check = await _diagnostics.CheckOneOrderAsync(_db, id, ct);

        if (check == null)
        {
            return NotFound($"Order {id} was not found");
        }

        return Ok(check);
    }

    [HttpPost("stock-check-sequential")]
    public async Task<ActionResult<object>> CheckManySequential(
        [FromQuery] int n,
        CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");
        }

        var orderIds = _seeder.SeedOrders(n);

        if (orderIds.Count == 0)
        {
            return BadRequest("Run /seed first so customers, products, and inventory exist.");
        }

        var results = await _diagnostics.CheckManyOrdersSequentialAsync(_db, orderIds, ct);

        return Ok(new
        {
            created = orderIds.Count,
            checkedOrders = results.Count,
            orderIds,
            results
        });
    }

    [HttpPost("stock-check-concurrent")]
    public async Task<ActionResult<object>> CheckManyConcurrent(
        [FromQuery] int n,
        CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");
        }

        var orderIds = _seeder.SeedOrders(n);

        if (orderIds.Count == 0)
        {
            return BadRequest("Run /seed first so customers, products, and inventory exist.");
        }

        var results = await _concurrentDiagnostics.CheckManyOrdersConcurrentAsync(orderIds, ct);

        return Ok(new
        {
            created = orderIds.Count,
            checkedOrders = results.Count,
            orderIds,
            results
        });
    }

    [HttpPost("stock-check-benchmark")]
    public async Task<ActionResult<object>> BenchmarkStockCheck(
        [FromQuery] int n,
        CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");
        }

        var orderIds = _seeder.SeedOrders(n);

        if (orderIds.Count == 0)
        {
            return BadRequest("Run /seed first so customers, products, and inventory exist.");
        }

        var sequentialTimer = Stopwatch.StartNew();
        var sequentialResults = await _diagnostics.CheckManyOrdersSequentialAsync(_db, orderIds, ct);
        sequentialTimer.Stop();

        var concurrentTimer = Stopwatch.StartNew();
        var concurrentResults = await _concurrentDiagnostics.CheckManyOrdersConcurrentAsync(orderIds, ct);
        concurrentTimer.Stop();

        var sequentialMs = sequentialTimer.ElapsedMilliseconds;
        var concurrentMs = concurrentTimer.ElapsedMilliseconds;
        double? speedup = concurrentMs == 0
            ? null
            : Math.Round((double)sequentialMs / concurrentMs, 2);

        return Ok(new
        {
            ordersCreated = orderIds.Count,
            sequentialMs,
            concurrentMs,
            speedup,
            note = "Training benchmark: sequential uses one DbContext with foreach/await; concurrent uses one DbContext per task with Task.WhenAll.",
            sequentialChecked = sequentialResults.Count,
            concurrentChecked = concurrentResults.Count,
            orderIds,
            sample = concurrentResults.Take(5)
        });
    }
}
