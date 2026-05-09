using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddLogging(ctx =>
{
    ctx.ClearProviders();
    ctx.AddJsonConsole();
});

builder.AddProject<Hetzner_Container_Management_Api>("hetzner-container-management-api")
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
