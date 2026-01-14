using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record RestartPolicy
{
    [JsonPropertyName("Name")]
    public required string Name { get; init; }

    [JsonPropertyName("MaximumRetryCount")]
    public int MaximumRetryCount { get; init; }
}
