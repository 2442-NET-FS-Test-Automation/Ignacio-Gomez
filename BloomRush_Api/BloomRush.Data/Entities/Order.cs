using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BloomRush.Data.Enums;


namespace BloomRush.Data.Entities;

[Table("Orders")]
// Order is the parent workflow row.
// Seeder creates sample orders, and FulfillmentService changes Status later.
public class Order
{
    public int Id { get; set; }

    // N:1 relation to Customers.
    // This FK column is what creates the database link to Customers.Id.
    public int CustomerId { get; set; }

    // Navigation back to the parent Customer row.
    public Customer Customer { get; set; } = default!;

    // Priority is available for future scheduling rules.
    // The current seed/burst flow creates Standard orders to keep fulfillment simple.
    public Priority Priority { get; set; }

    // Status starts as Pending, then FulfillmentService changes it to Fulfilled or Backordered.
    public Status Status {get; set; }

    // Created when the order row is made.
    public DateTime CreatedAtUtc{get; set; } = DateTime.UtcNow;

    // Filled in when FulfillmentService finishes processing the order.
    public DateTime? CompletedAtUtc{get; set; }

    // 1:N relation: one Order can contain many OrderLines.
    // This is how Order <-> Product becomes N:N through the join table OrderLines.
    public List<OrderLine> Lines{get; set; } = new();

    // 1:N relation: one Order can produce many FulfillmentEvents over time.
    public List<FulfillmentEvent> Events {get; set;} = new();
}
