namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerCreateRequest
{
    public required string Image { get; init; }
    public string[]? Cmd { get; init; }
    public string[]? Env { get; init; }
    public Dictionary<string, object>? ExposedPorts { get; init; }
    public HostConfig? HostConfig { get; init; }
}
