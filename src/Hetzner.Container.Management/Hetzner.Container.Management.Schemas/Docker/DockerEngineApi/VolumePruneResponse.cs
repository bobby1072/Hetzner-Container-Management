namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record VolumePruneResponse
{
    public string[]? VolumesDeleted { get; init; }
    public long SpaceReclaimed { get; init; }
}
