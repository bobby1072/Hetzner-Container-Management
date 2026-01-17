using System.Net.Sockets;
using BT.Common.Helpers.Extensions;
using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Hetzner.Container.Management.Services.Docker.Concrete;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hetzner.Container.Management.Services.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddContainerManagementApplication<TInfraExplorer>(
        this IHostApplicationBuilder hostAppBuilder,
        Func<IServiceProvider, TInfraExplorer>? explorerCreator = null
    )
        where TInfraExplorer : class, ICurrentInfrastructureExplorer
    {
        hostAppBuilder.AddHttpClientStuff();

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

        hostAppBuilder.Services.AddScoped<IDockerProcessExecutor, DockerProcessExecutor>();
        
        return hostAppBuilder;
    }

    public static T CheckAndAddSingletonOptions<T>(
        this IHostApplicationBuilder hostAppBuilder,
        string? nameofSection = null
    )
        where T : class
    {
        var sectname = nameofSection ?? typeof(T).Name;

        var configSection = hostAppBuilder.Configuration.GetSection(sectname);

        if (!configSection.Exists())
        {
            throw new ArgumentException(sectname);
        }

        hostAppBuilder.Services.ConfigureSingletonOptions<T>(configSection);

        return configSection.Get<T>() ?? throw new ArgumentException(sectname);
    }

    private static IHostApplicationBuilder AddHttpClientStuff(
        this IHostApplicationBuilder hostAppBuilder
    )
    {
        var dockerEngineApiSettings =
            hostAppBuilder.CheckAndAddSingletonOptions<DockerEngineApiSettings>();
        var dockerHubApiSettings =
            hostAppBuilder.CheckAndAddSingletonOptions<DockerHubApiSettings>();

        hostAppBuilder.Services.AddMemoryCache();
        hostAppBuilder.Services.AddHttpClient();
        hostAppBuilder.Services.AddHttpClientWithResilience<IDockerHubClient, DockerHubClient>(
            dockerHubApiSettings
        );
        hostAppBuilder
            .Services.AddHttpClientWithResilience<IDockerEngineClient, DockerEngineClient>(
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

        return hostAppBuilder;
    }
}
