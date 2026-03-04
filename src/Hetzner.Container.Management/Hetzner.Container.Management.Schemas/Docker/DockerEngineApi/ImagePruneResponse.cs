namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImagePruneResponse
{
    public ImageDeleteItem[]? ImagesDeleted { get; init; }
    public long SpaceReclaimed { get; init; }
}

public sealed record ImageDeleteItem
{
    public string? Deleted { get; init; }
    public string? Untagged { get; init; }
}
