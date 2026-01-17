namespace Hetzner.Container.Management.Schemas.Input;

public sealed record DockerHubDetailsWithRepositoryName: DockerHubDetails
{
    public required string Namespace { get; init; }
    public required string RepositoryName { get; init; }
}