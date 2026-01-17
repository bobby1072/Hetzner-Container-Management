using System.Diagnostics.CodeAnalysis;
using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Extensions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.Docker.Abstract;
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
        _containerUpdateServicesServiceProvider = new ContainerUpdateServicesServiceProvider(serviceProvider);
        _logger = logger;
    }

    public async Task<InfrastructureDocument> UpdateCurrentInfrastructure(InfrastructureUpdateDocument[] infrastructureDocuments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger
                .LogInformation("Attempting to update the current infrastructure with {NumberOfComponents} components",
                    infrastructureDocuments.Length);
            
            BasicValidateInput(infrastructureDocuments);
            
            
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApiException(LogLevel.Error, HttpStatusCode.InternalServerError, "Internal server error", ex);    
        }
    }

    private void BasicValidateInput(InfrastructureUpdateDocument[] infrastructureDocuments)
    {
        var validationResults = infrastructureDocuments.Select(x => (x.Validate(), x)).ToArray();
        if (validationResults.Any(x => !x.Item1.IsValid))
        {
            var errorString = string.Join(". ",
                validationResults.Select(x => $"{x.x.ContainerName} : {x.Item1.Errors}"));
                
            _logger.LogInformation("Infrastructure components are not valid with errors: {Errors}",
                errorString);
                
            throw new ApiException(LogLevel.Information, HttpStatusCode.BadRequest,errorString);
        }
    }
    private sealed record ContainerUpdateServicesServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        [field: AllowNull, MaybeNull]
        public IDockerHubClient _dockerHubClient =>
            field ??= _serviceProvider.GetRequiredService<IDockerHubClient>();

        [field: AllowNull, MaybeNull]
        public IDockerEngineClient _dockerEngineClient =>
            field ??= _serviceProvider.GetRequiredService<IDockerEngineClient>();
        
        [field: AllowNull, MaybeNull]
        public IDockerProcessExecutor _dockerProcessExecutor =>
            field ??= _serviceProvider.GetRequiredService<IDockerProcessExecutor>();
        
        [field: AllowNull, MaybeNull]
        public ICurrentInfrastructureExplorer _currentInfrastructureExplorer =>
            field ??= _serviceProvider.GetRequiredService<ICurrentInfrastructureExplorer>();

        public ContainerUpdateServicesServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
