namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record ContainerHostConfig
{
    public string? NetworkMode { get; init; }
    public Dictionary<string, string>? Annotations { get; init; }
}
