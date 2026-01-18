namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record NetworkingConfig
{
    public Dictionary<string, EndpointSettings>? EndpointsConfig { get; init; }
}
