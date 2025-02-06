using etymo.ApiService.Postgres;
using Npgsql;
using Dapper;
using Shared.Models;
using etymo.ApiService.Postgres.Handlers;
using etymo.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace etymo.Tests;

public class PostgresControllerTests : IAsyncLifetime
{
    private readonly NpgsqlConnection _connection;
    private readonly PostgresService _postgresService;
    private readonly PostgresController _postgresController;


    public PostgresControllerTests()
    {
        var connectionString = "Host=localhost;Port=5433;Database=etymo_test;Username=postgres;Password=postgres";
        _connection = new NpgsqlConnection(connectionString);

        // Register custom type handler for Dapper sql queries.
        SqlMapper.AddTypeHandler(new DictionaryTypeHandler());

        _postgresService = new PostgresService(_connection);
        _postgresController = new PostgresController(_postgresService);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetWordListOverviewsByUserIdAsync_ReturnsWordListOverviews()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.GetWordListOverviewsByUserIdAsync(wordListOverview.CreatedByUserGuid);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task UpsertWordListAsync_ReturnsOk_WhenUpsertIsSuccessful()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();

        // Act - No prior word list, insert it.
        var response = await _postgresController.UpsertWordListAsync(wordList);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(response);
        Assert.True(actionResult.StatusCode == 200);
    }

    [Fact]
    public async Task UpsertWordListOverviewAsync_ReturnsOk_WhenUpsertIsSuccessful()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.UpsertWordListOverviewAsync(wordListOverview);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(response);
        Assert.True(actionResult.StatusCode == 200);
    }

    [Fact]
    public async Task DeleteWordListOverviewAsync_ReturnsNoContent_WhenDeleteIsSuccessful()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.DeleteWordListOverviewAsync(wordListOverview.Guid);

        // Assert
        var actionResult = Assert.IsType<NoContentResult>(response);
        Assert.True(actionResult.StatusCode == 204);
    }

    [Fact]
    public async Task DeleteWordListOverviewAsync_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var response = await _postgresController.DeleteWordListOverviewAsync(guid);

        // Assert
        var actionResult = Assert.IsType<NotFoundResult>(response);
        Assert.True(actionResult.StatusCode == 404);
    }
}