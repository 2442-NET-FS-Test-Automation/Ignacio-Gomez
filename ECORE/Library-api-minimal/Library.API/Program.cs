using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Data.Entities;
using Serilog;
using Library.Api.Fulfillment;
using System.Diagnostics;

// This is my API program.cs
// No main. We can think of it as 2 sections
// Registering things with the builder. 
// And then configuring things on the app
// And at the very bottom that app object that represents our entire API calls its run method

// Builder area
var builder = WebApplication.CreateBuilder(args);

// The first thing that we need is to give our builder a connection string to our database
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa;Password=LibraryPass1!;TrustServerCertificate=true";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Write to console, and write to a file - starting a new file each day.
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Tell the builder to use Serilog for logging


// Tell the builder to use our LibraryDbContext with the connection string above
// By registering our DbContext class (or even classes, technically you use one per Database)
// we hand off the managing of creating and destroying these DbContext objects to ASP.NET's
// dependency injection container. Like spring beans if you're familiar. 

// ASP.NET has few different scope types. 
// Transient - a new instance is created every time it's requested.
// Scoped - a new instance per HTTP request 
// Singleton - A single instance for the entire runtime of the app 
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton); // Scoped is the default, but we can be explicit - and allow for SingletonScope 
                                                        // when needed

// We know we will need more than one LibraryDbContext in one or more of these methods. But we don't know how many 
// before runtime. So we can use a DbContextFactory to create as many as we need at runtime.
builder.Services.AddDbContextFactory<LibraryDbContext>(options => options.UseSqlServer(conn_string));

// Registered our custom service with the builder
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<BurstPlanner>(); // adding our BurstPlanner, will be used in FulfillmentService
builder.Services.AddScoped<OrderFactory>(); 

// Swagger stuff added to builder
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App area
var app = builder.Build();

// Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

// Endpoint area
app.MapGet("/", () => "Hello World!");

// Get all items from the inventory
app.MapGet("/inventory", async (LibraryDbContext db) => {
    // we should probably await this - may not matter because we are local
    return await db.Inventory.ToListAsync();
});

// Lets use LINQ - Language Integrated Query
// LINQ is a library that just lets us query collections
// The logic actually flows from SQL DQL - You can use method OR sql query syntax
// You can even save the queries themselves as C# objects if you want to 
app.MapGet("/inventory/by-value", (LibraryDbContext db) => {
    
    return db.Inventory.Include(i => i.Product)
        .GroupBy(i => i.CurrentStock >= 5 ? "well-stocked": "low")  //group by just like in sql
        .Select(g => new { tier = g.Key, count = g.Count(), units = g.Sum(i => i.CurrentStock) })
        .ToList();
});


// Any endpoints that start with "/peek/*" are diagnostic/demo
// We are going to use them to expose things like EF Core change tracking and other 
// underlying behaviors for learning. A real app would have no reason to expose HTTP endpoints
// to outside users to make this stuff observable. 

app.MapGet("/peek/tracking", (LibraryDbContext db) =>
{
    // Lets see the underlying EF Core change tracker
    var unchanged = db.Products.First(); // grab the first object. Read but not modified => Unchanged
    var modified = db.Products.Skip(1).First(); // queried... still Unchanged as of here.

    modified.Price += 1; // state => Modified

    // When we create a new object and call the dbset's .Add() method it's state is
    // "Added" - this has not actually hit the database yet. But it's tracked to be added.
    db.Products.Add(new Product { Sku = "BK-TMP", Name = "Tmp", Price = 1m});

    // This bit of code is the non-production demo bit
    // We are accessing the LibraryDbContext object's change tracker to pull info. 
    // At most you'd debug with this. 
    var states = db.ChangeTracker.Entries()
        .Select(e => new { entity = e.Entity.GetType().Name, state = e.State.ToString()})
        .ToList();

    db.SaveChanges();

    // Clearing the change tracker manually
    db.ChangeTracker.Clear();

    return states;
});

// Peek - Loading Strategies
app.MapGet("/peek/loading", (LibraryDbContext db) =>
{
    Product product = db.Products.First(); // grab the first product from DB table
    // Explicit loading via Load()
    db.Entry(product).Reference(p => p.Inventory).Load(); // making another trip to the database to populate the property

});


