using System.Globalization;

namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record RawInfrastructureDocument
{
    public IReadOnlyCollection<InfrastructureComponent> Components { get; init; } = [];
    public string? LastUpdated { get; init; }
    public int UpdateNumber { get; init; } = 0;

    public InfrastructureDocument ToActualDocument()
    {
        return new InfrastructureDocument
        {
            UpdateNumber = UpdateNumber,
            Components = Components,
            LastUpdated = DateTime.TryParse(
                LastUpdated,
                CultureInfo.InvariantCulture,
                out var foundDate
            )
                ? foundDate.ToUniversalTime()
                : DateTime.UtcNow,
        };
    }
}
