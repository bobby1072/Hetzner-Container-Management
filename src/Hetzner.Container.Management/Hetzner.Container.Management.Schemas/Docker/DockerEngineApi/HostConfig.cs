namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record HostConfig
{
    public Dictionary<string, PortBinding[]>? PortBindings { get; init; }
    public RestartPolicy? RestartPolicy { get; init; }
}
