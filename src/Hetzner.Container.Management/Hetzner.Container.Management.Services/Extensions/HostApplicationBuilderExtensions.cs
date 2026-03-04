using System.Net.Sockets;
using System.Reflection;
using BT.Common.Api.Helpers.Models;
using BT.Common.Helpers.Extensions;
using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Hetzner.Container.Management.Services.Docker.Concrete;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BT.Common.Services.Extensions;

namespace Hetzner.Container.Management.Services.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IServiceCollection AddContainerManagementCleanerApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceInfo serviceInfo
    )
    {
        services
            .AddHttpClientStuff(configuration);
        
        services
            .AddTelemetryService(
                string.IsNullOrWhiteSpace(serviceInfo.ReleaseName)
                    ? Assembly
                        .GetExecutingAssembly()
                        .GetName()
                        .FullName:
                    serviceInfo.ReleaseName
            );

        services.AddScoped<IDockerProcessExecutor, DockerProcessExecutor>();

        return services; 
    }
    public static IHostApplicationBuilder AddContainerManagementApiApplication<TInfraExplorer>(
        this IHostApplicationBuilder hostAppBuilder,
        ServiceInfo serviceInfo,
        Func<IServiceProvider, TInfraExplorer>? explorerCreator = null
    )
        where TInfraExplorer : class, ICurrentInfrastructureExplorer
    {
        hostAppBuilder.Services
            .AddHttpClientStuff(hostAppBuilder.Configuration);
        
        hostAppBuilder
            .Services
            .AddTelemetryService(
                string.IsNullOrWhiteSpace(serviceInfo.ReleaseName)
                ? Assembly
                    .GetExecutingAssembly()
                    .GetName()
                    .FullName:
                serviceInfo.ReleaseName
            );

        if (explorerCreator is not null)
        {
            hostAppBuilder.Services.AddScoped<ICurrentInfrastructureExplorer, TInfraExplorer>(
                explorerCreator
            );
        }
        else
        {
            hostAppBuilder.Services.AddScoped<ICurrentInfrastructureExplorer, TInfraExplorer>();
        }

        hostAppBuilder
            .Services.AddScoped<IDockerProcessExecutor, DockerProcessExecutor>()
            .AddScoped<IContainerManagementUpdateService, ContainerManagementUpdateUpdateService>()
            .AddSingleton<IContainerManagementOperationQueue, ContainerManagementOperationQueue>()
            .AddHostedService<ContainerManagementUpdateBackgroundExecutor>();

        return hostAppBuilder;
    }
    private static IServiceCollection AddHttpClientStuff(
        this IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        var dockerEngineApiSettings =
            serviceCollection.CheckAndAddSingletonOptions<DockerEngineApiSettings>(configuration);
        var dockerHubApiSettings =
            serviceCollection.CheckAndAddSingletonOptions<DockerHubApiSettings>(configuration);

        serviceCollection.AddMemoryCache();
        serviceCollection.AddHttpClient();
        serviceCollection.AddHttpClientWithResilience<IDockerHubClient, DockerHubClient>(
            dockerHubApiSettings
        );
        if (dockerEngineApiSettings.UseTestHttpEndPoint)
        {
            serviceCollection.AddHttpClientWithResilience<
                IDockerEngineClient,
                DockerEngineClient
            >(dockerEngineApiSettings);
        }
        else
        {
            serviceCollection.AddHttpClientWithResilience<IDockerEngineClient, DockerEngineClient>(
                    dockerEngineApiSettings
                )
                .ConfigurePrimaryHttpMessageHandler(
                    () =>
                        new SocketsHttpHandler
                        {
                            ConnectCallback = async (_, cancellationToken) =>
                            {
                                var socket = new Socket(
                                    AddressFamily.Unix,
                                    SocketType.Stream,
                                    ProtocolType.Unspecified
                                );

                                var endpoint = new UnixDomainSocketEndPoint(
                                    dockerEngineApiSettings.UnixDomainSocketEndPoint
                                );
                                await socket.ConnectAsync(endpoint, cancellationToken);

                                return new NetworkStream(socket, ownsSocket: true);
                            },
                        }
                );
        }

        return serviceCollection;
    }
}
