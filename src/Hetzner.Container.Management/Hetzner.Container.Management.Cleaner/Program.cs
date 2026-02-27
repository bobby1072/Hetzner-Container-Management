using BT.Common.Api.Helpers.Models;
using BT.Common.Helpers;
using BT.Common.Helpers.Extensions;
using Hetzner.Container.Management.Services.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var localLogger = LoggingHelper.CreateLogger();

try
{
    localLogger.LogInformation("Application starting...");
    using var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            config
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(Path.GetFullPath("appsettings.json"), false);
        })
        .ConfigureLogging(logBuilder =>
        {
            logBuilder.AddJsonLogging();
        })
        .ConfigureServices((builder, services) =>
        {
            var serviceInfo = services.CheckAndAddSingletonOptions<ServiceInfo>(builder.Configuration);
            
            localLogger.LogInformation("Starting {ReleaseName} on version: {ReleaseVersion}",
                serviceInfo.ReleaseName, 
                serviceInfo.ReleaseVersion);
            
            services.AddContainerManagementCleanerApplication(builder.Configuration, serviceInfo);
        })
        .Build();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    localLogger.LogError(ex, "Unexpected error occured during startup");
}
finally
{
    localLogger.LogInformation("Application exiting...");
}