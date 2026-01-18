using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;

internal interface IContainerManagementService
{
    Task<InfrastructureDocument> UpdateCurrentInfrastructure(
        InfrastructureComponentUpdateInput[] infrastructureDocuments,
        CancellationToken cancellationToken
    );
}
