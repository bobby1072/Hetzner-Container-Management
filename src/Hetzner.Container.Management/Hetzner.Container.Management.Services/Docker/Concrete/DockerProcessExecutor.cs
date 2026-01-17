using System.Diagnostics;
using BT.Common.Helpers;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

public sealed class DockerProcessExecutor: IDockerProcessExecutor
{
    private readonly Process _currentProcessWindow = new();
    private readonly string _dockerHubRegistryUri;
    private readonly PlatformID OsPlatform = Environment.OSVersion.Platform;
    private readonly ILogger<DockerProcessExecutor> _logger;

    public DockerProcessExecutor(string dockerHubRegistryUri, ILogger<DockerProcessExecutor> logger)
    {
        _dockerHubRegistryUri = dockerHubRegistryUri;
        _logger = logger;
    }
    
    public void Dispose()
    {
        _currentProcessWindow.Dispose();
    }

    public async Task<string> LoginToDockerHub(DockerHubDetails details, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting login to Docker Hub for user: {Username}", details.Username);

        var startInfo = ProcessHelper.GetDefaultProcessStartInfo();

        _currentProcessWindow.StartInfo = startInfo;
        _currentProcessWindow.Start();

        var command =
            $"Write-Output '{details.Password}' | docker login {_dockerHubRegistryUri} -u {details.Username} --password-stdin";
        await _currentProcessWindow.StandardInput.WriteLineAsync(command);
        
        await _currentProcessWindow.StandardInput.FlushAsync(cancellationToken);
        _currentProcessWindow.StandardInput.Close();

        var result = await Task.WhenAll(_currentProcessWindow.StandardOutput.ReadToEndAsync(cancellationToken),
            _currentProcessWindow.StandardError.ReadToEndAsync(cancellationToken));

        var standardOutput = ProcessHelper.GetInnerStandardOutput(result.First(), command);
        
        _logger.LogInformation("Docker Hub login completed with: {StandardOutput} and {ErrorOutput}",
            standardOutput, 
            result.Last()
        );
        
        return standardOutput;
    }
}