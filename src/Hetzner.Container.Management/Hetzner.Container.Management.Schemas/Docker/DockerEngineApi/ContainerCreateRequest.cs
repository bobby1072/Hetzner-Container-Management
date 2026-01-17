namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerCreateRequest
{
    public string? Hostname { get; init; }
    public string? Domainname { get; init; }
    public string? User { get; init; }
    public bool AttachStdin { get; init; }
    public bool AttachStdout { get; init; }
    public bool AttachStderr { get; init; }
    public bool Tty { get; init; }
    public bool OpenStdin { get; init; }
    public bool StdinOnce { get; init; }
    public string[]? Env { get; init; }
    public string[]? Cmd { get; init; }
    public string? Entrypoint { get; init; }
    public required string Image { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public Dictionary<string, object>? Volumes { get; init; }
    public string? WorkingDir { get; init; }
    public bool NetworkDisabled { get; init; }
    public Dictionary<string, object>? ExposedPorts { get; init; }
    public string? StopSignal { get; init; }
    public int? StopTimeout { get; init; }
    public HostConfig? HostConfig { get; init; }
    public NetworkingConfig? NetworkingConfig { get; init; }
}
