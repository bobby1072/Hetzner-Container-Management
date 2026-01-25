using System.Net.Http.Json;
using BT.Common.Http.Extensions;
using BT.Common.Http.Models;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerEngineClient : BaseDockerClient, IDockerEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly DockerEngineApiSettings _dockerEngineApiSettings;
    private const string ApiVersion = "v1.52";

    public DockerEngineClient(
        HttpClient httpClient,
        DockerEngineApiSettings dockerEngineApiSettings,
        ILogger<DockerEngineClient> logger
    )
        : base(logger)
    {
        _httpClient = httpClient;
        _dockerEngineApiSettings = dockerEngineApiSettings;
    }

    public async Task<DockerApiActionResult<ContainerSummaryResponse[]?>> ListContainersAsync(
        bool all = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
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
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerSummaryResponse[]?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerSummaryResponse[]?>(ex, nameof(ListContainersAsync));
        }
    }

    public async Task<DockerApiActionResult> RemoveContainerAsync(
        string containerId,
        bool force = false,
        bool deleteVolumes = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (force)
            {
                builder = builder.AppendQueryParameter("force", "true");
            }

            if (deleteVolumes)
            {
                builder = builder.AppendQueryParameter("v", "true");
            }

            await builder.SendAsync(_httpClient, HttpMethod.Delete, cancellationToken);

            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(StartContainerAsync));
        }
    }

    public async Task<DockerApiActionResult> StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("start")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stop")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
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
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("pause")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("unpause")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("rename")
                .AppendQueryParameter("name", newName)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("update")
                .WithApplicationJson(updateRequest)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostJsonAsync<object>(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
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
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("restart")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(RestartContainerAsync));
        }
    }

    public async Task<DockerApiActionResult<ContainerCreateResponse?>> CreateContainerAsync(
        ContainerCreateRequest request,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment("create")
                .AppendQueryParameter("name", name)
                .WithApplicationJson(request)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            var data = await builder.PostJsonAsync<ContainerCreateResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerCreateResponse?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerCreateResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
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
            var builder = GetBaseUrl()
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
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerStatsResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
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
            var builder = GetBaseUrl()
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
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<string?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<string?>(ex, nameof(GetContainerLogsAsync));
        }
    }

    public async Task<DockerApiActionResult<ImageSummaryResponse[]?>> ListImagesAsync(
        bool all = false,
        string? filters = null,
        bool sharedSize = false,
        bool digests = false,
        bool manifests = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("images")
                .AppendPathSegment("json")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (all)
            {
                builder = builder.AppendQueryParameter("all", "true");
            }

            if (!string.IsNullOrWhiteSpace(filters))
            {
                builder = builder.AppendQueryParameter("filters", filters);
            }

            if (sharedSize)
            {
                builder = builder.AppendQueryParameter("shared-size", "true");
            }

            if (digests)
            {
                builder = builder.AppendQueryParameter("digests", "true");
            }

            if (manifests)
            {
                builder = builder.AppendQueryParameter("manifests", "true");
            }

            var data = await builder.GetJsonAsync<ImageSummaryResponse[]>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ImageSummaryResponse[]?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ImageSummaryResponse[]?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ImageSummaryResponse[]?>(ex, nameof(ListImagesAsync));
        }
    }

    public async Task<DockerApiActionResult<ImageInspectResponse?>> InspectImageAsync(
        string imageName,
        bool manifests = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("images")
                .AppendPathSegment(imageName)
                .AppendPathSegment("json")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (manifests)
            {
                builder = builder.AppendQueryParameter("manifests", "true");
            }

            var data = await builder.GetJsonAsync<ImageInspectResponse?>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ImageInspectResponse?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ImageInspectResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ImageInspectResponse?>(ex, nameof(InspectImageAsync));
        }
    }

    public async Task<DockerApiActionResult<ContainerInspectResponse?>> InspectContainerAsync(
        string containerId,
        bool size = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("json")
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            if (size)
            {
                builder = builder.AppendQueryParameter("size", "true");
            }

            var data = await builder.GetJsonAsync<ContainerInspectResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerInspectResponse?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerInspectResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerInspectResponse?>(ex, nameof(InspectContainerAsync));
        }
    }

    public async Task<DockerApiActionResult<VolumeCreateResponse?>> CreateVolumeAsync(
        VolumeCreateRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment("create")
                .WithApplicationJson(request)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            var data = await builder.PostJsonAsync<VolumeCreateResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<VolumeCreateResponse?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<VolumeCreateResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<VolumeCreateResponse?>(ex, nameof(CreateVolumeAsync));
        }
    }

    public async Task<DockerApiActionResult<VolumeInspectResponse?>> InspectVolumeAsync(
        string volumeName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment(volumeName)
                .AddErrorExtractor(x => ErrorExtractor(x, cancellationToken));

            var data = await builder.GetJsonAsync<VolumeInspectResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<VolumeInspectResponse?> { Data = data };
        }
        catch (BT.Common.Http.Exceptions.HttpRequestException ex)
        {
            return new DockerApiActionResult<VolumeInspectResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<VolumeInspectResponse?>(ex, nameof(InspectVolumeAsync));
        }
    }

    private HttpRequestBuilder GetBaseUrl() =>
        _dockerEngineApiSettings.UseTestHttpEndPoint
            ? _dockerEngineApiSettings.TestUnixHttpEndPoint.AppendPathSegment(ApiVersion)
            : "http://localhost".AppendPathSegment(ApiVersion);

    private static async Task<string?> ErrorExtractor(
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
