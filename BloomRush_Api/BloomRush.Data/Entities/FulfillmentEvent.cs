using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BloomRush.Data.Enums;


namespace BloomRush.Data.Entities;


[Table("FulfillmentEvents")]
public class FulfillmentEvent
{
    public int Id { get; set; }

    // N:1 relation to Orders.
    // Many FulfillmentEvents can belong to one Order.
    public int OrderId { get; set; }

    // Navigation to the parent Order.
    public Order Order { get; set; } = default!;
    public FulfillmentEventType Type { get; set; }

    [Required, MaxLength(500)]
    public string Message { get; set; } = default!;

    public DateTime TimestampUtc { get; set; }

}
