using BloomRush.Api.Fulfillment;
using BloomRush.Api.Seeding;
using BloomRush.Data;
using BloomRush.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog writes structured events to the console.
// The burst background task logs one event per processed order.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

// Program.cs is the HTTP entry point.
// Swagger/browser/Postman call these endpoints first.
// When an endpoint needs business logic, it calls a service such as Seeder or FulfillmentService.

// Register the DbContext factory in dependency injection.
// Any endpoint or service can ask for IDbContextFactory<BloomRushDbContext>.
// The factory creates a fresh DbContext when code needs to talk to SQL Server.
builder.Services.AddDbContextFactory<BloomRushDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloomRushDb")));

// Register our own app services.
// ISeeder is implemented by Seeder in Seeding/Seeder.cs.
// IFulfillmentService is implemented by FulfillmentService in Fulfillment/FulfillmentService.cs.
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();

// Swagger services let us test the endpoints from the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.Lifetime.ApplicationStopped.Register(() => Log.CloseAndFlush());

app.MapGet("/", () => "BloomRush API ready");

// POST /seed
// This endpoint does not create the data directly.
// It receives ISeeder from dependency injection, then calls Seeder.RestoreBaseline().
// Seeder returns SeedResult, and Program.cs converts that result into the HTTP response.
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

// GET /orders
// Read-only endpoint for seeing every order at a summary level.
// It creates a DbContext, reads Orders + Customer + Lines, then sends a shaped JSON response.
app.MapGet("/orders", async (
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    // CreateDbContextAsync gives this endpoint its own short-lived database connection unit.
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    // AsNoTracking means EF Core reads data but does not prepare it for updates.
    // Include tells EF Core to also load the related Customer and Lines.
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

// GET /orders/{orderId}
// Read-only endpoint for inspecting one order.
// This is the "what does this order contain?" endpoint:
// Order -> Customer, OrderLines -> Products, and FulfillmentEvents.
app.MapGet("/orders/{orderId:int}", async (
    int orderId,
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    // The Select creates the exact JSON shape that Swagger receives.
    // We are not sending the EF entity directly; we send only the fields we want to show.
    var order = await db.Orders
        .AsNoTracking()
        .Include(order => order.Customer)
        .Include(order => order.Lines)
            .ThenInclude(line => line.Product)
        .Include(order => order.Events)
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
            order.CompletedAtUtc,
            lines = order.Lines
                .OrderBy(line => line.Id)
                .Select(line => new
                {
                    line.Id,
                    line.ProductId,
                    sku = line.Product.Sku,
                    productName = line.Product.Name,
                    line.Quantity
                })
                .ToList(),
            events = order.Events
                .OrderBy(evt => evt.TimestampUtc)
                .Select(evt => new
                {
                    evt.Id,
                    type = evt.Type.ToString(),
                    evt.Message,
                    evt.TimestampUtc
                })
                .ToList()
        })
        .FirstOrDefaultAsync(ct);

    if (order == null)
    {
        return Results.NotFound($"Order {orderId} was not found.");
    }

    return Results.Ok(order);
});

// GET /inventory
// Read-only endpoint for checking stock.
// Use this before and after /orders/{id}/fulfill to see QuantityOnHand decrease.
app.MapGet("/inventory", async (
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    var inventory = await db.InventoryItems
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

    return Results.Ok(inventory);
});

// POST /orders/seed?n=5&expedited=false
// Program.cs receives the query values n and expedited from the URL.
// It calls Seeder.SeedOrders(), which creates Order and OrderLine rows.
// Seeder returns only the new order IDs, then this endpoint sends those IDs to Swagger.
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
        expedited = expedited,
        orderIds = orderIds
    });
});

// POST /orders/burst?n=20&expedited=false
// This creates sample orders, starts background fulfillment, then returns immediately.
// The endpoint does not wait for fulfillment to finish because Task.Run keeps working
// after the HTTP request is already done.
app.MapPost("/orders/burst", (
    int n,
    bool expedited,
    ISeeder seeder,
    IServiceScopeFactory scopes,
    IHostApplicationLifetime lifetime) =>
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

    // This token is canceled when the app is shutting down.
    // It is better than using the HTTP request token because this work should keep running
    // after Swagger already got its response.
    var appStopping = lifetime.ApplicationStopping;

    _ = Task.Run(async () =>
    {
        try
        {
            // The background task is outside the HTTP request.
            // CreateScope gives it a fresh dependency-injection scope.
            using var scope = scopes.CreateScope();
            var fulfillmentService = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();

            // FulfillBurstAsync fans out over the single-order path.
            // Each order still goes through FulfillOneAsync, including concurrency retry.
            await fulfillmentService.FulfillBurstAsync(orderIds, appStopping);
        }
        catch (OperationCanceledException) when (appStopping.IsCancellationRequested)
        {
            // The app is shutting down, so this is expected.
        }
        catch (Exception ex)
        {
            // Fire-and-forget tasks do not return exceptions to the endpoint,
            // so we must log errors here.
            Log.Error(ex, "Burst fulfillment failed");
        }
    }, appStopping);

    Log.Information(
        "Burst accepted {OrderCount} orders. Expedited: {Expedited}. OrderIds: {@OrderIds}",
        orderIds.Count,
        expedited,
        orderIds);

    return Results.Accepted("/orders", new
    {
        message = "Burst accepted. Fulfillment is running in a background task.",
        created = orderIds.Count,
        expedited = expedited,
        orderIds = orderIds
    });
});

// POST /orders/{orderId}/fulfill
// Program.cs receives the orderId from the URL.
// It does a small validation check, then calls FulfillmentService.FulfillOneAsync().
// The service owns the real business logic: check stock, decrement inventory,
// change order status, and create a FulfillmentEvent.
app.MapPost("/orders/{orderId:int}/fulfill", async (
    int orderId,
    IDbContextFactory<BloomRushDbContext> contextFactory,
    IFulfillmentService fulfillmentService,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    // The endpoint checks if the order exists before asking the service to process it.
    var orderStatus = await db.Orders
        .Where(order => order.Id == orderId)
        .Select(order => (Status?)order.Status)
        .FirstOrDefaultAsync(ct);

    if (orderStatus == null)
    {
        return Results.NotFound($"Order {orderId} was not found.");
    }

    // Fulfillment should only run once. If the order already finished,
    // return its current state without decrementing inventory again.
    if (orderStatus != Status.Pending)
    {
        return Results.Ok(new
        {
            orderId,
            result = orderStatus.ToString(),
            message = $"Order {orderId} is already {orderStatus}."
        });
    }

    // The service returns only Fulfilled or Backordered.
    var result = await fulfillmentService.FulfillOneAsync(orderId, ct);

    return Results.Ok(new
    {
        orderId,
        result = result.ToString()
    });
});

// GET /reports/top-products
// LINQ report endpoint required by the project.
// This is not a raw table dump: it groups OrderLines by product and calculates totals.
app.MapGet("/reports/top-products", async (
    IDbContextFactory<BloomRushDbContext> contextFactory,
    CancellationToken ct) =>
{
    await using var db = await contextFactory.CreateDbContextAsync(ct);

    // Join connects OrderLines to Products so the report can show SKU/name instead of only ProductId.
    // GroupBy creates one result row per product.
    // Sum and Count are the aggregation part of the report.
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
