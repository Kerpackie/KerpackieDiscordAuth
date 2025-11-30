var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireDemo_Basic_ApiService>("apiservice")
    .WithHttpEndpoint(port: 5001, name: "api")
    .WithHttpHealthCheck("/health");

builder.AddNpmApp("webfrontend", "../AspireDemo.Basic.Web", "dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();


builder.Build().Run();