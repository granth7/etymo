using etymo.ApiService;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

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

builder.AddNpgsqlDataSource(connectionName: "existingPostgres");

builder.Services.AddScoped<PostgresService>();


builder.Services.AddAuthentication()
                .AddKeycloakJwtBearer("keycloak", realm: "Etymo", options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Audience = "etymo.api";
                });

builder.Services.AddAuthorizationBuilder();

var app = builder.Build();
var latinPrefixJson = File.ReadAllText(@"latinPrefixes.json");
var latinPrefixes = JsonConvert.DeserializeObject<Dictionary<string, string>>(latinPrefixJson);

var latinSuffixJson = File.ReadAllText(@"latinSuffixes.json");
var latinSuffixes = JsonConvert.DeserializeObject<Dictionary<string, string>>(latinSuffixJson);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();


app.MapGet("/morphemelist", (string gameType) =>
{
    var morphemeList = new Dictionary<string, string>();
    if (gameType == "latinPrefixes")
    {
        morphemeList = latinPrefixes;
    }
    else if (gameType == "latinSuffixes")
    {
        morphemeList = latinSuffixes;
    }
    else
    {
        morphemeList = null;
    }

    if (morphemeList != null && morphemeList.Count > 0)
    {
        int morphemeListSize = 5;
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

//app.MapGet("/privatewordlist", () =>
//{
//    // Use the service provider to create a scope and resolve PostgresService 
//    using (var scope = app.Services.CreateScope())
//    {
//        var postgresService = scope.ServiceProvider.GetRequiredService<PostgresService>();

//        // Call a method from PostgresService 
//        var wordListOverviews = await postgresService.GetPrivateWordListOverviewsAsync();

//        return wordListOverviews;
//    }
//}).RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();

record Morepheme(KeyValuePair<string, string> Kvp)
{
    public bool IsOnTop { get; set; }
    public bool GuessedCorrectly { get; set; }
}
