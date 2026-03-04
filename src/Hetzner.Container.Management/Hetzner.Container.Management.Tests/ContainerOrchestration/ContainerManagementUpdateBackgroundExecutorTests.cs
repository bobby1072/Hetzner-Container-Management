using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hetzner.Container.Management.Tests.ContainerOrchestration;

public sealed class ContainerManagementUpdateBackgroundExecutorTests
{
    private readonly Mock<IContainerManagementOperationQueue> _mockOperationQueue;
    private readonly Mock<IContainerManagementUpdateService> _mockContainerManagementService;

    public ContainerManagementUpdateBackgroundExecutorTests()
    {
        _mockOperationQueue = new Mock<IContainerManagementOperationQueue>();
        _mockContainerManagementService = new Mock<IContainerManagementUpdateService>();
    }

    private ContainerManagementUpdateBackgroundExecutor CreateSut()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_mockContainerManagementService.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new ContainerManagementUpdateBackgroundExecutor(
            scopeFactory,
            _mockOperationQueue.Object,
            new NullLogger<ContainerManagementUpdateBackgroundExecutor>()
        );
    }

    private static InfrastructureComponentUpdateInput[] CreateTestInput() =>
        [
            new InfrastructureComponentUpdateInput
            {
                ContainerName = "test-container",
                ImageTag = "latest",
                InternalPortNumber = 8080,
                PublicFacingPortNumber = 80,
                ConfigMap = new Dictionary<string, string?> { { "ENV", "test" } },
                DockerHubDetails = new DockerHubDetails
                {
                    Namespace = "test-ns",
                    RepositoryName = "test-repo",
                },
            },
        ];

    private static InfrastructureComponent[] CreateTestComponents() =>
        [
            new InfrastructureComponent
            {
                ContainerName = "test-container",
                DockerhubName = "test-repo",
                DockerhubNamespace = "test-ns",
                ImageVersionTag = "latest",
                InternalPortNumber = "8080",
                PublicFacingPortNumber = "80",
                ConfigMap = new Dictionary<string, string?> { { "ENV", "test" } },
            },
        ];

    [Fact]
    public async Task ExecuteAsync_DequeuesAndExecutesOperation()
    {
        // Arrange
        var input = CreateTestInput();
        var expectedComponents = CreateTestComponents();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _mockOperationQueue
            .SetupSequence(x => x.DequeueUpdateOperationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new KeyValuePair<
                    Guid,
                    (
                        InfrastructureComponentUpdateInput[] Input,
                        Func<
                            Guid,
                            InfrastructureComponent[]?,
                            ApiException?,
                            CancellationToken,
                            Task
                        >? AddToCompleteQueueFunc
                    )
                >(jobId, (input, null))
            )
            .ThrowsAsync(new OperationCanceledException());

        _mockContainerManagementService
            .Setup(x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expectedComponents);

        var sut = CreateSut();

        // Act - start the background service and cancel after a short time
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);

        // Allow time for the operation to execute
        try
        {
            await Task.Delay(1000, cts.Token);
        }
        catch (OperationCanceledException) { }

        await sut.StopAsync(CancellationToken.None);

        // Assert
        _mockContainerManagementService.Verify(
            x =>
                x.UpdateCurrentInfrastructure(
                    It.Is<InfrastructureComponentUpdateInput[]>(i =>
                        i[0].ContainerName == "test-container"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_InvokesCompleteCallback()
    {
        // Arrange
        var input = CreateTestInput();
        var expectedComponents = CreateTestComponents();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        InfrastructureComponent[]? callbackResult = null;
        ApiException? callbackException = null;
        var callbackInvoked = new TaskCompletionSource<bool>();

        Func<
            Guid,
            InfrastructureComponent[]?,
            ApiException?,
            CancellationToken,
            Task
        > completeCallback = (id, components, ex, ct) =>
        {
            callbackResult = components;
            callbackException = ex;
            callbackInvoked.TrySetResult(true);
            return Task.CompletedTask;
        };

        _mockOperationQueue
            .SetupSequence(x => x.DequeueUpdateOperationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new KeyValuePair<
                    Guid,
                    (
                        InfrastructureComponentUpdateInput[] Input,
                        Func<
                            Guid,
                            InfrastructureComponent[]?,
                            ApiException?,
                            CancellationToken,
                            Task
                        >? AddToCompleteQueueFunc
                    )
                >(jobId, (input, completeCallback))
            )
            .ThrowsAsync(new OperationCanceledException());

        _mockContainerManagementService
            .Setup(x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expectedComponents);

        var sut = CreateSut();

        // Act
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await sut.StartAsync(cts.Token);

        // Wait for the callback to be invoked
        await Task.WhenAny(callbackInvoked.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callbackInvoked.Task.IsCompleted);
        Assert.NotNull(callbackResult);
        Assert.Single(callbackResult);
        Assert.Equal("test-container", callbackResult[0].ContainerName);
        Assert.Null(callbackException);
    }

    [Fact]
    public async Task ExecuteAsync_WhenApiExceptionThrown_InvokesCallbackWithException()
    {
        // Arrange
        var input = CreateTestInput();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var expectedException = new ApiException(
            LogLevel.Error,
            HttpStatusCode.InternalServerError,
            "Update failed"
        );

        InfrastructureComponent[]? callbackResult = null;
        ApiException? callbackException = null;
        var callbackInvoked = new TaskCompletionSource<bool>();

        Func<
            Guid,
            InfrastructureComponent[]?,
            ApiException?,
            CancellationToken,
            Task
        > completeCallback = (id, components, ex, ct) =>
        {
            callbackResult = components;
            callbackException = ex;
            callbackInvoked.TrySetResult(true);
            return Task.CompletedTask;
        };

        _mockOperationQueue
            .SetupSequence(x => x.DequeueUpdateOperationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new KeyValuePair<
                    Guid,
                    (
                        InfrastructureComponentUpdateInput[] Input,
                        Func<
                            Guid,
                            InfrastructureComponent[]?,
                            ApiException?,
                            CancellationToken,
                            Task
                        >? AddToCompleteQueueFunc
                    )
                >(jobId, (input, completeCallback))
            )
            .ThrowsAsync(new OperationCanceledException());

        _mockContainerManagementService
            .Setup(x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(expectedException);

        var sut = CreateSut();

        // Act
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await sut.StartAsync(cts.Token);

        await Task.WhenAny(callbackInvoked.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callbackInvoked.Task.IsCompleted);
        Assert.Null(callbackResult);
        Assert.NotNull(callbackException);
        Assert.Equal(HttpStatusCode.InternalServerError, callbackException.StatusCode);
        Assert.Contains("Update failed", callbackException.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnhandledExceptionThrown_InvokesCallbackWithWrappedApiException()
    {
        // Arrange
        var input = CreateTestInput();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        ApiException? callbackException = null;
        var callbackInvoked = new TaskCompletionSource<bool>();

        Func<
            Guid,
            InfrastructureComponent[]?,
            ApiException?,
            CancellationToken,
            Task
        > completeCallback = (id, components, ex, ct) =>
        {
            callbackException = ex;
            callbackInvoked.TrySetResult(true);
            return Task.CompletedTask;
        };

        _mockOperationQueue
            .SetupSequence(x => x.DequeueUpdateOperationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new KeyValuePair<
                    Guid,
                    (
                        InfrastructureComponentUpdateInput[] Input,
                        Func<
                            Guid,
                            InfrastructureComponent[]?,
                            ApiException?,
                            CancellationToken,
                            Task
                        >? AddToCompleteQueueFunc
                    )
                >(jobId, (input, completeCallback))
            )
            .ThrowsAsync(new OperationCanceledException());

        _mockContainerManagementService
            .Setup(x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var sut = CreateSut();

        // Act
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await sut.StartAsync(cts.Token);

        await Task.WhenAny(callbackInvoked.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callbackInvoked.Task.IsCompleted);
        Assert.NotNull(callbackException);
        Assert.Equal(HttpStatusCode.InternalServerError, callbackException.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCallback_DoesNotThrow()
    {
        // Arrange
        var input = CreateTestInput();
        var expectedComponents = CreateTestComponents();
        var jobId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _mockOperationQueue
            .SetupSequence(x => x.DequeueUpdateOperationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new KeyValuePair<
                    Guid,
                    (
                        InfrastructureComponentUpdateInput[] Input,
                        Func<
                            Guid,
                            InfrastructureComponent[]?,
                            ApiException?,
                            CancellationToken,
                            Task
                        >? AddToCompleteQueueFunc
                    )
                >(jobId, (input, null))
            )
            .ThrowsAsync(new OperationCanceledException());

        _mockContainerManagementService
            .Setup(x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expectedComponents);

        var sut = CreateSut();

        // Act & Assert - should not throw
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);
        try
        {
            await Task.Delay(1000, cts.Token);
        }
        catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        _mockContainerManagementService.Verify(
            x =>
                x.UpdateCurrentInfrastructure(
                    It.IsAny<InfrastructureComponentUpdateInput[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
