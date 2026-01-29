using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Helpers.Models;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementBackgroundOperationExecutor: BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IContainerManagementOperationQueue _containerManagementOperationQueue;
    private readonly ILogger<ContainerManagementBackgroundOperationExecutor> _logger;

    public ContainerManagementBackgroundOperationExecutor(
        IServiceScopeFactory serviceScopeFactory,
        IContainerManagementOperationQueue containerManagementOperationQueue,
        ILogger<ContainerManagementBackgroundOperationExecutor> logger
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _containerManagementOperationQueue = containerManagementOperationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{BackgroundServiceName} is starting up...", nameof(ContainerManagementBackgroundOperationExecutor));

        while (!stoppingToken.IsCancellationRequested)
        {
            var dequeuedOperationJob = await _containerManagementOperationQueue.DequeueUpdateOperationAsync(stoppingToken);
            
            var executionResult = await ExecuteOperationAsync(dequeuedOperationJob.Key, 
                dequeuedOperationJob.Value.Input,
                stoppingToken);

            if (dequeuedOperationJob.Value.AddToCompleteQueueFunc is not null)
            {
                await dequeuedOperationJob.Value.AddToCompleteQueueFunc.Invoke(dequeuedOperationJob.Key,
                    executionResult.Item1,
                    executionResult.Item2,
                    stoppingToken);
            }
        }
        
        throw new NotImplementedException();
    }

    private async Task<(InfrastructureComponent[]?, ApiException?)> ExecuteOperationAsync(Guid jobId, InfrastructureComponentUpdateInput[] input, CancellationToken cancellationToken)
    {
        using (_logger.BeginScope(new LoggingScopeVariableDictionary { ["JobId"] = jobId }))
        {
            try
            {
                await using var asyncScope = _serviceScopeFactory.CreateAsyncScope();
                var managerService = asyncScope.ServiceProvider.GetRequiredService<IContainerManagementService>();
                
                var result = await managerService.UpdateCurrentInfrastructure(input, cancellationToken);
                
                _logger.LogInformation("Infrastructure has successfully been updated");
                
                return (result, null);
            }
            catch (ApiException exception)
            {
                return (null, exception);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception occurred executing update operation in the background");
                
                return (null, new ApiException(LogLevel.Error, 
                    HttpStatusCode.InternalServerError, 
                    ApplicationConstants.ExceptionConstants.InternalError, exception));
            }
        }
    }
}