namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record MountVolumeOptions
{
    public bool? NoCopy { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public MountVolumeDriverConfig? DriverConfig { get; init; }
    public string? Subpath { get; init; }
}
