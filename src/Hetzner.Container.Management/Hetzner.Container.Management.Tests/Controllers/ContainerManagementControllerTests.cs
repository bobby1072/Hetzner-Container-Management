using System.Net;
using AutoFixture;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Api.Helpers.Models;
using Hetzner.Container.Management.Api.Controllers;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hetzner.Container.Management.Tests.Controllers;

public sealed class ContainerManagementControllerTests
{
    private readonly Mock<IContainerManagementOperationQueue> _mockOperationQueue;
    private readonly Mock<ILogger<ContainerManagementController>> _mockLogger;
    private readonly ContainerManagementController _sut;
    private readonly Fixture _fixture;

    public ContainerManagementControllerTests()
    {
        _mockOperationQueue = new Mock<IContainerManagementOperationQueue>();
        _mockLogger = new Mock<ILogger<ContainerManagementController>>();
        _sut = new ContainerManagementController(_mockOperationQueue.Object, _mockLogger.Object);
        _fixture = new Fixture();
    }

    #region QueueInfrastructureUpdate Tests

    [Fact]
    public async Task QueueInfrastructureUpdate_WithValidInput_ReturnsOkWithGuid()
    {
        // Arrange
        var input = CreateValidInput();
        var expectedGuid = Guid.NewGuid();
        _mockOperationQueue
            .Setup(x => x.QueueUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGuid);

        // Act
        var result = await _sut.QueueInfrastructureUpdate(input);

        // Assert
        var okResult = Assert.IsType<Ok<WebOutcome<Guid>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(expectedGuid, okResult.Value.Data);
        _mockOperationQueue.Verify(
            x => x.QueueUpdateOperation(input, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueInfrastructureUpdate_WhenApiExceptionThrown_ReturnsCorrectProblemResult()
    {
        // Arrange
        var input = CreateValidInput();
        var apiException = new ApiException(
            LogLevel.Warning,
            HttpStatusCode.BadRequest,
            "Invalid input provided"
        );

        _mockOperationQueue
            .Setup(x => x.QueueUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        // Act
        var result = await _sut.QueueInfrastructureUpdate(input);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal((int)HttpStatusCode.BadRequest, problemResult.StatusCode);
        Assert.Equal("Invalid input provided", problemResult.ProblemDetails.Detail);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("An api exception occured during request")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueInfrastructureUpdate_WhenUnexpectedExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var input = CreateValidInput();
        var exception = new InvalidOperationException("Unexpected error");

        _mockOperationQueue
            .Setup(x => x.QueueUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.QueueInfrastructureUpdate(input);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problemResult.StatusCode);
        Assert.Equal(
            ApplicationConstants.ExceptionConstants.InternalError,
            problemResult.ProblemDetails.Detail
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("An exception occured during request")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueInfrastructureUpdate_WithCancellationToken_PassesTokenToQueue()
    {
        // Arrange
        var input = CreateValidInput();
        var expectedGuid = Guid.NewGuid();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockOperationQueue
            .Setup(x => x.QueueUpdateOperation(input, cancellationToken))
            .ReturnsAsync(expectedGuid);

        // Act
        await _sut.QueueInfrastructureUpdate(input, cancellationToken);

        // Assert
        _mockOperationQueue.Verify(
            x => x.QueueUpdateOperation(input, cancellationToken),
            Times.Once
        );
    }

    #endregion

    #region QueueAndWaitForInfrastructureUpdate Tests

    [Fact]
    public async Task QueueAndWaitForInfrastructureUpdate_WithValidInput_ReturnsOkWithInfrastructureDocument()
    {
        // Arrange
        var input = CreateValidInput();
        var expectedDocument = CreateInfrastructureDocument();
        _mockOperationQueue
            .Setup(x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _sut.QueueAndWaitForInfrastructureUpdate(input);

        // Assert
        var okResult = Assert.IsType<Ok<InfrastructureDocument>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equivalent(expectedDocument, okResult.Value);
        _mockOperationQueue.Verify(
            x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueAndWaitForInfrastructureUpdate_WhenApiExceptionThrown_ReturnsCorrectProblemResult()
    {
        // Arrange
        var input = CreateValidInput();
        var apiException = new ApiException(
            LogLevel.Error,
            HttpStatusCode.Conflict,
            "Operation conflict detected"
        );

        _mockOperationQueue
            .Setup(x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        // Act
        var result = await _sut.QueueAndWaitForInfrastructureUpdate(input);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal((int)HttpStatusCode.Conflict, problemResult.StatusCode);
        Assert.Equal("Operation conflict detected", problemResult.ProblemDetails.Detail);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("An api exception occured during request")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueAndWaitForInfrastructureUpdate_WhenUnexpectedExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var input = CreateValidInput();
        var exception = new TimeoutException("Operation timed out");

        _mockOperationQueue
            .Setup(x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.QueueAndWaitForInfrastructureUpdate(input);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problemResult.StatusCode);
        Assert.Equal(
            ApplicationConstants.ExceptionConstants.InternalError,
            problemResult.ProblemDetails.Detail
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("An exception occured during request")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueAndWaitForInfrastructureUpdate_WithCancellationToken_PassesTokenToQueue()
    {
        // Arrange
        var input = CreateValidInput();
        var expectedDocument = CreateInfrastructureDocument();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockOperationQueue
            .Setup(x => x.QueueAndWaitForUpdateOperation(input, cancellationToken))
            .ReturnsAsync(expectedDocument);

        // Act
        await _sut.QueueAndWaitForInfrastructureUpdate(input, cancellationToken);

        // Assert
        _mockOperationQueue.Verify(
            x => x.QueueAndWaitForUpdateOperation(input, cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueAndWaitForInfrastructureUpdate_WithEmptyInput_CallsQueueOperation()
    {
        // Arrange
        var input = Array.Empty<InfrastructureComponentUpdateInput>();
        var expectedDocument = CreateInfrastructureDocument();

        _mockOperationQueue
            .Setup(x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _sut.QueueAndWaitForInfrastructureUpdate(input);

        // Assert
        var okResult = Assert.IsType<Ok<InfrastructureDocument>>(result);
        Assert.NotNull(okResult.Value);
        _mockOperationQueue.Verify(
            x => x.QueueAndWaitForUpdateOperation(input, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task QueueInfrastructureUpdate_WithMultipleComponents_ReturnsOk()
    {
        // Arrange
        var input = new[]
        {
            CreateValidInfrastructureComponentUpdateInput("container1"),
            CreateValidInfrastructureComponentUpdateInput("container2"),
            CreateValidInfrastructureComponentUpdateInput("container3"),
        };
        var expectedGuid = Guid.NewGuid();

        _mockOperationQueue
            .Setup(x => x.QueueUpdateOperation(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGuid);

        // Act
        var result = await _sut.QueueInfrastructureUpdate(input);

        // Assert
        var okResult = Assert.IsType<Ok<WebOutcome<Guid>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(expectedGuid, okResult.Value.Data);
    }

    #endregion

    #region Helper Methods

    private InfrastructureComponentUpdateInput[] CreateValidInput()
    {
        return [CreateValidInfrastructureComponentUpdateInput()];
    }

    private InfrastructureComponentUpdateInput CreateValidInfrastructureComponentUpdateInput(
        string? containerName = null
    )
    {
        return new InfrastructureComponentUpdateInput
        {
            ContainerName = containerName ?? "test-container",
            ImageTag = "latest",
            InternalPortNumber = 8080,
            PublicFacingPortNumber = 80,
            ConfigMap = new Dictionary<string, string?>
            {
                { "ENV_VAR_1", "value1" },
                { "ENV_VAR_2", "value2" },
            },
            VolumeName = null,
            DockerHubDetails = new DockerHubDetailsWithRepositoryName
            {
                RepositoryName = "test-repo",
                Username = "test-user",
                Password = "test-password",
            },
        };
    }

    private InfrastructureDocument CreateInfrastructureDocument()
    {
        return new InfrastructureDocument
        {
            Components =
            [
                new InfrastructureComponent
                {
                    ContainerName = "test-container",
                    ImageVersionTag = "latest",
                    DockerhubName = "test-repo",
                    DockerhubNamespace = "test-namespace",
                    InternalPortNumber = "8080",
                    PublicFacingPortNumber = "80",
                    ConfigMap = new Dictionary<string, string?>(),
                },
            ],
            LastUpdated = DateTime.UtcNow,
            UpdateNumber = 1,
        };
    }

    #endregion
}
