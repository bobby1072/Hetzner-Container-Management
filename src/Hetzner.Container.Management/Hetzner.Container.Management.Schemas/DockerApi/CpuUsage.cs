using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record CpuUsage
{
    [JsonPropertyName("total_usage")]
    public long TotalUsage { get; init; }
    [JsonPropertyName("percpu_usage")]
    public long[]? PerCpuUsage { get; init; }
}
