namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerCreateResponse
{
    public required string Id { get; init; }
    public string[]? Warnings { get; init; }
}
