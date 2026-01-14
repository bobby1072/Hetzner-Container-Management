using System.Net.Sockets;
using BT.Common.Helpers.Extensions;
using BT.Common.Http.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Hetzner.Container.Management.Services.DockerApi.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hetzner.Container.Management.Services.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddContainerManagementApplication(this IHostApplicationBuilder hostAppBuilder)
    {
        hostAppBuilder.CheckAndAddSingletonOptions<DockerHubDetails>();
        var apiSettings = hostAppBuilder.CheckAndAddSingletonOptions<DockerApiSettings>();

        hostAppBuilder.Services.AddHttpClient();
        hostAppBuilder.Services
            .AddHttpClientWithResilience<IDockerHttpClient, IDockerHttpClient>(apiSettings)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectCallback = async (_, cancellationToken) =>
                {
                    var socket = new Socket(
                        AddressFamily.Unix,
                        SocketType.Stream,
                        ProtocolType.Unspecified
                    );

                    var endpoint = new UnixDomainSocketEndPoint(apiSettings.UnixDomainSocketEndPoint);
                    await socket.ConnectAsync(endpoint, cancellationToken);

                    return new NetworkStream(socket, ownsSocket: true);
                }
            });
        
        
        return hostAppBuilder;
    }

    public static T CheckAndAddSingletonOptions<T>(this IHostApplicationBuilder hostAppBuilder,
        string? nameofSection = null) where T : class
    {
        var sectname = nameofSection ?? typeof(T).Name;
        
        var configSection = hostAppBuilder.Configuration.GetSection(sectname);

        if (!configSection.Exists())
        {
            throw new ArgumentException(sectname);
        }

        hostAppBuilder.Services
            .ConfigureSingletonOptions<T>(configSection);
        
        return configSection.Get<T>() ?? throw new ArgumentException(sectname);
    }
}