using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record HostConfig
{
    [JsonPropertyName("PortBindings")]
    public Dictionary<string, PortBinding[]>? PortBindings { get; init; }

    [JsonPropertyName("RestartPolicy")]
    public RestartPolicy? RestartPolicy { get; init; }
}
