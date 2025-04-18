using Microsoft.AspNetCore.Antiforgery;

namespace etymo.Web.Components.Services
{
    public interface IAntiforgeryService
    {
        Task<string> GetAntiforgeryTokenAsync();
    }

    public class AntiforgeryService(IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor) : IAntiforgeryService
    {
        private readonly IAntiforgery _antiforgery = antiforgery;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public Task<string> GetAntiforgeryTokenAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(string.Empty);
            }

            var tokenSet = _antiforgery.GetAndStoreTokens(httpContext);
            return Task.FromResult(tokenSet.RequestToken ?? string.Empty);
        }
    }
}
