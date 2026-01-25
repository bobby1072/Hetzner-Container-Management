namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record MountTmpfsOptions
{
    public long? SizeBytes { get; init; }
    public int? Mode { get; init; }
    public Dictionary<string, string>? Options { get; init; }
}
