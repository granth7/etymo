var builder = DistributedApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__existingPostgres");

// If the environment variable exists, then the app has been deployed to a cluster. So, connect to the database running in the cluster.
if (connectionString != null)
{
    configuration["ConnectionStrings:existingPostgres"] = connectionString;
}
// If the environment variable doesn't exist, then the app is running locally or the cluster secret does not exist.
// Enter the credentials (do not commit) or create a local environment variable. 
// Example: setx ConnectionStrings__existingPostgres "Host=myserver;Database=mydb;Username=myuser;Password=mypassword"
// Reopen Visual Studio to pick up the changes.
else if (connectionString == null)
{
    configuration["ConnectionStrings:existingPostgres"] = "Host=myserver;Database=mydb;Username=myuser;Password=mypassword";
}

var cache = builder.AddRedis("cache");

var keycloak = builder.AddKeycloak("keycloak", 8080)
                      .WithDataVolume()
                      .WithRealmImport("../realms");

var existingPostgres = builder.AddConnectionString("existingPostgres");

var apiService = builder.AddProject<Projects.etymo_ApiService>("etymo-apiservice")
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
