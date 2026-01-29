using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Input;

public sealed record VolumeInfo
{
    [JsonPropertyName("volumeName")]
    [JsonRequired]
    public required string VolumeName { get; init; }
    [JsonPropertyName("internalMountTarget")]
    [JsonRequired]
    public required string InternalMountTarget { get; init; }
}