using Hetzner.Container.Management.Services.Docker.Abstract;

namespace Hetzner.Container.Management.Services.Docker.Concrete;

public sealed class DockerHttpClient: IDockerHttpClient
{
    private readonly HttpClient _httpClient;

    public DockerHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}