using System.Net.Http.Headers;
using System.Security.Claims;

namespace etymo.Tests.Handlers
{
    public class TestAuthHandler : DelegatingHandler
    {
        private readonly ClaimsPrincipal _userClaimsPrincipal;

        public TestAuthHandler(ClaimsPrincipal userClaimsPrincipal)
        {
            _userClaimsPrincipal = userClaimsPrincipal;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the user GUID from the claims principal
            var userGuid = _userClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            request.Headers.Authorization = new AuthenticationHeaderValue("TestAuth", "test-token");
            request.Headers.Add("X-Test-User-Guid", userGuid);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
