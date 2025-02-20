using etymo.Tests.Handlers;
using etymo.Tests.Helpers;
using Aspire.Hosting;
using System.Security.Claims;

namespace etymo.Tests.Fixtures
{
    public class IntegrationTestFixture : IAsyncDisposable
    {
        private DistributedApplication? _app;
        public HttpClient HttpClient { get; private set; }
        public Guid UserGuid { get; }
        public ClaimsPrincipal UserClaimsPrincipal { get; }

        public IntegrationTestFixture()
        {
            UserGuid = Guid.NewGuid();
            UserClaimsPrincipal = TestDataHelper.CreateUserClaimsPrincipalFromGuid(UserGuid);
            HttpClient = new HttpClient();

            // Set the environment to Testing.
            // Currently these values are being overridden by launchsettings.json, so set them there until we can find a workaround.
            //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            //Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        }

        public async Task InitializeAsync()
        {
            await TestDataHelper.RunDockerComposeDown();

            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.etymo_AppHost>();
            _app = await appHost.BuildAsync();
            await _app.StartAsync();

            var apiClient = _app.CreateHttpClient("etymo-apiservice");
            var baseUri = apiClient.BaseAddress;

            var testAuthHandler = new TestAuthHandler(UserClaimsPrincipal);
            HttpClient = new HttpClient(testAuthHandler)
            {
                BaseAddress = baseUri
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (_app != null)
            {
                await _app.DisposeAsync();
            }
            HttpClient.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
