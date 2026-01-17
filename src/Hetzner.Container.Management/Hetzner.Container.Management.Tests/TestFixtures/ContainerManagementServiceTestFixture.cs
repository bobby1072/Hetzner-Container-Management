using AutoFixture;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;

namespace Hetzner.Container.Management.Tests.TestFixtures;

public class ContainerManagementServiceTestFixture
{
    private readonly Fixture _fixture;

    public ContainerManagementServiceTestFixture()
    {
        _fixture = new Fixture();
    }

    public InfrastructureComponentUpdateInput CreateValidInfrastructureComponentUpdateInput(
        string? containerName = null,
        string? imageTag = null,
        int? internalPort = null,
        int? publicPort = null,
        Dictionary<string, string>? configMap = null,
        string? volumeName = null)
    {
        return new InfrastructureComponentUpdateInput
        {
            ContainerName = containerName ?? _fixture.Create<string>(),
            ImageTag = imageTag ?? "latest",
            InternalPortNumber = internalPort ?? 8080,
            PublicFacingPortNumber = publicPort ?? 80,
            ConfigMap = configMap ?? new Dictionary<string, string>
            {
                { "ENV_VAR_1", "value1" },
                { "ENV_VAR_2", "value2" }
            },
            VolumeName = volumeName,
            DockerHubDetails = new DockerHubDetailsWithRepositoryName
            {
                Namespace = "test-namespace",
                RepositoryName = "test-repo",
                Username = "test-user",
                Password = "test-password"
            }
        };
    }

    public InfrastructureDocument CreateInfrastructureDocument(params InfrastructureComponent[] components)
    {
        return new InfrastructureDocument
        {
            Components = components,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            UpdateNumber = _fixture.Create<int>()
        };
    }

    public InfrastructureComponent CreateInfrastructureComponent(
        string? containerName = null,
        string? imageVersionTag = null,
        int? internalPort = null,
        int? publicPort = null)
    {
        return new InfrastructureComponent
        {
            ContainerName = containerName ?? _fixture.Create<string>(),
            DockerhubName = "test-repo",
            DockerhubNamespace = "test-namespace",
            ImageVersionTag = imageVersionTag ?? "latest",
            InternalPortNumber = internalPort ?? 8080,
            PublicFacingPortNumber = publicPort ?? 80,
            ConfigMap = new Dictionary<string, string>
            {
                { "ENV_VAR_1", "value1" }
            },
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        };
    }

    public GetRepositoryResponse CreateGetRepositoryResponse(string? name = null, string? ns = null)
    {
        return new GetRepositoryResponse
        {
            Name = name ?? "test-repo",
            Namespace = ns ?? "test-namespace",
            Description = _fixture.Create<string>(),
            IsPrivate = false
        };
    }

    public RepositoryTag CreateRepositoryTag(string? tagName = null)
    {
        return new RepositoryTag
        {
            Name = tagName ?? "latest",
            LastUpdated = DateTime.UtcNow.AddDays(-1).ToString("o"),
            Digest = _fixture.Create<string>()
        };
    }

    public ContainerInspectResponse CreateContainerInspectResponse(
        string? containerName = null,
        string? imageName = null,
        string? imageTag = null,
        int? internalPort = null,
        int? publicPort = null,
        Dictionary<string, string>? configMap = null,
        string? volumeName = null)
    {
        var fullImage = $"{imageName ?? "test-repo"}:{imageTag ?? "latest"}";
        var internalPortStr = $"{internalPort ?? 8080}/tcp";
        var envVars = configMap?.Select(kvp => $"{kvp.Key}={kvp.Value}").ToArray() 
                      ?? new[] { "ENV_VAR_1=value1", "ENV_VAR_2=value2" };

        var volumes = volumeName != null 
            ? new Dictionary<string, object> { { volumeName, new Dictionary<object, object>() } }
            : null;

        return new ContainerInspectResponse
        {
            Id = _fixture.Create<string>(),
            Name = containerName ?? _fixture.Create<string>(),
            Config = new ContainerConfig
            {
                Image = fullImage,
                Env = envVars,
                ExposedPorts = new Dictionary<string, object>
                {
                    { internalPortStr, new Dictionary<object, object>() }
                },
                Volumes = volumes
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, PortBinding[]>
                {
                    {
                        internalPortStr,
                        new[]
                        {
                            new PortBinding { HostPort = (publicPort ?? 80).ToString() }
                        }
                    }
                },
                RestartPolicy = new RestartPolicy
                {
                    Name = "always",
                    MaximumRetryCount = 3
                }
            },
            State = new ContainerState
            {
                Running = true,
                Status = "running"
            }
        };
    }

    public ContainerCreateResponse CreateContainerCreateResponse(string? id = null)
    {
        return new ContainerCreateResponse
        {
            Id = id ?? _fixture.Create<string>(),
            Warnings = Array.Empty<string>()
        };
    }

    public ImageInspectResponse CreateImageInspectResponse(string imageName, string imageTag)
    {
        return new ImageInspectResponse
        {
            Id = _fixture.Create<string>(),
            RepoTags = new[] { $"{imageName}:{imageTag}" },
            Created = DateTime.UtcNow.AddDays(-10).ToString("o")
        };
    }
}
