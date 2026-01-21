namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record EndpointSettings
{
    public IPAMConfig? IPAMConfig { get; init; }
    public string[]? Links { get; init; }
    public string? MacAddress { get; init; }
    public string[]? Aliases { get; init; }
    public Dictionary<string, string>? DriverOpts { get; init; }
    public int? GwPriority { get; init; }
    public string? NetworkID { get; init; }
    public string? EndpointID { get; init; }
    public string? Gateway { get; init; }
    public string? IPAddress { get; init; }
    public int? IPPrefixLen { get; init; }
    public string? IPv6Gateway { get; init; }
    public string? GlobalIPv6Address { get; init; }
    public int? GlobalIPv6PrefixLen { get; init; }
    public string[]? DNSNames { get; init; }
}
