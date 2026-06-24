using Microsoft.AspNetCore.Http;
using Prym.Agent.Configuration;
using Prym.Agent.Middleware;

namespace Prym.Agent.Tests;

public class BasicAuthMiddlewareInternalTokenTests
{
    private const string InternalTokenHeader = "X-Agent-Internal-Token";
    private const string ValidToken = "agent-secret";

    [Theory]
    [InlineData("/api/agent/updates/check", "GET", StatusCodes.Status200OK)]
    [InlineData("/api/agent/updates/download", "POST", StatusCodes.Status202Accepted)]
    public async Task InternalUpdateEndpoint_ValidToken_AllowsRequest(
        string path,
        string method,
        int expectedStatusCode)
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            _.Response.StatusCode = expectedStatusCode;
            return Task.CompletedTask;
        });

        var context = CreateContext(path, method);
        context.Request.Headers[InternalTokenHeader] = ValidToken;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/agent/updates/check", "GET")]
    [InlineData("/api/agent/updates/download", "POST")]
    public async Task InternalUpdateEndpoint_MissingToken_Returns401(string path, string method)
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            _.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        var context = CreateContext(path, method);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/agent/updates/check", "GET")]
    [InlineData("/api/agent/updates/download", "POST")]
    public async Task InternalUpdateEndpoint_WrongToken_Returns401(string path, string method)
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            _.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        var context = CreateContext(path, method);
        context.Request.Headers[InternalTokenHeader] = "wrong-token";

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    private static BasicAuthMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, new AgentOptions
        {
            InternalApiToken = ValidToken,
            UI = new UiOptions
            {
                Username = "admin",
                Password = "Admin#123!"
            }
        });

    private static DefaultHttpContext CreateContext(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
