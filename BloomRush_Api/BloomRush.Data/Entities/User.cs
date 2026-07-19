using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloomRush.Data.Entities;

[Table("Users")]
public class User
{
    public int Id {get; set;}
    
    [MaxLength(64)]
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // we NEVER store the password in plain text
    public string Role { get; set; } = "consumer"; // "consumer" || "admin"
}