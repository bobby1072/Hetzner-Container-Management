using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record PagedResponse<T>
{
    [JsonPropertyName("count")]
    public int Count { get; init; }
    
    [JsonPropertyName("next")]
    public string? Next { get; init; }
    
    [JsonPropertyName("previous")]
    public string? Previous { get; init; }
    
    [JsonPropertyName("results")]
    public required T[] Results { get; init; }
}
