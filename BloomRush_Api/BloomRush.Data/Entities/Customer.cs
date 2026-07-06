using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloomRush.Data.Entities;

[Table("Customers")]
public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    // 1:N relation: one Customer can place many Orders.
    // The other side of the relation is Order.CustomerId + Order.Customer.
    public List<Order> Orders { get; set; } = new();
}
