using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace etymo.ApiService.Postgres.Handlers
{
    // Used to test authentication when the app is running in the 'Testing' mode
    public class TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.TryGetValue("X-Test-User-Guid", out var userGuidValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("No test user GUID provided"));
            }

            var userGuid = userGuidValues.ToString();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userGuid),
                new(ClaimTypes.Name, "testuser"),
                new("sub", userGuid)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
