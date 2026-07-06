namespace Library.Data.Entities;

public class OrderLines
{
     public int Id {get; set; }
     public int OrderId {get; set;}
     public int ProductId {get; set;}
     public int Quantity{get; set;}
}