using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.DockerEngineApi;
using Hetzner.Container.Management.Services.DockerEngineApi.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.DockerEngineApi.Concrete;

internal sealed class DockerEngineClient : IDockerEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DockerEngineClient> _logger;
    private const string ApiVersion = "v1.52";

    public DockerEngineClient(HttpClient httpClient, ILogger<DockerEngineClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ContainerSummaryResponse[]?> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = $"/{ApiVersion}/containers/json".AppendPathSegment(string.Empty);

            if (all)
            {
                builder = builder.AppendQueryParameter("all", "true");
            }

            return await builder.GetJsonAsync<ContainerSummaryResponse[]>(
                _httpClient,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
            return null;
        }
    }

    public async Task StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("start");

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task StopContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("stop");

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("kill");

            if (!string.IsNullOrWhiteSpace(signal))
            {
                builder = builder.AppendQueryParameter("signal", signal);
            }

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task PauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("pause");

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task UnpauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("unpause");

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("rename")
                .AppendQueryParameter("name", newName);

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentNullException.ThrowIfNull(updateRequest);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("update")
                .WithApplicationJson(updateRequest);

            await builder.PostJsonAsync<object>(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("restart");

            await builder.PostStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
        }
    }

    public async Task<ContainerCreateResponse?> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var builder = $"/{ApiVersion}/containers/create"
                .AppendPathSegment(string.Empty)
                .WithApplicationJson(request);

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder = builder.AppendQueryParameter("name", name);
            }

            return await builder.PostJsonAsync<ContainerCreateResponse>(
                _httpClient,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
            return null;
        }
    }

    public async Task<ContainerStatsResponse?> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("stats")
                .AppendQueryParameter("stream", stream.ToString().ToLowerInvariant());

            return await builder.GetJsonAsync<ContainerStatsResponse>(
                _httpClient,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
            return null;
        }
    }

    public async Task<string?> GetContainerLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool timestamps = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = $"/{ApiVersion}/containers"
                .AppendPathSegment(containerId)
                .AppendPathSegment("logs")
                .AppendQueryParameter("stdout", stdout.ToString().ToLowerInvariant())
                .AppendQueryParameter("stderr", stderr.ToString().ToLowerInvariant())
                .AppendQueryParameter("timestamps", timestamps.ToString().ToLowerInvariant());

            return await builder.GetStringAsync(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _httpClient.BaseAddress
            );
            return null;
        }
    }
}
