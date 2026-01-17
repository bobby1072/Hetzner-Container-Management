namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerHostConfig
{
    public string? NetworkMode { get; init; }
    public Dictionary<string, string>? Annotations { get; init; }
}
