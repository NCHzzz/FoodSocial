using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Identity.Infrastructure.Persistence;

public sealed class IdentityDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__identitydb")
            ?? "Host=localhost;Port=5432;Database=identitydb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__IdentityMigrationsHistory"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
