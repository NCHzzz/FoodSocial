using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Place.Infrastructure.Persistence;

/// <summary>
/// Creates Place DbContext instances for EF Core design-time tooling.
/// </summary>
public sealed class PlaceDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlaceDbContext>
{
    /// <inheritdoc />
    public PlaceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__placedb")
            ?? throw new InvalidOperationException("Set ConnectionStrings__placedb before creating the design-time Place DbContext.");

        var options = new DbContextOptionsBuilder<PlaceDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__PlaceMigrationsHistory"))
            .Options;

        return new PlaceDbContext(options);
    }
}
