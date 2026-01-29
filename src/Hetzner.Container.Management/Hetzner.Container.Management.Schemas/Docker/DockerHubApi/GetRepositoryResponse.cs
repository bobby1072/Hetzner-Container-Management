using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker.DockerHubApi;

public sealed record GetRepositoryResponse
{
    [JsonPropertyName("name")]
    [JsonRequired]
    public required string Name { get; init; }

    [JsonPropertyName("namespace")]
    [JsonRequired]
    public required string Namespace { get; init; }
}
