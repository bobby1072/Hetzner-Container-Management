using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerProcessExecutor: IDisposable
{
    Task<string> LoginToDockerHub(DockerHubDetails details, string? workingDirectory = null,
        CancellationToken cancellationToken = default);
    
}