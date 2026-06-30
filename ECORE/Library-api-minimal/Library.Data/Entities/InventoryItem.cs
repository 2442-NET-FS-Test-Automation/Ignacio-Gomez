namespace Library.Data.Entities;

public class InventoryItem
{
    public int Id{get; set;}
    public int ProductId{get; set;}
    public Product Product {get; set;} = default!; // we can have EF give a default value
    public int CurrentStock {get; set;}

}