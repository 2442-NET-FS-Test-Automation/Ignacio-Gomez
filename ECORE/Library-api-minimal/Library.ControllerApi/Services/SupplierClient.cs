namespace Library.ControllerApi.Services;

public class SupplierClient : ISupplierClient
{
    // This class will call an outside API using HTTP Client
    private readonly HttpClient _http;

    public SupplierClient(HttpClient http)
    {
        _http = http;
    }

    //Record to represent the response "Shape" of the outside API
    private record SupplierProduct(int Id, string Title, decimal Price);

    //This method sends GET to a traning API called dummyJson
    // Get https://dummyjson.com/product/{id}
    public async Task<decimal?> GetListPriceAsync(string sku)
    {
        //Lets pretend we are grabbing the wholesale price of our product from the supplier
        var digits = new string(sku.Where(char.IsDigit).ToArray()); //BK-001 -> 001
        if (!int.TryParse(digits, out var id)) return null;

        var product = await _http.GetFromJsonAsync<SupplierProduct>($"products/{id}");
        
        return product?.Price;
    }
}