using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ErrorResponse
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
