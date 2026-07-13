namespace Library.ControllerApi.DTOs;

// I won't need to add methods or a constructor to this - it's only job
// is passing info to <-> from the frontend (swagger, React website, etc)
// This solves the JSON loop - as well as saves the front end from having to pass
// massive objects for no reason.
public record InventoryDto(string Sku, string Name, int CurrentStock);