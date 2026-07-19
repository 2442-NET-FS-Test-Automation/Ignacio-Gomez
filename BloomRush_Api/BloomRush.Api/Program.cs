using BloomRush.Api.Fulfillment;
using BloomRush.Api.Services;
using BloomRush.Api.Seeding;
using BloomRush.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using BloomRush.Api.Middleware;


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

// Register the normal scoped DbContext in dependency injection.
// Endpoints and services receive BloomRushDbContext directly.
builder.Services.AddDbContext<BloomRushDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloomRushDb")));

// Fulfillment uses a DbContext factory because burst/concurrent processing
// needs fresh DbContext instances that are not shared between tasks.
builder.Services.AddDbContextFactory<BloomRushDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloomRushDb")),
    ServiceLifetime.Scoped);

// Register our own app services.
// ISeeder is implemented by Seeder in Seeding/Seeder.cs.
// IFulfillmentService is implemented by FulfillmentService in Fulfillment/FulfillmentService.cs.
builder.Services.AddControllers();
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();


var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


// Swagger services let us test the endpoints from the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddMemoryCache();
// builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Enable controllers
app.UseHttpsRedirection();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// 4. Map the controller endpoints in the middleware pipeline
app.MapControllers();

// Graceful shutdown log flushing:
// When the app stops, Serilog flushes any buffered log events before exit.
app.Lifetime.ApplicationStopped.Register(() => Log.CloseAndFlush());

app.MapGet("/", () => "BloomRush API ready");


app.Run();
Log.CloseAndFlush();
