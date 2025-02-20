using etymo.Tests.Fixtures;
using System.Security.Claims;

namespace etymo.Tests.Base
{
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly IntegrationTestFixture Fixture;
        protected HttpClient HttpClient => Fixture.HttpClient;
        protected Guid UserGuid => Fixture.UserGuid;
        protected ClaimsPrincipal UserClaimsPrincipal => Fixture.UserClaimsPrincipal;

        protected IntegrationTestBase()
        {
            Fixture = new IntegrationTestFixture();
        }

        public async Task InitializeAsync()
        {
            await Fixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await Fixture.DisposeAsync();
        }
    }
}
