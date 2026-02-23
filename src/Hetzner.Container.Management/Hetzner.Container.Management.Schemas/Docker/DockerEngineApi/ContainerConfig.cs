namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerConfig
{
    public string? Hostname { get; init; }
    public string? Domainname { get; init; }
    public string? User { get; init; }
    public bool AttachStdin { get; init; }
    public bool AttachStdout { get; init; }
    public bool AttachStderr { get; init; }
    public Dictionary<string, object>? ExposedPorts { get; init; }
    public bool Tty { get; init; }
    public bool OpenStdin { get; init; }
    public bool StdinOnce { get; init; }
    public string[]? Env { get; init; }
    public string[]? Cmd { get; init; }
    public ImageHealthcheck? Healthcheck { get; init; }
    public bool ArgsEscaped { get; init; }
    public string? Image { get; init; }
    public Dictionary<string, object>? Volumes { get; init; }
    public string? WorkingDir { get; init; }
    public string[]? Entrypoint { get; init; }
    public bool NetworkDisabled { get; init; }
    public string[]? OnBuild { get; init; }
    public Dictionary<string, string?>? Labels { get; init; }
    public string? StopSignal { get; init; }
    public int? StopTimeout { get; init; }
    public string[]? Shell { get; init; }
}
