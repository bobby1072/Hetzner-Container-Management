namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record RestartPolicy
{
    public required string Name { get; init; }
    public int MaximumRetryCount { get; init; }
}
