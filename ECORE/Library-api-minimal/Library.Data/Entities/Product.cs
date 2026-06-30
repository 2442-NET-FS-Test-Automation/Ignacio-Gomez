using Microsoft.EntityFrameworkCore;
namespace Library.Data.Entities;

public class Product
{
    public int Id {get; set; }
    public string Sku {get; set; }
    public string Name {get; set; }
    
    //In this case, 10 total digits, 2 after the decimal place
    [Precision(10, 2)]
    public decimal Price {get; set; }
    
    //Below is an example of using a collection to denote a relationship
    //A product has a inventory item, an inventory item is associated with one product
    public InventoryItem? Inventory {get; set; }

}