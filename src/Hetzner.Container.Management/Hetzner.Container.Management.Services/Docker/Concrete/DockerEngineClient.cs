using System.Net.Http.Json;
using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.DockerEngineApi;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerEngineClient : BaseDockerClient, IDockerEngineClient
{
    private readonly HttpClient _httpClient;
    private const string ApiVersion = "v1.52";

    public DockerEngineClient(HttpClient httpClient, ILogger<DockerEngineClient> logger)
        : base(logger)
    {
        _httpClient = httpClient;
    }

    public async Task<DockerApiActionResult<ContainerSummaryResponse[]?>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment("json")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (all)
            {
                builder = builder.AppendQueryParameter("all", "true");
            }

            var data = await builder.GetJsonAsync<ContainerSummaryResponse[]>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerSummaryResponse[]?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerSummaryResponse[]?>
            {
                ExceptionMessage = ex.Message,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerSummaryResponse[]?>(ex, nameof(ListContainersAsync));
        }
    }

    public async Task<DockerApiActionResult> StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("start")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(StartContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> StopContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stop")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(StopContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("kill")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (!string.IsNullOrWhiteSpace(signal))
            {
                builder = builder.AppendQueryParameter("signal", signal);
            }

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(KillContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> PauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("pause")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(PauseContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> UnpauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("unpause")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(UnpauseContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("rename")
                .AppendQueryParameter("name", newName)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(RenameContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentNullException.ThrowIfNull(updateRequest);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("update")
                .WithApplicationJson(updateRequest)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostJsonAsync<object>(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(UpdateContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("restart")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(RestartContainerAsync));
        }
    }

    public async Task<DockerApiActionResult<ContainerCreateResponse?>> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment("create")
                .WithApplicationJson(request)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder = builder.AppendQueryParameter("name", name);
            }

            var data = await builder.PostJsonAsync<ContainerCreateResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerCreateResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerCreateResponse?>
            {
                ExceptionMessage = ex.Message,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerCreateResponse?>(ex, nameof(CreateContainerAsync));
        }
    }

    public async Task<DockerApiActionResult<ContainerStatsResponse?>> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stats")
                .AppendQueryParameter("stream", stream.ToString().ToLowerInvariant())
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            var data = await builder.GetJsonAsync<ContainerStatsResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerStatsResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerStatsResponse?>
            {
                ExceptionMessage = ex.Message,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerStatsResponse?>(ex, nameof(GetContainerStatsAsync));
        }
    }

    public async Task<DockerApiActionResult<string?>> GetContainerLogsAsync(
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

            var builder = "http://localhost"
                .AppendPathSegment(ApiVersion)
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("logs")
                .AppendQueryParameter("stdout", stdout.ToString().ToLowerInvariant())
                .AppendQueryParameter("stderr", stderr.ToString().ToLowerInvariant())
                .AppendQueryParameter("timestamps", timestamps.ToString().ToLowerInvariant())
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            var data = await builder.GetStringAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult<string?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<string?> { ExceptionMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return HandleError<string?>(ex, nameof(GetContainerLogsAsync));
        }
    }

    private async Task<string?> ErrorExtractor(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var foundErrorMessage = await TryGetDataFromResponse<ErrorResponse>(
            response,
            cancellationToken
        );

        return foundErrorMessage?.Message;
    }

    private static async Task<T?> TryGetDataFromResponse<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
