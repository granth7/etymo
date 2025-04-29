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

            // Check if the user is an admin (this check is independent of resource ownership)
            bool isAdmin = context.HttpContext.User.IsInRole("admin");

            // First try to find an ICreatorOwned object
            var creatorOwnedResource = context.ActionArguments.Values
                .FirstOrDefault(v => v is ICreatorOwned);

            // Logic to handle both cases:
            // ICreatorOwned object found OR
            // userId passed as a separate parameter (not ICreatorOwned object).
            bool isCreator;
            string? creatorGuidForLogging = "Unknown";

            if (creatorOwnedResource is ICreatorOwned resource)
            {
                // We found an ICreatorOwned resource
                isCreator = resource.CreatorGuid.ToString().Equals(userGuid,
                    StringComparison.OrdinalIgnoreCase);
                creatorGuidForLogging = resource.CreatorGuid.ToString();
            }
            else
            {
                // Try to find a userId parameter
                var userIdParameter = context.ActionArguments
                    .FirstOrDefault(arg => arg.Key.Equals("userId", StringComparison.OrdinalIgnoreCase) ||
                                          arg.Key.Equals("creatorGuid", StringComparison.OrdinalIgnoreCase) ||
                                          arg.Key.Equals("creatorId", StringComparison.OrdinalIgnoreCase));

                if (userIdParameter.Value == null)
                {
                    // If we can't find either, log and forbid (unless they're an admin)
                    if (isAdmin)
                    {
                        _logger.LogInformation("Admin access granted for user {UserGuid}", userGuid);
                        return; // Allow admins to proceed
                    }

                    _logger.LogWarning("Neither ICreatorOwned resource nor userId parameter found in arguments");
                    context.Result = new ForbidResult();
                    return;
                }

                // Convert the parameter value to string for comparison
                string? parameterUserId = userIdParameter.Value.ToString();
                creatorGuidForLogging = parameterUserId;

                // Handle both GUID and string formats
                if (Guid.TryParse(parameterUserId, out Guid parameterGuid))
                {
                    isCreator = parameterGuid.ToString().Equals(userGuid, StringComparison.OrdinalIgnoreCase);
                }
                else if (parameterUserId != null)
                {
                    isCreator = parameterUserId.Equals(userGuid, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    isCreator = false;
                }
            }

            // If not the creator and not an admin, return forbidden
            if (!isCreator && !isAdmin)
            {
                _logger.LogWarning(
                    "User {UserGuid} is not creator ({CreatorGuid}) or admin",
                    userGuid,
                    creatorGuidForLogging);
                context.Result = new ForbidResult();
                return;
            }

            _logger.LogInformation(
                "Authorization successful for user {UserGuid} on resource {ResourceGuid}",
                userGuid,
                creatorGuidForLogging);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed
        }
    }
}

