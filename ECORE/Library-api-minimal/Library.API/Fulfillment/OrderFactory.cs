using Library.Data.Entities;

namespace Library.Api.Fulfillment;

public class OrderFactory
{
    private readonly IFulfillmentService _fs;

    public OrderFactory(IFulfillmentService fulfillment)
    {
        _fs = fulfillment;
    }   

    public Order CreateOrder(string kind, int customerId, IEnumerable<(string sku, int qty)> lines)
    {
        switch (kind)
        {
            case "normal":
                return BuildOrder(Priority.Normal, customerId, lines);
            case "expedited":
                return BuildOrder(Priority.Expedited, customerId, lines);
            default:
                throw new ArgumentException($"Unknown order kind: {kind}");
        }
    }

    private Order BuildOrder(Priority priority, int customerId, IEnumerable<(string sku, int qty)> lines)
    {
        return new Order
        {
            CustomerId = customerId,
            Priority = priority,
            Status = Status.Pending,
            Lines = lines.Select(l => new OrderLine
            {
                ProductId = _fs.ResolveProductId(l.sku), // unknown SKU -> UnknownSkuException
                Quantity = l.qty
            }).ToList()
        };
    }

}