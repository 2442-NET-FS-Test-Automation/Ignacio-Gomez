using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloomRush.Data.Entities;

[Table("Products")]
// Product is the catalog item.
// Seeder creates baseline products, InventoryItem stores stock for them,
// and OrderLine points to them when an order requests a product.
public class Product
{
    public int Id {get; set;}
    
    [Required, MaxLength(100)]
    public string Sku {get; set;} = default!;
    [Required, MaxLength(120)]
    public string Name {get; set;} = default!;
    public decimal Price {get; set;}

    // 1:1 relation to InventoryItem.
    // The FK lives on InventoryItem.ProductId and will be enforced in DbContext.
    public InventoryItem Inventory {get; set;}= default!;

    // 1:N relation: one Product can appear in many OrderLines.
    // Together with Order.Lines, this models the N:N between Orders and Products.
    public List<OrderLine> OrderLines {get; set; } = new();
}
