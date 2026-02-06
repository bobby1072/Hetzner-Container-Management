using BT.Common.Http.Extensions;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.Docker;
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
    }

    public async Task<DockerApiActionResult<GetRepositoryResponse?>> GetRepositoryAsync(
        DockerHubDetails dockerHubDetails,
        string? accessToken = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(dockerHubDetails.Namespace), dockerHubDetails.Namespace);
        activity?.SetTag(nameof(dockerHubDetails.RepositoryName), dockerHubDetails.RepositoryName);
        
        try
        {

            var builder = _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(dockerHubDetails.Namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(dockerHubDetails.RepositoryName);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
               builder = builder 
                .WithHeader(HeaderNames.Authorization, $"Bearer {accessToken}");
            }
            var result = await builder
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
        DockerHubDetails dockerHubDetails,
        string tag,
        string? accessToken = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(dockerHubDetails.Namespace), dockerHubDetails.Namespace);
        activity?.SetTag(nameof(dockerHubDetails.RepositoryName), dockerHubDetails.RepositoryName);
        activity?.SetTag(nameof(tag), tag);
        
        try
        {
            
            var builder = _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("namespaces")
                .AppendPathSegment(dockerHubDetails.Namespace)
                .AppendPathSegment("repositories")
                .AppendPathSegment(dockerHubDetails.RepositoryName)
                .AppendPathSegment("tags")
                .AppendPathSegment(tag)
                .WithHeader(HeaderNames.Authorization, $"Bearer {accessToken}");
                
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                builder = builder 
                    .WithHeader(HeaderNames.Authorization, $"Bearer {accessToken}");
            }
            var result = await builder
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

    public async Task<DockerApiActionResult<string?>> CreateAccessTokenAsync(
        string identifier,
        string secret,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(identifier), identifier);
        
        try
        {
            var cacheKey = GetDockerAccessTokenCacheKey(identifier);

            if (_memoryCache.TryGetValue<string>(cacheKey, out var cachedToken))
            {
                return new DockerApiActionResult<string?> { Data = cachedToken };
            }

            var request = new AuthCreateTokenRequest { Identifier = identifier, Secret = secret };

            var response = await _settings
                .BaseUrl.AppendPathSegment("v2")
                .AppendPathSegment("auth")
                .AppendPathSegment("token")
                .WithApplicationJson(request)
                .PostJsonAsync<AuthCreateTokenResponse>(_httpClient, cancellationToken);
            
            _memoryCache.Set(cacheKey, response.AccessToken, GetCacheOptions());

            return new DockerApiActionResult<string?> { Data = response.AccessToken };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<string?> { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError<string?>(ex, nameof(GetRepositoryTagAsync));
        }
    }
    private static MemoryCacheEntryOptions GetCacheOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(45),
    };
    private static string GetDockerAccessTokenCacheKey(string identifier) => $"dockerhub_access_token_{identifier}";
}
