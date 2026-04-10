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
        var optionsBuilder = new DbContextOptionsBuilder<UserProfileDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=userprofiledb;Username=postgres;Password=postgres");
        return new UserProfileDbContext(optionsBuilder.Options);
    }
}
