using BT.Common.Polly.Models.Concrete;

namespace Hetzner.Container.Management.Schemas.Configuration;

public sealed record DockerApiSettings : PollyRetrySettings
{
    public required string UnixDomainSocketEndPoint { get; init; }
    public required string DockerApiUrl { get; init; }
}