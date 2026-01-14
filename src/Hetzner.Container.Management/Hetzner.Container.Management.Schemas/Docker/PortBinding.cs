using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record PortBinding
{
    [JsonPropertyName("HostPort")]
    public string? HostPort { get; init; }
}
