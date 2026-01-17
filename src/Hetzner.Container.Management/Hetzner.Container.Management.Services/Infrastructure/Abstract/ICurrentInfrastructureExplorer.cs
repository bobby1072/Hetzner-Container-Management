using Hetzner.Container.Management.Schemas.Infrastructure;

namespace Hetzner.Container.Management.Services.Infrastructure.Abstract;

public interface ICurrentInfrastructureExplorer: IDisposable
{
    Task<InfrastructureDocument?> TryGetCurrentInfrastructureDocumentAsync(CancellationToken cancellationToken = default);

    Task ReplaceCurrentInfrastructureAsync(InfrastructureDocument infrastructureDocument,
        CancellationToken cancellationToken = default);
}