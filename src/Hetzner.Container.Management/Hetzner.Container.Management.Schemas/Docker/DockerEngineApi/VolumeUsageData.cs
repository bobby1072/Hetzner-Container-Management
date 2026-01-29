namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record VolumeUsageData
{
    public long Size { get; init; }
    public int RefCount { get; init; }
}
