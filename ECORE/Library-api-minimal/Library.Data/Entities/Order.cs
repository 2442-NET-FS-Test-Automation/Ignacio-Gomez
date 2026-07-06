using Library.Data.Entities;

namespace Library.Date.Entities;

public class Order
{
     public int Id {get; set; }
     public int CustomerId {get; set;}
     public Customer Customer {get; set; } = default!;
     public Priority Priority {get; set; }
     public Status Status {get; set; }
     public DateTime CreatedUtc {get; set;} = DateTime.UtcNow;
     public DateTime? CompletedUtc {get; set;}
     public List<OrderLines> Lines {get; set;} = new();
}