using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Http.Headers;

namespace etymo.Web.Components.Handlers
{
    public class AuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AuthorizationHandler> logger) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        private readonly IDataProtectionProvider _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        private readonly ILogger<AuthorizationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private const string DataProtectionPurpose = "Etymo.Auth.Cookie";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                var token = await GetAccessTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogTrace("Added Bearer token to request");
                }
                else
                {
                    _logger.LogDebug("No access token available for API request to {Url}", request.RequestUri);
                }

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authorize request");
                throw;
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                // Safely get HttpContext
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogDebug("HttpContext is not available");
                    return null;
                }

                // 1. Try to get token directly from authentication properties
                var authToken = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(authToken))
                {
                    _logger.LogDebug("Found access token in authentication properties");
                    return authToken;
                }

                // 2. Fallback to cookie decryption
                if (httpContext.Request.Cookies.TryGetValue(".Etymo.Auth", out var authCookie)
                    && !string.IsNullOrEmpty(authCookie))
                {
                    var protector = _dataProtectionProvider.CreateProtector(DataProtectionPurpose);
                    var ticketDataFormat = new TicketDataFormat(protector);

                    var ticket = ticketDataFormat.Unprotect(authCookie);
                    if (ticket?.Properties != null)
                    {
                        if (ticket.Properties.Items.TryGetValue(".Token.access_token", out var cookieToken)
                            && !string.IsNullOrEmpty(cookieToken))
                        {
                            _logger.LogDebug("Found access token in auth cookie");
                            return cookieToken;
                        }

                        // Alternative token location
                        if (ticket.Properties.GetTokenValue("access_token") is string altToken
                            && !string.IsNullOrEmpty(altToken))
                        {
                            _logger.LogDebug("Found access token in ticket properties");
                            return altToken;
                        }
                    }
                }

                _logger.LogDebug("No access token found in any source");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve access token");
                return null;
            }
        }
    }
}