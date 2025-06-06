using Dapper;
using etymo.ApiService.Postgres.Handlers;
using etymo.ApiService.Postgres;
using etymo.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Shared.Models;

namespace etymo.Tests.Tests;

[Trait("Category", "Unit")]
public class PostgresControllerTests : IAsyncLifetime
{
    private readonly NpgsqlConnection _connection;
    private readonly PostgresService _postgresService;
    private readonly PostgresController _postgresController;
    private static Guid _userGuid;

    public PostgresControllerTests()
    {
        var connectionString = "Host=localhost;Port=5433;Database=etymo_test;Username=postgres;Password=postgres";
        _connection = new NpgsqlConnection(connectionString);

        _userGuid = new Guid();

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
    public async Task GetPublicWordListOverviewsAsync_ReturnsWordListOverviews()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(creatorGuid: _userGuid, wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.GetWordListOverviewsAsync(wordListOverview.CreatorGuid);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task GetPrivateWordListOverviewsAsync_ReturnsWordListOverviews()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(creatorGuid: _userGuid, wordListGuid: wordList.Guid, isPublic: false);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.GetPrivateWordListOverviewsAsync(wordListOverview.CreatorGuid);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(actionResult.Value);
    }

    [Fact]
    public async Task UpsertWordListAsync_ReturnsOk_WhenUpsertIsSuccessful()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);

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
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid, creatorGuid: _userGuid);

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
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresController.DeleteWordListOverviewAsync(wordListOverview.Guid, wordListOverview.CreatorGuid);

        // Assert
        var actionResult = Assert.IsType<NoContentResult>(response);
        Assert.True(actionResult.StatusCode == 204);
    }

    [Fact]
    public async Task DeleteWordListOverviewAsync_ReturnsNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: _userGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Seed test data
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act - Delete the item, then try to delete it again
        await _postgresController.DeleteWordListOverviewAsync(wordListOverview.Guid, wordListOverview.CreatorGuid);
        var response = await _postgresController.DeleteWordListOverviewAsync(wordListOverview.Guid, wordListOverview.CreatorGuid);

        // Assert
        var actionResult = Assert.IsType<NotFoundResult>(response);
        Assert.True(actionResult.StatusCode == 404);
    }
}