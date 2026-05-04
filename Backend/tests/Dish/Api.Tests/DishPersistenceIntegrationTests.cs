using FluentAssertions;
using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Application;
using FoodSocial.Dish.Infrastructure.Persistence;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FoodSocial.Dish.Api.Tests;

public sealed class DishPersistenceIntegrationTests
{
    [Fact]
    public async Task Persistence_RealSqliteStore_CreatesSearchesAndDeprecatesDish()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new DishCatalogService(new DishStore(dbContext));

        var created = await service.CreateDishAsync(
            new CreateDishRequest("Cơm Tấm", "Broken rice", "Rice", "Vietnamese", ["com tam"], ["pork", "rice"], []),
            CancellationToken.None);

        created.Status.Should().Be(DishOperationStatus.Created);

        var search = await service.SearchDishesAsync(new DishSearchRequest("com", "Rice", "Vietnamese", IncludeDeprecated: false, Limit: 20), CancellationToken.None);
        search.Items.Should().ContainSingle().Which.Id.Should().Be(created.Dish!.Id);

        var deprecated = await service.DeprecateDishAsync(created.Dish.Id, CancellationToken.None);
        deprecated.Status.Should().Be(DishOperationStatus.Deprecated);

        var afterDeprecation = await service.SearchDishesAsync(new DishSearchRequest("com", null, null, IncludeDeprecated: false, Limit: 20), CancellationToken.None);
        afterDeprecation.Items.Should().BeEmpty();

        var retrieved = await service.GetDishAsync(created.Dish.Id, CancellationToken.None);
        retrieved!.Status.Should().Be("Deprecated");
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_DuplicateNormalizedNameKeepsSingleRow()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new DishCatalogService(new DishStore(dbContext));

        await service.CreateDishAsync(new CreateDishRequest("Phở Bò", null, null, "Vietnamese", [], [], []), CancellationToken.None);
        var duplicate = await service.CreateDishAsync(new CreateDishRequest("pho bo", null, null, "Vietnamese", [], [], []), CancellationToken.None);

        duplicate.Status.Should().Be(DishOperationStatus.AlreadyExists);
        (await dbContext.Dishes.LongCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_AliasSearchHonorsCategoryAndCuisineFilters()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new DishCatalogService(new DishStore(dbContext));

        var vietnameseDish = await service.CreateDishAsync(
            new CreateDishRequest("Phở Bò", null, "Noodle Soup", "Vietnamese", ["beef noodle soup"], [], []),
            CancellationToken.None);
        await service.CreateDishAsync(
            new CreateDishRequest("Ramen", null, "Noodle Soup", "Japanese", ["beef noodle soup"], [], []),
            CancellationToken.None);
        await service.CreateDishAsync(
            new CreateDishRequest("Beef Stew", null, "Stew", "Vietnamese", ["beef noodle soup"], [], []),
            CancellationToken.None);

        var search = await service.SearchDishesAsync(
            new DishSearchRequest("beef noodle", "Noodle Soup", "Vietnamese", IncludeDeprecated: false, Limit: 10),
            CancellationToken.None);

        search.Items.Should().ContainSingle().Which.Id.Should().Be(vietnameseDish.Dish!.Id);
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_AliasSearchUsesSingleBoundedDatabaseQuery()
    {
        await using var connection = await OpenDatabaseAsync();
        var queryCounter = new SelectCommandCounterInterceptor();
        await using var dbContext = CreateDbContext(connection, queryCounter);
        var service = new DishCatalogService(new DishStore(dbContext));

        await service.CreateDishAsync(
            new CreateDishRequest("Cơm Tấm", null, "Rice", "Vietnamese", ["broken rice"], [], []),
            CancellationToken.None);
        await service.CreateDishAsync(
            new CreateDishRequest("Bánh Mì", null, "Sandwich", "Vietnamese", ["vietnamese sandwich"], [], []),
            CancellationToken.None);

        queryCounter.Reset();

        var search = await service.SearchDishesAsync(
            new DishSearchRequest("broken rice", "Rice", "Vietnamese", IncludeDeprecated: false, Limit: 1),
            CancellationToken.None);

        search.Items.Should().ContainSingle().Which.Name.Should().Be("Cơm Tấm");
        queryCounter.SelectCommandCount.Should().Be(1, "alias search should be folded into one bounded SQL query instead of loading a fallback candidate set in memory");
    }

    private static DishDbContext CreateDbContext(SqliteConnection connection, IInterceptor? interceptor = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DishDbContext>()
            .UseSqlite(connection);

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new DishDbContext(optionsBuilder.Options);
    }

    private sealed class SelectCommandCounterInterceptor : DbCommandInterceptor
    {
        internal int SelectCommandCount { get; private set; }

        internal void Reset() => SelectCommandCount = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CountSelectCommand(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountSelectCommand(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CountSelectCommand(DbCommand command)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                SelectCommandCount++;
            }
        }
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection($"Data Source={Path.Combine(Path.GetTempPath(), $"dish-tests-{Guid.NewGuid():N}.db")}");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ChangeTracker.Clear();

        return connection;
    }
}
