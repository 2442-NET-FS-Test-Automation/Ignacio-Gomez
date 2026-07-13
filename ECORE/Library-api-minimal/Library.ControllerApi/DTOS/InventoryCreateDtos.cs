
using System.ComponentModel.DataAnnotations;

namespace Library.ControllerApi.DTOs;

public record InventoryCreateDto(
    [Required, MaxLength(20)] string Sku,
    [Required, MaxLength(200)]string Name,
    [Required, Range(0.01,100000)]decimal Price,
    [Required, Range(0, int.MaxValue)]int CurrentStock
);