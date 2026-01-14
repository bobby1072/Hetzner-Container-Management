using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record NetworkStats
{
    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; init; }

    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; init; }
}
