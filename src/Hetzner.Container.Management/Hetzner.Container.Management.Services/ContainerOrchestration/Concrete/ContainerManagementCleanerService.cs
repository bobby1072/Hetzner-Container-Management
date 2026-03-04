using System.Diagnostics;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementCleanerService
{
    private readonly IDockerEngineClient _dockerEngineClient;
    private readonly ILogger<ContainerManagementCleanerService> _logger;
    private static double BytesToMB(long bytes) => bytes / 1_000_000.0;
    public ContainerManagementCleanerService(IDockerEngineClient dockerEngineClient,
        ILogger<ContainerManagementCleanerService> logger)
    {
        _dockerEngineClient = dockerEngineClient;
        _logger = logger;
    }

    public async Task CleanDaemonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = TelemetryHelperService.ActivitySource.StartActivity();
            _logger.LogInformation("About to start docker daemon clean up...");

            var stopWatch = Stopwatch.StartNew();
            
            await Task.WhenAll(CleanImagesAsync(cancellationToken),
                CleanContainersAsync(cancellationToken),
                CleanVolumesAsync(cancellationToken));
            
            stopWatch.Stop();
            
            _logger.LogInformation("Finished cleaning docker daemon, took {TimeTakenInMs}ms in total to clean", stopWatch.ElapsedMilliseconds);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception occured during CleanDaemon");
        }
    }

    private async Task CleanImagesAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        _logger.LogInformation("Cleaning images on docker daemon...");
        
        var imageClean = await _dockerEngineClient.DeleteUnusedImages(null, cancellationToken);

        if (!imageClean.IsSuccess || !string.IsNullOrWhiteSpace(imageClean.ExceptionMessage))
        {
            _logger.LogError("Request to delete unused images failed with exception message: {ExceptionMessage} and response status code: {StatusCode}",
                imageClean.ExceptionMessage,
                imageClean.StatusCode);
            return;
        }
        
        _logger.LogInformation("Successfully deleted unused images: {@ImagesDeleted} and reclaimed space: {SpaceReclaimed}Mb",
            imageClean.Data?.ImagesDeleted,
            BytesToMB(imageClean.Data?.SpaceReclaimed ?? 0));
    }
    
    private async Task CleanContainersAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        _logger.LogInformation("Cleaning containers on docker daemon...");
        var containerClean = await _dockerEngineClient.DeleteStoppedContainers(null, cancellationToken);

        if (!containerClean.IsSuccess || !string.IsNullOrWhiteSpace(containerClean.ExceptionMessage))
        {
            _logger.LogError("Request to delete unused containers failed with exception message: {ExceptionMessage} and response status code: {StatusCode}",
                containerClean.ExceptionMessage,
                containerClean.StatusCode);
            return;
        }
        
        _logger.LogInformation("Successfully deleted unused containers: {@ContainersDeleted} and reclaimed space: {SpaceReclaimed}Mb",
            containerClean.Data?.ContainersDeleted,
            BytesToMB(containerClean.Data?.SpaceReclaimed ?? 0));
    }    
    private async Task CleanVolumesAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        _logger.LogInformation("Cleaning volumes from docker daemon...");
        var volumeClean = await _dockerEngineClient.DeleteUnusedVolumes(null, cancellationToken);

        if (!volumeClean.IsSuccess || !string.IsNullOrWhiteSpace(volumeClean.ExceptionMessage))
        {
            _logger.LogError("Request to delete unused volumes failed with exception message: {ExceptionMessage} and response status code: {StatusCode}",
                volumeClean.ExceptionMessage,
                volumeClean.StatusCode);
            return;
        }
        
        _logger.LogInformation("Successfully deleted unused volumes: {@VolumesDeleted} and reclaimed space: {SpaceReclaimed}Mb",
            volumeClean.Data?.VolumesDeleted,
            BytesToMB(volumeClean.Data?.SpaceReclaimed ?? 0));
    }
}