using Hetzner.Container.Management.Schemas.DockerEngineApi;

namespace Hetzner.Container.Management.Services.DockerEngineApi.Abstract;

internal interface IDockerEngineClient
{
    Task<ContainerSummaryResponse[]?> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );
    Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ContainerCreateResponse?> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    );
    Task<ContainerStatsResponse?> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    );
}
