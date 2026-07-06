using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BloomRush.Data.Entities;

[Table("InventoryItems")]
public class InventoryItem
{
    public int Id {get; set; }

    // 1:1 relation to Products.
    // This FK points to Products.Id and is what links stock to a single product.
    public int ProductId{get; set; }

    // Navigation back to the related Product.
    public Product Product{get; set; } = default!;

    [Required]
    public int QuantityOnHand {get; set; }

    // Concurrency token used later to prevent overselling.
    public byte[] RowVersion{get; set;} = default!;
}
