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
        var options = new DbContextOptionsBuilder<PlaceDbContext>()
            .UseNpgsql("Host=localhost;Database=placedb;Username=postgres;Password=postgres", npgsql =>
                npgsql.MigrationsHistoryTable("__PlaceMigrationsHistory"))
            .Options;

        return new PlaceDbContext(options);
    }
}
