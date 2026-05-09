using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Helpers.Models;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementUpdateBackgroundExecutor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IContainerManagementOperationQueue _containerManagementOperationQueue;
    private readonly ILogger<ContainerManagementUpdateBackgroundExecutor> _logger;
    public ContainerManagementUpdateBackgroundExecutor(
        IServiceScopeFactory serviceScopeFactory,
        IContainerManagementOperationQueue containerManagementOperationQueue,
        ILogger<ContainerManagementUpdateBackgroundExecutor> logger
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _containerManagementOperationQueue = containerManagementOperationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();

        _logger.LogInformation(
            "{BackgroundServiceName} is starting up...",
            nameof(ContainerManagementUpdateBackgroundExecutor)
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            var dequeuedOperationJob =
                await _containerManagementOperationQueue.DequeueUpdateOperationAsync(stoppingToken);
            using (_logger.BeginScope(new LoggingScopeVariableDictionary { ["JobId"] = dequeuedOperationJob.Key }))
            {
                await using var asyncScope = _serviceScopeFactory.CreateAsyncScope();
                var managerService =
                    asyncScope.ServiceProvider.GetRequiredService<IContainerManagementUpdateService>();

                var executionResult = await ExecuteOperationAsync(
                    dequeuedOperationJob.Key,
                    dequeuedOperationJob.Value.Input,
                    managerService,
                    stoppingToken
                );

                if (dequeuedOperationJob.Value.AddToCompleteQueueFunc is not null)
                {
                    await dequeuedOperationJob.Value.AddToCompleteQueueFunc.Invoke(
                        dequeuedOperationJob.Key,
                        executionResult.Item1,
                        executionResult.Item2,
                        stoppingToken
                    );
                }
                
                var cleanerService = asyncScope.ServiceProvider.GetRequiredService<IContainerManagementCleanerService>();
                
                await cleanerService.CleanDaemonAsync(stoppingToken);
            }
        }
    }

    private async Task<(InfrastructureComponent[]?, ApiException?)> ExecuteOperationAsync(
        Guid jobId,
        InfrastructureComponentUpdateInput[] input,
        IContainerManagementUpdateService managerService,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(jobId), jobId);
        activity?.SetTag(nameof(input), input.Length);
        try
        {
            _containerManagementOperationQueue.CacheJobProgress(jobId, new ContainerUpdateJobState { Status = ContainerUpdateJobStatusEnum.InProgress, JobId = jobId });

            var result = await managerService.UpdateCurrentInfrastructure(
                input,
                cancellationToken
            );
            
            _logger.LogInformation("Infrastructure has successfully been updated");

            _containerManagementOperationQueue.CacheJobProgress(jobId, new ContainerUpdateJobState { Status = ContainerUpdateJobStatusEnum.Succeeded, JobId = jobId });
            
            return (result, null);
        }
        catch (ApiException exception)
        {
            _containerManagementOperationQueue.CacheJobProgress(jobId, new ContainerUpdateJobState { Status = ContainerUpdateJobStatusEnum.Failed, JobId = jobId, ApiException = exception });
            
            return (null, exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred executing update operation in the background"
            );

            _containerManagementOperationQueue.CacheJobProgress(jobId, new ContainerUpdateJobState { Status = ContainerUpdateJobStatusEnum.Failed, JobId = jobId });
            
            return (
                null,
                new ApiException(
                    LogLevel.Error,
                    HttpStatusCode.InternalServerError,
                    ApplicationConstants.ExceptionConstants.InternalError,
                    exception
                )
            );
        }
    }
}
