using System.Net;
using System.Net.Mime;
using BT.Common.Api.Helpers;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Api.Helpers.Models;
using Hetzner.Container.Management.Services;

namespace Hetzner.Container.Management.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (ApiException exception)
        {
            logger.Log(exception.LogLevel, exception,
                "A PokeGame exception of type: {ExceptionName} was thrown during request with status code: {StatusCode}",
                nameof(ApiException),
                exception.StatusCode);

            await SendExceptionResponseAsync(context, ApplicationConstants.ExceptionConstants.InternalError, (int)exception.StatusCode, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occured during request");

            await SendExceptionResponseAsync(context, ApplicationConstants.ExceptionConstants.InternalError,
                (int)HttpStatusCode.InternalServerError, logger);
        }
        
    }
    
    private static async Task SendExceptionResponseAsync(HttpContext context, string message, int statusCode, ILogger<ExceptionHandlingMiddleware> logger)
    {
        var foundCorrelationId = context.Response.Headers[ApiConstants.CorrelationIdHeader].ToString();
        context.Response.Clear();
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = statusCode;


        if (!string.IsNullOrEmpty(foundCorrelationId))
        {
            if (!context.Response.Headers.TryAdd(ApiConstants.CorrelationIdHeader, foundCorrelationId))
            {
                logger.LogWarning("Failed to add correlationId: {CorrelationId} to http response headers", foundCorrelationId);
            }
            else
            {
                logger.LogInformation("CorrelationId: {CorrelationId} added to response headers successfully", foundCorrelationId);
            }
        }
        
        await context.Response.WriteAsJsonAsync(new WebOutcome { ExceptionMessage = message });
    }
}