namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerInspectNetworkSettings
{
    public string SandboxID { get; init; } = string.Empty;
    public string SandboxKey { get; init; } = string.Empty;
    public Dictionary<string, PortBinding[]?>? Ports { get; init; }
    public Dictionary<string, EndpointSettings>? Networks { get; init; }
}
