namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record ContainerSummaryResponse
{
    public required string Id { get; init; }
    public string[] Names { get; init; } = [];
    public required string Image { get; init; }
    public required string ImageId { get; init; }
    public required string Command { get; init; }
    public required long Created { get; init; }
    public required string State { get; init; }
    public required string Status { get; init; }
}
