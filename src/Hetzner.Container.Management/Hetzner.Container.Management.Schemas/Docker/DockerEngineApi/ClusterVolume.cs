namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ClusterVolume
{
    public string? ID { get; init; }
    public object? Version { get; init; }
    public string? CreatedAt { get; init; }
    public string? UpdatedAt { get; init; }
    public object? Spec { get; init; }
    public object? Info { get; init; }
    public object[]? PublishStatus { get; init; }
}
