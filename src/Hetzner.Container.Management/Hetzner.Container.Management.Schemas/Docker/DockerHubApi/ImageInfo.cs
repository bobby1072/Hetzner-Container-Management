using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerHubApi;

public sealed record ImageInfo
{
    [JsonPropertyName("architecture")]
    public string? Architecture { get; init; }

    [JsonPropertyName("features")]
    public string? Features { get; init; }

    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    [JsonPropertyName("digest")]
    public string? Digest { get; init; }

    [JsonPropertyName("layers")]
    public string[]? Layers { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }

    [JsonPropertyName("os_features")]
    public string? OsFeatures { get; init; }

    [JsonPropertyName("os_version")]
    public string? OsVersion { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("last_pulled")]
    public DateTime? LastPulled { get; init; }

    [JsonPropertyName("last_pushed")]
    public DateTime? LastPushed { get; init; }
}
