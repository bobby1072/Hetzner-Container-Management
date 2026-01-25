namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ClusterVolumeSpec
{
    public string? Group { get; init; }
    public object? AccessMode { get; init; }
}
