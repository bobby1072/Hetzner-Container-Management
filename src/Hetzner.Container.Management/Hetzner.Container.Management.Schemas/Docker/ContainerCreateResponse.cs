using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record ContainerCreateResponse
{
    [JsonPropertyName("Id")]
    public required string Id { get; init; }

    [JsonPropertyName("Warnings")]
    public string[]? Warnings { get; init; }
}
