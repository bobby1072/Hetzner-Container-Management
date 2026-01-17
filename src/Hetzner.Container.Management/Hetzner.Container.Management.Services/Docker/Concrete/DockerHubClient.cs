using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerHubClient : BaseDockerClient, IDockerHubClient
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
        : base(logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<DockerApiActionResult<GetRepositoryResponse?>> GetRepositoryAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var token = await CreateAccessTokenAsync(
                dockerHubDetails.Username,
                dockerHubDetails.Password,
                cancellationToken
            );

            var result = await _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(dockerHubDetails.Namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(dockerHubDetails.RepositoryName)
                .WithHeader(HeaderNames.Authorization, $"Bearer {token}")
                .GetJsonAsync<GetRepositoryResponse>(_httpClient, cancellationToken);

            return new DockerApiActionResult<GetRepositoryResponse?> { Data = result };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<GetRepositoryResponse?>
            {
                ExceptionMessage = ex.Message,
            };
        }
        catch (Exception ex)
        {
            return HandleError<GetRepositoryResponse?>(ex, nameof(GetRepositoryAsync));
        }
    }

    public async Task<DockerApiActionResult<RepositoryTag?>> GetRepositoryTagAsync(
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string tag,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var token = await CreateAccessTokenAsync(
                dockerHubDetails.Username,
                dockerHubDetails.Password,
                cancellationToken
            );

            var result = await _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(dockerHubDetails.Namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(dockerHubDetails.RepositoryName)
                .AppendPathSegment("tags")
                .AppendPathSegment(tag)
                .WithHeader(HeaderNames.Authorization, $"Bearer {token}")
                .GetJsonAsync<RepositoryTag>(_httpClient, cancellationToken);

            return new DockerApiActionResult<RepositoryTag?> { Data = result };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<RepositoryTag?> { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError<RepositoryTag?>(ex, nameof(GetRepositoryTagAsync));
        }
    }

    private async Task<string> CreateAccessTokenAsync(
        string identifier,
        string secret,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = $"dockerhub_access_token_{identifier}";

        if (_memoryCache.TryGetValue<string>(cacheKey, out var cachedToken))
        {
            return cachedToken!;
        }

        var request = new AuthCreateTokenRequest { Identifier = identifier, Secret = secret };

        var response = await _settings
            .BaseUrl.AppendPathSegment("v2")
            .AppendPathSegment("auth")
            .AppendPathSegment("token")
            .WithApplicationJson(request)
            .PostJsonAsync<AuthCreateTokenResponse>(_httpClient, cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45),
        };

        _memoryCache.Set(cacheKey, response.AccessToken, cacheOptions);

        return response.AccessToken;
    }
}
