using BT.Common.Helpers.Extensions;
using Hetzner.Container.Management.Schemas.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hetzner.Container.Management.Services.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddContainerManagementApplication(this IHostApplicationBuilder hostAppBuilder)
    {
        hostAppBuilder.CheckAndAddSingletonOptions<DockerHubDetails>();
        
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