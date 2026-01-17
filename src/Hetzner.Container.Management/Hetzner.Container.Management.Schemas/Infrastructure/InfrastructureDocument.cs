namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record InfrastructureDocument
{
    public object[] Components { get; init; } = [];
    public required DateTime LastUpdated
    {
        get => field.ToUniversalTime();
        init;
    }
    public required int UpdateNumber { get; init; }
}