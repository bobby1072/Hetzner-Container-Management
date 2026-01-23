using System.Diagnostics.CodeAnalysis;
using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Helpers.Models;
using BT.Common.Polly.Extensions;
using BT.Common.Polly.Models.Concrete;
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

internal sealed class ContainerManagementService : IContainerManagementService
{
    private static readonly PollyRetrySettings _commonOperationRetrySettings =
        new() { TotalAttempts = 2 };

    private readonly ContainerUpdateServicesServiceProvider _containerUpdateServicesServiceProvider;
    private readonly ILogger<ContainerManagementService> _logger;

    public ContainerManagementService(
        IServiceProvider serviceProvider,
        ILogger<ContainerManagementService> logger
    )
    {
        _containerUpdateServicesServiceProvider = new ContainerUpdateServicesServiceProvider(
            serviceProvider
        );
        _logger = logger;
    }

    public async Task<InfrastructureDocument> UpdateCurrentInfrastructure(
        InfrastructureComponentUpdateInput[] infrastructureDocuments,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (infrastructureDocuments.Length == 0)
            {
                throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest, "Input list must have at least one object");
            }
            
            var currentInfrastructureDocument =
                await _containerUpdateServicesServiceProvider
                    .CurrentInfrastructureExplorer
                    .TryGetCurrentInfrastructureDocumentAsync(
                        cancellationToken
                    )
                        ?? throw new ApiException(
                            LogLevel.Error,
                            HttpStatusCode.InternalServerError,
                            "Failed to retrieve current infrastructure infrastructure document."
                        );

            var updateAttemptRetryPipeline = _commonOperationRetrySettings.ToPipeline();

            var updateInfraComponents = await updateAttemptRetryPipeline.ExecuteAsync(
                async ct =>
                    await Task.WhenAll(
                        infrastructureDocuments.Select(x =>
                            UpdateInfrastructureComponentAsync(x, ct)
                        )
                    ),
                cancellationToken
            );

            var didUpdate = updateInfraComponents.All(x =>
                currentInfrastructureDocument.Components.Any(y => y.IsSame(x))
            );

            var newInfraStructureDocument = currentInfrastructureDocument;
            if (didUpdate)
            {
                newInfraStructureDocument = currentInfrastructureDocument with
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

                await _containerUpdateServicesServiceProvider.CurrentInfrastructureExplorer.ReplaceCurrentInfrastructureAsync(
                    newInfraStructureDocument,
                    cancellationToken
                );
            }
            else if(currentInfrastructureDocument.Components.Length == 0)
            {
                newInfraStructureDocument = currentInfrastructureDocument with
                {
                    LastUpdated = DateTime.UtcNow,
                    Components = updateInfraComponents,
                    UpdateNumber = currentInfrastructureDocument.UpdateNumber + 1,
                };
                
                await _containerUpdateServicesServiceProvider.CurrentInfrastructureExplorer.ReplaceCurrentInfrastructureAsync(
                    newInfraStructureDocument,
                    cancellationToken
                );
            }

            return newInfraStructureDocument;
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
                    await _containerUpdateServicesServiceProvider.DockerProcessExecutor.PullDockerImageFromHub(
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
        var combinedImageNameAndTag =
            $"{dockerHubFetchedDetails.RepoResp.Namespace}/{dockerHubFetchedDetails.RepoResp.Name}:{dockerHubFetchedDetails.RepoTag.Name}";
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
                && existingContainer.Config?.Image?.Contains(combinedImageNameAndTag) != true
            )
            {
                throw new ApiException(
                    LogLevel.Information,
                    HttpStatusCode.BadRequest,
                    $"A different container with name: {infrastructureComponentInput.ContainerName} already exists."
                );
            }

