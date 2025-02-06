using etymo.ApiService.Postgres;
using Npgsql;
using Dapper;
using Shared.Models;
using etymo.ApiService.Postgres.Handlers;
using etymo.Tests.Helpers;

namespace etymo.Tests;

public class PostgresServiceTests : IAsyncLifetime
{
    private readonly NpgsqlConnection _connection;
    private readonly PostgresService _postgresService;

    public PostgresServiceTests()
    {
        var connectionString = "Host=localhost;Port=5433;Database=etymo_test;Username=postgres;Password=postgres";
        _connection = new NpgsqlConnection(connectionString);

        // Register custom type handler for Dapper sql queries.
        SqlMapper.AddTypeHandler(new DictionaryTypeHandler());

        _postgresService = new PostgresService(_connection);
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
    public async Task CreateWordListOverview_ReturnsCreated()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Act
        await _postgresService.InsertWordListAsync(wordList);
        var rowsAffected = await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Assert
        Assert.Equal(1, rowsAffected);
    }

    [Fact]
    public async Task GetWordListOverviewAsync_ReturnsWordListOverview_WhenFound()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        // Act
        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);
        var result = await _postgresService.SelectWordListOverviewsByUserIdAsync(wordListOverview.CreatedByUserGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(wordListOverview.Guid, result.First().Guid);
        Assert.Equal("Test Title", result.First().Title);
        Assert.Equal("Test Description", result.First().Description);
    }

    [Fact]
    public async Task DeleteWordListOverview_ReturnsTrue()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList();
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid);

        await _postgresService.InsertWordListAsync(wordList);
        await _postgresService.InsertWordListOverviewAsync(wordListOverview);

        // Act
        var response = await _postgresService.DeleteWordListOverviewByGuidAsync(wordListOverview.Guid);

        // Assert
        Assert.True(response);
    }
}