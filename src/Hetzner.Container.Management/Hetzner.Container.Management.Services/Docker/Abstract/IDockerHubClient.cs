using Hetzner.Container.Management.Schemas.DockerHubApi;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerHubClient
{
    Task<PagedResponse<RepositoryListEntry>?> ListNamespaceRepositoriesAsync(
        string @namespace,
        int page = 1,
        int pageSize = 10,
        string? name = null,
        string? ordering = null,
        CancellationToken cancellationToken = default
    );

    Task<PagedResponse<RepositoryTag>?> ListRepositoryTagsAsync(
        string @namespace,
        string repository,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default
    );

    Task<RepositoryTag?> GetRepositoryTagAsync(
        string @namespace,
        string repository,
        string tag,
        CancellationToken cancellationToken = default
    );
}