            if (existingContainer is not  null)
            {
                await RemoveExistingContainerAsync(
                    existingContainer.Name,
                    IsVolumesDifferent(infrastructureComponentInput, existingContainer),
                    cancellationToken
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
        var requestModel = BuildCreateContainerRequest(
            infrastructureComponentInput,
            dockerHubFetchedDetails,
            IsVolumesDifferent(infrastructureComponentInput, containerInspectResponse)
        );

        var createResult =
            await _containerUpdateServicesServiceProvider.DockerEngineClient.CreateContainerAsync(
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
        var startResult = await _containerUpdateServicesServiceProvider.DockerEngineClient
            .StartContainerAsync(containerId, cancellationToken);

        if (!startResult.IsSuccess)
        {
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                $"Failed to start the container with exception message: {startResult.ExceptionMessage}"
            );
        }
    }
    private async Task RemoveExistingContainerAsync(
        string containerName,
        bool removeVolumes,
        CancellationToken cancellationToken
    )
    {
        var result =
            await _containerUpdateServicesServiceProvider.DockerEngineClient.RemoveContainerAsync(
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
        var dockerEngineResult =
            await _containerUpdateServicesServiceProvider.DockerEngineClient.InspectContainerAsync(
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
            throw new ApiException(LogLevel.Error, HttpStatusCode.InternalServerError, "Failed to make inspect container request properly.");
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
        var listImages =
            await _containerUpdateServicesServiceProvider.DockerEngineClient.ListImagesAsync(true, null, false, false,
                false, cancellationToken);

        var dockerEngineResult = listImages.Data?.FirstOrDefault(x => x.RepoTags.Any(y => y == $"{imageName}:{imageTag}"))
            ?? listImages.Data?.FirstOrDefault(x => x.RepoTags.Any(y => y == $"{@namespace}/{imageName}:{imageTag}"));
        
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
        DockerHubDetailsWithRepositoryName dockerHubDetails,
        string imageVersionTag,
        CancellationToken cancellationToken
    )
    {
        var accessToken = await _containerUpdateServicesServiceProvider.DockerHubClient.CreateAccessTokenAsync(dockerHubDetails.Username, dockerHubDetails.Password, cancellationToken);

        if (!accessToken.IsSuccess || accessToken.Data is null)
        {
            throw new ApiException(
                LogLevel.Information,
                HttpStatusCode.BadRequest,
                !string.IsNullOrWhiteSpace(accessToken.ExceptionMessage) ? accessToken.ExceptionMessage
                : "Failed to get docker hub access token" 
            );
        }
        
        var getRepoJob = _containerUpdateServicesServiceProvider.DockerHubClient.GetRepositoryAsync(
            dockerHubDetails,
            accessToken.Data,
            cancellationToken
        );

        var getTagJob =
            _containerUpdateServicesServiceProvider.DockerHubClient.GetRepositoryTagAsync(
                dockerHubDetails,
                imageVersionTag,
                accessToken.Data,
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
                $"{infrastructureComponentInputs.ContainerName} : {validateResult.Errors}"
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

    private ContainerCreateRequest BuildCreateContainerRequest(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        (GetRepositoryResponse RepoResp, RepositoryTag RepoTag) dockerHubFetchedDetails,
        bool createVolume = false
    )
    {
        var imageFull =
            $"{dockerHubFetchedDetails.RepoResp.Namespace}/{dockerHubFetchedDetails.RepoResp.Name}:{dockerHubFetchedDetails.RepoTag.Name}";

        var request = new ContainerCreateRequest
        {
            Image = imageFull,
            Env = infrastructureComponentInput.CreateEnvStringArrayFromConfigMap(),
            ExposedPorts = new Dictionary<string, object>
            {
                { infrastructureComponentInput.InternalPortNumber.ToString(), new Dictionary<object, object>() },
            },
            Labels = new Dictionary<string, string>
            {
                { "com.hetzner.container.name", infrastructureComponentInput.ContainerName },
                { "com.hetzner.container.image", dockerHubFetchedDetails.RepoResp.Name },
                { "com.hetzner.container.tag", dockerHubFetchedDetails.RepoTag.Name },
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, PortBinding[]>
                {
                    {
                        infrastructureComponentInput.InternalPortNumber.ToString(),
                        [
                            new PortBinding
                            {
                                HostPort =
                                    infrastructureComponentInput.PublicFacingPortNumber.ToString(),
                            },
                        ]
                    },
                },
                RestartPolicy = new RestartPolicy { Name = "always" },
            },
        };

        if (createVolume && !string.IsNullOrWhiteSpace(infrastructureComponentInput.VolumeName))
        {
            request = request with
            {
                Volumes = new Dictionary<string, object>
                {
                    { infrastructureComponentInput.VolumeName, new Dictionary<object, object>() },
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
            .Key ?? throw new ApiException(
            LogLevel.Error,
            HttpStatusCode.InternalServerError,
            ApplicationConstants.ExceptionConstants.InternalError
        );
        
        return new InfrastructureComponent
        {
            Id = containerInspectResponse.Id, 
            ConfigMap = containerInspectResponse.ConvertConfigEnvStringArrayToDict(),
            ContainerName = containerInspectResponse.Name,
            DockerhubName = dockerHubFetchedDetails.RepoResp.Name,
            DockerhubNamespace = dockerHubFetchedDetails.RepoResp.Namespace,
            ImageVersionTag = dockerHubFetchedDetails.RepoTag.Name,
            InternalPortNumber = foundInternalPortNumber,
            PublicFacingPortNumber = containerInspectResponse.HostConfig?.PortBindings?.FirstOrDefault(x => 
                                         x.Key == foundInternalPortNumber).Value.FirstOrDefault()?.HostPort
                                     ?? throw new ApiException(
                                         LogLevel.Error,
                                         HttpStatusCode.InternalServerError,
                                         ApplicationConstants.ExceptionConstants.InternalError
                                     ),
            VolumeName = containerInspectResponse.Config.Volumes?.FirstOrDefault().Key,
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
            || stringArrayEnv.All(x => containerInspectResponse.Config?.Env?.Contains(x) == true) != true
            || containerInspectResponse.Config?.ExposedPorts?.Any(x =>
                x.Key.Contains(infrastructureComponentInput.InternalPortNumber.ToString())
            ) != true
            || containerInspectResponse.HostConfig?.PortBindings?.Values.SelectMany(x => x).Any(x => 
                x.HostPort == infrastructureComponentInput.PublicFacingPortNumber.ToString()
            ) != true;
    }

    private static bool IsVolumesDifferent(
        InfrastructureComponentUpdateInput infrastructureComponentInput,
        ContainerInspectResponse? containerInspectResponse
    ) =>
        !string.IsNullOrWhiteSpace(infrastructureComponentInput.VolumeName)
            ? (
                containerInspectResponse is null
                || containerInspectResponse.Config?.Volumes?.ContainsKey(
                    infrastructureComponentInput.VolumeName
                ) != true
            )
            : containerInspectResponse?.Config?.Volumes?.Any() == true;

    private sealed record ContainerUpdateServicesServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        [field: AllowNull, MaybeNull]
        public IDockerHubClient DockerHubClient =>
            field ??= _serviceProvider.GetRequiredService<IDockerHubClient>();

        [field: AllowNull, MaybeNull]
        public IDockerEngineClient DockerEngineClient =>
            field ??= _serviceProvider.GetRequiredService<IDockerEngineClient>();

        [field: AllowNull, MaybeNull]
        public IDockerProcessExecutor DockerProcessExecutor =>
            field ??= _serviceProvider.GetRequiredService<IDockerProcessExecutor>();

        [field: AllowNull, MaybeNull]
        public ICurrentInfrastructureExplorer CurrentInfrastructureExplorer =>
            field ??= _serviceProvider.GetRequiredService<ICurrentInfrastructureExplorer>();

        public ContainerUpdateServicesServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
