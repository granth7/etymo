using System.Net;
using System.Net.Http.Json;
using etymo.Tests.Base;
using etymo.Tests.Helpers;
using k8s.KubeConfigModels;

namespace etymo.Tests.Tests;

[Trait("Category", "Integration")]
public class PostgresControllerIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task UpsertWordList_UnauthorizedCreator_ReturnsForbidden()
    {
        // Arrange
        var differentUserGuid = Guid.NewGuid();
        var wordList = TestDataHelper.CreateWordList(creatorGuid: differentUserGuid);

        // Act
        var response = await HttpClient.PutAsJsonAsync("postgres/word-list", wordList);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpsertWordListOverview_UnauthorizedCreator_ReturnsForbidden()
    {
        // Arrange
        var differentUserGuid = Guid.NewGuid();
        var wordListOverview = TestDataHelper.CreateWordListOverview(creatorGuid: differentUserGuid);

        // Act
        var response = await HttpClient.PutAsJsonAsync("postgres/word-list-overview", wordListOverview);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpsertWordList_AuthorizedCreator_ReturnsOk()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: UserGuid);

        // Act
        var response = await HttpClient.PutAsJsonAsync("postgres/word-list", wordList);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpsertWordListOverview_AuthorizedCreator_ReturnsOk()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: UserGuid);
        var wordListOverview = TestDataHelper.CreateWordListOverview(wordListGuid: wordList.Guid, creatorGuid: UserGuid);
        await HttpClient.PutAsJsonAsync("postgres/word-list", wordList);

        // Act
        var response = await HttpClient.PutAsJsonAsync("postgres/word-list-overview", wordListOverview);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWordListOverview_UnauthorizedCreator_ReturnsForbidden()
    {
        // Arrange
        var differentUserGuid = Guid.NewGuid();
        var wordListOverview = TestDataHelper.CreateWordListOverview();
        string url = $"postgres/word-list-overview?wordListOverviewId={wordListOverview.Guid}&userId={differentUserGuid}";

        // Act
        await HttpClient.PutAsJsonAsync("postgres/word-list-overviews", wordListOverview);
        var response = await HttpClient.DeleteAsync(url);
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}