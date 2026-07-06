using BloomRush.Data.Entities;
using Serilog;
// Migration notes:
// No extra application code is required just to create or apply an EF Core migration.
// EF Core reads the existing entities, the BloomRushDbContext, and the connection string.
//
// Commands used:
//
// dotnet build
// Restores packages if needed and compiles the solution.
//
// dotnet ef migrations add InitialCreate --project BloomRush.Data\BloomRush.Data.csproj --startup-project BloomRush.Api\BloomRush.Api.csproj --output-dir Migrations
// Creates the first migration files from the current entity model and DbContext configuration.
//
// dotnet ef database update --project BloomRush.Data\BloomRush.Data.csproj --startup-project BloomRush.Api\BloomRush.Api.csproj
// Applies the migration to SQL Server and creates the BloomRushDb database tables.

using BloomRush.Data;
using Microsoft.EntityFrameworkCore;

// This prepares the application configuration. No database call happens here.
var builder = WebApplication.CreateBuilder(args);

// Registers BloomRushDbContext in the API dependency injection container.
// The connection string named "BloomRushDb" comes from appsettings.json.
// This is setup only: it tells the API how to create a DbContext later.
// It does not connect to SQL Server yet.
builder.Services.AddDbContext<BloomRushDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloomRushDb")));

// Swagger setup only. These lines prepare the API documentation/testing page.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Builds the web app from the configuration above. No database call happens here.
var app = builder.Build();

// Enables the Swagger JSON and Swagger UI page.
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World!");

// Endpoint to create a product in the database.
// The request data arrives from the HTTP body as CreateProductRequest.
// The DbContext is provided by dependency injection for this HTTP request.
app.MapPost("/products", async (CreateProductRequest request, BloomRushDbContext db) =>
{
    try
    {
        // Memory only: this creates a C# object from the request data.
        // Nothing has been sent to SQL Server yet.
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price
        };

        // Preparation only: EF Core starts tracking this product as "to be inserted".
        // This still does not call the database.
        db.Products.Add(product);

        // Real database call: EF Core sends the INSERT command to SQL Server here.
        // After this finishes, SQL Server has generated the product Id.
        await db.SaveChangesAsync();

        // Log after the database confirms the insert worked.
        Log.Information("Product created with Id {ProductId} and SKU {Sku}", product.Id, product.Sku);

        // Sends the HTTP response back to the client with the created product.
        return Results.Created($"/products/{product.Id}", product);
    }
    catch (Exception ex)
    {
        // Logs database or validation errors, such as a duplicated unique SKU.
        Log.Error(ex, "Error creating product with SKU {Sku}", request.Sku);
        return Results.Problem("An error occurred while creating the product.");
    }
});

// Test endpoint to insert one fixed product.
// It does not need a request body because the product data is hardcoded.
app.MapPost("/seed-product", (BloomRushDbContext db) =>
{
    // Memory only: creates a C# object with fixed test data.
    var product = new Product
    {
        Sku = "ROSE-RED-12",
        Name = "Red Roses Bouquet",
        Price = 49.99m
    };

    // Preparation only: marks this product as pending insert.
    db.Products.Add(product);

    // Real database call: sends the INSERT command to SQL Server.
    db.SaveChanges();

    // Sends the inserted product back to the client.
    return Results.Ok(product);
});

// Endpoint to query products from the database using LINQ.
app.MapGet("/products/cheap", async (BloomRushDbContext db) =>
{
    // Query preparation: Where builds the SQL filter, but the query has not run yet.
    var products = await db.Products
        .Where(p => p.Price < 50)
        // Real database call: ToListAsync executes the query in SQL Server.
        // The data from SQL Server arrives here as a List<Product>.
        .ToListAsync();

    // Sends the products found in the database back to the client.
    return products;
});

// Starts the API and begins listening for HTTP requests.
app.Run();

record CreateProductRequest(string Sku, string Name, decimal Price);
