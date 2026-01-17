namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record InfrastructureComponent
{
    public required string ContainerName { get; init; }
    public required int PublicFacingPortNumber { get; init; }
    public required string Version { get; init; }
    public required Dictionary<string, object?> ConfigMap { get; init; }
}