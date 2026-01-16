using System.Text.Json;
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

    public async Task<DockerEngineActionResult<ContainerSummaryResponse[]>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = ApiVersion.AppendPathSegment("containers").AppendPathSegment("json");

            if (all)
            {
                builder = builder.AppendQueryParameter("all", "true");
            }

            var data = await builder.GetJsonAsync<ContainerSummaryResponse[]>(
                _httpClient,
                cancellationToken
            );
            return new DockerEngineActionResult<ContainerSummaryResponse[]> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError<ContainerSummaryResponse[]>(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError<ContainerSummaryResponse[]>(ex);
        }
    }

    public async Task<DockerEngineActionResult> StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("start");

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> StopContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stop");

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("kill");

            if (!string.IsNullOrWhiteSpace(signal))
            {
                builder = builder.AppendQueryParameter("signal", signal);
            }

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> PauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("pause");

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> UnpauseContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("unpause");

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("rename")
                .AppendQueryParameter("name", newName);

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> UpdateContainerAsync(
        string containerId,
        object updateRequest,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
            ArgumentNullException.ThrowIfNull(updateRequest);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("update")
                .WithApplicationJson(updateRequest);

            await builder.PostJsonAsync<object>(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult> RestartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("restart");

            await builder.PostStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<DockerEngineActionResult<ContainerCreateResponse>> CreateContainerAsync(
        ContainerCreateRequest request,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment("create")
                .WithApplicationJson(request);

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder = builder.AppendQueryParameter("name", name);
            }

            var data = await builder.PostJsonAsync<ContainerCreateResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerEngineActionResult<ContainerCreateResponse> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError<ContainerCreateResponse>(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError<ContainerCreateResponse>(ex);
        }
    }

    public async Task<DockerEngineActionResult<ContainerStatsResponse>> GetContainerStatsAsync(
        string containerId,
        bool stream = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stats")
                .AppendQueryParameter("stream", stream.ToString().ToLowerInvariant());

            var data = await builder.GetJsonAsync<ContainerStatsResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerEngineActionResult<ContainerStatsResponse> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError<ContainerStatsResponse>(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError<ContainerStatsResponse>(ex);
        }
    }

    public async Task<DockerEngineActionResult<string>> GetContainerLogsAsync(
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

            var builder = ApiVersion
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("logs")
                .AppendQueryParameter("stdout", stdout.ToString().ToLowerInvariant())
                .AppendQueryParameter("stderr", stderr.ToString().ToLowerInvariant())
                .AppendQueryParameter("timestamps", timestamps.ToString().ToLowerInvariant());

            var data = await builder.GetStringAsync(_httpClient, cancellationToken);
            return new DockerEngineActionResult<string> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = await ExtractApiErrorMessageAsync(ex);
            return HandleHttpError<string>(errorMessage);
        }
        catch (Exception ex)
        {
            return HandleError<string>(ex);
        }
    }

    private static async Task<string> ExtractApiErrorMessageAsync(HttpRequestException ex)
    {
        try
        {
            if (ex.StatusCode.HasValue)
            {
                // Try to read response content if available
                var message = ex.Message;
                // HttpRequestException message often contains the response body
                if (message.Contains("{") && message.Contains("message"))
                {
                    var startIndex = message.IndexOf("{");
                    var jsonPart = message.Substring(startIndex);
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(jsonPart);
                    if (errorResponse.TryGetProperty("message", out var messageProperty))
                    {
                        return messageProperty.GetString()
                            ?? $"HTTP {(int)ex.StatusCode.Value} error";
                    }
                }
                return $"HTTP {(int)ex.StatusCode.Value} error: {ex.Message}";
            }
        }
        catch
        {
            // Fall back to exception message
        }

        return ex.Message;
    }

    private DockerEngineActionResult HandleHttpError(string apiErrorMessage)
    {
        _logger.LogError(
            "HTTP error occurred during request to {BaseUrl}: {Message}",
            _httpClient.BaseAddress,
            apiErrorMessage
        );
        return new DockerEngineActionResult { ExceptionMessage = apiErrorMessage };
    }

    private DockerEngineActionResult<T> HandleHttpError<T>(string apiErrorMessage)
    {
        _logger.LogError(
            "HTTP error occurred during request to {BaseUrl}: {Message}",
            _httpClient.BaseAddress,
            apiErrorMessage
        );
        return new DockerEngineActionResult<T>
        {
            Data = default,
            ExceptionMessage = apiErrorMessage,
        };
    }

    private DockerEngineActionResult HandleError(Exception ex)
    {
        _logger.LogError(
            ex,
            "Unexpected exception occurred during request to {BaseUrl}",
            _httpClient.BaseAddress
        );
        return new DockerEngineActionResult { ExceptionMessage = ex.Message };
    }

    private DockerEngineActionResult<T> HandleError<T>(Exception ex)
    {
        _logger.LogError(
            ex,
            "Unexpected exception occurred during request to {BaseUrl}",
            _httpClient.BaseAddress
        );
        return new DockerEngineActionResult<T> { Data = default, ExceptionMessage = ex.Message };
    }
}
