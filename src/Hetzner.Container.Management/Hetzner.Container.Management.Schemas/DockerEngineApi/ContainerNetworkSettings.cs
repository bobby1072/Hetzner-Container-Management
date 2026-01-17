namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record ContainerNetworkSettings
{
    public Dictionary<string, EndpointSettings>? Networks { get; init; }
}
