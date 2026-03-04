namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerPruneResponse
{
    public string[]? ContainersDeleted { get; init; }
    public long SpaceReclaimed { get; init; }
}
