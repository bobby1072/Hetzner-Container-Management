using System.Diagnostics;
using System.Net;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed partial class ContainerManagementCleanerService : IContainerManagementCleanerService
{
    private readonly IDockerEngineClient _dockerEngineClient;
    private readonly ILogger<ContainerManagementCleanerService> _logger;

    private static double BytesToMB(long bytes) => bytes / 1_000_000.0;

    public ContainerManagementCleanerService(
        IDockerEngineClient dockerEngineClient,
        ILogger<ContainerManagementCleanerService> logger
    )
    {
        _dockerEngineClient = dockerEngineClient;
        _logger = logger;
    }

    public async Task CleanDaemonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = TelemetryHelperService.ActivitySource.StartActivity();
            LogStartingDaemonCleanup(_logger);

            var stopWatch = Stopwatch.StartNew();

            await Task.WhenAll(
                CleanImagesAsync(cancellationToken),
                CleanContainersAsync(cancellationToken),
                CleanVolumesAsync(cancellationToken)
            );

            stopWatch.Stop();

            LogFinishedDaemonCleanup(_logger, stopWatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogDaemonCleanupError(_logger, ex);
        }
    }

    private async Task CleanImagesAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        LogCleaningImages(_logger);

        var imageClean = await _dockerEngineClient.DeleteUnusedImages(null, cancellationToken);

        if (!imageClean.IsSuccess || !string.IsNullOrWhiteSpace(imageClean.ExceptionMessage))
        {
            LogDeleteUnusedImagesFailed(
                _logger,
                imageClean.ExceptionMessage,
                imageClean.StatusCode
            );
            return;
        }

        LogDeletedUnusedImages(
            _logger,
            imageClean.Data?.ImagesDeleted,
            BytesToMB(imageClean.Data?.SpaceReclaimed ?? 0)
        );
    }

    private async Task CleanContainersAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        LogCleaningContainers(_logger);
        var containerClean = await _dockerEngineClient.DeleteStoppedContainers(
            null,
            cancellationToken
        );

        if (
            !containerClean.IsSuccess || !string.IsNullOrWhiteSpace(containerClean.ExceptionMessage)
        )
        {
            LogDeleteStoppedContainersFailed(
                _logger,
                containerClean.ExceptionMessage,
                containerClean.StatusCode
            );
            return;
        }

        LogDeletedStoppedContainers(
            _logger,
            containerClean.Data?.ContainersDeleted,
            BytesToMB(containerClean.Data?.SpaceReclaimed ?? 0)
        );
    }

    private async Task CleanVolumesAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        LogCleaningVolumes(_logger);
        var volumeClean = await _dockerEngineClient.DeleteUnusedVolumes(null, cancellationToken);

        if (!volumeClean.IsSuccess || !string.IsNullOrWhiteSpace(volumeClean.ExceptionMessage))
        {
            LogDeleteUnusedVolumesFailed(
                _logger,
                volumeClean.ExceptionMessage,
                volumeClean.StatusCode
            );
            return;
        }

        LogDeletedUnusedVolumes(
            _logger,
            volumeClean.Data?.VolumesDeleted,
            BytesToMB(volumeClean.Data?.SpaceReclaimed ?? 0)
        );
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "About to start docker daemon clean up..."
    )]
    private static partial void LogStartingDaemonCleanup(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Finished cleaning docker daemon, took {TimeTakenInMs}ms in total to clean"
    )]
    private static partial void LogFinishedDaemonCleanup(ILogger logger, long timeTakenInMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unexpected exception occured during CleanDaemon"
    )]
    private static partial void LogDaemonCleanupError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaning images on docker daemon...")]
    private static partial void LogCleaningImages(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Request to delete unused images failed with exception message: {ExceptionMessage} and response status code: {StatusCode}"
    )]
    private static partial void LogDeleteUnusedImagesFailed(
        ILogger logger,
        string? exceptionMessage,
        HttpStatusCode? statusCode
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully deleted unused images: {ImagesDeleted} and reclaimed space: {SpaceReclaimed}Mb"
    )]
    private static partial void LogDeletedUnusedImages(
        ILogger logger,
        ImageDeleteItem[]? imagesDeleted,
        double spaceReclaimed
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cleaning containers on docker daemon..."
    )]
    private static partial void LogCleaningContainers(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Request to delete unused containers failed with exception message: {ExceptionMessage} and response status code: {StatusCode}"
    )]
    private static partial void LogDeleteStoppedContainersFailed(
        ILogger logger,
        string? exceptionMessage,
        HttpStatusCode? statusCode
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully deleted unused containers: {ContainersDeleted} and reclaimed space: {SpaceReclaimed}Mb"
    )]
    private static partial void LogDeletedStoppedContainers(
        ILogger logger,
        string[]? containersDeleted,
        double spaceReclaimed
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cleaning volumes from docker daemon..."
    )]
    private static partial void LogCleaningVolumes(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Request to delete unused volumes failed with exception message: {ExceptionMessage} and response status code: {StatusCode}"
    )]
    private static partial void LogDeleteUnusedVolumesFailed(
        ILogger logger,
        string? exceptionMessage,
        HttpStatusCode? statusCode
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully deleted unused volumes: {VolumesDeleted} and reclaimed space: {SpaceReclaimed}Mb"
    )]
    private static partial void LogDeletedUnusedVolumes(
        ILogger logger,
        string[]? volumesDeleted,
        double spaceReclaimed
    );
}
