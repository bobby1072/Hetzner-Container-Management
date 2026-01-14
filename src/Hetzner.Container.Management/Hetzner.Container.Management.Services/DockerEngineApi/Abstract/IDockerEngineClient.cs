using Hetzner.Container.Management.Schemas.DockerEngineApi;

namespace Hetzner.Container.Management.Services.DockerEngineApi.Abstract;

internal interface IDockerEngineClient
{
    /// <summary>
    /// Lists all containers
    /// </summary>
    /// <param name="all">Show all containers (default shows just running)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of container summaries</returns>
    Task<ContainerSummaryResponse[]> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Restarts a container
    /// </summary>
    /// <param name="containerId">Container ID or name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new container
    /// </summary>
    /// <param name="request">Container creation request</param>
    /// <param name="name">Optional container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Container creation response</returns>
    Task<ContainerCreateResponse> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets container statistics
    /// </summary>
    /// <param name="containerId">Container ID or name</param>
    /// <param name="stream">Stream statistics (default is false for single snapshot)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Container statistics</returns>
    Task<ContainerStatsResponse> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    );
}
