using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerEngineClient
{
    Task<DockerApiActionResult<ContainerSummaryResponse[]?>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> StopContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> PauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> UnpauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<ContainerCreateResponse?>> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<ContainerStatsResponse?>> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<string?>> GetContainerLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool timestamps = false,
        CancellationToken cancellationToken = default
    );
}
