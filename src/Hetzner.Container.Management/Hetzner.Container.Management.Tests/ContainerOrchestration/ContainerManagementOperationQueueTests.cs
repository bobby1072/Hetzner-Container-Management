using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hetzner.Container.Management.Tests.ContainerOrchestration;

public sealed class ContainerManagementOperationQueueTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly ContainerManagementOperationQueue _sut;

    public ContainerManagementOperationQueueTests()
    {
        _sut = new ContainerManagementOperationQueue(
            _memoryCache,
            new NullLogger<ContainerManagementOperationQueue>()
        );
    }

    public void Dispose()
    {
        _sut.Dispose();
        _memoryCache.Dispose();
    }

    private static InfrastructureComponentUpdateInput[] CreateTestInput(
        string containerName = "test-container"
    ) =>
        [
            new InfrastructureComponentUpdateInput
            {
                ContainerName = containerName,
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

    #region QueueUpdateOperation Tests

    [Fact]
    public async Task QueueUpdateOperation_ReturnsNonEmptyGuid()
    {
        // Arrange
        var input = CreateTestInput();

        // Act
        var result = await _sut.QueueUpdateOperation(input);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task QueueUpdateOperation_MultipleCalls_ReturnUniqueGuids()
    {
        // Arrange
        var input1 = CreateTestInput("container-1");
        var input2 = CreateTestInput("container-2");

        // Act
        var guid1 = await _sut.QueueUpdateOperation(input1);
        var guid2 = await _sut.QueueUpdateOperation(input2);

        // Assert
        Assert.NotEqual(guid1, guid2);
    }

    [Fact]
    public async Task QueueUpdateOperation_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var input = CreateTestInput();

        // Act
        var result = await _sut.QueueUpdateOperation(input, cts.Token);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
    }

    #endregion

    #region DequeueUpdateOperationAsync Tests

    [Fact]
    public async Task DequeueUpdateOperationAsync_ReturnsQueuedItem()
    {
        // Arrange
        var input = CreateTestInput();
        var queuedGuid = await _sut.QueueUpdateOperation(input);

        // Act
        var dequeued = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);

        // Assert
        Assert.Equal(queuedGuid, dequeued.Key);
        Assert.Equivalent(input, dequeued.Value.Input);
        Assert.Null(dequeued.Value.AddToCompleteQueueFunc);
    }

    [Fact]
    public async Task DequeueUpdateOperationAsync_PreservesOrderFIFO()
    {
        // Arrange
        var input1 = CreateTestInput("container-1");
        var input2 = CreateTestInput("container-2");
        var guid1 = await _sut.QueueUpdateOperation(input1);
        var guid2 = await _sut.QueueUpdateOperation(input2);

        // Act
        var dequeued1 = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);
        var dequeued2 = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);

        // Assert
        Assert.Equal(guid1, dequeued1.Key);
        Assert.Equal(guid2, dequeued2.Key);
        Assert.Equal("container-1", dequeued1.Value.Input[0].ContainerName);
        Assert.Equal("container-2", dequeued2.Value.Input[0].ContainerName);
    }

    [Fact]
    public async Task DequeueUpdateOperationAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.DequeueUpdateOperationAsync(cts.Token)
        );
    }

    [Fact]
    public async Task DequeueUpdateOperationAsync_WhenEmpty_WaitsForItem()
    {
        // Arrange
        var input = CreateTestInput();
        var dequeueTask = _sut.DequeueUpdateOperationAsync(CancellationToken.None);

        // The task should not be completed yet because queue is empty
        Assert.False(dequeueTask.IsCompleted);

        // Act - queue an item after a delay
        await _sut.QueueUpdateOperation(input);

        var result = await dequeueTask;

        // Assert
        Assert.Equivalent(input, result.Value.Input);
    }

    #endregion

    #region QueueAndWaitForUpdateOperation Tests

    [Fact]
    public async Task QueueAndWaitForUpdateOperation_WhenCompletedSuccessfully_ReturnsComponents()
    {
        // Arrange
        var input = CreateTestInput();
        var expectedComponents = new[]
        {
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
        };

        // Start the queue and wait operation
        var waitTask = _sut.QueueAndWaitForUpdateOperation(input);

        // Dequeue and complete with a successful result
        var dequeued = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);
        Assert.NotNull(dequeued.Value.AddToCompleteQueueFunc);
        await dequeued.Value.AddToCompleteQueueFunc!.Invoke(
            dequeued.Key,
            expectedComponents,
            null,
            CancellationToken.None
        );

        // Act
        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("test-container", result[0].ContainerName);
    }

    [Fact]
    public async Task QueueAndWaitForUpdateOperation_WhenCompletedWithException_ThrowsApiException()
    {
        // Arrange
        var input = CreateTestInput();
        var expectedException = new ApiException(
            LogLevel.Error,
            HttpStatusCode.InternalServerError,
            "Something went wrong"
        );

        var waitTask = _sut.QueueAndWaitForUpdateOperation(input);

        // Dequeue and complete with an error
        var dequeued = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);
        Assert.NotNull(dequeued.Value.AddToCompleteQueueFunc);
        await dequeued.Value.AddToCompleteQueueFunc!.Invoke(
            dequeued.Key,
            null,
            expectedException,
            CancellationToken.None
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => waitTask);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("Something went wrong", exception.Message);
    }

    [Fact]
    public async Task QueueAndWaitForUpdateOperation_WhenCompletedWithNullResult_ThrowsApiException()
    {
        // Arrange
        var input = CreateTestInput();

        var waitTask = _sut.QueueAndWaitForUpdateOperation(input);

        // Dequeue and complete with null result and no exception
        var dequeued = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);
        Assert.NotNull(dequeued.Value.AddToCompleteQueueFunc);
        await dequeued.Value.AddToCompleteQueueFunc!.Invoke(
            dequeued.Key,
            null,
            null,
            CancellationToken.None
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => waitTask);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    [Fact]
    public async Task QueueAndWaitForUpdateOperation_DequeuedItemHasCompleteCallback()
    {
        // Arrange
        var input = CreateTestInput();

        var waitTask = _sut.QueueAndWaitForUpdateOperation(input);

        // Act
        var dequeued = await _sut.DequeueUpdateOperationAsync(CancellationToken.None);

        // Assert - the callback should be set so the caller is notified
        Assert.NotNull(dequeued.Value.AddToCompleteQueueFunc);
        Assert.Equivalent(input, dequeued.Value.Input);

        // Clean up - complete the operation so the wait task finishes
        await dequeued.Value.AddToCompleteQueueFunc!.Invoke(
            dequeued.Key,
            [
                new InfrastructureComponent
                {
                    ContainerName = "test-container",
                    DockerhubName = "test-repo",
                    DockerhubNamespace = "test-ns",
                    ImageVersionTag = "latest",
                    InternalPortNumber = "8080",
                    PublicFacingPortNumber = "80",
                    ConfigMap = new Dictionary<string, string?>(),
                },
            ],
            null,
            CancellationToken.None
        );
        await waitTask;
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_PreventsNewItemsFromBeingQueued()
    {
        // Arrange - use a separate instance so the shared one isn't affected
        using var localMemoryCache = new MemoryCache(new MemoryCacheOptions());
        var queue = new ContainerManagementOperationQueue(
            localMemoryCache,
            new NullLogger<ContainerManagementOperationQueue>()
        );
        var input = CreateTestInput();
        queue.Dispose();

        // Act & Assert - writing to a completed channel throws
        await Assert.ThrowsAsync<System.Threading.Channels.ChannelClosedException>(
            () => queue.QueueUpdateOperation(input)
        );
    }

    #endregion
}
