using etymo.Web;
using etymo.Web.Components;
using etymo.Web.Components.Extensions;
using etymo.Web.Components.Handlers;
using etymo.Web.Components.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor()
                .AddTransient<AuthorizationHandler>();

// Configure the HttpClient for MorphemeApiClient specifically with custom certificate handling
builder.Services.AddHttpClient<MorphemeApiClient>(client =>
{
    // Specify http in dev, https in prod.
    client.BaseAddress = builder.Environment.IsDevelopment()
        ? new Uri("http://etymo-apiservice")
        : new Uri("https://etymo-apiservice:8443");
})
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        // Only apply certificate handling in production where HTTPS is used
        if (!builder.Environment.IsDevelopment())
        {
            // Try to load the mounted certificate and only add it for the ApiService domain
            var certPath = "/etc/ssl/certs/apiservice.crt";
            if (File.Exists(certPath))
            {
                try
                {
                    var cert = new X509Certificate2(certPath);

                    // Instead of adding to the certificate store, set up a custom validator
                    // that only accepts our self-signed cert for the apiservice domain
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        // If the request is to our API service, validate with our custom cert
                        if (message.RequestUri?.Host == "etymo-apiservice")
                        {
                            // If there are errors, check if they're because of our self-signed cert
                            if (errors != System.Net.Security.SslPolicyErrors.None)
                            {
                                // Compare the incoming certificate with our trusted one
                                return cert?.GetCertHashString() ==
                                       new X509Certificate2(certPath).GetCertHashString();
                            }
                            return true;
                        }

                        // For all other domains, use normal certificate validation
                        return errors == System.Net.Security.SslPolicyErrors.None;
                    };

                    Console.WriteLine("API service certificate configured for custom validation");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load API certificate: {ex.Message}");
                    // Fallback to ignoring certificate validation ONLY for the API service
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        // Only bypass validation for our API service
                        return message.RequestUri?.Host == "etymo-apiservice" ||
                               errors == System.Net.Security.SslPolicyErrors.None;
                    };
                }
            }
            else
            {
                Console.WriteLine("API certificate not found, using insecure connection for API service only");
                // Fallback to ignoring certificate validation ONLY for the API service
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Only bypass validation for our API service
                    return message.RequestUri?.Host == "etymo-apiservice" ||
                           errors == System.Net.Security.SslPolicyErrors.None;
                };
            }
        }

        return handler;
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

// Register a named HttpClient specifically for Keycloak OIDC
builder.Services.AddHttpClient("KeycloakBackchannel")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Standard handler with default certificate validation for Keycloak
        UseCookies = false,
        CheckCertificateRevocationList = true
    });

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
                    options.Scope.Add("openid profile email roles"); // Explicitly request roles
                    options.Scope.Add("etymo:all");
                    options.Scope.Add("offline_access");
                    options.RequireHttpsMetadata = environment != "Development";
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
                    options.SaveTokens = true;
                    options.SignInScheme = cookieScheme;
                    options.UseTokenLifetime = true;
                    // Ensure role claims are mapped
                    options.ClaimActions.MapJsonKey(ClaimTypes.Role, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

                    // Use our explicitly configured HttpClient for backchannel communication
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        // Standard validation for Keycloak SSL certificate
                        ServerCertificateCustomValidationCallback = null
                    };

                    // For diagnostic purposes, see if there are any backchannel errors
                    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
                    {
                        OnRemoteFailure = context =>
                        {
                            Console.WriteLine($"Remote failure: {context.Failure}");
                            return Task.CompletedTask;
                        }
                    };
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