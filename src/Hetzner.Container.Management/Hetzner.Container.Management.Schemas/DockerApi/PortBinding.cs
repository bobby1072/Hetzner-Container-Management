namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record PortBinding
{
    public string? HostPort { get; init; }
}
