using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Data.Entities;
using Serilog;
using Microsoft.Extensions.Options;
using Library.API.Fulfillment;
using Library.Date.Entities;
using System.Diagnostics;
using Library.Api.Fulfillment;

//This is my API programs.cs
// No main. We can think of it as 2 sections
// Registering things with the builder
// And the configuring thing on the app
// And at the very botton that app object that represents our entire API Calls

var builder = WebApplication.CreateBuilder(args);
// The first thing that we need is to give our builder a connection string to our database
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User ID=sa;Password=LibraryPass1!;TrustServerCertificate=true";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fulfilments-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
// "Server=localhost,1433;Database=LibraryMinimalDb;User ID=sa;Password=LibraryPass1!;TrustServerCertificate=true"

//By registering our DbContext class (or even classes, technically you use one per database)
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string),
ServiceLifetime.Scoped, ServiceLifetime.Singleton);

builder.Services.AddDbContextFactory<LibraryDbContext>(options => options.UseSqlServer(conn_string));
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<BurstPlanner>();
builder.Services.AddScoped<OrderFactory>();

// Swagger stuff added to builder
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//App area
var app = builder.Build();

// Keep the local demo database schema aligned with the EF model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    db.Database.Migrate();
}

//Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

  
//Endpoint Area
app.MapGet("/", () => "Hello World!");

// Get all Items from the inventory
app.MapGet("/inventory",async (LibraryDbContext db) =>
{
    return await db.Inventory.ToListAsync();
});

//Lets use LiNQ - Language Integrated Query
//LINQ is a library that just letsus query collections
// The logic actually flows from SQL DQL - you can use method OR sql query syntax
// You can even save the queries themselves as C# objects if you want

app.MapGet("/inventory/by-value", (LibraryDbContext db) =>
{
   return db.Inventory.Include(i => i.Product)
        .GroupBy(i => i.CurrentStock >= 5 ? "Well-stocked" : "low")
        .Select(g => new { tier = g.Key, count = g.Count(), units = g.Sum(i => i.CurrentStock)})
        .ToList(); 
});

//Any endpoints that start with "/peek/" are diagnostic/demo
//We are going to use them to expose things like EF core change tracking and other
//underlying behaviors for learning. A real app would have no reason to expose HTTP endpoints
// to outside users to make this stuff observable

app.MapGet("/peek/tracking",(LibraryDbContext db) =>
{
    var unchanged = db.Products.First(); // grab the first object => Read but not modified
    var modified = db.Products.Skip(1).First();

    modified.Price += 1;
    db.Products.Add(new Product {Sku = "BK-TMP", Name="Tmp",Price=1m});
    //This bit of code is the non-production demo bit
    var states = db.ChangeTracker.Entries()
        .Select(e => new { entity = e.Entity.GetType().Name, state = e.State.ToString()})
        .ToList();
    db.ChangeTracker.Clear();
    return states;
});

app.MapGet("/peek/loading", (LibraryDbContext db) =>
{
   Product product = db.Products.First();
   db.Entry(product).Reference(p => p.Inventory).Load();
     
});


//Lets manually go out our way to create a conflict
app.MapGet("/peek/conflict", (IServiceScopeFactory scopes) =>
{
    using var scopeA = scopes.CreateScope();
    using var scopeB = scopes.CreateScope();
    var firstDb = scopeA.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var secondDb = scopeB.ServiceProvider.GetRequiredService<LibraryDbContext>();

    //Each dbContext reads from the same database BUT they track changes independently
    var firstInventory = firstDb.Inventory.First(i => i.Id == 1);
    var secondInvetory = secondDb.Inventory.First(i => i.Id == 1);

    firstInventory.CurrentStock --;
    firstDb.SaveChanges();

    secondInvetory.CurrentStock --;

    try
    {
        secondDb.SaveChanges(); //This should fail as RowVersions dont match
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // In this case we want EF to retry the UPDATE
        // Asking for the actual change tracker entry that threw exception
        // this is EF Core specific
        var entry = ex.Entries.Single();
        var current = entry.GetDatabaseValues();
        entry.OriginalValues.SetValues(current!);
        ((InventoryItem)entry.Entity).CurrentStock =
            current!.GetValue<int>(nameof(InventoryItem.CurrentStock)) - 1;
        secondDb.SaveChanges();
    }

    return Results.Ok("Conflict caught, reloaded and retried");

});

//Endpoint to reset the stock of the items im my catalog - useful for testing and demo
// might need to hit this endpoint 
app.MapPost("/inventory/rest", (LibraryDbContext db, ILogger<Program> logger) =>
{   
    // We just ask for an ILogger like we do our dbcontext
    // then use it as normal
    logger.LogInformation("Started seeing database");
    
    // What I want to do is reset the items that I know I stuck into the db.
    foreach (InventoryItem inv in db.Inventory) // for each item in my db Inventory table... do something
    {
        // I only want to do something if the primary key is 1, 2, or 3.... 
        switch (inv.Id)
        {
            case 1:
                inv.CurrentStock = 5;
                break;
            case 2: 
                inv.CurrentStock = 3;
                break;
            case 3: 
                inv.CurrentStock = 8;
                break;
            default:
                break;
        }

    }

    db.SaveChanges(); // persisting to db
    logger.LogInformation("Stock reset");
    return Results.Ok("stock reset");

});



