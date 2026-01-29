namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record MountImageOptions
{
    public string? Reference { get; init; }
}
