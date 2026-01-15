using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerUpdateService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContainerUpdateService> _logger;

    public ContainerUpdateService(IServiceProvider serviceProvider,
        ILogger<ContainerUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
}