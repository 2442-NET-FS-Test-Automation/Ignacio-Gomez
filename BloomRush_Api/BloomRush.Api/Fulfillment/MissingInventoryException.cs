namespace BloomRush.Api.Fulfillment;

// Custom exception:
// This carries business data with the error, not just a text message.
// If an order asks for a product but the inventory row is missing,
// fulfillment can log exactly which order/product caused the problem.
public class MissingInventoryException : Exception
{
    public int OrderId { get; }
    public int ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public MissingInventoryException(
        int orderId,
        int productId,
        int requestedQuantity,
        int availableQuantity)
        : base($"Order {orderId} requested product {productId}, but no inventory row was found.")
    {
        OrderId = orderId;
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}
