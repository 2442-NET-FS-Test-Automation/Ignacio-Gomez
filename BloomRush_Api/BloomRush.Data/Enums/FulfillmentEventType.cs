namespace BloomRush.Data.Enums;

public enum FulfillmentEventType
{
    OrderReceived,
    Fulfilled,
    Backordered,
    RetryDueToConcurrency,
    Failed
}
