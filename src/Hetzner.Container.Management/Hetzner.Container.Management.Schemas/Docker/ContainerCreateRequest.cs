using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record ContainerCreateRequest
{
    [JsonPropertyName("Image")]
    public required string Image { get; init; }

    [JsonPropertyName("Cmd")]
    public string[]? Cmd { get; init; }

    [JsonPropertyName("Env")]
    public string[]? Env { get; init; }

    [JsonPropertyName("ExposedPorts")]
    public Dictionary<string, object>? ExposedPorts { get; init; }

    [JsonPropertyName("HostConfig")]
    public HostConfig? HostConfig { get; init; }
}
