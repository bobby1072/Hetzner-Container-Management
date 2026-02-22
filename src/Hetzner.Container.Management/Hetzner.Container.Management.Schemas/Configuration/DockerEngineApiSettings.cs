using BT.Common.Polly.Models.Concrete;

namespace Hetzner.Container.Management.Schemas.Configuration;

public sealed record DockerEngineApiSettings : PollyRetrySettings
{
    public required string UnixDomainSocketEndPoint { get; init; }
    public string TestUnixHttpEndPoint { get; init; } = string.Empty;
    public bool UseTestHttpEndPoint { get; init; }
    public string ApiVersion { get; init; } = "v1.47";
}
