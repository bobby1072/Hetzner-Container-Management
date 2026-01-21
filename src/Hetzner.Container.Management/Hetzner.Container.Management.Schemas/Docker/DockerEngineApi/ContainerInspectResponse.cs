namespace Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;

public sealed record ContainerInspectResponse
{
    public required string Id { get; init; }
    public required string Created { get; init; }
    public required string Path { get; init; }
    public string[] Args { get; init; } = [];
    public required ContainerState State { get; init; }
    public required string Image { get; init; }
    public string ResolvConfPath { get; init; } = string.Empty;
    public string HostnamePath { get; init; } = string.Empty;
    public string HostsPath { get; init; } = string.Empty;
    public string LogPath { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int RestartCount { get; init; }
    public string Driver { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public ImageManifestDescriptor? ImageManifestDescriptor { get; init; }
    public string MountLabel { get; init; } = string.Empty;
    public string ProcessLabel { get; init; } = string.Empty;
    public string AppArmorProfile { get; init; } = string.Empty;
    public string[] ExecIDs { get; init; } = [];
    public HostConfig? HostConfig { get; init; }
    public GraphDriver? GraphDriver { get; init; }
    public ContainerStorage? Storage { get; init; }
    public string? SizeRw { get; init; }
    public string? SizeRootFs { get; init; }
    public Mount[] Mounts { get; init; } = [];
    public ContainerConfig? Config { get; init; }
    public ContainerInspectNetworkSettings? NetworkSettings { get; init; }


    public Dictionary<string, string?> ConvertConfigEnvStringArrayToDict(string splitter = "=")
    {
        return Config?
            .Env?
            .ToDictionary(
                x => x.Split(splitter)[0], 
                x => (string?)x.Split(splitter)[1]
            ) ?? new Dictionary<string, string?>();
    }
}
