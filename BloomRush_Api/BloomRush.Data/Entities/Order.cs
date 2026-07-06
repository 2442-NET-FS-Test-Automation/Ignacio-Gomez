using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BloomRush.Data.Enums;


namespace BloomRush.Data.Entities;

[Table("Orders")]
public class Order
{
    public int Id { get; set; }

    // N:1 relation to Customers.
    // This FK column is what creates the database link to Customers.Id.
    public int CustomerId { get; set; }

    // Navigation back to the parent Customer row.
    public Customer Customer { get; set; } = default!;
    public Priority Priority { get; set; }
    public Status Status {get; set; }
    public DateTime CreatedAtUtc{get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc{get; set; }

    // 1:N relation: one Order can contain many OrderLines.
    // This is how Order <-> Product becomes N:N through the join table OrderLines.
    public List<OrderLine> Lines{get; set; } = new();

    // 1:N relation: one Order can produce many FulfillmentEvents over time.
    public List<FulfillmentEvent> Events {get; set;} = new();
}
