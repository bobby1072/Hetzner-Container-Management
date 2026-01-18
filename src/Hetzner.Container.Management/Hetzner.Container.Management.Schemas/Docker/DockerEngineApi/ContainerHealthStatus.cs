namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerHealthStatus
{
    public string Status { get; init; } = string.Empty;
    public int FailingStreak { get; init; }
    public HealthLog[] Log { get; init; } = [];
}
