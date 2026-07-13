using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BloomRush.Data.Enums;


namespace BloomRush.Data.Entities;


[Table("FulfillmentEvents")]
// FulfillmentEvent is the audit trail for an order.
// FulfillmentService creates one when it fulfills or backorders an order.
// It records what happened; it does not change inventory by itself.
public class FulfillmentEvent
{
    public int Id { get; set; }

    // N:1 relation to Orders.
    // Many FulfillmentEvents can belong to one Order.
    public int OrderId { get; set; }

    // Navigation to the parent Order.
    public Order Order { get; set; } = default!;

    // Type is the short category, for example Fulfilled or Backordered.
    public FulfillmentEventType Type { get; set; }

    [Required, MaxLength(500)]
    // Message is the human-readable explanation.
    public string Message { get; set; } = default!;

    // When the event happened.
    public DateTime TimestampUtc { get; set; }

}
