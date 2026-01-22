using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerHubClient
{
    Task<DockerApiActionResult<GetRepositoryResponse?>> GetRepositoryAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string accessToken,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<RepositoryTag?>> GetRepositoryTagAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string tag,
        string accessToken,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<string?>> CreateAccessTokenAsync(
        string identifier,
        string secret,
        CancellationToken cancellationToken = default
    );
}
