using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace etymo.Web.Components.Handlers;

public class AuthorizationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Only add auth if HttpContext is available and user is authenticated
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        // Always proceed, even if no HttpContext
        return await base.SendAsync(request, cancellationToken);
    }
}