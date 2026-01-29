using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;

public interface IContainerManagementOperationQueue: IDisposable
{
    internal Task<KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureComponent[]?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>> DequeueUpdateOperationAsync(
        CancellationToken cancellationToken);
    Task<Guid> QueueUpdateOperation(InfrastructureComponentUpdateInput[] input, CancellationToken cancellationToken = default);
    Task<InfrastructureComponent[]> QueueAndWaitForUpdateOperation(InfrastructureComponentUpdateInput[] input, CancellationToken cancellationToken = default);
}