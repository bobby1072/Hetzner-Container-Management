using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.DockerHubApi;

public sealed record AuthCreateTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
}
