using Serilog;

namespace BloomRush.Api.Fulfillment;

public interface IFulfillmentConcurrentService
{
    Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManyConcurrentAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct);
}

public class FulfillmentConcurrentService : IFulfillmentConcurrentService
{
    private readonly IFulfillmentService _fulfillmentService;

    public FulfillmentConcurrentService(IFulfillmentService fulfillmentService)
    {
        _fulfillmentService = fulfillmentService;
    }

    public async Task<IReadOnlyList<BurstFulfillmentItemResult>> FulfillManyConcurrentAsync(
        IReadOnlyList<int> orderIds,
        CancellationToken ct)
    {
        var tasks = orderIds.Select(async orderId =>
        {
            var result = await _fulfillmentService.FulfillOneAsync(orderId, ct);

            Log.Information(
                "Concurrent fulfillment completed for order {OrderId} with result {Result}",
                orderId,
                result);

            return new BurstFulfillmentItemResult(orderId, result);
        });

        return await Task.WhenAll(tasks);
    }
}
