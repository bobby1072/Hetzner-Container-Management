using Hetzner.Container.Management.Schemas.DockerEngineApi;
using Hetzner.Container.Management.Schemas.DockerHubApi;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerHubClient
{
    Task<DockerApiActionResult<GetRepositoryResponse?>> GetRepositoryAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<RepositoryTag?>> GetRepositoryTagAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string tag,
        CancellationToken cancellationToken = default
    );
}
