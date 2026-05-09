using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerHubClient
{
    Task<DockerApiActionResult<GetRepositoryResponse?>> GetRepositoryAsync(
        DockerHubDetails dockerHubDetails,
        string? accessToken = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<RepositoryTag?>> GetRepositoryTagAsync(
        DockerHubDetails dockerHubDetails,
        string tag,
        string? accessToken = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<string?>> CreateAccessTokenAsync(
        string identifier,
        string secret,
        CancellationToken cancellationToken = default
    );
}
