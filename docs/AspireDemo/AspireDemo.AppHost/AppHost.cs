var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice")
    .WithHttpEndpoint(port: 5001, name: "api")
    .WithHttpHealthCheck("/health");

builder.AddNpmApp("webfrontend", "../AspireDemo.Web", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
