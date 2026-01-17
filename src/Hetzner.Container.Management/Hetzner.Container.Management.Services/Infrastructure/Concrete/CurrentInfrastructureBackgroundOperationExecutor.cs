using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Infrastructure.Concrete;

internal sealed class CurrentInfrastructureBackgroundOperationExecutor: BackgroundService
{
    private readonly ICurrentInfrastructureUpdateJobQueue _currentInfrastructureUpdateJobQueue;
    private readonly ICurrentInfrastructureExplorer _currentInfrastructureExplorer; 
    private readonly ILogger<CurrentInfrastructureBackgroundOperationExecutor> _logger;

    public CurrentInfrastructureBackgroundOperationExecutor(
        ICurrentInfrastructureUpdateJobQueue currentInfrastructureUpdateJobQueue,
        ICurrentInfrastructureExplorer currentInfrastructureExplorer,
        ILogger<CurrentInfrastructureBackgroundOperationExecutor> logger)
    {
        _currentInfrastructureUpdateJobQueue = currentInfrastructureUpdateJobQueue;
        _currentInfrastructureExplorer = currentInfrastructureExplorer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger
            .LogInformation("{BackgroundServiceName} service is starting...",
                nameof(CurrentInfrastructureBackgroundOperationExecutor));

        while (!stoppingToken.IsCancellationRequested)
        {
            var jobToDo = await _currentInfrastructureUpdateJobQueue
                .DequeueAsync(stoppingToken);
            
            await _currentInfrastructureExplorer.ReplaceCurrentInfrastructureAsync(jobToDo, stoppingToken);
        }
    }
}