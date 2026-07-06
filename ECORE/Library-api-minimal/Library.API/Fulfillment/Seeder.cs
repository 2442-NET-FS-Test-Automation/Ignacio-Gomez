using Library.Data;
using Library.Data.Entities;
using Library.Date.Entities;
using Microsoft.EntityFrameworkCore;

// In production our orders would come from users these APIS' run locally
// So we could either - create a post for a single order and run a shell script or something
// or we could create a seeding endpoint from here to generate some orders for us
public interface ISeeder
{
    IReadOnlyList<int> SeedOrders(int n, bool expedited);
    IReadOnlyList<int> ResetAndCreateOrders(int n);
}

public class Seeder : ISeeder
{
    private static readonly string[] Skus = {"BK-001", "BK-002","BK-003"};
    private readonly IDbContextFactory<LibraryDbContext> _factory;
    
    public Seeder(IDbContextFactory<LibraryDbContext> factory)
    {
        _factory =  factory;
    }
    public IReadOnlyList<int> SeedOrders(int n, bool expedited)
    {
        using var db = _factory.CreateDbContext();
        var pid = db.Products.ToDictionary(p => p.Sku, p => p.Id);
        var ids = new List<int>(n);

        //Based on n (number of orders the user want to seed)
        //lets use a for loop to create those orders programatically

        for (int i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1, 3), // random number - bounded
                Priority = expedited ? Priority.Expedited : Priority.Normal,
                Lines = { new OrderLines {ProductId = pid[Skus[i % Skus.Length]], Quantity = 1}}

            };
            db.Orders.Add(order); //Add - stage changes in EF Core change tracker
            db.SaveChanges();
            ids.Add(order.Id);
        }
        return ids;

    }
    public IReadOnlyList<int> ResetAndCreateOrders(int n)
    {
        using var db = _factory.CreateDbContext();
        foreach (InventoryItem inv in db.Inventory)
        {
            switch (inv.ProductId)
            {
                case 1:
                    inv.CurrentStock = 5;
                    break;
                case 2:
                    inv.CurrentStock = 3;
                    break;
                case 3:
                    inv.CurrentStock = 4;
                    break;
                default:
                    break;
            }
        }
        db.SaveChanges();
        var pid = db.Products.ToDictionary(p => p.Sku, p => p.Id);
        var ids = new List<int>(n);

        for( var i = 0; i < n; i++)
        {
            var order = new Order
            {
                CustomerId = Random.Shared.Next(1, 3),
                Priority = i % 3 == 0 ? Priority.Expedited : Priority.Normal,
                Lines = {new OrderLines {ProductId = pid[new []{"BK-001","BK-002","BK-003"}[i % 3]], Quantity = 1}}
            };
            db.Orders.Add(order);
            db.SaveChanges();
            ids.Add(order.Id);
        }
        return ids;
    }
}