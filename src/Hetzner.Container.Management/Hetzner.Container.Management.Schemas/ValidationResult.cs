namespace Hetzner.Container.Management.Schemas;

public sealed record ValidationResult
{
    public string[] Errors { get; init; } = [];
    public bool IsValid { get; init; }
}