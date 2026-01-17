namespace Hetzner.Container.Management.Schemas.Docker;

public record DockerApiActionResult
{
    public string? ExceptionMessage { get; init; }
    public bool IsSuccess => string.IsNullOrWhiteSpace(ExceptionMessage);
}

public sealed record DockerApiActionResult<T> : DockerApiActionResult
{
    public T? Data { get; init; }
}
