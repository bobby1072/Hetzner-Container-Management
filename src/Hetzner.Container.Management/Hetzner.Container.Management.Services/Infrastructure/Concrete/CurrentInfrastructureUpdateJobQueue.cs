using System.Threading.Channels;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;

namespace Hetzner.Container.Management.Services.Infrastructure.Concrete;

internal sealed class CurrentInfrastructureUpdateJobQueue: ICurrentInfrastructureUpdateJobQueue
{
    private readonly Channel<InfrastructureDocument> _infrastructureDocumentUpdateChannel = Channel.CreateUnbounded<InfrastructureDocument>();
    
    public Task EnqueueAsync(InfrastructureDocument infrastructureDocument, CancellationToken cancellationToken = default)
        => _infrastructureDocumentUpdateChannel.Writer.WriteAsync(infrastructureDocument, cancellationToken).AsTask();
    public async Task<InfrastructureDocument> DequeueAsync(CancellationToken cancellationToken = default)
        => await _infrastructureDocumentUpdateChannel.Reader.ReadAsync(cancellationToken);

    public void Dispose() => _infrastructureDocumentUpdateChannel.Writer.Complete();
}