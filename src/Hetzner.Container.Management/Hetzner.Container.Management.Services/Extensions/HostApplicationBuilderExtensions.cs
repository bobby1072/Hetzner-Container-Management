using System.Net.Sockets;
using BT.Common.Helpers.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hetzner.Container.Management.Services.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddContainerManagementApplication(this IHostApplicationBuilder hostAppBuilder)
    {
        hostAppBuilder.CheckAndAddSingletonOptions<DockerHubDetails>();
        
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                var endpoint = new UnixDomainSocketEndPoint("/var/run/docker.sock");

                await socket.ConnectAsync(endpoint, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
        };
        hostAppBuilder.Services.AddHttpClient();
        
        
        return hostAppBuilder;
    }

    public static IHostApplicationBuilder CheckAndAddSingletonOptions<T>(this IHostApplicationBuilder hostAppBuilder,
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
        
        return hostAppBuilder;
    }
}