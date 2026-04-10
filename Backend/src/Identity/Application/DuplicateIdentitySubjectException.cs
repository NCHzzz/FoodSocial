namespace FoodSocial.Identity.Application;

/// <summary>
/// Represents a duplicate subject write detected by the identity persistence boundary.
/// </summary>
public sealed class DuplicateIdentitySubjectException(string subject, Exception innerException)
    : Exception($"A local identity already exists for subject '{subject}'.", innerException)
{
    /// <summary>
    /// Gets the trusted subject that collided with an existing identity record.
    /// </summary>
    public string Subject { get; } = string.IsNullOrWhiteSpace(subject)
        ? throw new ArgumentException("A subject is required.", nameof(subject))
        : subject;
}
