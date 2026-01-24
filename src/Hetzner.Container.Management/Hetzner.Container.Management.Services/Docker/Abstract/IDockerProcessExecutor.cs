using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerProcessExecutor
{
    Task PullDockerImageFromHub(DockerHubDetails dockerHubDetails,
        string imageName,
        string versionTag,
        string @namespace,
        CancellationToken cancellationToken = default);

}