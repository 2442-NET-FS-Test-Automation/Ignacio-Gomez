namespace Library.Data.Entities;

public class FulfillmentEvent
{
    public int Id {get; set; }
    public int OrderId {get; set;}
    public string Type {get; set;} = default!;
    public DateTime FulfilledAtUtc {get; set; } = DateTime.UtcNow;
}