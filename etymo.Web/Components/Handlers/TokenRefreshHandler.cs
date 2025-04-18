using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

namespace etymo.Web.Components.Handlers
{
    public static class TokenRefreshHandler
    {
        public static string? Environment { get; set; }

        // Enhanced timing configuration
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15); // Match Keycloak setting
        private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(2); // Start refreshing 2 mins before expiry
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30); // Persistent session duration
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(5); // Match frontend heartbeat

        public static async Task RefreshTokenIfNeeded(CookieValidatePrincipalContext context)
        {
            string keycloakBaseUrl = Environment != "Development"
                ? "https://sso.hender.tech/realms/Etymo"
                : "http://localhost:8080/realms/Etymo";

            // Get current tokens
            var refreshToken = context.Properties.GetTokenValue("refresh_token");
            var accessToken = context.Properties.GetTokenValue("access_token");

            if (string.IsNullOrEmpty(refreshToken))
            {
                context.RejectPrincipal();
                return;
            }

            // Calculate token age and remaining lifetime
            var tokenIssued = context.Properties.IssuedUtc?.UtcDateTime ?? DateTime.UtcNow;
            var tokenExpiry = GetTokenExpiry(accessToken) ?? tokenIssued.Add(AccessTokenLifetime);
            var timeRemaining = tokenExpiry - DateTime.UtcNow;

            // Force refresh if:
            // 1. Token is about to expire (within threshold)
            // 2. We're past 50% of token lifetime (whichever comes first)
            // 3. Last refresh was before our heartbeat interval
            var lastRefresh = context.Properties.GetString("last_refresh");
            var lastRefreshTime = lastRefresh != null ? DateTime.Parse(lastRefresh) : DateTime.MinValue;

            if (timeRemaining <= RefreshThreshold ||
                DateTime.UtcNow - tokenIssued > AccessTokenLifetime / 2 ||
                DateTime.UtcNow - lastRefreshTime > HeartbeatInterval)
            {
                await ForceTokenRefresh(context, keycloakBaseUrl, refreshToken);
            }
        }

        private static async Task ForceTokenRefresh(CookieValidatePrincipalContext context, string keycloakBaseUrl, string refreshToken)
        {
            try
            {
                var services = context.HttpContext.RequestServices;
                var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                    .Get("keycloak");

                // Build token endpoint
                var tokenEndpoint = options.Configuration?.TokenEndpoint ??
                    $"{options.Authority?.TrimEnd('/') ?? keycloakBaseUrl}/protocol/openid-connect/token";

                using var httpClient = new HttpClient();
                var tokenResponse = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = tokenEndpoint,
                    ClientId = options.ClientId,
                    RefreshToken = refreshToken,
                    ClientCredentialStyle = ClientCredentialStyle.PostBody
                });

                if (!tokenResponse.IsError)
                {
                    // Update tokens
                    context.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                    context.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken);

                    // Force 30-day session lifetime
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(SessionLifetime);
                    context.Properties.Items["last_refresh"] = DateTime.UtcNow.ToString("o");
                    context.ShouldRenew = true;

                    // Debug info
                    context.Properties.Items["refresh_debug"] =
                        $"Last refresh: {DateTime.UtcNow} | Next check: {DateTime.UtcNow.Add(HeartbeatInterval)}";
                }
                else
                {
                    await LogError(tokenResponse);
                    context.RejectPrincipal();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh exception: {ex.Message}");
                context.RejectPrincipal();
            }
        }

        private static DateTime? GetTokenExpiry(string? accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);
                var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                return expClaim != null ?
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value)).UtcDateTime :
                    null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task LogError(TokenResponse response)
        {
            var errorMsg = $"Refresh failed: {response.Error}";
            if (!string.IsNullOrEmpty(response.ErrorDescription))
            {
                errorMsg += $" - {response.ErrorDescription}";
            }
            if (response.HttpResponse != null)
            {
                errorMsg += $"\nStatus: {response.HttpStatusCode}";
                errorMsg += $"\nResponse: {await response.HttpResponse.Content.ReadAsStringAsync()}";
            }
            Console.WriteLine(errorMsg);
        }
    }
}