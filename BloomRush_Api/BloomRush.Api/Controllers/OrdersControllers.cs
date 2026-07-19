using System.Diagnostics;
using BloomRush.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloomRush.Api.Seeding;
using BloomRush.Api.Fulfillment;
using BloomRush.Data.Enums;
using Serilog;

namespace BloomRush.Api.Controllers;

[ApiController] 
[Route("orders")] 
public class OrdersController : ControllerBase
{
    private readonly BloomRushDbContext _context;
    private readonly ISeeder _seeder;
    private readonly IFulfillmentService _fulfillment;
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostApplicationLifetime _lifetime;

    public OrdersController(
        BloomRushDbContext context,
        ISeeder seeder,
        IFulfillmentService fulfillment,
        IServiceScopeFactory scopes,
        IHostApplicationLifetime lifetime)
    {
        _context = context;
        _seeder = seeder;
        _fulfillment = fulfillment;
        _scopes = scopes;
        _lifetime = lifetime;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken ct)
    {
        var orders = await _context.Orders
        .AsNoTracking()
        .Include(order => order.Customer)
        .Include(order => order.Lines)
        .OrderByDescending(order => order.CreatedAtUtc)
        .Select(order => new
        {
            order.Id,
            customerId = order.CustomerId,
            customerName = order.Customer.Name,
            customerEmail = order.Customer.Email,
            priority = order.Priority.ToString(),
            status = order.Status.ToString(),
            order.CreatedAtUtc,
            order.CompletedAtUtc,
            lineCount = order.Lines.Count,
            totalUnits = order.Lines.Sum(line => line.Quantity)
        })
        .ToListAsync(ct);

        return Ok(orders);
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken ct)
    {
        var orderHeader = await _context.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new
            {
                order.Id,
                customerId = order.CustomerId,
                customerName = order.Customer.Name,
                customerEmail = order.Customer.Email,
                priority = order.Priority.ToString(),
                status = order.Status.ToString(),
                order.CreatedAtUtc,
                order.CompletedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        if (orderHeader == null)
        {
            return NotFound($"Order {orderId} was not found.");
        }

        var lines = await _context.OrderLines
            .AsNoTracking()
            .Where(line => line.OrderId == orderId)
            .OrderBy(line => line.Id)
            .Select(line => new
            {
                line.Id,
                line.ProductId,
                sku = line.Product.Sku,
                productName = line.Product.Name,
                line.Quantity
            })
            .ToListAsync(ct);

        var events = await _context.FulfillmentEvents
            .AsNoTracking()
            .Where(evt => evt.OrderId == orderId)
            .OrderBy(evt => evt.TimestampUtc)
            .Select(evt => new
            {
                evt.Id,
                type = evt.Type.ToString(),
                evt.Message,
                evt.TimestampUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            orderHeader.Id,
            orderHeader.customerId,
            orderHeader.customerName,
            orderHeader.customerEmail,
            orderHeader.priority,
            orderHeader.status,
            orderHeader.CreatedAtUtc,
            orderHeader.CompletedAtUtc,
            lines,
            events
        });
    }

    [HttpDelete("{orderId:int}/cascade")]
    public async Task<IActionResult> DeleteOrderCascade(int orderId, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(order => order.Lines)
            .Include(order => order.Events)
            .FirstOrDefaultAsync(order => order.Id == orderId, ct);

        if (order == null)
        {
            return NotFound($"Order {orderId} was not found.");
        }

        var deletedLines = order.Lines.Count;
        var deletedEvents = order.Events.Count;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            orderId,
            deletedLines,
            deletedEvents,
            message = "Order deleted with cascade."
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetOrderStatusCounts(CancellationToken ct)
    {
        var results = await _context.Orders
            .AsNoTracking()
            .GroupBy(order => order.Status)
            .Select(group => new
            {
                status = group.Key.ToString(),
                count = group.Count()
            })
            .ToListAsync(ct);

        return Ok(results);
    }
    [HttpPost("seed")]
    public async Task<IActionResult> SeedOrders([FromQuery]int n, CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");   
        }

        var orderIds = await _seeder.SeedOrdersAsync(n, ct);

        if (orderIds.Count == 0)
        {
            return BadRequest("Run /resetbaseline first so customers, products, and inventory exist.");
        }
        return Ok(new
        {
            created = orderIds.Count,
            orderIds = orderIds
        });
        
    }

    [HttpPost("{orderId:int}/fulfill")]
    public async Task<IActionResult> FulFill(int orderId, CancellationToken ct)
    {
        var orderStatus = await _context.Orders
        .Where(order => order.Id == orderId)
        .Select(order =>  (Status?)order.Status)
        .FirstOrDefaultAsync(ct);

        if (orderStatus == null)
        {
            return NotFound($"Order {orderId} was not found.");
        }
        if (orderStatus != Status.Pending)
        {
        return Ok(new
            {
                orderId,
                result = orderStatus.ToString(),
                message = $"Order {orderId} is already {orderStatus}."
            });
        }
        var result = await _fulfillment.FulfillOneAsync(orderId, ct);

        return Ok(new
        {
            orderId,
            result = result.ToString()
        });
    }

    [HttpPost("burst")]
    public async Task<IActionResult> BurstOrders([FromQuery] int n, CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");
        }

        var orderIds = await _seeder.SeedOrdersAsync(n, ct);

        if (orderIds.Count == 0)
        {
            return BadRequest("Run /resetbaseline first so customers, products, and inventory exist.");
        }

        var appStopping = _lifetime.ApplicationStopping;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var fulfillmentService = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();

                await fulfillmentService.FulfillBurstAsync(orderIds, appStopping);
            }
            catch (OperationCanceledException) when (appStopping.IsCancellationRequested)
            {
                Log.Information(
                    "Burst fulfillment stopped because application shutdown was requested. OrderCount: {OrderCount}",
                    orderIds.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Burst fulfillment failed");
            }
        }, appStopping);

        Log.Information(
            "Burst accepted {OrderCount} orders. OrderIds: {@OrderIds}",
            orderIds.Count,
            orderIds);

        return Accepted("/orders", new
        {
            message = "Burst accepted. Fulfillment is running in a background task.",
            created = orderIds.Count,
            orderIds
        });
    }

    [HttpPost("fulfillment-benchmark")]
    public async Task<IActionResult> FulfillmentBenchmark([FromQuery] int n, CancellationToken ct)
    {
        if (n <= 0)
        {
            return BadRequest("n must be greater than zero.");
        }

        var sequentialOrderIds = await _seeder.SeedOrdersAsync(n, ct);

        if (sequentialOrderIds.Count == 0)
        {
            return BadRequest("Run /resetbaseline first so customers, products, and inventory exist.");
        }

        var sequentialTimer = Stopwatch.StartNew();
        var sequentialResults = await _fulfillment.FulfillManySequentialAsync(sequentialOrderIds, ct);
        sequentialTimer.Stop();

        var concurrentOrderIds = await _seeder.SeedOrdersAsync(n, ct);

        if (concurrentOrderIds.Count == 0)
        {
            return BadRequest("Run /resetbaseline first so customers, products, and inventory exist.");
        }

        var concurrentTimer = Stopwatch.StartNew();
        var concurrentResults = await _fulfillment.FulfillManyConcurrentAsync(concurrentOrderIds, ct);
        concurrentTimer.Stop();

        var sequentialMs = sequentialTimer.ElapsedMilliseconds;
        var concurrentMs = concurrentTimer.ElapsedMilliseconds;

        return Ok(new
        {
            ordersRequested = n,
            sequentialMs,
            concurrentMs,
            note = "This endpoint does not reset baseline. Call POST /resetbaseline before it when you need a clean starting point.",
            sequential = new
            {
                created = sequentialOrderIds.Count,
                fulfilled = sequentialResults.Count(result => result.Result == FulfillmentResult.Fulfilled),
                backordered = sequentialResults.Count(result => result.Result == FulfillmentResult.Backordered),
                orderIds = sequentialOrderIds,
                sample = sequentialResults.Take(5)
            },
            concurrent = new
            {
                created = concurrentOrderIds.Count,
                fulfilled = concurrentResults.Count(result => result.Result == FulfillmentResult.Fulfilled),
                backordered = concurrentResults.Count(result => result.Result == FulfillmentResult.Backordered),
                orderIds = concurrentOrderIds,
                sample = concurrentResults.Take(5)
            }
        });
    }
}
