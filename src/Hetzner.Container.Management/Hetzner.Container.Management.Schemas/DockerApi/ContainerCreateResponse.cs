namespace Hetzner.Container.Management.Schemas.DockerApi;

public sealed record ContainerCreateResponse
{
    public required string Id { get; init; }
    public string[]? Warnings { get; init; }
}
