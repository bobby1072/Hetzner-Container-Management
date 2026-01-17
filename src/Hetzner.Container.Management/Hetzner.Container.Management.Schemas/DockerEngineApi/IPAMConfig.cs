namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record IPAMConfig
{
    public string? IPv4Address { get; init; }
    public string? IPv6Address { get; init; }
    public string[]? LinkLocalIPs { get; init; }
}
