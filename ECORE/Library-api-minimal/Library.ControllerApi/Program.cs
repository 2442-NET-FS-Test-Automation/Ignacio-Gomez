using Library.ControllerApi.Filters;
using Library.ControllerApi.Mapping;
using Library.ControllerApi.Middleware;
using Library.ControllerApi.Services;
using Library.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Adding connection string 
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa;Password=LibraryPass1!;TrustServerCertificate=true";



Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Write to console, and write to a file - starting a new file each day.
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Tell the builder to use Serilog for logging

//Adding CORS 
const string SpaCorsPolicy = "spa"; // string name for our policy

builder.Services.AddCors(o => o.AddPolicy(SpaCorsPolicy, p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
));

builder.Services.AddHttpClient<ISupplierClient, SupplierClient>(c => 
    c.BaseAddress = new Uri("https://dummyjson.com/"));



builder.Services.AddDbContextFactory<LibraryDbContext>(o => o.UseSqlServer(conn_string));

// Registering our custom Repo and Service Layer methods like we did before
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>(); // Could later swap for InventoryMongoRepo
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Adding our mapping profile for AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

// having our filter apply to every controller
builder.Services.AddControllers(o => o.Filters.Add<TimingFilter>());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Adding swagger back
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>(); // wraps all middleware below it, catches their exceptions

// Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// This is a simple diagnostic middleware. All it will do is time our requests for us and log that.
// It takes in ctx (HttpContext -> everything about the request AND the response) 
// next - represents a call to the subsequent middleware
app.Use(async (ctx, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    await next(ctx); // calling the next middleware in the chain - whatever that is

    sw.Stop();
    Log.Information("{Method} {Path} -> {StatusCode} in {Elapsed} ms",
        ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds
    );
});

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Headers.ContainsKey("X-Maintenance"))
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsync("Down for maintenance");
        return; // don't call next() - never hits controllers
    }

    await next(ctx);
});

app.UseSerilogRequestLogging();

app.UseCors(SpaCorsPolicy);

app.UseHttpsRedirection();

app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
Log.CloseAndFlush();
