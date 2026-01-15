using Hetzner.Container.Management.Schemas.DockerEngineApi;

namespace Hetzner.Container.Management.Services.DockerEngineApi.Abstract;

internal interface IDockerEngineClient
{
    Task<ContainerSummaryResponse[]?> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );
    Task StartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    );
    Task PauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task UnpauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    );
    Task UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    );
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
    Task<string?> GetContainerLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool timestamps = false,
        CancellationToken cancellationToken = default
    );
}
