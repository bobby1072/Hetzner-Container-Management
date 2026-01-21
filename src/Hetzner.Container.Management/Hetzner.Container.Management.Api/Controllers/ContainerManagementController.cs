using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Api.Helpers.Models;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Schemas.Input;
using Hetzner.Container.Management.Services;
using Hetzner.Container.Management.Services.ContainerOrchestration.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hetzner.Container.Management.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("Api/[controller]")]
public sealed class ContainerManagementController : ControllerBase
{
    private readonly IContainerManagementOperationQueue _containerManagementOperationQueue;
    private readonly ILogger<ContainerManagementController> _logger;

    public ContainerManagementController(
        IContainerManagementOperationQueue containerManagementOperationQueue,
        ILogger<ContainerManagementController> logger
    )
    {
        _containerManagementOperationQueue = containerManagementOperationQueue;
        _logger = logger;
    }

    [HttpPost("[action]")]
    public async Task<IResult> QueueInfrastructureUpdate(
        [FromBody] InfrastructureComponentUpdateInput[] input,
        CancellationToken token = default
    )
    {
        try
        {
            var queueResult = await _containerManagementOperationQueue.QueueUpdateOperation(
                input,
                token
            );

            return Results.Ok(new WebOutcome<Guid> { Data = queueResult });
        }
        catch (ApiException ex)
        {
            _logger.Log(
                ex.LogLevel,
                "An api exception occured during request with message: {ExMessage}",
                ex.Message
            );

            return ex.ToResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An exception occured during request with message: {Message}",
                ex.Message
            );

            return Results.Problem(
                ApplicationConstants.ExceptionConstants.InternalError,
                null,
                (int)HttpStatusCode.InternalServerError
            );
        }
    }

    [HttpPost("[action]")]
    public async Task<IResult> QueueAndWaitForInfrastructureUpdate(
        [FromBody] InfrastructureComponentUpdateInput[] input,
        CancellationToken token = default
    )
    {
        try
        {
            var queueResult =
                await _containerManagementOperationQueue.QueueAndWaitForUpdateOperation(
                    input,
                    token
                );

            //Remove container summaries on api response
            queueResult = queueResult with
            {
                Components = queueResult.Components.Select(x =>
                {
                    x = x with { LatestContainerSummary = null };
                    return x;
                }).ToArray()
            };

            return Results.Ok(queueResult);
        }
        catch (ApiException ex)
        {
            _logger.Log(
                ex.LogLevel,
                "An api exception occured during request with message: {ExMessage}",
                ex.Message
            );

            return ex.ToResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An exception occured during request with message: {Message}",
                ex.Message
            );

            return Results.Problem(
                ApplicationConstants.ExceptionConstants.InternalError,
                null,
                (int)HttpStatusCode.InternalServerError
            );
        }
    }
}
