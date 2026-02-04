using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using BT.Common.Services.Concrete;
using Hetzner.Container.Management.Services;

namespace Hetzner.Container.Management.Api.Middlewares;

public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext httpContext)
    {
        using var activity = TelemetryHelperService.ActivitySource.StartActivity();
        var apiKeysFromConfig =
            httpContext.RequestServices.GetRequiredKeyedService<string[]>(ApplicationConstants.ServiceKeys.ApiKeyServiceKey);
        
        activity?.SetTag(nameof(apiKeysFromConfig), apiKeysFromConfig);
        
        if (!httpContext.Request.Headers.TryGetValue(ApplicationConstants.CustomHeaders.ApiKeyHeaderName,
                out var apiKeyFromRequest) || string.IsNullOrWhiteSpace(apiKeyFromRequest) || !apiKeysFromConfig.Contains(apiKeyFromRequest.ToString()))
        {
            throw new ApiServerException(HttpStatusCode.Unauthorized, "You do not have a valid API key attached to this request.");
        }
        
        return _next.Invoke(httpContext);
    }
}