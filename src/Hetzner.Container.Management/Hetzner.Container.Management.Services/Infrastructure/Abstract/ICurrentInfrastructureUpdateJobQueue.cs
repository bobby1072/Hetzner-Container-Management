using Hetzner.Container.Management.Schemas.Infrastructure;

namespace Hetzner.Container.Management.Services.Infrastructure.Abstract;

internal interface ICurrentInfrastructureUpdateJobQueue: IDisposable
{
    Task EnqueueAsync(InfrastructureDocument infrastructureDocument, CancellationToken cancellationToken = default);
    Task<InfrastructureDocument> DequeueAsync(CancellationToken cancellationToken = default);
}