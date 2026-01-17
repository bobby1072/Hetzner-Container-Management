namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record ImageManifestDescriptor
{
    public string? MediaType { get; init; }
    public string? Digest { get; init; }
    public long? Size { get; init; }
    public string[]? Urls { get; init; }
    public Dictionary<string, string>? Annotations { get; init; }
    public object? Data { get; init; }
    public Platform? Platform { get; init; }
    public string? ArtifactType { get; init; }
}
