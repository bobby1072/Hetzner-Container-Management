namespace Hetzner.Container.Management.Schemas.Input;

public record DockerHubDetails
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}