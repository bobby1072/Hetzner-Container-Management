namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record HealthLog
{
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
}
