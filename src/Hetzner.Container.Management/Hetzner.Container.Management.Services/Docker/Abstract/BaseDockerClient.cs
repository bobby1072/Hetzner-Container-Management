using Hetzner.Container.Management.Schemas.DockerEngineApi;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Abstract;

internal abstract class BaseDockerClient
{
    private readonly ILogger<BaseDockerClient> _logger;

    protected BaseDockerClient(ILogger<BaseDockerClient> logger)
    {
        _logger = logger;
    }
    
    protected DockerApiActionResult HandleError(Exception ex, string nameofAction)
    {
        _logger.LogError(
            ex,
            "Unexpected exception occurred during request to {ActionName}",
            nameofAction
        );
        return new DockerApiActionResult
        {
            ExceptionMessage = $"Failed to process {nameofAction} request",
        };
    }

    protected DockerApiActionResult<T> HandleError<T>(Exception ex, string nameofAction)
    {
        _logger.LogError(
            ex,
            "Unexpected exception occurred during request to {ActionName}",
            nameofAction
        );
        return new DockerApiActionResult<T>
        {
            Data = default,
            ExceptionMessage = $"Failed to process {nameofAction} request",
        };
    }
}