using BT.Common.Polly.Models.Concrete;

namespace Hetzner.Container.Management.Schemas.Configuration;

public sealed record DockerEngineApiSettings : PollyRetrySettings
{
    public required string UnixDomainSocketEndPoint { get; init; }
}