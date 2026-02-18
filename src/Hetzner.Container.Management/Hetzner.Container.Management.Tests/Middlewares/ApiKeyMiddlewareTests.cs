using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Api.Middlewares;
using Hetzner.Container.Management.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hetzner.Container.Management.Tests.Middlewares;

public sealed class ApiKeyMiddlewareTests
{
    private const string ValidApiKey = "test-api-key-123";
    private const string AnotherValidApiKey = "test-api-key-456";
    private static readonly string[] ValidApiKeys = [ValidApiKey, AnotherValidApiKey];

    private static ApiKeyMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        return new ApiKeyMiddleware(next ?? (_ => Task.CompletedTask));
    }

    private static HttpContext CreateHttpContext(string? apiKeyHeaderValue = null)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton(ApplicationConstants.ServiceKeys.ApiKeyServiceKey, ValidApiKeys);
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };

        if (apiKeyHeaderValue is not null)
        {
            context.Request.Headers[ApplicationConstants.CustomHeaders.ApiKeyHeaderName] =
                apiKeyHeaderValue;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithValidApiKey_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext(ValidApiKey);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithSecondValidApiKey_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext(AnotherValidApiKey);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingApiKeyHeader_ThrowsUnauthorized()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(); // no header

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiServerException>(
            () => middleware.InvokeAsync(context)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyApiKey_ThrowsUnauthorized()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiServerException>(
            () => middleware.InvokeAsync(context)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceApiKey_ThrowsUnauthorized()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("   ");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiServerException>(
            () => middleware.InvokeAsync(context)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidApiKey_ThrowsUnauthorized()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("wrong-api-key");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiServerException>(
            () => middleware.InvokeAsync(context)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("valid API key", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotCallNext_WhenApiKeyInvalid()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext("wrong-api-key");

        // Act & Assert
        await Assert.ThrowsAsync<ApiServerException>(() => middleware.InvokeAsync(context));
        Assert.False(nextCalled);
    }
}
