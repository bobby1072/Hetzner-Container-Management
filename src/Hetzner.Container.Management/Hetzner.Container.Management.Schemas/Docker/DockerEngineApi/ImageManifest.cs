namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ImageManifest
{
    public required string ID { get; init; }
    public required ImageManifestDescriptor Descriptor { get; init; }
    public bool Available { get; init; }
    public ManifestSize? Size { get; init; }
    public string? Kind { get; init; }
    public ImageData? ImageData { get; init; }
    public AttestationData? AttestationData { get; init; }
}

public sealed record ManifestSize
{
    public long Total { get; init; }
    public long Content { get; init; }
}

public sealed record ImageData
{
    public Platform? Platform { get; init; }
    public string[]? Containers { get; init; }
    public ImageSizeInfo? Size { get; init; }
}

public sealed record ImageSizeInfo
{
    public long Unpacked { get; init; }
}

public sealed record AttestationData
{
    public string? For { get; init; }
}
