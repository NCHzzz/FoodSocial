namespace FoodSocial.Identity.Domain;

public sealed class IdentityUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Subject { get; private set; }

    public string? PreferredUsername { get; private set; }

    public string? Email { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private IdentityUser()
    {
        Subject = string.Empty;
    }

    public IdentityUser(string subject, string? preferredUsername, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Subject = subject;
        PreferredUsername = preferredUsername;
        Email = email;
    }

    public void UpdateProfile(string? preferredUsername, string? email)
    {
        PreferredUsername = preferredUsername;
        Email = email;
    }
}
