using Aspire.Hosting.Testing;
using etymo.Tests.Base;
using System.Net;
using System.Net.Http;

namespace etymo.Tests.Tests;

[Trait("Category", "Integration")]
public class WebTests : IntegrationTestBase
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.etymo_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Create a handler that ignores certificate validation
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };


        // Act
        var httpClient = app.CreateHttpClient("etymo-webfrontend");

        // Configure a handler to ignore certificate validation during ci tests.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        using var configuredClient = new HttpClient(handler);
        // Get the base address from the original client
        configuredClient.BaseAddress = httpClient.BaseAddress;

        var response = await configuredClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
