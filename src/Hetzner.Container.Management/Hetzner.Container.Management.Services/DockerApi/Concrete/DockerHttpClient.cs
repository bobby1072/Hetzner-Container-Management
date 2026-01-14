using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.DockerApi;
using Hetzner.Container.Management.Services.DockerApi.Abstract;

namespace Hetzner.Container.Management.Services.DockerApi.Concrete;

public sealed class DockerHttpClient : IDockerHttpClient
{
    private readonly HttpClient _httpClient;
    private const string ApiVersion = "v1.52";

    public DockerHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ContainerSummaryResponse[]> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    )
    {
        var builder = $"/{ApiVersion}/containers/json".AppendPathSegment(string.Empty);

        if (all)
        {
            builder = builder.AppendQueryParameter("all", "true");
        }

        return await builder.GetJsonAsync<ContainerSummaryResponse[]>(_httpClient, cancellationToken);
    }

    public async Task RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        var builder = $"/{ApiVersion}/containers"
            .AppendPathSegment(containerId)
            .AppendPathSegment("restart");

        await builder.PostStringAsync(_httpClient, cancellationToken);
    }

    public async Task<ContainerCreateResponse> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    )
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

    public async Task<ContainerStatsResponse> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        var builder = $"/{ApiVersion}/containers"
            .AppendPathSegment(containerId)
            .AppendPathSegment("stats")
            .AppendQueryParameter("stream", stream.ToString().ToLowerInvariant());

        return await builder.GetJsonAsync<ContainerStatsResponse>(_httpClient, cancellationToken);
    }
}
