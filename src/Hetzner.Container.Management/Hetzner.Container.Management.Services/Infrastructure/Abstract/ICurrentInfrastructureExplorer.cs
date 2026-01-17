using Hetzner.Container.Management.Schemas.Infrastructure;

namespace Hetzner.Container.Management.Services.Infrastructure.Abstract;

public interface ICurrentInfrastructureExplorer
{
    Task<InfrastructureDocument> GetCurrentInfrastructureDocumentAsync(CancellationToken cancellationToken = default);

    Task ReplaceCurrentInfrastructureAsync(InfrastructureDocument infrastructureDocument,
        CancellationToken cancellationToken = default);
}