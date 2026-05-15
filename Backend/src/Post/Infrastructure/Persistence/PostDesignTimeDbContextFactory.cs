using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Post.Infrastructure.Persistence;

public sealed class PostDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostDbContext>
{
    public PostDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__postdb")
            ?? throw new InvalidOperationException("Set ConnectionStrings__postdb before creating the design-time Post DbContext.");

        var options = new DbContextOptionsBuilder<PostDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__PostMigrationsHistory"))
            .Options;
        return new PostDbContext(options);
    }
}
