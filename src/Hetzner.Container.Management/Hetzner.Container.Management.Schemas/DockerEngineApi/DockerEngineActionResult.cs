namespace Hetzner.Container.Management.Schemas.DockerEngineApi;

public record DockerEngineActionResult
{
    public string? ExceptionMessage { get; init; }
    public bool IsSuccess => string.IsNullOrWhiteSpace(ExceptionMessage);
}

public sealed record DockerEngineActionResult<T>: DockerEngineActionResult
{
    public T? Data { get; init; }
}

