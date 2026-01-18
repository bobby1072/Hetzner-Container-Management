using System.Net;
using System.Threading.Channels;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.Extensions.Logging;

namespace Hetzner.Container.Management.Services.ContainerOrchestration.Concrete;

internal sealed class ContainerManagementOperationQueue: IContainerManagementOperationQueue
{
    private readonly Channel<KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>> _updateOperationChannel = 
        Channel.CreateUnbounded<KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>>();
    
    private readonly ILogger<ContainerManagementOperationQueue> _logger;

    public ContainerManagementOperationQueue(ILogger<ContainerManagementOperationQueue> logger)
    {
        _logger = logger;
    }

    public async Task<Guid> QueueUpdateOperation(InfrastructureComponentUpdateInput[] input,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        
        _logger.LogInformation("About to queue update operation with job id: {JobId}", jobId);

        await _updateOperationChannel.Writer.WriteAsync(
            new KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>(jobId,
                (input, null)), cancellationToken);
        
        return jobId;
    }

    public async Task<InfrastructureDocument> QueueAndWaitForUpdateOperation(InfrastructureComponentUpdateInput[] input,
        CancellationToken cancellationToken = default)
    {
        
        var completeQueue = 
            Channel.CreateUnbounded<KeyValuePair<Guid, (InfrastructureDocument? Response, ApiException? Exception)>>();
        
        await QueueUpdateOperation(input, 
            (id, ifd,ex, ct) => completeQueue.Writer.WriteAsync(new KeyValuePair<Guid, (InfrastructureDocument? Response, ApiException? Exception)>(id, (ifd, ex)), ct).AsTask(),
            cancellationToken);

        var dequeuedItem = await completeQueue.Reader.ReadAsync(cancellationToken);
        var updatedDocument = dequeuedItem.Value;
        
        completeQueue.Writer.TryComplete();
        
        if (updatedDocument.Item1 is null || updatedDocument.Item2 is not null)
        {
            throw updatedDocument.Item2
                ?? new ApiException(LogLevel.Error, HttpStatusCode.InternalServerError,
                ApplicationConstants.ExceptionConstants.InternalError);
        }
        
        return updatedDocument.Item1;
    }
    public async Task<KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>> DequeueUpdateOperationAsync(CancellationToken cancellationToken)
    {
        return await _updateOperationChannel.Reader.ReadAsync(cancellationToken);
    }

    public void Dispose() => _updateOperationChannel.Writer.Complete();
    
    private async Task<Guid> QueueUpdateOperation(InfrastructureComponentUpdateInput[] input,
        Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task> addToCompleteQueueFunc,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        
        _logger.LogInformation("About to queue update operation with job id: {JobId}", jobId);

        await _updateOperationChannel.Writer.WriteAsync(
            new KeyValuePair<Guid, (InfrastructureComponentUpdateInput[] Input, Func<Guid, InfrastructureDocument?, ApiException?, CancellationToken, Task>? AddToCompleteQueueFunc)>(jobId,
                (input, addToCompleteQueueFunc)), cancellationToken);
        
        return jobId;
    }
}