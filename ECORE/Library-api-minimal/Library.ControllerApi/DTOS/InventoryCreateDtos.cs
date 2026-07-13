

using System.ComponentModel.DataAnnotations;

namespace Library.ControllerApi.DTOs;

// This DTO is for taking in info to then create a new row in my DB
// I can use Data Annotations to enforce constraints on the information
// If the front end/user violates the rules I setup, ASP.NET bounces back a 400 automatically (Bad Request)
public record InventoryCreateDto(
    [Required, MaxLength(20)] string Sku,
    [Required, MaxLength(200)] string Name,
    [Required, Range(0.01, 100000)] decimal Price,
    [Required, Range(0, int.MaxValue)] int CurrentStock
);