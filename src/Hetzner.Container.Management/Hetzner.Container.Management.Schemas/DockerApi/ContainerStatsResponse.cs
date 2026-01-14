using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record ContainerStatsResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("cpu_stats")]
    public CpuStats? CpuStats { get; init; }
    [JsonPropertyName("precpu_stats")]
    public CpuStats? PreCpuStats { get; init; }
    [JsonPropertyName("memory_stats")]
    public MemoryStats? MemoryStats { get; init; }
    [JsonPropertyName("networks")]
    public Dictionary<string, NetworkStats>? Networks { get; init; }
}
