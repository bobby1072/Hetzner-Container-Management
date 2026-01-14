using System.Text.Json;
using BT.Common.Api.Helpers.Extensions;
using BT.Common.Api.Helpers.Models;
using BT.Common.Helpers;
using Hetzner.Container.Management.Services.Extensions;
using Microsoft.AspNetCore.Http.Timeouts;

var localLogger = LoggingHelper.CreateLogger();

try
{
    localLogger.LogInformation("Application starting...");

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
    
    builder.CheckAndAddSingletonOptions<ServiceInfo>();
    
    var requestTimeout = builder.Configuration.GetValue<int>("RequestTimeout");

    builder.Services.AddRequestTimeouts(opts =>
    {
        opts.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(requestTimeout > 0 ? requestTimeout : 60),
        };
    });

    builder.Services.AddHealthChecks();
    
    builder.Logging.AddJsonLogging();

    builder.Services.AddResponseCompression();
    
    builder.Services
        .AddControllers()
        .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    
    builder.Services.AddOpenApi();

    localLogger.LogInformation(
        "About to build application with {NumberOfServices} services",
        builder.Services.Count
    );

    builder.AddContainerManagementApplication();
    
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    app.UseRouting();

    app.UseResponseCompression();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.UseCorrelationIdMiddleware();
    
    app.MapControllers();

    app
        .UseHealthGetEndpoints();
    
    await app.RunAsync();
}
catch (Exception ex)
{
    localLogger.LogCritical(
        ex,
        "Unhandled exception in application with message: {ExMessage}",
        ex.Message
    );
}
finally
{
    localLogger.LogInformation("Application is exiting...");
}
