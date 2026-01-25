namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record Mount
{
    public required string Target { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public bool ReadOnly { get; init; }
    public string? Consistency { get; init; }
    public MountBindOptions? BindOptions { get; init; }
    public MountVolumeOptions? VolumeOptions { get; init; }
    public MountImageOptions? ImageOptions { get; init; }
    public MountTmpfsOptions? TmpfsOptions { get; init; }
}
