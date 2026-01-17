using Hetzner.Container.Management.Schemas.DockerHubApi;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerHubClient
{
    Task<PagedResponse<RepositoryListEntry>?> ListNamespaceRepositoriesAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        int page = 1,
        int pageSize = 10,
        string? name = null,
        string? ordering = null,
        CancellationToken cancellationToken = default
    );

    Task<PagedResponse<RepositoryTag>?> ListRepositoryTagsAsync(        
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default
    );

    Task<RepositoryTag?> GetRepositoryTagAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string tag,
        CancellationToken cancellationToken = default
    );
}
