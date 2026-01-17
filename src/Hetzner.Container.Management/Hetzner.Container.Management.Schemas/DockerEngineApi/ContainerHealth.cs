namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record ContainerHealth
{
    public string? Status { get; init; }
    public int? FailingStreak { get; init; }
}
