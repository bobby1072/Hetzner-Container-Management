namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record VolumeInspectResponse
{
    public required string Name { get; init; }
    public required string Driver { get; init; }
    public required string Mountpoint { get; init; }
    public string? CreatedAt { get; init; }
    public Dictionary<string, string>? Status { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public string? Scope { get; init; }
    public ClusterVolume? ClusterVolume { get; init; }
    public Dictionary<string, string>? Options { get; init; }
    public VolumeUsageData? UsageData { get; init; }
}
