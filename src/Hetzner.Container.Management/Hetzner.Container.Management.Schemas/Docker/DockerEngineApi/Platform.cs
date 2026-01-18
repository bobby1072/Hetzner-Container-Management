using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record Platform
{
    [JsonPropertyName("Architecture")]
    public string? Architecture { get; init; }
    [JsonPropertyName("os")]
    public string? Os { get; init; }
    [JsonPropertyName("os.version")]
    public string? OsVersion { get; init; }
    [JsonPropertyName("os.features")]
    public string[]? OsFeatures { get; init; }
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }
}
