namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record Platform
{
    public string? Architecture { get; init; }
    public string? Os { get; init; }
    public string? OsVersion { get; init; }
    public string[]? OsFeatures { get; init; }
    public string? Variant { get; init; }
}
