namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerStorage
{
    public ContainerRootFS? RootFS { get; init; }
}

public sealed record ContainerRootFS
{
    public ContainerSnapshot? Snapshot { get; init; }
}

public sealed record ContainerSnapshot
{
    public string Name { get; init; } = string.Empty;
}
