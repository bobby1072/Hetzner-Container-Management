namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record GraphDriver
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, string>? Data { get; init; }
}
