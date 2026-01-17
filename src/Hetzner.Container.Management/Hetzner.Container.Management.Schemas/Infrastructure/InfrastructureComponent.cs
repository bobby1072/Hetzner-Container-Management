using Hetzner.Container.Management.Schemas.DockerEngineApi;

namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record InfrastructureComponent
{
    public required string ContainerName { get; init; }
    public required string DockerhubNamespace { get; init; }
    public required string DockerhubName { get; init; }
    public required int PublicFacingPortNumber { get; init; }
    public required int InternalPortNumber { get; init; }
    public required string ImageVersionTag { get; init; }
    public required Dictionary<string, object?> ConfigMap { get; init; }
    public ContainerSummaryResponse? LatestContainerSummary { get; init; }
}