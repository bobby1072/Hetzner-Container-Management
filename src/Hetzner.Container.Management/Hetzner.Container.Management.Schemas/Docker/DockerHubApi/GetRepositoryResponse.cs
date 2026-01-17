using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record GetRepositoryResponse
{
    [JsonPropertyName("name")]
    [JsonRequired]
    public required string Name { get; init; }

    [JsonPropertyName("namespace")]
    [JsonRequired]
    public required string Namespace { get; init; }

    [JsonPropertyName("repository_type")]
    [JsonRequired]
    public required string RepositoryType { get; init; }

    [JsonPropertyName("status")]
    [JsonRequired]
    public required int Status { get; init; }

    [JsonPropertyName("status_description")]
    public string StatusDescription { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("is_private")]
    [JsonRequired]
    public required bool IsPrivate { get; init; }

    [JsonPropertyName("is_automated")]
    [JsonRequired]
    public required bool IsAutomated { get; init; }

    [JsonPropertyName("star_count")]
    [JsonRequired]
    public required int StarCount { get; init; }

    [JsonPropertyName("pull_count")]
    [JsonRequired]
    public required int PullCount { get; init; }

    [JsonPropertyName("last_updated")]
    [JsonRequired]
    public required DateTimeOffset LastUpdated { get; init; }

    [JsonPropertyName("date_registered")]
    [JsonRequired]
    public required DateTimeOffset DateRegistered { get; init; }

    [JsonPropertyName("collaborator_count")]
    [JsonRequired]
    public required int CollaboratorCount { get; init; }

    [JsonPropertyName("hub_user")]
    [JsonRequired]
    public required string HubUser { get; init; }

    [JsonPropertyName("has_starred")]
    [JsonRequired]
    public required bool HasStarred { get; init; }

    [JsonPropertyName("full_description")]
    public string FullDescription { get; init; } = string.Empty;

    [JsonPropertyName("media_types")]
    public string[] MediaTypes { get; init; } = [];

    [JsonPropertyName("content_types")]
    public string[] ContentTypes { get; init; } = [];

    [JsonPropertyName("categories")]
    public string[] Categories { get; init; } = [];

    [JsonPropertyName("immutable_tags_settings")]
    [JsonRequired]
    public required ImmutableTagsSettings ImmutableTagsSettings { get; init; }

    [JsonPropertyName("storage_size")]
    public long? StorageSize { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }
}
