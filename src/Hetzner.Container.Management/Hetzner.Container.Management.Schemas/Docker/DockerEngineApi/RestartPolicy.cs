namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record RestartPolicy
{
    public required string Name { get; init; }
    public int MaximumRetryCount { get; init; }
}
