namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerSummaryResponse
{
    public required string Id { get; init; }
    public string[] Names { get; init; } = [];
    public required string Image { get; init; }
    public required string ImageID { get; init; }
    public ImageManifestDescriptor? ImageManifestDescriptor { get; init; }
    public required string Command { get; init; }
    public required string Created { get; init; }
    public Port[] Ports { get; init; } = [];
    public string? SizeRw { get; init; }
    public string? SizeRootFs { get; init; }
    public Dictionary<string, string> Labels { get; init; } = new();
    public required string State { get; init; }
    public required string Status { get; init; }
    public ContainerHostConfig? HostConfig { get; init; }
    public ContainerNetworkSettings? NetworkSettings { get; init; }
    public Mount[] Mounts { get; init; } = [];
    public ContainerHealth? Health { get; init; }
}
