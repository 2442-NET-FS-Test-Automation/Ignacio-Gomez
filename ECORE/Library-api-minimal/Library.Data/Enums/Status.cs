namespace Library.Data.Entities;

public enum Status
{
    // In my application if an order is yet to be processed it is pending.
    // Fulfilled means the sale completed
    // Backorder happens when someone places a buy request we don't have stock for 
    Pending,
    Fulfilled,
    Backordered
}