// Lets manually go out of our way to create a conflict - obviously, don't do this in a real app
app.MapGet("/peek/conflict", (IServiceScopeFactory scopes) =>
{
    // Manually asking for scopes. Normally each endpoint method call gets its own scope tracked
    // by ASP.NET under the hood during runtime. We can, for various reasons good and bad do this manually.
    using var scopeA = scopes.CreateScope();
    using var scopeB = scopes.CreateScope();

    // Now, remember that a dbContext is generated per scope, so we have to do that too
    var firstDb = scopeA.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var secondDb = scopeB.ServiceProvider.GetRequiredService<LibraryDbContext>();

    // Each dbContext reads from the same database BUT they track changes independently 
    // remember we gave Inventory entities a RowVersion - not just a property named RowVersion
    // but an actual OnModelCreation FluentAPI config for a RowVersion
    // Both of these start with the same RowVersion value
    var firstInventory = firstDb.Inventory.First(i => i.Id == 1); // RowVersion = 1
    var secondInventory = secondDb.Inventory.First(i => i.Id == 1); //RowVersion = 1

    // Lets modify one AND save its changes, while just modifying the other
    firstInventory.CurrentStock --; // decrement => Modified
    firstDb.SaveChanges(); // save changes is what persists any created, deleted or modified objects
    // that row in the DB now has a RowVersion of 2 

    // Calling SaveChanges() above modifies the RowVersion value

    // This object, that should represent the exact same row in the DB now has a stale RowVersion
    // before EF tries to persist any changes, it will check the RowVersion. It won't match
    // and an exception will be thrown
    secondInventory.CurrentStock --; // Rowversion still 1 - doesnt match DB

    try
    {
        secondDb.SaveChanges(); // this should fail as RowVersions don't match
    } 
    catch (DbUpdateConcurrencyException ex)
    {
        // In this case we want EF to retry the UPDATE 
        // Asking for the actual ChangeTracker entry that threw the exception 
        // this is EF Core specific.
        var entry = ex.Entries.Single(); 

        // For the entry that threw the exception - grab it's current values from the DB
        // not the object, just the values 
        var current = entry.GetDatabaseValues();

        // Every entry in the change tracker tracks two sets of values. 
        // OriginalValues = the values of the object when it was loaded from the db
        // CurrentValues = the new modified values we changed on the object in our app
        // Here we manually set the OriginalValues to the values from the DB we JUST grabbed
        entry.OriginalValues.SetValues(current!);

        // Using the entry to grab the actual item - going somewhat backwards
        ((InventoryItem)entry.Entity).CurrentStock =
            current!.GetValue<int>(nameof(InventoryItem.CurrentStock)) - 1;

        secondDb.SaveChanges();
    }
    
    // I can send back specific codes via methods like .Ok() with messages inside 
    // others include Problem(), NotFound(), etc
    return Results.Ok("Conflict caught, reloaded and retried.");

});

// Endpoint to reset the stock of the items in my catalog - useful for testing and demo 
// might need to hit this endpoint while we work
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

// Fulfillment stuff for orders goes down here
// Im going to take in info from the front end (swagger for now)
// I have a few options
// I can take in from the uri/query string
// I can also take in parameters from the body

// Quick method to fulfill one order
app.MapPost("/orders", async ( OrderPaylod orderRequest,  IDbContextFactory<LibraryDbContext> factory,
            CancellationToken ct, IFulfillmentService fSvc) =>
{
    // Remember we create an order in our db
    // And then try to create a Successful fulfillment record against the db
    await using var db = await factory.CreateDbContextAsync(ct); // ask for db context to place order

    var newOrder = new Order
    {
        CustomerId = orderRequest.CustomerId,
        Priority = Priority.Normal,
        // Using the orderRequest from the HTTP request body to create my order
        Lines = { new OrderLine { ProductId = orderRequest.ProductId, Quantity = orderRequest.Quantity}}
    };

    db.Orders.Add(newOrder); // add new order 
    await db.SaveChangesAsync(ct); // save that order to db

    // Now that we've added the order - we try to fulfill it
    FulfillmentResult result = await fSvc.FulfillOneAsync(newOrder.Id, ct); // newOrder is now in the db, we can ask for its pk
    return Results.Ok(new {orderId = newOrder.Id, result = result.ToString()});
});



// Burst endpoint
// Forgoing creating a record - we will take these from a the query string
// IHostAPplicationLifetime - this lets us see events related to the app lifetime
// We are going to use it to make sure we "flush" pending orders if the app is asked to stop
app.MapPost("/orders/burst", (int n, bool expedited,ISeeder seeder, 
    IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) =>
{
    var ids = seeder.SeedOrders(n, expedited); // calling the seed orders method with the stuff from front end
    var appStopping = lifetime.ApplicationStopping; // gives us a cancellation token that is called when app goes to shutdown

    _ = Task.Run( async () => // assigning the task result to a discard runs this as a background task
    {
        try
        {
            using var scope = scopes.CreateScope(); // ask for a fresh scope
            var service  = scope.ServiceProvider.GetRequiredService<IFulfillmentService>(); //grab a fulfillment service
            await service.FulfillBurstAsync(ids, appStopping); // use it to call fulfillBurstAsync()
        } 
        catch (Exception ex)
        {   
            // This task is fire and forget because we aren't waiting or storing its result
            // any exceptions would be "swallowed" i.e. they would die with the task in the background 
            Log.Error(ex, "Burst fulfillment failed");
        }
    }, appStopping);

});

