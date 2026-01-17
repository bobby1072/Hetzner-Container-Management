namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record PortBinding
{
    public string? HostPort { get; init; }
}
