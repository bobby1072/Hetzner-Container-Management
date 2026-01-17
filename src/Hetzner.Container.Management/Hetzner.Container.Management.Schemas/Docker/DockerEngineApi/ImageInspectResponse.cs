namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageInspectResponse
{
    public required string Id { get; init; }
    public ImageManifestDescriptor? Descriptor { get; init; }
    public ImageManifest[]? Manifests { get; init; }
    public string[] RepoTags { get; init; } = [];
    public string[] RepoDigests { get; init; } = [];
    public string Comment { get; init; } = string.Empty;
    public required string Created { get; init; }
    public string Author { get; init; } = string.Empty;
    public ImageConfig? Config { get; init; }
    public string Architecture { get; init; } = string.Empty;
    public string? Variant { get; init; }
    public string Os { get; init; } = string.Empty;
    public string? OsVersion { get; init; }
    public long Size { get; init; }
    public GraphDriver? GraphDriver { get; init; }
    public RootFS? RootFS { get; init; }
    public ImageMetadata? Metadata { get; init; }
}
