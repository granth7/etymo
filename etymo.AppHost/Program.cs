using Microsoft.Extensions.Configuration;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);
var configuration = builder.Configuration;


var environment = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__existingPostgres");

// If the environment variable exists, then the app has been deployed to a cluster. So, connect to the database running in the cluster.

// If the environment variable doesn't exist, then the app is running locally or the cluster secret does not exist, so create one.
// DEBUG ONLY: If the app is running locally and you want to port-forward to the prod db to debug, follow these steps:
// 1. Enter the credentials (do not commit) or create a local environment variable, for example:
// Example: setx ConnectionStrings__existingPostgres "Host=myserver;Database=mydb;Username=myuser;Password=mypassword"
// 2. Reopen Visual Studio to pick up the changes.
// 3. Port-forward the prod database: kubectl port-forward service/postgres16-rw 8090:5432 -n database
if (connectionString != null)
{
    configuration["ConnectionStrings:existingPostgres"] = connectionString;
}

// If you want to test locally without port-forwarding, don't set the environment variable.
// Instead, a local postgres docker container will be spun up. (Development and Testing modes only.)
else if (connectionString == null)
{
    configuration["ConnectionStrings:existingPostgres"] = "Host=localhost;Port=5433;Database=etymo_test;Username=postgres;Password=postgres";

    if (environment == "Development" || environment == "Testing")
    {
        RunDockerComposeUp();
    }
}

var cache = builder.AddRedis("cache");

var keycloak = builder.AddKeycloak("keycloak", 8080)
                      .WithDataVolume()
                      .WithRealmImport("../realms");

var existingPostgres = builder.AddConnectionString("existingPostgres");

// Start by adding the project with environment variables if testing
var apiService = environment == "Testing"
    ? builder.AddProject<Projects.etymo_ApiService>("etymo-apiservice")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
    : builder.AddProject<Projects.etymo_ApiService>("etymo-apiservice");

// Then add the remaining configuration
apiService = apiService
            .WithReference(existingPostgres)
            .WithReference(keycloak)
            .WaitFor(keycloak);

builder.AddProject<Projects.etymo_Web>("etymo-webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(keycloak)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

// Spin up a postgres container for local debugging, only used in 'Development' or 'Testing' mode.
static void RunDockerComposeUp()
{
    try
    {
        // Get the path to the workflows directory
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string workflowsDirectory = Path.Combine(currentDirectory, "../../../../.github/workflows");
        workflowsDirectory = Path.GetFullPath(workflowsDirectory);

        // Check if docker-compose.yml exists
        string composeFilePath = Path.Combine(workflowsDirectory, "docker-compose.yml");
        if (!File.Exists(composeFilePath))
        {
            Console.WriteLine($"docker-compose.yml not found at: {composeFilePath}");
            return;
        }

        // Start Docker Compose
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composeFilePath}\" up -d",
            WorkingDirectory = workflowsDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new () { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd();
            Console.WriteLine($"Error running docker-compose: {error}");
        }
        else
        {
            Console.WriteLine("Docker Compose started and database seeded.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}