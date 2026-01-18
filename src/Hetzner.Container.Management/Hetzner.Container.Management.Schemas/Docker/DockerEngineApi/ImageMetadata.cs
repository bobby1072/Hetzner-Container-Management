namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageMetadata
{
    public string? LastTagTime { get; init; }
}
