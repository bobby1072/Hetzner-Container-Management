using System.Net.Http.Json;
using BT.Common.Http.Extensions;
using BT.Common.Http.Models;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Microsoft.Extensions.Logging;
using HttpRequestException = BT.Common.Http.Exceptions.HttpRequestException;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

internal sealed class DockerEngineClient : BaseDockerClient, IDockerEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly DockerEngineApiSettings _dockerEngineApiSettings;

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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(all), all);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment("json")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (all)
            {
                builder = builder.AppendQueryParameter("all", true.ToString().ToLowerInvariant());
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(force), force);
        activity?.SetTag(nameof(deleteVolumes), deleteVolumes);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (force)
            {
                builder = builder.AppendQueryParameter("force", true.ToString().ToLowerInvariant());
            }

            if (deleteVolumes)
            {
                builder = builder.AppendQueryParameter("v", true.ToString().ToLowerInvariant());
            }

            await builder.SendAsync(_httpClient, HttpMethod.Delete, cancellationToken);

            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(SetContainerRunningAsync));
        }
    }

    public async Task<DockerApiActionResult> SetContainerRunningAsync(
        string containerId,
        bool running = true,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(running), running);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment(running ? "start" : "stop")
                .AddAsyncErrorExtractor(ErrorExtractor);

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(SetContainerRunningAsync));
        }
    }

    public async Task<DockerApiActionResult> KillContainerAsync(
        string containerId,
        string? signal = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(signal), signal);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("kill")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (!string.IsNullOrWhiteSpace(signal))
            {
                builder = builder.AppendQueryParameter("signal", signal);
            }

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
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

    public async Task<DockerApiActionResult> SetContainerPausedAsync(
        string containerId,
        bool paused = true,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(paused), paused);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment(paused ? "pause" : "unpause")
                .AddAsyncErrorExtractor(ErrorExtractor);

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(SetContainerPausedAsync));
        }
    }

    public async Task<DockerApiActionResult> RenameContainerAsync(
        string containerId,
        string newName,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(newName), newName);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("rename")
                .AppendQueryParameter("name", newName)
                .AddAsyncErrorExtractor(ErrorExtractor);

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("update")
                .WithApplicationJson(updateRequest)
                .AddAsyncErrorExtractor(ErrorExtractor);

            await builder.PostJsonAsync<object>(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("restart")
                .AddAsyncErrorExtractor(ErrorExtractor);

            await builder.PostAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(name), name);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment("create")
                .AppendQueryParameter("name", name)
                .WithApplicationJson(request)
                .AddAsyncErrorExtractor(ErrorExtractor);

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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(stream), stream);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("stats")
                .AppendQueryParameter("stream", stream.ToString().ToLowerInvariant())
                .AddAsyncErrorExtractor(ErrorExtractor);

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
        long? since = null,
        long? until = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(stdout), stdout);
        activity?.SetTag(nameof(stderr), stderr);
        activity?.SetTag(nameof(since), since);
        activity?.SetTag(nameof(until), until);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("logs")
                .AppendQueryParameter("stdout", stdout.ToString().ToLowerInvariant())
                .AppendQueryParameter("stderr", stderr.ToString().ToLowerInvariant())
                .AppendQueryParameter("timestamps", true.ToString().ToLowerInvariant())
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (since.HasValue)
            {
                builder = builder.AppendQueryParameter("since", since.Value.ToString());
            }

            if (until.HasValue)
            {
                builder = builder.AppendQueryParameter("until", until.Value.ToString());
            }

            var data = await builder.GetStringAsync(_httpClient, cancellationToken);
            return new DockerApiActionResult<string?> { Data = data };
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(all), all);
        activity?.SetTag(nameof(filters), filters);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("images")
                .AppendPathSegment("json")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (all)
            {
                builder = builder.AppendQueryParameter("all", true.ToString().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(filters))
            {
                builder = builder.AppendQueryParameter("filters", filters);
            }

            if (sharedSize)
            {
                builder = builder.AppendQueryParameter(
                    "shared-size",
                    true.ToString().ToLowerInvariant()
                );
            }

            if (digests)
            {
                builder = builder.AppendQueryParameter(
                    "digests",
                    true.ToString().ToLowerInvariant()
                );
            }

            if (manifests)
            {
                builder = builder.AppendQueryParameter(
                    "manifests",
                    true.ToString().ToLowerInvariant()
                );
            }

            var data = await builder.GetJsonAsync<ImageSummaryResponse[]>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ImageSummaryResponse[]?> { Data = data };
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(imageName), imageName);
        activity?.SetTag(nameof(manifests), manifests);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("images")
                .AppendPathSegment(imageName)
                .AppendPathSegment("json")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (manifests)
            {
                builder = builder.AppendQueryParameter(
                    "manifests",
                    true.ToString().ToLowerInvariant()
                );
            }

            var data = await builder.GetJsonAsync<ImageInspectResponse?>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ImageInspectResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);
        activity?.SetTag(nameof(size), size);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment(containerId)
                .AppendPathSegment("json")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (size)
            {
                builder = builder.AppendQueryParameter("size", true.ToString().ToLowerInvariant());
            }

            var data = await builder.GetJsonAsync<ContainerInspectResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerInspectResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment("create")
                .WithApplicationJson(request)
                .AddAsyncErrorExtractor(ErrorExtractor);

            var data = await builder.PostJsonAsync<VolumeCreateResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<VolumeCreateResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
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
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(volumeName), volumeName);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment(volumeName)
                .AddAsyncErrorExtractor(ErrorExtractor);

            var data = await builder.GetJsonAsync<VolumeInspectResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<VolumeInspectResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
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

    public async Task<DockerApiActionResult> RemoveVolumeAsync(
        string volumeName,
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(volumeName), volumeName);
        activity?.SetTag(nameof(force), force);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment(volumeName)
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (force)
            {
                builder = builder.AppendQueryParameter("force", true.ToString().ToLowerInvariant());
            }

            await builder.SendAsync(_httpClient, HttpMethod.Delete, cancellationToken);
            return new DockerApiActionResult();
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError(ex, nameof(RemoveVolumeAsync));
        }
    }

    public async Task<DockerApiActionResult<ImagePruneResponse?>> DeleteUnusedImages(
        string? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(filters), filters);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("images")
                .AppendPathSegment("prune")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (!string.IsNullOrWhiteSpace(filters))
            {
                builder = builder.AppendQueryParameter("filters", filters);
            }

            var data = await builder.PostJsonAsync<ImagePruneResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ImagePruneResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<ImagePruneResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ImagePruneResponse?>(ex, nameof(DeleteUnusedImages));
        }
    }

    public async Task<DockerApiActionResult<ContainerPruneResponse?>> DeleteStoppedContainers(
        string? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(filters), filters);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("containers")
                .AppendPathSegment("prune")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (!string.IsNullOrWhiteSpace(filters))
            {
                builder = builder.AppendQueryParameter("filters", filters);
            }

            var data = await builder.PostJsonAsync<ContainerPruneResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<ContainerPruneResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<ContainerPruneResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<ContainerPruneResponse?>(ex, nameof(DeleteStoppedContainers));
        }
    }

    public async Task<DockerApiActionResult<VolumePruneResponse?>> DeleteUnusedVolumes(
        string? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(filters), filters);

        try
        {
            var builder = GetBaseUrl()
                .AppendPathSegment("volumes")
                .AppendPathSegment("prune")
                .AddAsyncErrorExtractor(ErrorExtractor);

            if (!string.IsNullOrWhiteSpace(filters))
            {
                builder = builder.AppendQueryParameter("filters", filters);
            }

            var data = await builder.PostJsonAsync<VolumePruneResponse>(
                _httpClient,
                cancellationToken
            );
            return new DockerApiActionResult<VolumePruneResponse?> { Data = data };
        }
        catch (HttpRequestException ex)
        {
            return new DockerApiActionResult<VolumePruneResponse?>
            {
                ExceptionMessage = ex.Message,
                StatusCode = ex.HttpStatusCode,
            };
        }
        catch (Exception ex)
        {
            return HandleError<VolumePruneResponse?>(ex, nameof(DeleteUnusedVolumes));
        }
    }

    private HttpRequestBuilder GetBaseUrl() =>
        _dockerEngineApiSettings.UseTestHttpEndPoint
            ? _dockerEngineApiSettings.TestUnixHttpEndPoint.AppendPathSegment(
                _dockerEngineApiSettings.ApiVersion
            )
            : "http://localhost".AppendPathSegment(_dockerEngineApiSettings.ApiVersion);

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
