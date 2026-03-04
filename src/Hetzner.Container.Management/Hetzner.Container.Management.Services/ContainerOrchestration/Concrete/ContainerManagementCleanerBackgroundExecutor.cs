using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementCleanerBackgroundExecutor: BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ContainerManagementCleanerBackgroundExecutor> _logger;

    public ContainerManagementCleanerBackgroundExecutor(IServiceScopeFactory serviceScopeFactory,
        ILogger<ContainerManagementCleanerBackgroundExecutor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        _logger.LogInformation(
            "{BackgroundServiceName} is starting up...",
            nameof(ContainerManagementCleanerBackgroundExecutor)
        );
        
        await using var asyncScope = _serviceScopeFactory.CreateAsyncScope();
        var cleanerService = asyncScope.ServiceProvider.GetRequiredService<IContainerManagementCleanerService>();

        await cleanerService.CleanDaemonAsync(stoppingToken);
    }
}