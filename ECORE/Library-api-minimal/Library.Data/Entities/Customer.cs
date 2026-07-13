using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library.Data.Entities;

// Adding some data annotations to show them off a bit
[Table("Customers")]
public class Customer
{

    public int Id { get; set; }

    // Data annotations can be stacked or inline in the same brackets.
    // They apply to the property directly below them. No fall through.
    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;
    [Required]
    public string Email { get; set; } = default!;

    //One customer can have many orders
    public List<Order> Orders { get; set; } = new(); // Good idea to initialize to empty list
}