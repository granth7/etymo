using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace etymo.ApiService.Postgres.Filters
{
    public class ValidateCustomAntiForgeryTokenFilter(IAntiforgery antiforgery) : IAsyncActionFilter
    {
        private readonly IAntiforgery _antiforgery = antiforgery;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Check if the header exists
            if (!context.HttpContext.Request.Headers.TryGetValue("X-CSRF-TOKEN", out _))
            {
                context.Result = new BadRequestObjectResult("Anti-forgery token is missing");
                return;
            }

            // You can add additional validation logic here if needed
            // For example, verify the token against a shared secret

            await next();
        }
    }
}
