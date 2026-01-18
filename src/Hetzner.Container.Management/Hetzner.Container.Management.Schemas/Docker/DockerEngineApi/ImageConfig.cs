namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageConfig
{
    public string? User { get; init; }
    public Dictionary<string, object>? ExposedPorts { get; init; }
    public string[]? Env { get; init; }
    public string[]? Cmd { get; init; }
    public ImageHealthcheck? Healthcheck { get; init; }
    public bool ArgsEscaped { get; init; }
    public Dictionary<string, object>? Volumes { get; init; }
    public string? WorkingDir { get; init; }
    public string[]? Entrypoint { get; init; }
    public string[]? OnBuild { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public string? StopSignal { get; init; }
    public string[]? Shell { get; init; }
}

public sealed record ImageHealthcheck
{
    public string[]? Test { get; init; }
    public long Interval { get; init; }
    public long Timeout { get; init; }
    public int Retries { get; init; }
    public long StartPeriod { get; init; }
    public long StartInterval { get; init; }
}
