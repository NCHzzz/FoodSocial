using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodSocial.Post.Infrastructure.Persistence;

public sealed class PostDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostDbContext>
{
    public PostDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostDbContext>()
            .UseNpgsql("Host=localhost;Database=postdb;Username=postgres;Password=postgres", npgsql => npgsql.MigrationsHistoryTable("__PostMigrationsHistory"))
            .Options;
        return new PostDbContext(options);
    }
}
