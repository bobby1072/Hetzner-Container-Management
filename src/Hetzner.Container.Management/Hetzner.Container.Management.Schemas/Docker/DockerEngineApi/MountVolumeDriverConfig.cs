namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record MountVolumeDriverConfig
{
    public string? Name { get; init; }
    public Dictionary<string, string>? Options { get; init; }
}
