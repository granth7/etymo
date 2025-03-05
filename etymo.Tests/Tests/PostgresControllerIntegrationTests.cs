using System.Net;
using System.Net.Http.Json;
using etymo.Tests.Base;
using etymo.Tests.Helpers;

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
    public async Task UpsertWordList_AuthorizedCreator_ReturnsOk()
    {
        // Arrange
        var wordList = TestDataHelper.CreateWordList(creatorGuid: UserGuid);

        // Act
        var response = await HttpClient.PutAsJsonAsync("postgres/word-list", wordList);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}