using System.Text.Json;
using BT.Common.Api.Helpers.Extensions;
using BT.Common.Api.Helpers.Models;
using BT.Common.Helpers;
using BT.Common.Helpers.Extensions;
using Hetzner.Container.Management.Api.Middlewares;
using Hetzner.Container.Management.Api.Services;
using Hetzner.Container.Management.Services;
using Hetzner.Container.Management.Services.Extensions;
using Microsoft.AspNetCore.Http.Timeouts;

var localLogger = LoggingHelper.CreateLogger();

try
{
    localLogger.LogInformation("Application starting...");

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
    
    var serviceOpts = builder.CheckAndAddSingletonOptions<ServiceInfo>();
    
    var requestTimeout = builder.Configuration.GetValue<int>("RequestTimeout");

    builder.Services.AddRequestTimeouts(opts =>
    {
        opts.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(requestTimeout > 0 ? requestTimeout : 60),
        };
    });
    
    var apiKeys = builder.Configuration.GetSection("ApiKey");
    if (!apiKeys.Exists())
    {
        throw new ArgumentNullException(nameof(apiKeys));
    }
    builder.Services.AddKeyedSingleton(ApplicationConstants.ServiceKeys.ApiKeyServiceKey, apiKeys.Get<string[]>() ?? throw new ArgumentException(nameof(apiKeys)));

    builder.Services.AddHealthChecks();
    
    builder.Logging.AddJsonLogging();

    builder.Services.AddResponseCompression();
    
    builder.Services
        .AddControllers()
        .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var infraJsonPath = builder.Configuration.GetValue<string>("InfrastructureJsonPath");
    
    builder.AddContainerManagementApiApplication<CurrentInfrastructureExplorer>(serviceOpts,sp => new CurrentInfrastructureExplorer(
        string.IsNullOrWhiteSpace(infraJsonPath) ? 
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), $"Data{Path.DirectorySeparatorChar}CurrentInfrastructure.json")) :
            Path.GetFullPath(infraJsonPath),
        sp.GetRequiredService<ILoggerFactory>().CreateLogger<CurrentInfrastructureExplorer>())
    );

    localLogger.LogInformation(
        "About to build application with {NumberOfServices} services",
        builder.Services.Count
    );
    
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseRouting();

    app.UseResponseCompression();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app
        .UseBadRequestExceptionHandlingMiddleware()
        .UseMiddleware<ExceptionHandlingMiddleware>()
        .UseCorrelationIdMiddleware()
        .UseMiddleware<ApiKeyMiddleware>();
    
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
