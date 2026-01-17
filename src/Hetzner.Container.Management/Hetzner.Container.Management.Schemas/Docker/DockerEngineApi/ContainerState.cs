namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerState
{
    public string Status { get; init; } = string.Empty;
    public bool Running { get; init; }
    public bool Paused { get; init; }
    public bool Restarting { get; init; }
    public bool OOMKilled { get; init; }
    public bool Dead { get; init; }
    public int Pid { get; init; }
    public int ExitCode { get; init; }
    public string Error { get; init; } = string.Empty;
    public string StartedAt { get; init; } = string.Empty;
    public string FinishedAt { get; init; } = string.Empty;
    public ContainerHealthStatus? Health { get; init; }
}
