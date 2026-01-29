using AutoFixture;
using Hetzner.Container.Management.Schemas.Docker;
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
        Dictionary<string, string?>? configMap = null,
        string? volumeName = null
    )
    {
        return new InfrastructureComponentUpdateInput
        {
            ContainerName = containerName ?? "test-container",
            ImageTag = imageTag ?? "latest",
            InternalPortNumber = internalPort ?? 8080,
            PublicFacingPortNumber = publicPort ?? 80,
            ConfigMap =
                configMap
                ?? new Dictionary<string, string?>
                {
                    { "ENV_VAR_1", "value1" },
                    { "ENV_VAR_2", "value2" },
                },
            VolumeName = volumeName,
            DockerHubDetails = new DockerHubDetails
            {
                RepositoryName = "test-repo",
                Username = "test-user",
                Password = "test-password",
            },
        };
    }

    public InfrastructureDocument CreateInfrastructureDocument(
        params InfrastructureComponent[] components
    )
    {
        return new InfrastructureDocument
        {
            Components = components,
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            UpdateNumber = _fixture.Create<int>(),
        };
    }

    public InfrastructureComponent CreateInfrastructureComponent(
        string? containerName = null,
        string? imageVersionTag = null,
        int? internalPort = null,
        int? publicPort = null
    )
    {
        return new InfrastructureComponent
        {
            ContainerName = containerName ?? _fixture.Create<string>(),
            DockerhubName = "test-repo",
            DockerhubNamespace = "test-namespace",
            ImageVersionTag = imageVersionTag ?? "latest",
            InternalPortNumber = $"{internalPort ?? 8080}",
            PublicFacingPortNumber = $"{publicPort ?? 80}",
            ConfigMap = new Dictionary<string, string?> { { "ENV_VAR_1", "value1" } },
            LastUpdated = DateTime.UtcNow.AddDays(-1),
        };
    }

    public GetRepositoryResponse CreateGetRepositoryResponse(string? name = null, string? ns = null)
    {
        return new GetRepositoryResponse
        {
            Name = name ?? "test-repo",
            Namespace = ns ?? "test-namespace",
            Description = _fixture.Create<string>(),
            IsPrivate = false,
            RepositoryType = "image",
            Status = 1,
            IsAutomated = false,
            StarCount = 0,
            PullCount = 0,
            LastUpdated = DateTimeOffset.UtcNow.AddDays(-10),
            DateRegistered = DateTimeOffset.UtcNow.AddDays(-100),
            CollaboratorCount = 1,
            HubUser = "test-user",
            HasStarred = false,
            ImmutableTagsSettings = new ImmutableTagsSettings { Enabled = false, Rules = [] },
        };
    }

    public RepositoryTag CreateRepositoryTag(string? tagName = null)
    {
        return new RepositoryTag
        {
            Name = tagName ?? "latest",
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            Id = _fixture.Create<int>(),
            Repository = _fixture.Create<int>(),
            FullSize = 1024,
        };
    }

    public ContainerInspectResponse CreateContainerInspectResponse(
        string? containerName = null,
        string? imageName = null,
        string? imageTag = null,
        int? internalPort = null,
        int? publicPort = null,
        Dictionary<string, string>? configMap = null,
        string? volumeName = null
    )
    {
        var fullImage = $"{imageName ?? "test-repo"}:{imageTag ?? "latest"}";
        var internalPortStr = $"{internalPort ?? 8080}/tcp";
        var envVars =
            configMap?.Select(kvp => $"{kvp.Key}={kvp.Value}").ToArray()
            ?? new[] { "ENV_VAR_1=value1", "ENV_VAR_2=value2" };

        var volumes =
            volumeName != null
                ? new Dictionary<string, object>
                {
                    { volumeName, new Dictionary<object, object>() },
                }
                : null;

        return new ContainerInspectResponse
        {
            Id = _fixture.Create<string>(),
            Name = containerName ?? _fixture.Create<string>(),
            Created = DateTime.UtcNow.AddDays(-1).ToString("o"),
            Path = "/app",
            Image = _fixture.Create<string>(),
            State = new ContainerState { Running = true, Status = "running" },
            Config = new ContainerConfig
            {
                Image = fullImage,
                Env = envVars,
                ExposedPorts = new Dictionary<string, object>
                {
                    { internalPortStr, new Dictionary<object, object>() },
                },
                Volumes = volumes,
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, PortBinding[]>
                {
                    {
                        internalPortStr,
                        new[] { new PortBinding { HostPort = (publicPort ?? 80).ToString() } }
                    },
                },
                RestartPolicy = new RestartPolicy { Name = "always", MaximumRetryCount = 3 },
            },
        };
    }

    public ContainerCreateResponse CreateContainerCreateResponse(string? id = null)
    {
        return new ContainerCreateResponse
        {
            Id = id ?? _fixture.Create<string>(),
            Warnings = Array.Empty<string>(),
        };
    }

    public ImageSummaryResponse CreateImageInspectResponse(string imageName, string imageTag)
    {
        return new ImageSummaryResponse
        {
            Id = _fixture.Create<string>(),
            RepoTags = new[] { $"{imageName}:{imageTag}" },
        };
    }

    public static DockerApiActionResult<T?> CreateSuccessResult<T>(T data)
    {
        return new DockerApiActionResult<T?> { Data = data, ExceptionMessage = null };
    }

    public static DockerApiActionResult<T?> CreateFailureResult<T>(
        string exceptionMessage,
        T? data = default
    )
    {
        return new DockerApiActionResult<T?> { Data = data, ExceptionMessage = exceptionMessage };
    }
}
