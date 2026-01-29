using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal interface IDockerEngineClient
{
    Task<DockerApiActionResult<ContainerSummaryResponse[]?>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    );

    Task<DockerApiActionResult> RemoveContainerAsync(
        string containerId,
        bool force = false,
        bool deleteVolumes = false,
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
        string name,
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
    Task<DockerApiActionResult<ImageSummaryResponse[]?>> ListImagesAsync(
        bool all = false,
        string? filters = null,
        bool sharedSize = false,
        bool digests = false,
        bool manifests = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<ImageInspectResponse?>> InspectImageAsync(
        string imageName,
        bool manifests = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<ContainerInspectResponse?>> InspectContainerAsync(
        string containerId,
        bool size = false,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<VolumeCreateResponse?>> CreateVolumeAsync(
        VolumeCreateRequest request,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult<VolumeInspectResponse?>> InspectVolumeAsync(
        string volumeName,
        CancellationToken cancellationToken = default
    );
    Task<DockerApiActionResult> RemoveVolumeAsync(
        string volumeName,
        bool force = false,
        CancellationToken cancellationToken = default
    );
}
