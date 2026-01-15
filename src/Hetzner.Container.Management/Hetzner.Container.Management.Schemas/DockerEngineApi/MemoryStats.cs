using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record MemoryStats
{
    [JsonPropertyName("usage")]
    public long Usage { get; init; }
    [JsonPropertyName("max_usage")]
    public long MaxUsage { get; init; }
    [JsonPropertyName("limit")]
    public long Limit { get; init; }
}
