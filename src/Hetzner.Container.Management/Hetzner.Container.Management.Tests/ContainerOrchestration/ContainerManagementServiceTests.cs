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
using Moq;

namespace Hetzner.Container.Management.Tests.ContainerOrchestration;

public class ContainerManagementServiceTests
{
    private readonly Mock<IDockerHubClient> _mockDockerHubClient;
    private readonly Mock<IDockerEngineClient> _mockDockerEngineClient;
    private readonly Mock<IDockerProcessExecutor> _mockDockerProcessExecutor;
    private readonly Mock<ICurrentInfrastructureExplorer> _mockInfrastructureExplorer;
    private readonly Mock<ILogger<ContainerManagementService>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ContainerManagementServiceTestFixture _fixture;
    private readonly IContainerManagementService _sut;

    public ContainerManagementServiceTests()
    {
        _mockDockerHubClient = new Mock<IDockerHubClient>();
        _mockDockerEngineClient = new Mock<IDockerEngineClient>();
        _mockDockerProcessExecutor = new Mock<IDockerProcessExecutor>();
        _mockInfrastructureExplorer = new Mock<ICurrentInfrastructureExplorer>();
        _mockLogger = new Mock<ILogger<ContainerManagementService>>();
        _fixture = new ContainerManagementServiceTestFixture();

        var services = new ServiceCollection();
        services.AddSingleton(_mockDockerHubClient.Object);
        services.AddSingleton(_mockDockerEngineClient.Object);
        services.AddSingleton(_mockDockerProcessExecutor.Object);
        services.AddSingleton(_mockInfrastructureExplorer.Object);
        _serviceProvider = services.BuildServiceProvider();

        _sut = new ContainerManagementService(_serviceProvider, _mockLogger.Object);
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
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateCurrentInfrastructure(input, CancellationToken.None));

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
            .Setup(x => x.GetRepositoryAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<GetRepositoryResponse> { ExceptionMessage = "Docker Hub error", Data = null });

        _mockDockerHubClient
            .Setup(x => x.GetRepositoryTagAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<RepositoryTag> { ExceptionMessage = null, Data = _fixture.CreateRepositoryTag() });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateCurrentInfrastructure(input, CancellationToken.None));

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
            .Setup(x => x.GetRepositoryAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<GetRepositoryResponse> { ExceptionMessage = null, Data = repoResponse });

        _mockDockerHubClient
            .Setup(x => x.GetRepositoryTagAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<RepositoryTag> { ExceptionMessage = "Tag not found", Data = null });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateCurrentInfrastructure(input, CancellationToken.None));

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
        var imageInspectResponse = _fixture.CreateImageInspectResponse(repoResponse.Name, tagResponse.Name);

        _mockInfrastructureExplorer
            .Setup(x => x.TryGetCurrentInfrastructureDocumentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentInfra);

        _mockDockerHubClient
            .Setup(x => x.GetRepositoryAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<GetRepositoryResponse> { ExceptionMessage = null, Data = repoResponse });

        _mockDockerHubClient
            .Setup(x => x.GetRepositoryTagAsync(It.IsAny<DockerHubDetailsWithRepositoryName>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<RepositoryTag> { ExceptionMessage = null, Data = tagResponse });

        _mockDockerEngineClient
            .Setup(x => x.InspectImageAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<ImageInspectResponse> { ExceptionMessage = null, Data = imageInspectResponse });

        _mockDockerEngineClient
            .Setup(x => x.InspectContainerAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<ContainerInspectResponse> { ExceptionMessage = "Not found", Data = null });

        _mockDockerEngineClient
            .Setup(x => x.CreateContainerAsync(It.IsAny<ContainerCreateRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerApiActionResult<ContainerCreateResponse> { ExceptionMessage = "Creation failed", Data = null });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _sut.UpdateCurrentInfrastructure(input, CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Failed to create the container", exception.Message);
    }
}
