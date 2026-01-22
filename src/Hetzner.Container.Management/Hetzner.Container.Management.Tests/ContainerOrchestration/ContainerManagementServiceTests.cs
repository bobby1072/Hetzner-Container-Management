using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Docker;
using Hetzner.Container.Management.Schemas.Docker.DockerEngineApi;
using Hetzner.Container.Management.Schemas.Docker.DockerHubApi;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;
using Hetzner.Container.Management.Services.Docker.Abstract;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;
using Hetzner.Container.Management.Tests.TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hetzner.Container.Management.Tests.ContainerOrchestration;

public sealed class ContainerManagementServiceTests
{
    private readonly Mock<IDockerHubClient> _mockDockerHubClient;
    private readonly Mock<IDockerEngineClient> _mockDockerEngineClient;
    private readonly Mock<IDockerProcessExecutor> _mockDockerProcessExecutor;
    private readonly Mock<ICurrentInfrastructureExplorer> _mockInfrastructureExplorer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ContainerManagementServiceTestFixture _fixture;
    private readonly ContainerManagementService _sut;

    public ContainerManagementServiceTests()
    {
        _mockDockerHubClient = new Mock<IDockerHubClient>();
        _mockDockerEngineClient = new Mock<IDockerEngineClient>();
        _mockDockerProcessExecutor = new Mock<IDockerProcessExecutor>();
        _mockInfrastructureExplorer = new Mock<ICurrentInfrastructureExplorer>();
        _fixture = new ContainerManagementServiceTestFixture();

        var services = new ServiceCollection();
        services.AddSingleton(_mockDockerHubClient.Object);
        services.AddSingleton(_mockDockerEngineClient.Object);
        services.AddSingleton(_mockDockerProcessExecutor.Object);
        services.AddSingleton(_mockInfrastructureExplorer.Object);
        _serviceProvider = services.BuildServiceProvider();

        _sut = new ContainerManagementService(
            _serviceProvider,
            new NullLogger<ContainerManagementService>()
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenCurrentInfrastructureIsNull_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((InfrastructureDocument?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Failed to retrieve current infrastructure", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenDockerHubRepoFails_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        var currentInfra = _fixture.CreateInfrastructureDocument();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<GetRepositoryResponse?>
                {
                    ExceptionMessage = "Docker Hub error",
                    Data = null,
                }
            );

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<RepositoryTag?>
                {
                    ExceptionMessage = null,
                    Data = _fixture.CreateRepositoryTag(),
                }
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("Docker Hub error", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenDockerHubTagFails_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<GetRepositoryResponse?>
                {
                    ExceptionMessage = null,
                    Data = repoResponse,
                }
            );

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<RepositoryTag?>
                {
                    ExceptionMessage = "Tag not found",
                    Data = null,
                }
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("Tag not found", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenContainerCreationFails_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag();
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<GetRepositoryResponse?>
                {
                    ExceptionMessage = null,
                    Data = repoResponse,
                }
            );

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<RepositoryTag?>
                {
                    ExceptionMessage = null,
                    Data = tagResponse,
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ImageSummaryResponse[]?>
                {
                    ExceptionMessage = null,
                    Data = [imageInspectResponse],
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerCreateResponse?>
                {
                    ExceptionMessage = "Creation failed",
                    Data = null,
                }
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Failed to create the container", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenEmptyInput_ThrowsApiException()
    {
        // Arrange
        var input = Array.Empty<InfrastructureComponentUpdateInput>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("Input list must have at least one object", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenInvalidInput_ThrowsApiException()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput(containerName: ""),
        };
        var currentInfra = _fixture.CreateInfrastructureDocument();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenSuccessfulNewContainer_UpdatesInfrastructure()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput("new-container", "v1.0"),
        };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("v1.0");
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );
        var containerCreateResponse = _fixture.CreateContainerCreateResponse();
        var containerInspectResponse = _fixture.CreateContainerInspectResponse(
            "new-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "v1.0",
            8080,
            80
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync(
                    containerCreateResponse.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerInspectResponse)
            );

        _mockInfrastructureExplorer
            .Setup(x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(currentInfra.UpdateNumber + 1, result.UpdateNumber);
        Assert.Contains(result.Components, c => c.ContainerName == "new-container");
        _mockInfrastructureExplorer.Verify(
            x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenContainerExistsAndDoesntNeedUpdate_DoesNotRecreate()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput(
                "existing-container",
                "latest",
                8080,
                80
            ),
        };
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("latest");
        var containerInspectResponse = _fixture.CreateContainerInspectResponse(
            "existing-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "latest",
            8080,
            80,
            new Dictionary<string, string> { { "ENV_VAR_1", "value1" }, { "ENV_VAR_2", "value2" } }
        );
        var existingComponent = _fixture.CreateInfrastructureComponent(
            "existing-container",
            "latest",
            8080,
            80
        );
        var currentInfra = _fixture.CreateInfrastructureDocument(existingComponent);
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerInspectResponse)
            );

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockDockerEngineClient.Verify(
            x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _mockDockerEngineClient.Verify(
            x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenImageNeedsPulling_PullsImage()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput("test-container", "v2.0"),
        };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("v2.0");
        var containerCreateResponse = _fixture.CreateContainerCreateResponse();
        var containerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "v2.0"
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    []
                )
            );

        _mockDockerProcessExecutor
            .Setup(x =>
                x.PullDockerImageFromHub(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync(
                    containerCreateResponse.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerInspectResponse)
            );

        _mockInfrastructureExplorer
            .Setup(x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockDockerProcessExecutor.Verify(
            x =>
                x.PullDockerImageFromHub(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    repoResponse.Name,
                    tagResponse.Name,
                    repoResponse.Namespace,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenContainerNeedsUpdateDifferentEnvVars_RecreatesContainer()
    {
        // Arrange - Testing update when environment variables change
        var newConfigMap = new Dictionary<string, string?>
        {
            { "ENV_VAR_1", "newvalue1" },
            { "ENV_VAR_2", "newvalue2" },
            { "ENV_VAR_3", "newvalue3" },
        };
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput(
                "test-container",
                "latest",
                configMap: newConfigMap
            ),
        };
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("latest");
        var oldContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "latest",
            configMap: new Dictionary<string, string>
            {
                { "ENV_VAR_1", "value1" },
                { "ENV_VAR_2", "value2" },
            }
        );
        var newContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "latest",
            configMap: newConfigMap
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!)
        );
        var existingComponent = _fixture.CreateInfrastructureComponent("test-container", "latest");
        var currentInfra = _fixture.CreateInfrastructureDocument(existingComponent);
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );
        var containerCreateResponse = _fixture.CreateContainerCreateResponse();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync("test-container", false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    oldContainerInspectResponse
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    newContainerInspectResponse
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync(
                    containerCreateResponse.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    newContainerInspectResponse
                )
            );

        _mockInfrastructureExplorer
            .Setup(x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockDockerEngineClient.Verify(
            x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    true,
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockDockerEngineClient.Verify(
            x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockDockerEngineClient.Verify(
            x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenStartContainerFails_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag();
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );
        var containerCreateResponse = _fixture.CreateContainerCreateResponse();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                }
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateFailureResult<object?>(
                    "Failed to start container"
                )
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Failed to start the container", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenRemoveContainerFails_ThrowsApiException()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput("test-container", "v2.0"),
        };
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("v2.0");
        var oldContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "v1.0"
        );
        var existingComponent = _fixture.CreateInfrastructureComponent("test-container", "v1.0");
        var currentInfra = _fixture.CreateInfrastructureDocument(existingComponent);
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync("test-container", false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    oldContainerInspectResponse
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateFailureResult<object?>(
                    "Failed to remove container"
                )
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains(
            "A different container with name: test-container already exists",
            exception.Message
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenDifferentContainerWithSameNameExists_ThrowsApiException()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput("test-container", "latest"),
        };
        var repoResponse = _fixture.CreateGetRepositoryResponse("my-repo");
        var tagResponse = _fixture.CreateRepositoryTag("latest");
        var existingContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            "different-namespace/different-repo",
            "latest"
        );
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync("test-container", false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    existingContainerInspectResponse
                )
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains(
            "A different container with name: test-container already exists",
            exception.Message
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenVolumeConfigChanges_RecreatesContainerWithVolume()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput(
                "test-container",
                "latest",
                volumeName: "/data"
            ),
        };
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag("latest");
        var oldContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "latest",
            volumeName: null
        );
        var newContainerInspectResponse = _fixture.CreateContainerInspectResponse(
            "test-container",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "latest",
            volumeName: "/data"
        );
        var existingComponent = _fixture.CreateInfrastructureComponent("test-container", "latest");
        var currentInfra = _fixture.CreateInfrastructureDocument(existingComponent);
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );
        var containerCreateResponse = _fixture.CreateContainerCreateResponse();

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync("test-container", false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    oldContainerInspectResponse
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    newContainerInspectResponse
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    true,
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .Setup(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.InspectContainerAsync(
                    containerCreateResponse.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(
                    newContainerInspectResponse
                )
            );

        _mockInfrastructureExplorer
            .Setup(x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockDockerEngineClient.Verify(
            x =>
                x.RemoveContainerAsync(
                    It.IsAny<string>(),
                    true,
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenMultipleComponents_UpdatesAll()
    {
        // Arrange
        var input = new[]
        {
            _fixture.CreateValidInfrastructureComponentUpdateInput("container-1", "v1.0"),
            _fixture.CreateValidInfrastructureComponentUpdateInput("container-2", "v2.0"),
        };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse1 = _fixture.CreateRepositoryTag("v1.0");
        var tagResponse2 = _fixture.CreateRepositoryTag("v2.0");
        var imageInspectResponse1 = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse1.Name
        );
        var imageInspectResponse2 = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse2.Name
        );
        var containerCreateResponse1 = _fixture.CreateContainerCreateResponse();
        var containerCreateResponse2 = _fixture.CreateContainerCreateResponse();
        var containerInspectResponse1 = _fixture.CreateContainerInspectResponse(
            "container-1",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "v1.0"
        );
        var containerInspectResponse2 = _fixture.CreateContainerInspectResponse(
            "container-2",
            $"{repoResponse.Namespace}/{repoResponse.Name}",
            "v2.0"
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .SetupSequence(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse1))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse2));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse1, imageInspectResponse2]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Not found",
                    Data = null,
                    StatusCode = HttpStatusCode.NotFound,
                }
            );

        _mockDockerEngineClient
            .SetupSequence(x =>
                x.CreateContainerAsync(
                    It.IsAny<ContainerCreateRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse1)
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerCreateResponse2)
            );

        _mockDockerEngineClient
            .Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult<object?>(null));

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(
                    containerCreateResponse1.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerInspectResponse1)
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(
                    containerCreateResponse2.Id,
                    false,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult(containerInspectResponse2)
            );

        _mockInfrastructureExplorer
            .Setup(x =>
                x.ReplaceCurrentInfrastructureAsync(
                    It.IsAny<InfrastructureDocument>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateCurrentInfrastructure(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Components.Length);
        Assert.Contains(result.Components, c => c.ContainerName == "container-1");
        Assert.Contains(result.Components, c => c.ContainerName == "container-2");
    }

    [Fact]
    public async Task UpdateCurrentInfrastructure_WhenInspectContainerFailsNotWithNotFound_ThrowsApiException()
    {
        // Arrange
        var input = new[] { _fixture.CreateValidInfrastructureComponentUpdateInput() };
        var currentInfra = _fixture.CreateInfrastructureDocument();
        var repoResponse = _fixture.CreateGetRepositoryResponse();
        var tagResponse = _fixture.CreateRepositoryTag();
        var imageInspectResponse = _fixture.CreateImageInspectResponse(
            repoResponse.Name,
            tagResponse.Name
        );

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x =>
                x.CreateAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult("test-access-token")
            );
        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(repoResponse));

        _mockDockerHubClient
            .Setup(x =>
                x.GetRepositoryTagAsync(
                    It.IsAny<DockerHubDetailsWithRepositoryName>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ContainerManagementServiceTestFixture.CreateSuccessResult(tagResponse));

        _mockDockerEngineClient
            .Setup(x =>
                x.ListImagesAsync(true, null, false, false, false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                ContainerManagementServiceTestFixture.CreateSuccessResult<ImageSummaryResponse[]?>(
                    [imageInspectResponse]
                )
            );

        _mockDockerEngineClient
            .Setup(x =>
                x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new DockerApiActionResult<ContainerInspectResponse?>
                {
                    ExceptionMessage = "Internal error",
                    Data = null,
                    StatusCode = HttpStatusCode.InternalServerError,
                }
            );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _sut.UpdateCurrentInfrastructure(input, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Failed to make inspect container request properly", exception.Message);
    }
}
