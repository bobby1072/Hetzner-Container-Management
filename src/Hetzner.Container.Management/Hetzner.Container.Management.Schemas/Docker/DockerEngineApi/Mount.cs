namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record Mount
{
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Source { get; init; }
    public string? Destination { get; init; }
    public string? Driver { get; init; }
    public string? Mode { get; init; }
    public bool? RW { get; init; }
    public string? Propagation { get; init; }
}
