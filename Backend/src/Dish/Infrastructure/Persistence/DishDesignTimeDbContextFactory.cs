using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Dish.Infrastructure.Persistence;

/// <summary>
/// Creates the Dish EF Core context for design-time migration commands.
/// </summary>
public sealed class DishDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DishDbContext>
{
    /// <inheritdoc />
    public DishDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__dishdb")
            ?? "Host=localhost;Database=dishdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<DishDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__DishMigrationsHistory"))
            .Options;

        return new DishDbContext(options);
    }
}
