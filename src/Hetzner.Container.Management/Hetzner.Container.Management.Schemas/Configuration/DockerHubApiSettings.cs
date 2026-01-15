using BT.Common.Polly.Models.Concrete;

namespace Hetzner.Container.Management.Schemas.Configuration;

public sealed record DockerHubApiSettings: PollyRetrySettings
{
    public required string BaseUrl { get; init; }
    public required string Identifier { get; init; }
    public required string Secret { get; init; }
}