app.MapGet("/verify/no-oversell", (LibraryDbContext db) =>
{
    var rows = db.Inventory.Include(i => i.Product).ToList(); // grab Inventory rows, include the product objects as well
    var negative = rows.Where(i => i.CurrentStock < 0).ToList(); //grab items with negative stock
    var fulfilled = db.FulfillmentEvents.Count(e => e.Type == "Fulfilled"); // count the fulfilled orders

    return new
    {
        anyNegative = negative.Any(),
        onHand = rows.Select(i => new {i.ProductId, i.CurrentStock}),
        unitsFulfilled = fulfilled 
    };

});


app.MapPost("/benchmark", async (int n, IFulfillmentService fs, ISeeder seeder, CancellationToken ct) =>
{
    // Lets see how sequential vs concurrent/arallel runs compare - with mixed orders
    var ids1 = seeder.ResetAndCreateOrders(n);

    // First, sequential
    var sw1 = Stopwatch.StartNew(); // start our stopwatch

    foreach ( var id in ids1)
        await fs.FulfillOneAsync(id, ct);

    sw1.Stop();

    // Next concurrent
    var ids2 = seeder.ResetAndCreateOrders(n);

    var sw2 = Stopwatch.StartNew(); // start second stopwatch
    await fs.FulfillBurstAsync(ids2, ct);
    sw2.Stop();

    return new
    {
        sequentialMs = sw1.ElapsedMilliseconds,
        concurrentMs = sw2.ElapsedMilliseconds
    };

});


// Completion report -- what orders got completed and when
// Note: In general Expedited orders should be completed first. In practice - it depends on how long each thread takes
// if for some reason an expedited order's thread slows down (due to some background process on the computer or something)
// then a normal order CAN beat it. But we should see a defined trend.
app.MapGet("/reports/by-completion", (LibraryDbContext db) =>
{
   return db.Orders // look inside orders table
        .Where(o => o.Status == Status.Fulfilled) // grab fulfilled orders
        .OrderBy(o => o.CompletedUtc) // order by when they were completed
        .Select(o => new { o.Id, o.Priority, o.CompletedUtc}) // use info from those orders to make some return objects
        .ToList(); // put them in a list and return them as JSON body of response

});

app.MapGet("/reports/top-products", (LibraryDbContext db) =>
{
    var ranked = db.FulfillmentEvents
        .Where(e => e.Type == "Fulfilled")
        .Join(db.OrderLines, e => e.OrderId, l => l.OrderId, (e, l) => l)
        .GroupBy(l => l.ProductId)
        .Select(g => new { ProductId = g.Key, Units = g.Sum(l => l.Quantity) })
        .OrderByDescending(x => x.Units)
        .ToList(); // LINQ is basically magic - pls learn to use it

    return ranked;
});

// Binary search on the sorted result
app.MapGet("/reports/rank-of/{units:int}", (int units, LibraryDbContext db) =>
{   
    // Find product ranking that sold x units 
    var unitsDesc = db.FulfillmentEvents
        .Where(e => e.Type == "Fulfilled")
        .Join(db.OrderLines, e => e.OrderId, l => l. OrderId, (e, l) => l)
        .GroupBy(l => l.ProductId)
        .Select(g => g.Sum(l => l.Quantity))
        .OrderByDescending(u => u)
        .ToArray(); 


    // sorted DESC => using Binary Search to find the index of a specific quantity sold
    // 1000, 400, 330, 34
    // Our BinarySearch needs a comparer - for something like an int or a char this is easy 
    // if you want to do this with custom classes - you need to override CompareTo like we do ToString
    var index = Array.BinarySearch(unitsDesc, units, Comparer<int>.Create((a, b) => b.CompareTo(a)));
    return new { units, rank = index >= 0 ? index + 1 : -1}; // If BinarySearch doesn't find a thing - it returns some bitwise 
    // complement or something - we collapse it to -1 
});


app.MapPost("/orders-with-factory", async (OrderRequest req, OrderFactory factory, 
    IDbContextFactory<LibraryDbContext> dbf, CancellationToken ct) =>
{
    try
    {
        Order newOrder = factory.CreateOrder(req.Kind, req.CustomerId, 
            req.Lines.Select(l => (l.Sku, l.Qty)));
        
        await using var db = await dbf.CreateDbContextAsync(ct);

        db.Orders.Add(newOrder);

        await db.SaveChangesAsync(ct);

        return Results.Created($"/orders/{newOrder.Id}", new {newOrder.Id});
    }
    catch (UnknownSkuException ex)
    {
        Log.Warning("Rejected order: unknown SKU {Sku}", ex.Sku);
        return Results.BadRequest(new {error = ex.Message, sku = ex.Sku});
    }
});

app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("Shutdown requested — draining in-flight fulfillments"));

// My file always ends with app.Run() - minimal API or Controller API
app.Run();
Log.CloseAndFlush();
public record OrderPaylod(int ProductId, int Quantity, int CustomerId);
public record OrderLineRequest(string Sku, int Qty);
public record OrderRequest(string Kind, int CustomerId, List<OrderLineRequest> Lines);

