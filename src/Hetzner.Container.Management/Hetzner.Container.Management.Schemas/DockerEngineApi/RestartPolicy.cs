namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record RestartPolicy
{
    public required string Name { get; init; }
    public int MaximumRetryCount { get; init; }
}
