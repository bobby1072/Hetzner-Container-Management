using Hetzner.Container.Management.Schemas.DockerEngineApi;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerEngineClient
{
    Task<DockerEngineActionResult<ContainerSummaryResponse[]?>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> StopContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> PauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> UnpauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult> UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult<ContainerCreateResponse?>> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult<ContainerStatsResponse?>> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerEngineActionResult<string?>> GetContainerLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool timestamps = false,
        CancellationToken cancellationToken = default
    );
}
