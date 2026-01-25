namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record VolumeCreateRequest
{
    public string? Name { get; init; }
    public string? Driver { get; init; }
    public Dictionary<string, string>? DriverOpts { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public ClusterVolumeSpec? ClusterVolumeSpec { get; init; }
}
