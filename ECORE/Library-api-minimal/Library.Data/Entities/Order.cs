namespace Library.Data.Entities;

public class Order
{
    public int Id {get; set;}

    public int CustomerId { get; set;} // FK -> Customer
    public Customer Customer { get; set; } = default!;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // stamp it upon object creation
    public DateTime? CompletedUtc { get; set; }

    // Every Order has one or more OrderLines
    // Orderlines are the actual product and quantity of a something on the order. 
    public List<OrderLine> Lines { get; set; } = new();

    
}