using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Interfaces;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Security.Claims;

namespace etymo.ApiService.Postgres.Filters
{
    public class CreatorOrAdminActionFilter(ILogger<CreatorOrAdminActionFilter> logger) : IActionFilter
    {
        private readonly ILogger<CreatorOrAdminActionFilter> _logger = logger;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("User is not authenticated");
                context.Result = new UnauthorizedResult();
                return;
            }

            var userGuid = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.HttpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userGuid))
            {
                _logger.LogWarning("No sub claim found in user claims");
                context.Result = new ForbidResult();
                return; 
            }

            // Now we can access the action arguments
            var creatorOwnedResource = context.ActionArguments.Values
                .FirstOrDefault(v => v is ICreatorOwned);

            if (creatorOwnedResource is not ICreatorOwned resource)
            {
                _logger.LogWarning("No ICreatorOwned resource found in arguments");
                context.Result = new ForbidResult();
                return;
            }

            bool isCreator = resource.CreatorGuid.ToString().Equals(userGuid,
                StringComparison.OrdinalIgnoreCase);

            bool isAdmin = context.HttpContext.User.Claims
                .Any(c => c.Type == "role" && c.Value.Equals("admin",
                    StringComparison.OrdinalIgnoreCase));

            if (!isCreator && !isAdmin)
            {
                _logger.LogWarning(
                    "User {UserGuid} is not creator ({CreatorGuid}) or admin",
                    userGuid,
                    resource.CreatorGuid);
                context.Result = new ForbidResult();
                return;
            }

            _logger.LogInformation(
                "Authorization successful for user {UserGuid} on resource {ResourceGuid}",
                userGuid,
                resource.CreatorGuid);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed
        }
    }
}

