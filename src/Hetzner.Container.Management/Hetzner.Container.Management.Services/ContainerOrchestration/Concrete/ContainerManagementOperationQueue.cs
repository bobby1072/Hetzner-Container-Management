using System.Net;
using System.Threading.Channels;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementOperationQueue : IContainerManagementOperationQueue
{
    private readonly Channel<
        KeyValuePair<
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
        >
    > _updateOperationChannel = Channel.CreateUnbounded<
        KeyValuePair<
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
        >
    >();
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ContainerManagementOperationQueue> _logger;

    public ContainerManagementOperationQueue(
        IMemoryCache memoryCache,
        ILogger<ContainerManagementOperationQueue> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<Guid> QueueUpdateOperation(
        InfrastructureComponentUpdateInput[] input,
        CancellationToken cancellationToken = default
    ) => await QueueUpdateOperation(input, null, cancellationToken);
    public async Task<InfrastructureComponent[]> QueueAndWaitForUpdateOperation(
        InfrastructureComponentUpdateInput[] input,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(input), input.Length);

        var completeQueue = Channel.CreateUnbounded<
            KeyValuePair<Guid, (InfrastructureComponent[]? Response, ApiException? Exception)>
        >();

        await QueueUpdateOperation(
            input,
            (id, ifd, ex, ct) =>
                completeQueue
                    .Writer.WriteAsync(
                        new KeyValuePair<
                            Guid,
                            (InfrastructureComponent[]? Response, ApiException? Exception)
                        >(id, (ifd, ex)),
                        ct
                    )
                    .AsTask(),
            cancellationToken
        );

        var dequeuedItem = await completeQueue.Reader.ReadAsync(cancellationToken);
        var updatedDocument = dequeuedItem.Value;

        completeQueue.Writer.TryComplete();

        if (updatedDocument.Item1 is null || updatedDocument.Item2 is not null)
        {
            throw updatedDocument.Item2
                ?? new ApiException(
                    LogLevel.Error,
                    HttpStatusCode.InternalServerError,
                    ApplicationConstants.ExceptionConstants.InternalError
                );
        }

        return updatedDocument.Item1;
    }

    public async Task<
        KeyValuePair<
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
        >
    > DequeueUpdateOperationAsync(CancellationToken cancellationToken)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();

        return await _updateOperationChannel.Reader.ReadAsync(cancellationToken);
    }

    public void Dispose() => _updateOperationChannel.Writer.Complete();

    private async Task<Guid> QueueUpdateOperation(
        InfrastructureComponentUpdateInput[] input,
        Func<
            Guid,
            InfrastructureComponent[]?,
            ApiException?,
            CancellationToken,
            Task
        >? addToCompleteQueueFunc,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        activity?.SetTag(nameof(input), input.Length);

        var jobId = Guid.NewGuid();

        _logger.LogInformation("About to queue update operation with job id: {JobId}", jobId);

        activity?.SetTag(nameof(jobId), jobId);
        
        await _updateOperationChannel.Writer.WriteAsync(
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
            >(jobId, (input, addToCompleteQueueFunc)),
            cancellationToken
        );

        _memoryCache.Set(jobId, new ContainerUpdateJobState { JobId = jobId, Status = ContainerUpdateJobStatusEnum.NotStarted });
        
        return jobId;
    }
}
