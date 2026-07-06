using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BloomRush.Data.Enums;


namespace BloomRush.Data.Entities;

[Table("OrderLines")]
public class OrderLine
{
    public int Id {get; set; }

    // N:1 relation to Orders.
    // Many OrderLines can belong to one Order.
    public int OrderId{get; set; }

    // Navigation to the parent Order.
    public Order Order{get; set;} = default!;

    // N:1 relation to Products.
    // Many OrderLines can point to one Product.
    public int ProductId{get; set;}

    // Navigation to the referenced Product.
    public Product Product{get; set;} = default!;

    [Required]
    public int Quantity {get; set;}
}
