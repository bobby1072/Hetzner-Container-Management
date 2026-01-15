using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record AuthCreateTokenRequest
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }
    
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }
}
