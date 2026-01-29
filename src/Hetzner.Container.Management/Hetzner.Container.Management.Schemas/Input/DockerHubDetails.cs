namespace Hetzner.Container.Management.Schemas.Input;

public sealed record DockerHubDetails
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public required string Namespace { get; init; }
    public required string RepositoryName { get; init; }
}