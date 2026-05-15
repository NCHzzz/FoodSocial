using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="UserProfileDbContext"/> instances for design-time tooling.
/// </summary>
public sealed class UserProfileDesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserProfileDbContext>
{
    /// <inheritdoc />
    public UserProfileDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__userprofiledb")
            ?? throw new InvalidOperationException("Set ConnectionStrings__userprofiledb before creating the design-time UserProfile DbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<UserProfileDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new UserProfileDbContext(optionsBuilder.Options);
    }
}
