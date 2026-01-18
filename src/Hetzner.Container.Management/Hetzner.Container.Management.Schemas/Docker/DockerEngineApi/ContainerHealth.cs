namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerHealth
{
    public string? Status { get; init; }
    public int? FailingStreak { get; init; }
}
