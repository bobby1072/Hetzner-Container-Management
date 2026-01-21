using System.Diagnostics;
using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Helpers;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

public sealed class DockerProcessExecutor: IDockerProcessExecutor
{
    private readonly Process _currentProcessWindow = new();
    private readonly string _dockerHubApiSettings;
    private readonly PlatformID _osPlatform = Environment.OSVersion.Platform;
    private readonly ILogger<DockerProcessExecutor> _logger;

    public DockerProcessExecutor(DockerHubApiSettings dockerHubApiSettings, ILogger<DockerProcessExecutor> logger)
    {
        _dockerHubApiSettings = dockerHubApiSettings.RegistryUri;
        _logger = logger;
        _currentProcessWindow.StartInfo = ProcessHelper.GetDefaultProcessStartInfo();
    }
    
    public void Dispose()
    {
        _currentProcessWindow.Dispose();
    }

    public async Task PullDockerImageFromHub(DockerHubDetails dockerHubDetails,
        string imageName,
        string versionTag,
        string @namespace,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"docker pull {@namespace}/{imageName}:{versionTag}";
        await LoginToDockerHub(dockerHubDetails, workingDirectory, cancellationToken);
        _currentProcessWindow.Start();
        
        await _currentProcessWindow.StandardInput.WriteLineAsync(command);
        
        await _currentProcessWindow.StandardInput.FlushAsync(cancellationToken);
        _currentProcessWindow.StandardInput.Close();

        var result = await Task.WhenAll(_currentProcessWindow.StandardOutput.ReadToEndAsync(cancellationToken),
            _currentProcessWindow.StandardError.ReadToEndAsync(cancellationToken));

        var errorOutput = result.Last();
        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            throw new ApiException(LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Unable to pull image from Docker Hub: {errorOutput}");
        }
    }
    private async Task<string> LoginToDockerHub(DockerHubDetails details, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting login to Docker Hub for user: {Username}", details.Username);

        string command = _osPlatform switch
        {
            PlatformID.Unix =>
                $"echo '{details.Password}' | docker login {_dockerHubApiSettings} -u {details.Username} --password-stdin",
            PlatformID.Win32NT =>
                $"Write-Output '{details.Password}' | docker login {_dockerHubApiSettings} -u {details.Username} --password-stdin",
            _ => throw new PlatformNotSupportedException("Unable to run cli commands on this platform.")
        };
        _currentProcessWindow.Start();

        
        await _currentProcessWindow.StandardInput.WriteLineAsync(command);
        
        await _currentProcessWindow.StandardInput.FlushAsync(cancellationToken);
        _currentProcessWindow.StandardInput.Close();

        var result = await Task.WhenAll(_currentProcessWindow.StandardOutput.ReadToEndAsync(cancellationToken),
            _currentProcessWindow.StandardError.ReadToEndAsync(cancellationToken));

        var errorOutput = result.Last();
        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            throw new InvalidOperationException($"Unable to login to Docker Hub: {errorOutput}");
        }
        
        var standardOutput = ProcessHelper.GetInnerStandardOutput(result.First(), command);
        _logger.LogInformation("Docker Hub login completed with: {StandardOutput}",
            standardOutput
        );
        
        return standardOutput;
    }
}