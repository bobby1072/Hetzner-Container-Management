using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerHubApi;

public sealed record AuthCreateTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
}