// Fulfillment stuff orders 
app.MapPost("/orders", async (OrderPayLod orderRequest, IDbContextFactory<LibraryDbContext> factory,
            CancellationToken ct, IFulfillmentService fSvc) =>
{
    await using var db = await factory.CreateDbContextAsync(ct);
    var neworder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        Lines = { new OrderLines {ProductId = orderRequest.ProductId, Quantity = orderRequest.Quantity}} 
    };
    db.Orders.Add(neworder);
    await db.SaveChangesAsync(ct);
    FulfillmentResult result = await fSvc.FulfillOneAsync(neworder.Id, ct);
    return Results.Ok(new{orderId = neworder.Id, result =  result.ToString()});
});

//Burst endpoint
app.MapPost("/orders/burst", (int n, bool expedited, ISeeder seeder,
        IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) =>
{
    var ids = seeder.SeedOrders(n, expedited);
    var appStopping = lifetime.ApplicationStopping; // gives us a cancellation token that is called when app goes to shutdown
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();
            await service.FulfillBurstAsync(ids, appStopping);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Burst fulfillment failed");
        }
    }, appStopping);
});

app.MapPost("/benchmark", async (int n, IFulfillmentService fs, ISeeder seeder, CancellationToken ct) =>
{
   //Lest see how sequetial vs parallel runs compare
   var ids1 = seeder.ResetAndCreateOrders(n);
   var sw1 = Stopwatch.StartNew(); 

   foreach (var id in ids1)
    {
        await fs.FulfillOneAsync(id, ct);
    }
    sw1.Stop();
    var ids2 = seeder.ResetAndCreateOrders(n);
    var sw2 = Stopwatch.StartNew();
    await fs.FulfillBurstAsync(ids2, ct);
    sw2.Stop();
    
    return new
    {
        sequentialMs = sw1.ElapsedMilliseconds,
        concurrentsMs = sw2.ElapsedMilliseconds
    };
});

app.MapGet("/verify/no-oversell", (LibraryDbContext db)=>
{
   var rows = db.Inventory.Include(i => i.Product).ToList();
   var negative = rows.Where(i => i.CurrentStock < 0).ToList();
   var fulfilled = db.FulfillmentEvents.Count(e => e.Type == "Fulfilled");

   return new
   {
     anyNegative = negative.Any(),
     onHand = rows.Select(i => new{i.ProductId, i.CurrentStock}),
     unitsFulfilled = fulfilled  
   };
});

app.MapGet("/reports/by-completion", (LibraryDbContext db) =>
{
   return db.Orders
        .Where(o => o.Status == Status.Fulfilled)
        .OrderBy(o => o.CompletedUtc)
        .Select(o => new { o.Id, o.Priority, o.CompletedUtc}) 
        .ToList();
});

app.MapGet("/report/top-products", (LibraryDbContext db) =>
{
    var ranked = db.FulfillmentEvents
        .Where(e => e.Type == "Fulfilled")
        .Join(db.OrderLines, e => e.OrderId, l => l.OrderId, (e, l) => l)
        .GroupBy(l => l.ProductId)
        .Select(g => new {ProductId = g.Key, Units = g.Sum(l => l.Quantity)})
        .OrderByDescending(x => x.Units)
        .ToList();

        return ranked;
});

// Binary Search on the sorted
app.MapGet("/Reports/rank-of/{units:int}",(int units, LibraryDbContext db) =>
{
   var unitsDesc = db.FulfillmentEvents
        .Where(e => e.Type == "Fulfilled")
        .Join(db.OrderLines, e => e.OrderId, l => l.OrderId, (e, l) => l)
        .GroupBy(l => l.ProductId)
        .Select(g => g.Sum(l => l.Quantity))
        .OrderByDescending(u => u)
        .ToArray();

    var index = Array.BinarySearch(unitsDesc, units, Comparer<int>.Create((a, b) => b.CompareTo(a)));
    while (index > 0 && unitsDesc[index - 1] == units)
    {
        index--;
    }

    return new { units, rank = index >= 0 ? index + 1 : -1};
    // complements or something - we collapse it to -1

});
app.MapPost("/orders-with-factory", async (OrderRequest req, OrderFactory factory,
        IDbContextFactory<LibraryDbContext> dbf, CancellationToken ct) =>
{
    try
    {
      Order newOrder = factory.CreateOrder(req.Kind, req.CustomerId,
            req.lines.Select(l => (l.Sku, l.Qty)));
      await using var db = await dbf.CreateDbContextAsync(ct);
      db.Orders.Add(newOrder);
      await db.SaveChangesAsync(ct);
      return Results.Created($"/orders/{newOrder.Id}", new {newOrder.Id});
    }
    catch (UnknownSkuException ex)
    {
        Log.Warning("Reject order: unknown SKU {Sku}", ex.Sku);
        return Results.BadRequest(new {error = ex.Message,sku = ex.Sku});
    }
});
//My file always ends with app.Run -  minimal API or Controller API
app.Run();

Log.CloseAndFlush();
public record OrderPayLod(int ProductId, int Quantity, int CustomerId);
public record OrderLineRequest(string Sku, int Qty);
public record OrderRequest(string Kind, int CustomerId, List<OrderLineRequest> lines);
