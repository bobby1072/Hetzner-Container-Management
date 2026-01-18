namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerNetworkSettings
{
    public Dictionary<string, EndpointSettings>? Networks { get; init; }
}
