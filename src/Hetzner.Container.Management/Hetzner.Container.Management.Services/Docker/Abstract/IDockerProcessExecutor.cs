using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerProcessExecutor: IDisposable
{
    Task<string> PullDockerImageFromHub(DockerHubDetails dockerHubDetails,
        string imageName,
        string versionTag,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

}