namespace FoodSocial.Messaging;

public enum IntegrationOutboxDeliveryState
{
    Pending = 0,
    Delivered = 1,
    FailedRetryable = 2,
    DeadLetter = 3
}
