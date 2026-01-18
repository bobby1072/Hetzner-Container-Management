namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record InfrastructureDocument
{
    public InfrastructureComponent[] Components { get; init; } = [];
    public DateTime LastUpdated
    {
        get => field.ToUniversalTime();
        init;
    } = DateTime.UtcNow;
    public required int UpdateNumber { get; init; }
}