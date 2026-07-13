namespace Library.Api.Fulfillment;

public sealed class UnknownSkuException : Exception
{
    public string Sku { get; }
    public UnknownSkuException(string sku) : base($"Unknown SKU: {sku}")
    {
        Sku = sku;
    }
}