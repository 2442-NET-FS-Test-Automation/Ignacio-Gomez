using Microsoft.EntityFrameworkCore;
using Library.Data;

//This is my API programs.cs
// No main. We can think of it as 2 sections
// Registering things with the builder
// And the configuring thing on the app
// And at the very botton that app object that represents our entire API Calls

var builder = WebApplication.CreateBuilder(args);
// The first thing that we need is to give our builder a connection string to our database
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User ID=sa;Password=LibraryPass1!;TrustServerCertificate=true";
// "Server=localhost,1433;Database=LibraryMinimalDb;User ID=sa;Password=LibraryPass1!;TrustServerCertificate=true"

//By registering our DbContext class (or even classes, technically you use one per database)
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(conn_string));
//App area
var app = builder.Build();

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

//My file always ends with app.Run -  minimal API or Controller API
app.Run();
