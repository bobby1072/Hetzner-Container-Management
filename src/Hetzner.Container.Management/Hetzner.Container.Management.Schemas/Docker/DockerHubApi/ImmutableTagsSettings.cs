using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record ImmutableTagsSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }
    [JsonPropertyName("rules")]
    public string[] Rules { get; init; } = [];
}