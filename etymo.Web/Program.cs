using etymo.Web;
using etymo.Web.Components;
using etymo.Web.Components.Extensions;
using etymo.Web.Components.Handlers;
using etymo.Web.Components.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

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
    // Specify http in dev, https in prod.
    client.BaseAddress = builder.Environment.IsDevelopment()
        ? new Uri("http://etymo-apiservice")
        : new Uri("https://etymo-apiservice:8443");
})
    .AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                              ForwardedHeaders.XForwardedProto |
                              ForwardedHeaders.XForwardedHost;

    // Instead of clearing, specify trusted proxies/networks
    options.KnownProxies.Add(IPAddress.Parse("10.0.100.55"));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.100.0"), 24));

    // Allow unlimited forwarded headers
    options.ForwardLimit = null; 
});

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

builder.Services.AddHostedService<HeartbeatService>();

var oidcScheme = "keycloak";
var cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
var keycloakSettings = builder.Configuration.GetSection("KeycloakSettings").Get<KeycloakSettings>() 
    ?? throw new InvalidOperationException("KeycloakSettings configuration is missing. Please ensure KeycloakSettings section is present in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = cookieScheme;
    options.DefaultChallengeScheme = oidcScheme;
    options.DefaultSignInScheme = cookieScheme;
})
    .AddKeycloakOpenIdConnect(oidcScheme, realm: "Etymo", oidcScheme, options =>
    {
        options.Authority = keycloakSettings.GetRealmUrl();
        options.ClientId = "EtymoWeb";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Add("openid profile email roles"); // Explicitly request roles
        options.Scope.Add("etymo:all");
        options.Scope.Add("offline_access");
        options.RequireHttpsMetadata = environment != "Development";
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
        options.SaveTokens = true;
        options.SignInScheme = cookieScheme;
        options.UseTokenLifetime = true;

        // Map Keycloak's role claims to the standard role claim type
        options.ClaimActions.MapJsonKey(ClaimTypes.Role, "roles");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Role, "realm_access", "roles");
        options.ClaimActions.MapJsonSubKey(ClaimTypes.Role, "resource_access", "roles");

        // Also clear the default role claim actions that might interfere
        options.ClaimActions.DeleteClaim("role");
        options.ClaimActions.DeleteClaim("roles");
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

if (environment == "Production")
{
    builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("Etymo");
}

var app = builder.Build();

app.UseForwardedHeaders();

// 1. Exception handling and Production policy setup.
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.MapLoginAndLogout();

app.Run();