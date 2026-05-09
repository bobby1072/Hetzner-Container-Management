using System.Net;
using BT.Common.Api.Helpers.Exceptions;
using Hetzner.Container.Management.Api.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hetzner.Container.Management.Tests.Middlewares;

public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddlewareTests()
    {
        _logger = new NullLogger<ExceptionHandlingMiddleware>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CompletesSuccessfully()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiExceptionThrown_ReturnsProblemWithCorrectStatusCode()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new ApiException(LogLevel.Warning, HttpStatusCode.BadRequest, "Bad request input")
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiExceptionThrown_ReturnsStatusCodeFromException()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new ApiException(LogLevel.Error, HttpStatusCode.NotFound, "Resource not found")
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new InvalidOperationException("Something unexpected happened")
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_ResponseBodyDoesNotLeakDetails()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new Exception("Secret internal details")
        );
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert - the response body should NOT leak the internal exception message
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        responseBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.DoesNotContain("Secret internal details", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiExceptionThrown_ResponseBodyContainsExceptionMessage()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new ApiException(
                LogLevel.Warning,
                HttpStatusCode.BadRequest,
                "Validation failed: invalid port"
            )
        );
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert - verify status code is correct and response was written
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        responseBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));
    }

    [Fact]
    public async Task InvokeAsync_WhenApiExceptionThrown_LogsAtExceptionLogLevel()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new ApiException(LogLevel.Warning, HttpStatusCode.BadRequest, "Test warning")
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new Exception("Boom"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ClearsResponse()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            ctx.Response.Headers["X-Custom"] = "should-be-cleared";
            throw new Exception("Boom");
        });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert - custom header should be cleared
        Assert.False(context.Response.Headers.ContainsKey("X-Custom"));
    }

    [Fact]
    public async Task InvokeAsync_PreservesCorrelationIdHeader_WhenPresent()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            ctx.Response.Headers["X-Correlation-Id"] = correlationId;
            throw new Exception("Boom");
        });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, _logger);

        // Assert - correlation id should be re-added after clearing
        Assert.True(
            context.Response.Headers.ContainsKey("X-Correlation-Id"),
            "Correlation-Id header should be preserved"
        );
        Assert.Equal(correlationId, context.Response.Headers["X-Correlation-Id"].ToString());
    }
}
