using Microsoft.EntityFrameworkCore;

namespace Library.Data.Entities;

// Product will act as a DB model - or entity. This class will be a 1:1 representation
// of the table + rows in the database.
public class Product 
{
    // do not forget getters and setters
    public int Id { get; set; }
    public string Sku { get; set; }

    public string Name { get; set; }

    // Using a data annotation to enforce a constraint on my column
    // In this case, 10 total digits, 2 after the decimal place.
    [Precision(10, 2)]
    public decimal Price { get; set; }
    
    // Below is an example of using a collection to denote a relationship
    // A product has an inventory item, an inventory item is associated with one product
    // 1 : 1 relationship for now
    public InventoryItem? Inventory { get; set; } 
}