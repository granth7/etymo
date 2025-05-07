using Dapper;
using etymo.ApiService.Postgres;
using etymo.ApiService.Postgres.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Models;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// If TLS certificates are available in Kubernetes, configure Kestrel to use them
var certPath = "/etc/tls/tls.crt";
var keyPath = "/etc/tls/tls.key";

if (File.Exists(certPath) && File.Exists(keyPath))
{
    try
    {
        // Log that we found certificates
        Console.WriteLine("TLS certificates found, configuring HTTPS...");

        // Load the certificate from the PEM files
        var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);

        // Configure Kestrel to use the certificate
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080); // HTTP
            options.ListenAnyIP(8443, listenOptions =>
            {
                listenOptions.UseHttps(cert);
            }); // HTTPS
        });

        Console.WriteLine("HTTPS successfully configured on port 8443");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error configuring HTTPS: {ex.Message}");
    }
}

var configuration = builder.Configuration;

var connectionString = Environment.GetEnvironmentVariable("existingPostgres");

// If this environment variable exists, then the app has retrieved it from the cluster deployment.
// So, connect to the database running in the cluster.
if (connectionString != null)
{
    configuration["ConnectionStrings:existingPostgres"] = connectionString;
}
// If the environment variable doesn't exist, then the app is running locally or the cluster secret does not exist.
else if (connectionString == null)
{
    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__existingPostgres");
    configuration["ConnectionStrings:existingPostgres"] = connectionString;
}

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Register custom type handler for Dapper sql queries.
SqlMapper.AddTypeHandler(new DictionaryTypeHandler());

builder.AddNpgsqlDataSource(connectionName: "existingPostgres");

builder.Services.AddScoped<PostgresService>();

builder.Services.AddControllers();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "Etymo", options =>
    {
        options.Authority = "https://keycloak:8443"; // Specify HTTPS and port explicitly
        options.RequireHttpsMetadata = true; // Enforce HTTPS
        options.Audience = "etymo.api";
    });

if (configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "TestAuth";
        options.DefaultChallengeScheme = "TestAuth";
    })
    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestAuth", _ => { });
}

var app = builder.Build();
var latinPrefixJson = File.ReadAllText(@"latinPrefixes.json");
var latinPrefixes = JsonConvert.DeserializeObject<Dictionary<string, string>>(latinPrefixJson);

var latinSuffixJson = File.ReadAllText(@"latinSuffixes.json");
var latinSuffixes = JsonConvert.DeserializeObject<Dictionary<string, string>>(latinSuffixJson);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/morphemelist", async (string wordListGuid, bool isPublic, string? userId, PostgresService postgresService) =>
{
    var morphemeList = new Dictionary<string, string>();

    var postgresController = new PostgresController(postgresService);

    IActionResult response;

    // Fetch the wordListOverview by guid and public visibility
    if (isPublic)
    {
        response = await postgresController.FetchWordListAsync(new Guid(wordListGuid));
    }
    else if (isPublic == false && userId is not null)
    {
        response = await postgresController.FetchPrivateWordListAsync(new Guid(wordListGuid), userId: Guid.Parse(userId));
    }
    else
    {
        response = new BadRequestResult();
    }

    if (response is OkObjectResult okResult && okResult.Value is WordList wordList)
    {
        morphemeList = wordList.Words;
    }

    if (morphemeList != null && morphemeList.Count > 0)
    {
        int morphemeListSize = morphemeList.Count;
        var random = new Random();
        var list = new List<Morepheme>();

        var indexList = new List<int>();
        int number;

        // Get a random, non-repeating list of integers to use to index the dictionary.
        for (int i = 0; i < morphemeListSize; i++)
        {
            do
            {
                number = random.Next(morphemeList.Count);
            } while (indexList.Contains(number));
            indexList.Add(number);
        }

        foreach (int index in indexList)
        {
            string key = morphemeList.Keys.ElementAt(index);
            string value = morphemeList.Values.ElementAt(index);

            var kvp = new KeyValuePair<string, string>(key, value);
            var morpheme = new Morepheme(kvp);

            // The first morpheme should be on top, so set IsOnTop = true.
            if (index == indexList[0])
            {
                morpheme.IsOnTop = true;
            }

            list.Add(morpheme);
        }

        return list.ToArray();
    }

    return null;
});

app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

record Morepheme(KeyValuePair<string, string> Kvp)
{
    public bool IsOnTop { get; set; }
    public bool GuessedCorrectly { get; set; }
}