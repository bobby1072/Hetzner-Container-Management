using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record InfrastructureComponent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ContainerName { get; init; }
    public required string DockerhubNamespace { get; init; }
    public required string DockerhubName { get; init; }
    public string? PublicFacingPortNumber { get; init; }
    public string? InternalPortNumber { get; init; }
    public required string ImageVersionTag { get; init; }
    public required IReadOnlyDictionary<string, string?> ConfigMap { get; init; }
    public IReadOnlyDictionary<string, string?> Labels { get; init; } =
        new Dictionary<string, string?>();
    public string? VolumeName { get; init; }
    public DateTime LastUpdated
    {
        get => field.ToUniversalTime();
        init;
    } = DateTime.UtcNow;

    public ContainerInspectResponse? LatestContainerSummary { get; init; }

    public bool IsSame(InfrastructureComponent? other)
    {
        if (other is null)
            return false;

        return ContainerName == other.ContainerName
            && DockerhubName == other.DockerhubName
            && DockerhubNamespace == other.DockerhubNamespace
            && PublicFacingPortNumber == other.PublicFacingPortNumber
            && InternalPortNumber == other.InternalPortNumber
            && ImageVersionTag == other.ImageVersionTag
            && Labels.Count == other.Labels.Count
            && Labels.All(kv => other.Labels.TryGetValue(kv.Key, out var val) && kv.Value == val)
            && ConfigMap.Count == other.ConfigMap.Count
            && ConfigMap.All(kv =>
                other.ConfigMap.TryGetValue(kv.Key, out var val) && kv.Value == val
            );
    }
}
