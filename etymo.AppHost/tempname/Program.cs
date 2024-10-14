var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.etymo_ApiService>("etymo-apiservice");

builder.AddProject<Projects.etymo_Web>("etymo-webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

builder.Build().Run();
