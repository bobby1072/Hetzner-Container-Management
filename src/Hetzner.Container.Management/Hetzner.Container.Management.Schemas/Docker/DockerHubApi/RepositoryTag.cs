using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerHubApi;

public sealed record RepositoryTag
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("images")]
    public ImageInfo[]? Images { get; init; }

    [JsonPropertyName("creator")]
    public int? Creator { get; init; }

    [JsonPropertyName("last_updated")]
    public DateTime? LastUpdated { get; init; }

    [JsonPropertyName("last_updater")]
    public int? LastUpdater { get; init; }

    [JsonPropertyName("last_updater_username")]
    public string? LastUpdaterUsername { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("repository")]
    public int Repository { get; init; }

    [JsonPropertyName("full_size")]
    public long FullSize { get; init; }

    [JsonPropertyName("v2")]
    public string? V2 { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("tag_last_pulled")]
    public DateTime? TagLastPulled { get; init; }

    [JsonPropertyName("tag_last_pushed")]
    public DateTime? TagLastPushed { get; init; }
}
