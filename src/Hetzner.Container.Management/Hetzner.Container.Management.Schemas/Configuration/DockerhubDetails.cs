namespace Hetzner.Container.Management.Schemas.Configuration;

public sealed record DockerHubDetails
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}