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
        ? "https://sso.hender.tech/realms/Etymo"
        : "http://localhost:8080/realms/Etymo";
    options.ClientId = "EtymoWeb";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("openid profile email roles");
    options.Scope.Add("etymo:all");
    options.Scope.Add("offline_access");
    options.RequireHttpsMetadata = environment != "Development";
    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
    options.TokenValidationParameters.RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    options.SaveTokens = true;
    options.SignInScheme = cookieScheme;
    options.UseTokenLifetime = true;
    options.ClaimActions.MapJsonKey(ClaimTypes.Role, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

    // Create a separate handler specifically for Keycloak
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        // Accept LetsEncrypt certificates
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            Console.WriteLine($"[Keycloak] Certificate validation for: {message.RequestUri?.Host}");
            Console.WriteLine($"[Keycloak] Certificate subject: {cert?.Subject}");
            Console.WriteLine($"[Keycloak] Certificate issuer: {cert?.Issuer}");
            Console.WriteLine($"[Keycloak] Certificate errors: {errors}");

            // Accept the certificate regardless of errors (for troubleshooting)
            return true;
        }
    };

    // More detailed diagnostic events
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            Console.WriteLine($"[Keycloak] Redirecting to identity provider: {context.ProtocolMessage.IssuerAddress}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            Console.WriteLine("[Keycloak] Message received from identity provider");
            if (context.ProtocolMessage.Error != null)
            {
                Console.WriteLine($"[Keycloak] Error: {context.ProtocolMessage.Error}");
                Console.WriteLine($"[Keycloak] Error description: {context.ProtocolMessage.ErrorDescription}");
            }
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            Console.WriteLine($"[Keycloak] Remote failure: {context.Failure?.Message}");
            if (context.Failure?.InnerException != null)
            {
                Console.WriteLine($"[Keycloak] Inner exception: {context.Failure.InnerException.Message}");
                Console.WriteLine($"[Keycloak] Stack trace: {context.Failure.InnerException.StackTrace}");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[Keycloak] Authentication failed: {context.Exception?.Message}");
            Console.WriteLine($"[Keycloak] Exception type: {context.Exception?.GetType().Name}");
            if (context.Exception?.InnerException != null)
            {
                Console.WriteLine($"[Keycloak] Inner exception: {context.Exception.InnerException.Message}");
                Console.WriteLine($"[Keycloak] Inner exception type: {context.Exception.InnerException.GetType().Name}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("[Keycloak] Token validated successfully");
            return Task.CompletedTask;
        },
        OnUserInformationReceived = context =>
        {
            Console.WriteLine("[Keycloak] User information received");
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


// Set up global HttpClient handling for all services
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Configure default TLS settings
    http.ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        // Enable TLS 1.2 and 1.3 as fallback options
        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                              System.Security.Authentication.SslProtocols.Tls13;

        // Only apply certificate handling in production where HTTPS is used
        if (!builder.Environment.IsDevelopment())
        {
            // Use domain-specific certificate validation
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                var host = message.RequestUri?.Host;
                Console.WriteLine($"[TLS] Validating certificate for {host}");

                // Allow self-signed certificate only for the API service
                if (host == "etymo-apiservice")
                {
                    Console.WriteLine("[TLS] Allowing self-signed certificate for API service");
                    return true;
                }

                // For Keycloak, log issues but allow it during troubleshooting
                if (host == "sso.hender.tech")
                {
                    if (errors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"[TLS] Keycloak certificate validation error: {errors}");
                        Console.WriteLine($"[TLS] Certificate: {cert?.Subject}, Issuer: {cert?.Issuer}");

                        // Temporarily accept during troubleshooting
                        return true;
                    }
                }

                // For all other services, use standard validation
                return errors == SslPolicyErrors.None;
            };
        }

        return handler;
    });
});


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

public class KeycloakDiagnosticService(ILogger<KeycloakDiagnosticService> logger) : IHostedService
{
    private readonly ILogger<KeycloakDiagnosticService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing connection to Keycloak...");

        // Test both HTTP and HTTPS connections to diagnose issues
        await TestKeycloakConnection("https://sso.hender.tech/realms/Etymo/.well-known/openid-configuration", cancellationToken);

        // Also attempt to connect to other common endpoints for debugging
        await TestKeycloakConnection("https://sso.hender.tech/", cancellationToken);

        // Try DNS resolution
        try
        {
            _logger.LogInformation("Testing DNS resolution for sso.hender.tech...");
            var hostEntry = await System.Net.Dns.GetHostEntryAsync("sso.hender.tech", cancellationToken);
            _logger.LogInformation($"DNS resolved. IP addresses: {string.Join(", ", hostEntry.AddressList.Select(a => a.ToString()))}");
        }
        catch (Exception dnsEx)
        {
            _logger.LogError($"DNS resolution failed: {dnsEx.Message}");
        }
    }

    private async Task TestKeycloakConnection(string url, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Testing connection to: {url}");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    _logger.LogInformation($"[CERT] URL: {message.RequestUri?.ToString()}");
                    _logger.LogInformation($"[CERT] Certificate Subject: {cert?.Subject}");
                    _logger.LogInformation($"[CERT] Certificate Issuer: {cert?.Issuer}");
                    _logger.LogInformation($"[CERT] Valid From: {cert?.NotBefore}, Valid To: {cert?.NotAfter}");

                    if (errors != SslPolicyErrors.None)
                    {
                        _logger.LogWarning($"[CERT] SSL Policy Error: {errors}");

                        // Log certificate chain details
                        if (chain != null)
                        {
                            _logger.LogWarning($"[CERT] Certificate chain has {chain.ChainElements.Count} elements");
                            for (int i = 0; i < chain.ChainElements.Count; i++)
                            {
                                var element = chain.ChainElements[i];
                                _logger.LogWarning($"[CERT] Chain element {i}: {element.Certificate.Subject}");
                                _logger.LogWarning($"[CERT] Chain element {i} issuer: {element.Certificate.Issuer}");

                                foreach (var status in element.ChainElementStatus)
                                {
                                    _logger.LogWarning($"[CERT]   Status: {status.Status} - {status.StatusInformation}");
                                }
                            }
                        }

                        // Always accept for diagnostic purposes
                        return true;
                    }

                    _logger.LogInformation("[CERT] Certificate validation successful");
                    return true;
                }
            };

            using var httpClient = new HttpClient(handler);

            // Add headers to potentially work around proxies
            httpClient.DefaultRequestHeaders.Add("User-Agent", "KeycloakDiagnosticService/1.0");

            // Set a reasonable timeout
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Use GET request instead of HEAD
            var response = await httpClient.GetAsync(url, cancellationToken);

            _logger.LogInformation($"[HTTP] Response: {(int)response.StatusCode} {response.StatusCode}");
            _logger.LogInformation($"[HTTP] Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation($"[HTTP] Connection test succeeded! Response length: {content.Length} bytes");
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning($"[HTTP] Connection test failed: {response.StatusCode}");
                _logger.LogWarning($"[HTTP] Response content: {content}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"[HTTP] Connection test error: {ex.Message}");
            _logger.LogError($"[HTTP] Status code: {ex.StatusCode}");
            _logger.LogError($"[HTTP] Inner exception: {ex.InnerException?.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"[HTTP] Connection timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[HTTP] General error: {ex.Message}");
            _logger.LogError($"[HTTP] Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"[HTTP] Inner exception: {ex.InnerException.Message}");
                _logger.LogError($"[HTTP] Inner exception type: {ex.InnerException.GetType().Name}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}