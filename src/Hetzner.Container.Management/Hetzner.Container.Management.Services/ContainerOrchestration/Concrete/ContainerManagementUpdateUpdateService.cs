using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Helpers.Models;
using BT.Common.Polly.Extensions;
using BT.Common.Polly.Models.Concrete;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Extensions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementUpdateUpdateService : IContainerManagementUpdateService
{
    private readonly IServiceProvider _serviceProvider;
    private Lazy<IDockerHubClient> DockerHubClient =>
        new(_serviceProvider.GetRequiredService<IDockerHubClient>);
    private Lazy<IDockerEngineClient> DockerEngineClient =>
        new(_serviceProvider.GetRequiredService<IDockerEngineClient>);
    private Lazy<IDockerProcessExecutor> DockerProcessExecutor =>
        new(_serviceProvider.GetRequiredService<IDockerProcessExecutor>);
    private Lazy<ICurrentInfrastructureExplorer> CurrentInfrastructureExplorer =>
        new(_serviceProvider.GetRequiredService<ICurrentInfrastructureExplorer>);
    private readonly ILogger<ContainerManagementUpdateUpdateService> _logger;
    private static readonly PollyRetrySettings _commonOperationRetrySettings =
        new() { TotalAttempts = 2 };

    public ContainerManagementUpdateUpdateService(
        IServiceProvider serviceProvider,
        ILogger<ContainerManagementUpdateUpdateService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<InfrastructureComponent[]> UpdateCurrentInfrastructure(
        InfrastructureComponentUpdateInput[] infrastructureDocuments,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(infrastructureDocuments), infrastructureDocuments.Length);

        try
        {
            if (infrastructureDocuments.Length == 0)
            {
                throw new ApiException(
                    LogLevel.Information,
                    HttpStatusCode.BadRequest,
                    "Input list must have at least one object"
                );
            }

            var currentInfrastructureDocument =
                await CurrentInfrastructureExplorer.Value.TryGetCurrentInfrastructureDocumentAsync(
                    cancellationToken
                )
                ?? throw new ApiException(
                    LogLevel.Error,
                    HttpStatusCode.InternalServerError,
                    "Failed to retrieve current infrastructure infrastructure document."
                );

            var updateAttemptRetryPipeline = _commonOperationRetrySettings.ToPipeline();

            var updateInfraComponents = await Task.WhenAll(
                infrastructureDocuments.Select(x =>
                    updateAttemptRetryPipeline
                        .ExecuteAsync(
                            async ct => await UpdateInfrastructureComponentAsync(x, ct),
                            cancellationToken
                        )
                        .AsTask()
                )
            );

            var didUpdate = updateInfraComponents.Any(x =>
                currentInfrastructureDocument.Components.Any(y => !y.IsSame(x))
            );

            if (didUpdate)
            {
                var newInfraStructureDocument = currentInfrastructureDocument with
                {
                    LastUpdated = DateTime.UtcNow,
                    Components = currentInfrastructureDocument
                        .Components.Where(x =>
                            !updateInfraComponents
                                .Select(y => y.ContainerName)
                                .Contains(x.ContainerName)
                        )
                        .Concat(updateInfraComponents)
                        .DistinctBy(x => x.ContainerName)
                        .ToArray(),
                    UpdateNumber = currentInfrastructureDocument.UpdateNumber + 1,
                };

                await CurrentInfrastructureExplorer.Value.ReplaceCurrentInfrastructureAsync(
                    newInfraStructureDocument,
                    cancellationToken
                );
            }
            else if (currentInfrastructureDocument.Components.Count == 0)
            {
                var newInfraStructureDocument = currentInfrastructureDocument with
                {
                    LastUpdated = DateTime.UtcNow,
                    Components = updateInfraComponents,
                    UpdateNumber = currentInfrastructureDocument.UpdateNumber + 1,
                };

                await CurrentInfrastructureExplorer.Value.ReplaceCurrentInfrastructureAsync(
                    newInfraStructureDocument,
                    cancellationToken
                );
            }

            return updateInfraComponents;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unhandled exception occurred while updating the infrastructure component"
            );

            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                ApplicationConstants.ExceptionConstants.InternalError,
                ex
            );
        }
    }

    private async Task<InfrastructureComponent> UpdateInfrastructureComponentAsync(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(
            nameof(infrastructureComponentInput.ContainerName),
            infrastructureComponentInput.ContainerName
        );
        activity?.SetTag(
            nameof(infrastructureComponentInput.ImageTag),
            infrastructureComponentInput.ImageTag
        );

        using (
            _logger.BeginScope(
                new LoggingScopeVariableDictionary
                {
                    [nameof(InfrastructureComponentUpdateInput.ContainerName)] =
                        infrastructureComponentInput.ContainerName,
                    [nameof(InfrastructureComponentUpdateInput.ImageTag)] =
                        infrastructureComponentInput.ImageTag,
                    [nameof(InfrastructureComponentUpdateInput.DockerHubDetails.RepositoryName)] =
                        infrastructureComponentInput.DockerHubDetails.RepositoryName,
                }
            )
        )
        {
            try
            {
                _logger.LogInformation(
                    "Attempting to update the infrastructure component with details: {@InfrastructureDetails}",
                    infrastructureComponentInput
                );

                BasicValidateInput(infrastructureComponentInput);

                var dockerHubFetchedDetails = await GetDockerHubRepositoryDetailsAsync(
                    infrastructureComponentInput.DockerHubDetails,
                    infrastructureComponentInput.ImageTag,
                    cancellationToken
                );

                if (
                    !await DoesImageAlreadyExistsInDockerEngineAsync(
                        dockerHubFetchedDetails.RepoResp.Name,
                        dockerHubFetchedDetails.RepoTag.Name,
                        dockerHubFetchedDetails.RepoResp.Namespace,
                        cancellationToken
                    )
                )
                {
                    await DockerProcessExecutor.Value.PullDockerImageFromHub(
                        infrastructureComponentInput.DockerHubDetails,
                        dockerHubFetchedDetails.RepoResp.Name,
                        dockerHubFetchedDetails.RepoTag.Name,
                        dockerHubFetchedDetails.RepoResp.Namespace,
                        cancellationToken
                    );
                }

                var createOrGetRetryPipeline = _commonOperationRetrySettings.ToPipeline();

                var containerInspectSummary = await createOrGetRetryPipeline.ExecuteAsync(
                    async ct =>
                        await GetOrCreateContainerAsync(
                            infrastructureComponentInput,
                            dockerHubFetchedDetails,
                            ct
                        ),
                    cancellationToken
                );

                var newInfraComp = CreateInfraCompFromContainerResponse(
                    containerInspectSummary,
                    dockerHubFetchedDetails
                );

                return newInfraComp;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An unhandled exception occurred while updating the infrastructure component"
                );

                throw new ApiException(
                    LogLevel.Error,
                    HttpStatusCode.InternalServerError,
                    ApplicationConstants.ExceptionConstants.InternalError,
                    ex
                );
            }
        }
    }

    private async Task<ContainerInspectResponse> GetOrCreateContainerAsync(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(
            nameof(infrastructureComponentInput.ContainerName),
            infrastructureComponentInput.ContainerName
        );
        var combinedImageName = $"{dockerHubFetchedDetails.RepoResp.Namespace}/{dockerHubFetchedDetails.RepoResp.Name}";
        var combinedImageNameAndTag =
            $"{combinedImageName}:{dockerHubFetchedDetails.RepoTag.Name}";
        var existingContainer = await GetExistingContainerFromDockerEngineAsync(
            infrastructureComponentInput.ContainerName,
            cancellationToken
        );

        if (
            existingContainer is null
            || DoesExistingContainerNeedUpdating(
                existingContainer,
                dockerHubFetchedDetails,
                infrastructureComponentInput
            )
        )
        {
            if (
                existingContainer is not null
                && existingContainer.Config?.Image?.Contains(combinedImageName) != true
            )
            {
                throw new ApiException(
                    LogLevel.Information,
                    HttpStatusCode.BadRequest,
                    $"A different container with name: {infrastructureComponentInput.ContainerName} already exists."
                );
            }

            var createdContainerName = await CreateAndStartContainerAsync(
                existingContainer,
                dockerHubFetchedDetails,
                infrastructureComponentInput,
                cancellationToken
            );

            existingContainer = await GetExistingContainerFromDockerEngineAsync(
                createdContainerName,
                cancellationToken
            );

            if (existingContainer is null)
            {
                throw new ApiException(
                    LogLevel.Error,
                    HttpStatusCode.InternalServerError,
                    $"Failed to create container with name: {infrastructureComponentInput.ContainerName}"
                );
            }
        }

        return existingContainer;
    }

    private async Task<string> CreateAndStartContainerAsync(
        ContainerInspectResponse? containerInspectResponse,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails,
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(
            nameof(infrastructureComponentInput.ContainerName),
            infrastructureComponentInput.ContainerName
        );

        var createAndUpdatePreContainerCreateJobList = new List<Task>();
        if (containerInspectResponse is not null)
        {
            createAndUpdatePreContainerCreateJobList.Add(
                RemoveExistingContainerAsync(
                    containerInspectResponse.Name,
                    false,
                    cancellationToken
                )
            );
        }

        var hangingVolumes = containerInspectResponse
            ?.HostConfig?.Mounts?.Where(x =>
                x.Source != infrastructureComponentInput.Volume?.VolumeName
            )
            .ToArray();

        if (hangingVolumes is not null && hangingVolumes.Length > 0)
        {
            createAndUpdatePreContainerCreateJobList = createAndUpdatePreContainerCreateJobList
                .Concat(
                    hangingVolumes.Select(x =>
                        DockerEngineClient.Value.RemoveVolumeAsync(
                            x.Source,
                            false,
                            cancellationToken
                        )
                    )
                )
                .ToList();
        }

        var isVolumeDiff = IsVolumesDifferent(
            infrastructureComponentInput,
            containerInspectResponse
        );
        if (isVolumeDiff)
        {
            createAndUpdatePreContainerCreateJobList.Add(
                GetNameOrCreateVolumeAsync(infrastructureComponentInput.Volume!, cancellationToken)
            );
        }

        await Task.WhenAll(createAndUpdatePreContainerCreateJobList);

        var requestModel = BuildCreateContainerRequest(
            infrastructureComponentInput,
            dockerHubFetchedDetails,
            isVolumeDiff
        );

        var createResult = await DockerEngineClient.Value.CreateContainerAsync(
            requestModel,
            infrastructureComponentInput.ContainerName,
            cancellationToken
        );

        if (!createResult.IsSuccess || createResult.Data is null)
        {
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Failed to create the container with exception message: {createResult.ExceptionMessage}"
            );
        }

        await StartContainerAsync(createResult.Data.Id, cancellationToken);

        return createResult.Data.Id;
    }

    private async Task StartContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerId), containerId);

        var preStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var startResult = await DockerEngineClient.Value.StartContainerAsync(
            containerId,
            cancellationToken
        );

        if (!startResult.IsSuccess)
        {
            var logValues = await DockerEngineClient.Value.GetContainerLogsAsync(
                containerId,
                true,
                true,
                preStartTimestamp,
                null,
                cancellationToken
            );

            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Failed to start the container with exception message: {startResult.ExceptionMessage}",
                null,
                new Dictionary<string, object?> { { "Logs", logValues } }
            );
        }
    }

    private async Task RemoveExistingContainerAsync(
        string containerName,
        bool removeVolumes,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerName), containerName);
        activity?.SetTag(nameof(removeVolumes), removeVolumes);

        var result = await DockerEngineClient.Value.RemoveContainerAsync(
            containerName,
            true,
            removeVolumes,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Failed to remove the container with name: {containerName}"
            );
        }
    }

    private async Task<ContainerInspectResponse?> GetExistingContainerFromDockerEngineAsync(
        string containerName,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(containerName), containerName);

        var dockerEngineResult = await DockerEngineClient.Value.InspectContainerAsync(
            containerName,
            false,
            cancellationToken
        );

        if (!dockerEngineResult.IsSuccess || dockerEngineResult.Data is null)
        {
            if (dockerEngineResult.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "No existing container found in docker engine. Api responded with: {ErrorMessage} and {@Data}",
                    dockerEngineResult.ExceptionMessage,
                    dockerEngineResult.Data
                );
                return null;
            }
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                "Failed to make inspect container request properly."
            );
        }

        return dockerEngineResult.Data;
    }

    private async Task<bool> DoesImageAlreadyExistsInDockerEngineAsync(
        string imageName,
        string imageTag,
        string @namespace,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(imageName), imageName);
        activity?.SetTag(nameof(imageTag), imageTag);
        activity?.SetTag(nameof(@namespace), @namespace);

        var listImages = await DockerEngineClient.Value.ListImagesAsync(
            true,
            null,
            false,
            false,
            false,
            cancellationToken
        );

        var dockerEngineResult =
            listImages.Data?.FirstOrDefault(x =>
                x.RepoTags.Any(y => y == $"{imageName}:{imageTag}")
            )
            ?? listImages.Data?.FirstOrDefault(x =>
                x.RepoTags.Any(y => y == $"{@namespace}/{imageName}:{imageTag}")
            );

        if (dockerEngineResult is null)
        {
            _logger.LogInformation(
                "Image does not exist in docker engine. Api responded with: {@Data}",
                dockerEngineResult
            );
            return false;
        }
        return true;
    }

    private async Task<(
        GetRepositoryResponse RepoResp,
        RepositoryTag RepoTag
    )> GetDockerHubRepositoryDetailsAsync(
        DockerHubDetails dockerHubDetails,
        string imageVersionTag,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(imageVersionTag), imageVersionTag);
        activity?.SetTag(nameof(dockerHubDetails.RepositoryName), dockerHubDetails.RepositoryName);

        string? accessToken = null;
        if (
            !string.IsNullOrWhiteSpace(dockerHubDetails.Username)
            && !string.IsNullOrWhiteSpace(dockerHubDetails.Password)
        )
        {
            var accessTokenApiResult = await DockerHubClient.Value.CreateAccessTokenAsync(
                dockerHubDetails.Username,
                dockerHubDetails.Password,
                cancellationToken
            );
            if (!accessTokenApiResult.IsSuccess || accessTokenApiResult.Data is null)
            {
                throw new ApiException(
                    LogLevel.Information,
                    HttpStatusCode.BadRequest,
                    !string.IsNullOrWhiteSpace(accessTokenApiResult.ExceptionMessage)
                        ? accessTokenApiResult.ExceptionMessage
                        : "Failed to get docker hub access token"
                );
            }

            accessToken = accessTokenApiResult.Data;
        }

        var getRepoJob = DockerHubClient.Value.GetRepositoryAsync(
            dockerHubDetails,
            accessToken,
            cancellationToken
        );

        var getTagJob = DockerHubClient.Value.GetRepositoryTagAsync(
            dockerHubDetails,
            imageVersionTag,
            accessToken,
            cancellationToken
        );

        await Task.WhenAll(getRepoJob, getTagJob);

        var getRepo = await getRepoJob;
        var getTag = await getTagJob;

        if (!getRepo.IsSuccess || !getTag.IsSuccess || getRepo.Data is null || getTag.Data is null)
        {
            throw new ApiException(
                LogLevel.Information,
                HttpStatusCode.BadRequest,
                !string.IsNullOrWhiteSpace(getRepo.ExceptionMessage) ? getRepo.ExceptionMessage
                    : !string.IsNullOrWhiteSpace(getTag.ExceptionMessage) ? getTag.ExceptionMessage
                    : ApplicationConstants.ExceptionConstants.InternalError
            );
        }

        return (getRepo.Data, getTag.Data);
    }

    private void BasicValidateInput(
        InfrastructureComponentUpdateInput infrastructureComponentInputs
    )
    {
        var validateResult = infrastructureComponentInputs.Validate();
        if (!validateResult.IsValid)
        {
            var errorString = string.Join(
                ". ",
                $"{infrastructureComponentInputs.ContainerName} : {string.Join(". ", validateResult.Errors)}"
            );

            _logger.LogInformation(
                "Infrastructure components are not valid with errors: {Errors}",
                errorString
            );

            throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest, errorString);
        }
        _logger.LogInformation(
            "Infrastructure component with container name: {ContainerName} passed basic validation",
            infrastructureComponentInputs.ContainerName
        );
    }

    private async Task<string> GetNameOrCreateVolumeAsync(
        VolumeInfo volumeInfo,
        CancellationToken cancellationToken
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(volumeInfo.VolumeName), volumeInfo.VolumeName);

        var foundVolume = await DockerEngineClient.Value.InspectVolumeAsync(
            volumeInfo.VolumeName,
            cancellationToken
        );

        if (foundVolume.Data is not null)
        {
            return foundVolume.Data.Name;
        }

        var createdVolume = await DockerEngineClient.Value.CreateVolumeAsync(
            new VolumeCreateRequest { Name = volumeInfo.VolumeName },
            cancellationToken
        );

        if (!createdVolume.IsSuccess || createdVolume.Data is null)
        {
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                "Failed to create volume."
            );
        }

        return createdVolume.Data.Name;
    }

    private ContainerCreateRequest BuildCreateContainerRequest(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails,
        bool mountVolume = false
    )
    {
        var imageFull =
            $"{dockerHubFetchedDetails.RepoResp.Namespace}/{dockerHubFetchedDetails.RepoResp.Name}:{dockerHubFetchedDetails.RepoTag.Name}";

        var request = new ContainerCreateRequest
        {
            Image = imageFull,
            Env = infrastructureComponentInput.CreateEnvStringArrayFromConfigMap(),

            Labels = infrastructureComponentInput
                .Labels.Concat(
                    new Dictionary<string, string>
                    {
                        {
                            "com.hetzner.container.name",
                            infrastructureComponentInput.ContainerName
                        },
                        { "com.hetzner.container.image", dockerHubFetchedDetails.RepoResp.Name },
                        { "com.hetzner.container.tag", dockerHubFetchedDetails.RepoTag.Name },
                    }
                )
                .ToDictionary(),
            HostConfig = new HostConfig { RestartPolicy = new RestartPolicy { Name = "always" } },
        };

        if (infrastructureComponentInput.Networks.Any())
        {
            request = request with
            {
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = infrastructureComponentInput
                        .Networks.Select(x => new KeyValuePair<string, EndpointSettings>(
                            x,
                            new EndpointSettings()
                        ))
                        .ToDictionary(),
                },
            };
        }
        if (
            mountVolume
            && !string.IsNullOrWhiteSpace(infrastructureComponentInput.Volume?.VolumeName)
        )
        {
            request = request with
            {
                HostConfig = request.HostConfig with
                {
                    Mounts =
                    [
                        new Mount
                        {
                            Type = "volume",
                            Source = infrastructureComponentInput.Volume.VolumeName,
                            Target = infrastructureComponentInput.Volume.InternalMountTarget,
                            ReadOnly = false,
                        },
                    ],
                },
            };
        }

        if (
            infrastructureComponentInput.PublicFacingPortNumber is not null
            && infrastructureComponentInput.InternalPortNumber is int internalPortNum
        )
        {
            request = request with
            {
                HostConfig = request.HostConfig with
                {
                    PortBindings = new Dictionary<string, PortBinding[]>
                    {
                        {
                            internalPortNum.ToString(),

                            [
                                new PortBinding
                                {
                                    HostPort =
                                        infrastructureComponentInput.PublicFacingPortNumber.ToString(),
                                },
                            ]
                        },
                    },
                },
            };
        }
        if (infrastructureComponentInput.InternalPortNumber is int internalPortNumber)
        {
            request = request with
            {
                ExposedPorts = new Dictionary<string, object>
                {
                    { internalPortNumber.ToString(), new Dictionary<object, object>() },
                },
            };
        }

        _logger.LogInformation(
            "Container create request to be sent: {@ContainerCreateRequest}",
            request
        );

        return request;
    }

    private static InfrastructureComponent CreateInfraCompFromContainerResponse(
        ContainerInspectResponse containerInspectResponse,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails
    )
    {
        var foundInternalPortNumber = containerInspectResponse
            .Config?.ExposedPorts?.FirstOrDefault()
            .Key;
        return new InfrastructureComponent
        {
            Id = containerInspectResponse.Id,
            ConfigMap = containerInspectResponse.ConvertConfigEnvStringArrayToDict(),
            ContainerName = containerInspectResponse.Name,
            DockerhubName = dockerHubFetchedDetails.RepoResp.Name,
            DockerhubNamespace = dockerHubFetchedDetails.RepoResp.Namespace,
            ImageVersionTag = dockerHubFetchedDetails.RepoTag.Name,
            InternalPortNumber = foundInternalPortNumber,
            Labels = containerInspectResponse.Config?.Labels ?? new Dictionary<string, string?>(),
            PublicFacingPortNumber = containerInspectResponse
                .HostConfig?.PortBindings?.FirstOrDefault(x => x.Key == foundInternalPortNumber)
                .Value?.FirstOrDefault()
                ?.HostPort,
            VolumeName = containerInspectResponse.Config?.Volumes?.FirstOrDefault().Key,
            LatestContainerSummary = containerInspectResponse,
            LastUpdated = DateTime.UtcNow,
        };
    }

    private static bool DoesExistingContainerNeedUpdating(
        ContainerInspectResponse containerInspectResponse,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails,
        InfrastructureComponentUpdateInput infrastructureComponentInput
    )
    {
        var stringArrayEnv = infrastructureComponentInput.CreateEnvStringArrayFromConfigMap();

        return containerInspectResponse.Config?.Image
                != $"{dockerHubFetchedDetails.RepoResp.Namespace}/{dockerHubFetchedDetails.RepoResp.Name}:{dockerHubFetchedDetails.RepoTag.Name}"
            || IsVolumesDifferent(infrastructureComponentInput, containerInspectResponse)
            || stringArrayEnv.All(x => containerInspectResponse.Config?.Env?.Contains(x) == true)
                != true
            || (
                containerInspectResponse.Config?.ExposedPorts?.Any(x =>
                    infrastructureComponentInput.InternalPortNumber is int internalPortNum
                    && x.Key.Contains(internalPortNum.ToString())
                ) != true
            )
            || containerInspectResponse
                .HostConfig?.PortBindings?.Values.SelectMany(x => x)
                .Any(x =>
                    x.HostPort == infrastructureComponentInput.PublicFacingPortNumber.ToString()
                ) != true
            || AreLabelsChanged(
                infrastructureComponentInput.Labels,
                containerInspectResponse.Config?.Labels
            );
    }

    private static bool AreLabelsChanged(
        IReadOnlyDictionary<string, string> inputLabels,
        Dictionary<string, string?>? containerLabels
    )
    {
        var containerNonSystemLabels =
            containerLabels
                ?.Where(kv => !kv.Key.StartsWith("com.hetzner.container."))
                .ToDictionary() ?? [];

        return inputLabels.Count != containerNonSystemLabels.Count
            || !inputLabels.All(kv =>
                containerNonSystemLabels.TryGetValue(kv.Key, out var val) && val == kv.Value
            );
    }

    private static bool IsVolumesDifferent(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        ContainerInspectResponse? containerInspectResponse
    )
    {
        if (string.IsNullOrWhiteSpace(infrastructureComponentInput.Volume?.VolumeName))
        {
            return false;
        }

        return containerInspectResponse?.Config?.Volumes?.ContainsKey(
                infrastructureComponentInput.Volume.VolumeName
            ) ?? true;
    }
}
