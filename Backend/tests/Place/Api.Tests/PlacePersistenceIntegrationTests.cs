using FluentAssertions;
using FoodSocial.Contracts.Places;
using FoodSocial.Place.Application;
using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodSocial.Place.Api.Tests;

public sealed class PlacePersistenceIntegrationTests
{
    [Fact]
    public async Task Persistence_RealSqliteStore_CreatesSearchesAndUpdatesPlaceStatus()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new PlaceCatalogService(new PlaceStore(dbContext));

        var created = await service.CreatePlaceAsync(
            new CreatePlaceRequest("Cơm Tấm Ba Ghiền", "Broken rice", "84 Đặng Văn Ngữ", "Phú Nhuận", "Ho Chi Minh City", "Vietnam", null, null, ["Vietnamese"], "$$"),
            CancellationToken.None);

        created.Status.Should().Be(PlaceOperationStatus.Created);

        var search = await service.SearchPlacesAsync(new PlaceSearchRequest("com tam", "Ho Chi Minh City", "Phú Nhuận", "Vietnamese", "$$", IncludeInactive: false, Limit: 20), CancellationToken.None);
        search.Items.Should().ContainSingle().Which.Id.Should().Be(created.Place!.Id);

        var closed = await service.UpdatePlaceStatusAsync(created.Place.Id, "Closed", CancellationToken.None);
        closed.Status.Should().Be(PlaceOperationStatus.StatusUpdated);

        var afterClosure = await service.SearchPlacesAsync(new PlaceSearchRequest("com tam", null, null, null, null, IncludeInactive: false, Limit: 20), CancellationToken.None);
        afterClosure.Items.Should().BeEmpty();

        var retrieved = await service.GetPlaceAsync(created.Place.Id, CancellationToken.None);
        retrieved!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_DuplicateNormalizedNameCityAreaKeepsSingleRow()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new PlaceCatalogService(new PlaceStore(dbContext));

        await service.CreatePlaceAsync(new CreatePlaceRequest("Phở Bò", null, "1 Street", "Ba Đình", "Hà Nội", "Vietnam", null, null, ["Vietnamese"], "$"), CancellationToken.None);
        var duplicate = await service.CreatePlaceAsync(new CreatePlaceRequest("pho bo", null, "2 Street", "ba dinh", "ha noi", "Vietnam", null, null, ["Vietnamese"], "$"), CancellationToken.None);

        duplicate.Status.Should().Be(PlaceOperationStatus.AlreadyExists);
        (await dbContext.Places.LongCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_CuisineFilterIsAppliedBeforeResultLimit()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var service = new PlaceCatalogService(new PlaceStore(dbContext));

        for (var index = 0; index < 4; index++)
        {
            await service.CreatePlaceAsync(
                new CreatePlaceRequest($"A Cafe {index}", null, $"{index} Coffee Street", "District 1", "Ho Chi Minh City", "Vietnam", null, null, ["coffee"], "$"),
                CancellationToken.None);
        }

        var vietnamesePlace = await service.CreatePlaceAsync(
            new CreatePlaceRequest("Z Bún Chả", null, "24 Lê Văn Hưu", "Hai Bà Trưng", "Hanoi", "Vietnam", null, null, ["Vietnamese"], "$$"),
            CancellationToken.None);

        var storedSearchText = await dbContext.Places
            .AsNoTracking()
            .Where(place => place.Id == vietnamesePlace.Place!.Id)
            .Select(place => place.CuisineTypesSearchText)
            .SingleAsync();
        storedSearchText.Should().Be("|vietnamese|");

        var search = await service.SearchPlacesAsync(
            new PlaceSearchRequest(null, null, null, "Vietnamese", null, IncludeInactive: false, Limit: 1),
            CancellationToken.None);

        search.Items.Should().ContainSingle().Which.Id.Should().Be(vietnamesePlace.Place!.Id);
    }

    private static PlaceDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PlaceDbContext>()
            .UseSqlite(connection)
            .Options;

        return new PlaceDbContext(options);
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection($"Data Source={Path.Combine(Path.GetTempPath(), $"place-tests-{Guid.NewGuid():N}.db")}");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ChangeTracker.Clear();

        return connection;
    }
}
