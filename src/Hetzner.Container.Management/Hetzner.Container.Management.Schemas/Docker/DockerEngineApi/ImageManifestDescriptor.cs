using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageManifestDescriptor
{
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; init; }
    [JsonPropertyName("digest")]
    public string? Digest { get; init; }
    [JsonPropertyName("size")]
    public long? Size { get; init; }
    [JsonPropertyName("urls")]
    public string[]? Urls { get; init; }
    [JsonPropertyName("annotations")]
    public Dictionary<string, string>? Annotations { get; init; }
    [JsonPropertyName("data")]
    public object? Data { get; init; }
    [JsonPropertyName("platform")]
    public Platform? Platform { get; init; }
    [JsonPropertyName("artifactType")]
    public string? ArtifactType { get; init; }
}
