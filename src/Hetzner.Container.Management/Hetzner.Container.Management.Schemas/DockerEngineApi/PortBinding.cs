namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record PortBinding
{
    public string? HostPort { get; init; }
}
