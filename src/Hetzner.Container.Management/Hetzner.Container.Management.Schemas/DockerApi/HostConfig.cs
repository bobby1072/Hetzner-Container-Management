namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record HostConfig
{
    public Dictionary<string, PortBinding[]>? PortBindings { get; init; }
    public RestartPolicy? RestartPolicy { get; init; }
}
