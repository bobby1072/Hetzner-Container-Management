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

            return await builder.GetJsonAsync<ContainerSummaryResponse[]>(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception occurred during request to {BaseUrl}", _httpClient.BaseAddress);
            return null;
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
            _logger.LogError(ex, "Unexpected exception occurred during request to {BaseUrl}", _httpClient.BaseAddress);
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

            return await builder.PostJsonAsync<ContainerCreateResponse>(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception occurred during request to {BaseUrl}", _httpClient.BaseAddress);
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

            return await builder.GetJsonAsync<ContainerStatsResponse>(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception occurred during request to {BaseUrl}", _httpClient.BaseAddress);
            return null;
        }
    }
}
