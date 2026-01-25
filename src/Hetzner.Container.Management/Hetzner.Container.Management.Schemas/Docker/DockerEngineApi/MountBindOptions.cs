namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record MountBindOptions
{
    public string? Propagation { get; init; }
    public bool? NonRecursive { get; init; }
    public bool? CreateMountpoint { get; init; }
    public bool? ReadOnlyNonRecursive { get; init; }
    public bool? ReadOnlyForceRecursive { get; init; }
}
