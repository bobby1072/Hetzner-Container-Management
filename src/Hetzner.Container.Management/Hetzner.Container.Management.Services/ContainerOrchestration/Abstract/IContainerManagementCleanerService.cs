namespace Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;

public interface IContainerManagementCleanerService
{
    Task CleanDaemonAsync(CancellationToken cancellationToken = default);
}