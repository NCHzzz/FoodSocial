using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Identity.Infrastructure.Persistence;

public sealed class IdentityDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__identitydb")
            ?? throw new InvalidOperationException("Set ConnectionStrings__identitydb before creating the design-time Identity DbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__IdentityMigrationsHistory"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
