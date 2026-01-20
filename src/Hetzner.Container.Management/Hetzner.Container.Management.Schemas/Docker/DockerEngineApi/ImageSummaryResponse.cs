namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageSummaryResponse
{
    public required string Id { get; init; }
    public string ParentId { get; init; } = string.Empty;
    public string[] RepoTags { get; init; } = [];
    public string[] RepoDigests { get; init; } = [];
    public long Created { get; init; }
    public long Size { get; init; }
    public long? SharedSize { get; init; }
    public Dictionary<string, string>? Labels { get; init; }
    public int? Containers { get; init; }
    public ImageManifest[]? Manifests { get; init; }
    public ImageManifestDescriptor? Descriptor { get; init; }
}
