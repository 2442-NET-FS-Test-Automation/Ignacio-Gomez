namespace Library.ControllerApi.Services;

public interface ISupplierClient
{
    Task<decimal?> GetListPriceAsync(String sku);
}