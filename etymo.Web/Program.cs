using etymo.Web;
using etymo.Web.Components;
using etymo.Web.Components.Extensions;
using etymo.Web.Components.Handlers;
using etymo.Web.Components.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor()
                .AddTransient<AuthorizationHandler>();

builder.Services.AddHttpClient<MorphemeApiClient>(client =>
    {
        // Use either HTTPS or HTTP explicitly - not both
        client.BaseAddress = builder.Environment.IsDevelopment()
            ? new Uri("http://etymo-apiservice")  // HTTP for development
            : new Uri("https://etymo-apiservice"); // HTTPS for production
    })
    .AddHttpMessageHandler<AuthorizationHandler>();

// Load the configuration
var environment = builder.Configuration.GetValue<string>("Environment");

if (environment is not null)
{
    TokenRefreshHandler.Environment = environment;
}


// Shared cookie configuration
Action<CookieBuilder> configureCookie = cookie =>
{
    cookie.HttpOnly = true;
    cookie.SecurePolicy = CookieSecurePolicy.Always;
    cookie.IsEssential = true;
    cookie.SameSite = SameSiteMode.Lax; // Default for auth cookies
};

var oidcScheme = "keycloak";
var cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

builder.Services.AddHostedService<HeartbeatService>();

builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = cookieScheme;
                    options.DefaultChallengeScheme = oidcScheme;
                    options.DefaultSignInScheme = cookieScheme;
                })
                .AddKeycloakOpenIdConnect(oidcScheme, realm: "Etymo", oidcScheme, options =>
                {
                    options.Authority = environment != "Development" 
                    ? "https://sso.hender.tech/realms/Etymo" // Explicitly use https in production
                    : "http://localhost:8080/realms/Etymo"; 
                    options.ClientId = "EtymoWeb";
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.Scope.Add("etymo:all");
                    options.Scope.Add("offline_access");
                    options.RequireHttpsMetadata = environment != "Development"; 
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.SaveTokens = true;
                    options.SignInScheme = cookieScheme;
                    options.UseTokenLifetime = true;
                })
                .AddCookie(cookieScheme, options =>
                {
                    options.Cookie.Name = ".Etymo.Auth";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                    configureCookie(options.Cookie);

                    // Handle automatic refresh on token expiration
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = TokenRefreshHandler.RefreshTokenIfNeeded
                    };
                });

// Add anti - forgery services
builder.Services.AddAntiforgery(options =>
{
    // Customize token generation and validation
    options.HeaderName = "X-CSRF-TOKEN";
    options.FormFieldName = "__RequestVerificationToken";
    options.Cookie.SameSite = SameSiteMode.Strict; // Tighter restriction for CSRF
    configureCookie(options.Cookie);
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IAntiforgeryService, AntiforgeryService>();
builder.Services.AddScoped<UserStateService>();

var app = builder.Build();

// 1. Exception handling FIRST
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// 2. Static files and redirection
app.UseHttpsRedirection();
app.UseStaticFiles();  // This must come before routing

// 3. Routing BEFORE auth middleware
app.UseRouting();

// 4. Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// 5. Output cache and anti-forgery
app.UseOutputCache();
app.UseAntiforgery();

// 6. Add the heartbeat endpoint
app.MapGet("/api/heartbeat", async (HttpContext context) =>
{
    try
    {
        // Only process authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Get the authentication properties
            var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (authenticateResult?.Succeeded == true)
            {
                // Get the cookie options and scheme from DI
                var optionsMonitor = context.RequestServices
                    .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
                var schemeProvider = context.RequestServices
                    .GetRequiredService<IAuthenticationSchemeProvider>();

                var schemeName = CookieAuthenticationDefaults.AuthenticationScheme;
                var options = optionsMonitor.Get(schemeName);
                var scheme = await schemeProvider.GetSchemeAsync(schemeName);

                if (scheme != null)
                {
                    // Create the authentication ticket
                    var ticket = new AuthenticationTicket(
                        authenticateResult.Principal,
                        authenticateResult.Properties,
                        schemeName);

                    // Create the validation context
                    var validateContext = new CookieValidatePrincipalContext(
                        context,
                        scheme,
                        options,
                        ticket);

                    // Call our token refresh handler
                    await TokenRefreshHandler.RefreshTokenIfNeeded(validateContext);

                    // If token was refreshed and principal isn't null, sign in with the new properties
                    if (validateContext.ShouldRenew && validateContext.Principal != null)
                    {
                        await context.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            validateContext.Principal,
                            validateContext.Properties);

                        return Results.Ok(new { refreshed = true, timestamp = DateTime.UtcNow });
                    }
                }
            }
        }

        return Results.Ok(new { refreshed = false, timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.MapLoginAndLogout();

app.Run();