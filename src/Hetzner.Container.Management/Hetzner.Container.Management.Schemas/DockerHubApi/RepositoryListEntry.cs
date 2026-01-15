using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record RepositoryListEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }
    
    [JsonPropertyName("repository_type")]
    public string? RepositoryType { get; init; }
    
    [JsonPropertyName("status")]
    public int Status { get; init; }
    
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; init; }
    
    [JsonPropertyName("star_count")]
    public int StarCount { get; init; }
    
    [JsonPropertyName("pull_count")]
    public int PullCount { get; init; }
    
    [JsonPropertyName("last_updated")]
    public DateTime? LastUpdated { get; init; }
    
    [JsonPropertyName("last_modified")]
    public DateTime? LastModified { get; init; }
    
    [JsonPropertyName("date_registered")]
    public DateTime? DateRegistered { get; init; }
    
    [JsonPropertyName("affiliation")]
    public string? Affiliation { get; init; }
    
    [JsonPropertyName("media_types")]
    public string[]? MediaTypes { get; init; }
    
    [JsonPropertyName("content_types")]
    public string[]? ContentTypes { get; init; }
    
    [JsonPropertyName("storage_size")]
    public long? StorageSize { get; init; }
}
