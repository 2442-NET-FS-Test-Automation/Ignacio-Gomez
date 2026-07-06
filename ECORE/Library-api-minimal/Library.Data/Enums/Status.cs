namespace Library.Data.Entities;

public enum Status
{
    //In my application if an order is yet to be processed it is pending
    // FuLFilled means the sale completed
    // BackOrder happens when someone places a buy request we dont have stock for
    Pending,
    Fulfilled,
    Backordered
}