using BloomRush.Api.Fulfillment;
using BloomRush.Api.Seeding;
using BloomRush.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<BloomRushDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloomRushDb")));

builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "BloomRush API ready");

app.MapPost("/seed", (ISeeder seeder) =>
{
    var result = seeder.RestoreBaseline();

    return Results.Ok(new
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
});

app.MapGet("/orders", async (
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    var orders = await db.Orders
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

    return Results.Ok(orders);
});

app.MapPost("/orders/seed", (int n, bool expedited, ISeeder seeder) =>
{
    if (n <= 0)
    {
        return Results.BadRequest("n must be greater than zero.");
    }

    var orderIds = seeder.SeedOrders(n, expedited);

    if (orderIds.Count == 0)
    {
        return Results.BadRequest("Run /seed first so customers, products, and inventory exist.");
    }

    return Results.Ok(new
    {
        created = orderIds.Count,
        expedited,
        orderIds
    });
});

app.MapPost("/orders/{orderId:int}/fulfill", async (
    int orderId,
    IFulfillmentService fulfillmentService,
    CancellationToken ct) =>
{
    var result = await fulfillmentService.FulfillOneAsync(orderId, ct);

    if (result.Outcome == FulfillmentOutcome.NotFound)
    {
        return Results.NotFound(result.Message);
    }

    if (result.Outcome == FulfillmentOutcome.NotReady)
    {
        return Results.Ok(new
        {
            orderId = result.OrderId,
            outcome = result.Outcome.ToString(),
            result.Message
        });
    }

    return Results.Ok(new
    {
        orderId = result.OrderId,
        outcome = result.Outcome.ToString(),
        result.Message
    });
});

app.MapGet("/reports/top-products", async (
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    var report = await db.OrderLines
        .AsNoTracking()
        .Join(
            db.Products,
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

    return Results.Ok(ranked);
});

app.Run();
