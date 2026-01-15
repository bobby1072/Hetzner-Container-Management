namespace Hetzner.Container.Management.Schemas.Input;

public sealed record DockerHubDetails
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string RepositoryName { get; init; }
}