using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.DockerHubApi;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerHubClient : IDockerHubClient
{
    private readonly HttpClient _httpClient;
    private readonly DockerHubApiSettings _settings;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DockerHubClient> _logger;

    public DockerHubClient(
        HttpClient httpClient,
        DockerHubApiSettings settings,
        IMemoryCache memoryCache,
        ILogger<DockerHubClient> logger
    )
    {
        _httpClient = httpClient;
        _settings = settings;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<PagedResponse<RepositoryListEntry>?> ListNamespaceRepositoriesAsync(
        string @namespace,
        int page = 1,
        int pageSize = 10,
        string? name = null,
        string? ordering = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

            var token = await CreateAccessTokenAsync(
                _settings.Identifier,
                _settings.Secret,
                cancellationToken
            );

            var builder = _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(@namespace)
                .AppendPathSegment("repositories")
                .AppendQueryParameter("page", page.ToString())
                .AppendQueryParameter("page_size", pageSize.ToString())
                .WithHeader(HeaderNames.Authorization, $"Bearer {token}");

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder = builder.AppendQueryParameter("name", name);
            }

            if (!string.IsNullOrWhiteSpace(ordering))
            {
                builder = builder.AppendQueryParameter("ordering", ordering);
            }

            return await builder.GetJsonAsync<PagedResponse<RepositoryListEntry>>(
                _httpClient,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _settings.BaseUrl
            );
            return null;
        }
    }

    public async Task<PagedResponse<RepositoryTag>?> ListRepositoryTagsAsync(
        string @namespace,
        string repository,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);
            ArgumentException.ThrowIfNullOrWhiteSpace(repository);

            var token = await CreateAccessTokenAsync(
                _settings.Identifier,
                _settings.Secret,
                cancellationToken
            );

            var builder = _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(@namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(repository)
                .AppendPathSegment("tags")
                .AppendQueryParameter("page", page.ToString())
                .AppendQueryParameter("page_size", pageSize.ToString())
                .WithHeader(HeaderNames.Authorization, $"Bearer {token}");

            return await builder.GetJsonAsync<PagedResponse<RepositoryTag>>(
                _httpClient,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _settings.BaseUrl
            );
            return null;
        }
    }

    public async Task<RepositoryTag?> GetRepositoryTagAsync(
        string @namespace,
        string repository,
        string tag,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);
            ArgumentException.ThrowIfNullOrWhiteSpace(repository);
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);

            var token = await CreateAccessTokenAsync(
                _settings.Identifier,
                _settings.Secret,
                cancellationToken
            );

            var builder = _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(@namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(repository)
                .AppendPathSegment("tags")
                .AppendPathSegment(tag)
                .WithHeader(HeaderNames.Authorization, $"Bearer {token}");

            return await builder.GetJsonAsync<RepositoryTag>(_httpClient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected exception occurred during request to {BaseUrl}",
                _settings.BaseUrl
            );
            return null;
        }
    }

    private async Task<string> CreateAccessTokenAsync(
        string identifier,
        string secret,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var cacheKey = $"dockerhub_access_token_{identifier}";

        if (_memoryCache.TryGetValue<string>(cacheKey, out var cachedToken))
        {
            return cachedToken!;
        }

        var request = new AuthCreateTokenRequest { Identifier = identifier, Secret = secret };

        var builder = _settings
            .BaseUrl.AppendPathSegment("v2")
            .AppendPathSegment("auth")
            .AppendPathSegment("token")
            .WithApplicationJson(request);

        var response = await builder.PostJsonAsync<AuthCreateTokenResponse>(
            _httpClient,
            cancellationToken
        );

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45),
        };

        _memoryCache.Set(cacheKey, response.AccessToken, cacheOptions);

        return response.AccessToken;
    }
}
