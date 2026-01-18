namespace Hetzner.Container.Management.Schemas.Input;

public sealed record DockerHubDetailsWithRepositoryName: DockerHubDetails
{
    public required string RepositoryName { get; init; }
}