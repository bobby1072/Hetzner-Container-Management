using System.Diagnostics;
using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerProcessExecutor : IDockerProcessExecutor
{
    private readonly ILogger<DockerProcessExecutor> _logger;
    public DockerProcessExecutor(
        ILogger<DockerProcessExecutor> logger
    )
    {
        _logger = logger;
    }

    public async Task PullDockerImageFromHub(
        DockerHubDetails dockerHubDetails,
        string imageName,
        string versionTag,
        string @namespace,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(imageName), imageName);
        activity?.SetTag(nameof(versionTag), versionTag);
        activity?.SetTag(nameof(@namespace), @namespace);
        
        if (!string.IsNullOrWhiteSpace(dockerHubDetails.Username) &&
            !string.IsNullOrWhiteSpace(dockerHubDetails.Password))
        {
            await LoginToDockerHub(dockerHubDetails, cancellationToken);
        }
        var command = $"pull {@namespace}/{imageName}:{versionTag}";
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        process.Start();
        
        var result = await Task.WhenAll(
            process.StandardOutput.ReadToEndAsync(cancellationToken),
            process.StandardError.ReadToEndAsync(cancellationToken)
        );

        await process.WaitForExitAsync(cancellationToken);
        var errorOutput = result.Last();
        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Unable to pull image from Docker Hub: {errorOutput}"
            );
        }
    }

    private async Task<string> LoginToDockerHub(
        DockerHubDetails details,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(details.Username), details.Username);
        
        _logger.LogInformation(
            "Attempting login to Docker Hub for user: {Username}",
            details.Username
        );
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"login -u {details.Username} -p {details.Password}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        process.Start();

        var result = await Task.WhenAll(
            process.StandardOutput.ReadToEndAsync(cancellationToken),
            process.StandardError.ReadToEndAsync(cancellationToken)
        );

        await process.WaitForExitAsync(cancellationToken);
        var errorOutput = result.Last();
        if (!string.IsNullOrWhiteSpace(errorOutput) && !errorOutput.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new InvalidOperationException($"Unable to login to Docker Hub: {errorOutput}");
        }
        
        var standardOutput = result.First();

        _logger.LogInformation("Docker Hub login completed with: {StandardOutput}", standardOutput);

        return standardOutput;
    }
}
