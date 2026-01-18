namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record RootFS
{
    public string Type { get; init; } = string.Empty;
    public string[]? Layers { get; init; }
}
