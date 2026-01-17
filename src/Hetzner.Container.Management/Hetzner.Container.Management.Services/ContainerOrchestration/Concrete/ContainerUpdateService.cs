using System.Diagnostics.CodeAnalysis;
using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Extensions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerUpdateService
{
    private readonly ContainerUpdateServicesServiceProvider _containerUpdateServicesServiceProvider;
    private readonly ILogger<ContainerUpdateService> _logger;

    public ContainerUpdateService(
        IServiceProvider serviceProvider,
        ILogger<ContainerUpdateService> logger
    )
    {
        _containerUpdateServicesServiceProvider = new ContainerUpdateServicesServiceProvider(
            serviceProvider
        );
        _logger = logger;
    }

    public async Task<InfrastructureDocument> UpdateCurrentInfrastructure(
        InfrastructureUpdateDocument[] infrastructureDocuments,
        CancellationToken cancellationToken
    )
    {

    }

    private async Task<InfrastructureComponent> GetCurrentInfrastructureComponentForUpdate(
        InfrastructureUpdateDocument infrastructureDocument,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Attempting to update the infrastructure component with details: {@InfrastructureDetails}",
                infrastructureDocument
            );

            BasicValidateInput(infrastructureDocument);

            var dockerHubFetchedDetails = await GetDockerHubRepositoryDetails(infrastructureDocument.DockerHubDetails, 
                infrastructureDocument.ImageTag,
                cancellationToken);

            if (!await DoesImageAlreadyExistsInDockerEngine(dockerHubFetchedDetails.RepoResp.Name,
                    dockerHubFetchedDetails.RepoTag.Name, cancellationToken))
            {
                await _containerUpdateServicesServiceProvider
                    .DockerProcessExecutor
                    .PullDockerImageFromHub(infrastructureDocument.DockerHubDetails,
                        dockerHubFetchedDetails.RepoResp.Name,
                        dockerHubFetchedDetails.RepoTag.Name,
                        null,
                        cancellationToken);
            }

            var existingContainer =
                await GetExistingContainerFromDockerEngine(infrastructureDocument.ContainerName,
                    dockerHubFetchedDetails.RepoResp.Name,
                    cancellationToken);

            if (existingContainer is null)
            {
                
            }
            
            throw new NotImplementedException();
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while updating the infrastructure component");
            
            throw new ApiException(
                LogLevel.Error,
                HttpStatusCode.InternalServerError,
                "Internal server error",
                ex
            );
        }
    }

    private async Task<ContainerInspectResponse> GetOrCreateContainer()
    {
        throw new NotImplementedException();
    }
    private async Task<ContainerInspectResponse?> GetExistingContainerFromDockerEngine(
        string containerName,
        string imageName,
        CancellationToken cancellationToken)
    {
        var dockerEngineResult = await _containerUpdateServicesServiceProvider
            .DockerEngineClient
            .InspectContainerAsync(containerName, false, cancellationToken);

        if (!dockerEngineResult.IsSuccess || dockerEngineResult.Data is null)
        {
            _logger.LogInformation("No existing container found in docker engine. Api responded with: {ErrorMessage} and {@Data}",
                dockerEngineResult.ExceptionMessage,
                dockerEngineResult.Data);
            return null;
        }

        if (dockerEngineResult.Data.Config?.Image?.Contains(imageName) != true)
        {
            throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest, $"A different container with name: {containerName} already exists.");
        }
        
        return dockerEngineResult.Data;
    }
    private async Task<bool> DoesImageAlreadyExistsInDockerEngine(string imageName,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var dockerEngineResult = await _containerUpdateServicesServiceProvider
            .DockerEngineClient
            .InspectImageAsync(imageName, false, cancellationToken);

        if (!dockerEngineResult.IsSuccess || dockerEngineResult.Data is null)
        {
            _logger.LogInformation("Image does not exist in docker engine. Api responded with: {ErrorMessage} and {@Data}",
                dockerEngineResult.ExceptionMessage, 
                dockerEngineResult.Data);
            return false;
        }

        return dockerEngineResult.Data.RepoTags
            .Contains($"{imageName}:{imageTag}");
    }
    private async Task<(GetRepositoryResponse RepoResp, RepositoryTag RepoTag)> GetDockerHubRepositoryDetails(
        DockerHubDetailsWithRepositoryName dockerHubDetails, 
        string imageVersionTag,
        CancellationToken cancellationToken
    )
    {
        var getRepoJob = _containerUpdateServicesServiceProvider
            .DockerHubClient
            .GetRepositoryAsync(dockerHubDetails, cancellationToken);
        
        var getTagJob = _containerUpdateServicesServiceProvider
            .DockerHubClient
            .GetRepositoryTagAsync(dockerHubDetails, imageVersionTag, cancellationToken);
        
        await Task.WhenAll(getRepoJob, getTagJob);
        
        var getRepo = await getRepoJob;
        var getTag = await getTagJob;

        if (!getRepo.IsSuccess || 
            !getTag.IsSuccess || 
            getRepo.Data is null || 
            getTag.Data is null)
        {
            throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest, !string.IsNullOrWhiteSpace(getRepo.ExceptionMessage) ? 
                getRepo.ExceptionMessage : !string.IsNullOrWhiteSpace(getTag.ExceptionMessage) ? 
                    getTag.ExceptionMessage: ApplicationConstants.ExceptionConstants.InternalError);
        }
        
        return (getRepo.Data, getTag.Data);
    }
    
    private void BasicValidateInput(InfrastructureUpdateDocument infrastructureDocuments)
    {
        var validateResult = infrastructureDocuments.Validate();
        if (!validateResult.IsValid)
        {
            var errorString = string.Join(
                ". ",
                $"{infrastructureDocuments.ContainerName} : {validateResult.Errors}"
            );

            _logger.LogInformation(
                "Infrastructure components are not valid with errors: {Errors}",
                errorString
            );

            throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest, errorString);
        }
        _logger.LogInformation("Infrastructure component with container name: {ContainerName} passed basic validation",
            infrastructureDocuments.ContainerName);
    }

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
