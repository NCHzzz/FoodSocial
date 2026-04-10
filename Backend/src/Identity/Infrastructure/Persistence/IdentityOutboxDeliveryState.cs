namespace FoodSocial.Identity.Infrastructure.Persistence;

/// <summary>
/// Represents the durable delivery lifecycle state for an identity outbox message.
/// </summary>
public enum IdentityOutboxDeliveryState
{
    Pending = 0,
    FailedRetryable = 1,
    Delivered = 2,
    DeadLetter = 3
}
