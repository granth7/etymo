using Castle.Core.Logging;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

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
