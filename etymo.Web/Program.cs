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
using System.Net.Security;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor()
                .AddTransient<AuthorizationHandler>();

// Add API client with self-signed cert handling
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
            // Specifically for the API service only
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Only accept any certificate for the API service
                if (message.RequestUri?.Host == "etymo-apiservice")
                {
                    return true; // Accept any certificate for the API service
                }

                // For all other domains (including Keycloak), use normal validation
                return errors == SslPolicyErrors.None;
            };

            Console.WriteLine("API service configured to accept self-signed certificates");
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

// Add diagnostic logger for HttpClient requests
if (!builder.Environment.IsDevelopment())
{
    Console.WriteLine("Enabling HTTP client logging");
    // Set to Debug to see detailed HTTP request/response info
    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
    });
}

// Add a diagnostic service that will test the Keycloak connection on app startup
builder.Services.AddHostedService<KeycloakDiagnosticService>();

// Now configure authentication with diagnostic info
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

                    // Using a handler with verbose SSL debugging
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                        {
                            if (errors != SslPolicyErrors.None)
                            {
                                Console.WriteLine($"SSL Policy Error for {message.RequestUri?.Host}: {errors}");
                                Console.WriteLine($"Certificate: {cert?.Subject}, Issued By: {cert?.Issuer}");

                                // Important: Log the chain for troubleshooting
                                for (int i = 0; i < chain?.ChainElements.Count; i++)
                                {
                                    var element = chain.ChainElements[i];
                                    Console.WriteLine($"Chain element {i}: {element.Certificate.Subject}");
                                    foreach (var status in element.ChainElementStatus)
                                    {
                                        Console.WriteLine($"  Status: {status.Status} - {status.StatusInformation}");
                                    }
                                }

                                // For troubleshooting in production, temporarily accept the certificate
                                return true;
                            }
                            return true;
                        }
                    };

                    // Add comprehensive diagnostic events
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            Console.WriteLine($"Redirecting to identity provider: {context.ProtocolMessage.IssuerAddress}");
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            Console.WriteLine("Message received from identity provider");
                            return Task.CompletedTask;
                        },
                        OnRemoteFailure = context =>
                        {
                            Console.WriteLine($"Remote failure: {context.Failure?.Message}");
                            if (context.Failure?.InnerException != null)
                            {
                                Console.WriteLine($"Inner exception: {context.Failure.InnerException.Message}");
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"Authentication failed: {context.Exception?.Message}");
                            if (context.Exception?.InnerException != null)
                            {
                                Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
                            }
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

// Add this diagnostic service class to your project
public class KeycloakDiagnosticService(ILogger<KeycloakDiagnosticService> logger) : IHostedService
{
    private readonly ILogger<KeycloakDiagnosticService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing connection to Keycloak...");

        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (errors != SslPolicyErrors.None)
                    {
                        _logger.LogWarning($"SSL Policy Error for {message.RequestUri?.Host}: {errors}");
                        _logger.LogWarning($"Certificate: {cert?.Subject}, Issued By: {cert?.Issuer}");

                        // Log certificate chain issues
                        for (int i = 0; i < chain?.ChainElements.Count; i++)
                        {
                            var element = chain.ChainElements[i];
                            _logger.LogWarning($"Chain element {i}: {element.Certificate.Subject}");
                            foreach (var status in element.ChainElementStatus)
                            {
                                _logger.LogWarning($"  Status: {status.Status} - {status.StatusInformation}");
                            }
                        }

                        // For troubleshooting, accept the certificate
                        return true;
                    }
                    return true;
                }
            };

            using var httpClient = new HttpClient(handler);
            var response = await httpClient.GetAsync("https://sso.hender.tech/realms/Etymo/.well-known/openid-configuration", cancellationToken);

            _logger.LogInformation($"Keycloak test result: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Keycloak connection test succeeded!");
                _logger.LogDebug($"Response content: {content}");
            }
            else
            {
                _logger.LogWarning($"Keycloak connection test failed: {response.StatusCode}");
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning($"Response content: {content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Keycloak connection test error: {ex.Message}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}