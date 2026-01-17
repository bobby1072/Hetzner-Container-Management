namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public sealed record Port
{
    public int PrivatePort { get; init; }
    public int? PublicPort { get; init; }
    public string? Type { get; init; }
}
