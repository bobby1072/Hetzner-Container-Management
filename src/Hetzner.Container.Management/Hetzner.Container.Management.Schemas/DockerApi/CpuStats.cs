using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record CpuStats
{
    [JsonPropertyName("cpu_usage")]
    public CpuUsage? CpuUsage { get; init; }
    [JsonPropertyName("system_cpu_usage")]
    public long SystemCpuUsage { get; init; }
    [JsonPropertyName("online_cpus")]
    public int OnlineCpus { get; init; }
}
