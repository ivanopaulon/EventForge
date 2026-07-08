using Microsoft.AspNetCore.Http;
using Prym.Agent.Configuration;
using Prym.Agent.Middleware;
using System.Text;

namespace Prym.Agent.Tests;

/// <summary>
/// Tests for <see cref="BasicAuthMiddleware"/> covering the internal-token validation path
/// used by the update proxy endpoints (<c>/api/agent/updates/check</c>,
/// <c>/api/agent/updates/download</c>) and the UI Basic Auth path.
/// </summary>
public class BasicAuthMiddlewareTests
{
    private const string ValidToken = "super-secret-token-123";
    private const string ValidUsername = "admin";
    private const string ValidPassword = "Admin#123!";

    // ── helpers ──────────────────────────────────────────────────────────────

    private static AgentOptions BuildOptions(
        string internalToken = ValidToken,
        string username = ValidUsername,
        string password = ValidPassword) =>
        new()
        {
            InternalApiToken = internalToken,
            UI = new UiOptions { Username = username, Password = password }
        };

    private static DefaultHttpContext BuildContext(
        string path,
        string? internalTokenHeader = null,
        string? authHeader = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.Body = new MemoryStream();

        if (internalTokenHeader is not null)
            ctx.Request.Headers["X-Agent-Internal-Token"] = internalTokenHeader;

        if (authHeader is not null)
            ctx.Request.Headers.Authorization = authHeader;

        return ctx;
    }

    private static string MakeBasicHeader(string user, string pass)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
        return $"Basic {encoded}";
    }

    private static Task NextDelegate(HttpContext _) => Task.CompletedTask;

    // ── internal paths: /api/agent/updates/check ────────────────────────────

    [Fact]
    public async Task UpdatesCheck_CorrectToken_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check", internalTokenHeader: ValidToken);

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesCheck_WrongToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check", internalTokenHeader: "wrong-token");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesCheck_MissingToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesCheck_EmptyToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check", internalTokenHeader: "");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    // ── internal paths: /api/agent/updates/download ──────────────────────────

    [Fact]
    public async Task UpdatesDownload_CorrectToken_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/download", internalTokenHeader: ValidToken);

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesDownload_WrongToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/download", internalTokenHeader: "bad");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesDownload_MissingToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/download");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    // ── internal paths: legacy (empty InternalApiToken) ──────────────────────

    [Fact]
    public async Task UpdatesCheck_EmptyConfiguredToken_AllowsWithoutHeader()
    {
        // Legacy localhost-trust: when no token is configured, the middleware
        // lets the request through without checking X-Agent-Internal-Token.
        var opts = BuildOptions(internalToken: "");
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check");

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesDownload_EmptyConfiguredToken_AllowsWithoutHeader()
    {
        var opts = BuildOptions(internalToken: "");
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/download");

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── internal paths: case-sensitive path matching ─────────────────────────

    [Fact]
    public async Task UpdatesCheck_PathCaseInsensitive_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/API/AGENT/UPDATES/CHECK", internalTokenHeader: ValidToken);

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── timing-safe comparison: different-length tokens always 401 ───────────

    [Fact]
    public async Task UpdatesCheck_LongerToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check",
            internalTokenHeader: ValidToken + "-extra");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UpdatesCheck_ShorterToken_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/updates/check",
            internalTokenHeader: ValidToken[..^1]);

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    // ── UI Basic Auth path ────────────────────────────────────────────────────

    [Fact]
    public async Task UIPath_ValidBasicAuth_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/status",
            authHeader: MakeBasicHeader(ValidUsername, ValidPassword));

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UIPath_WrongPassword_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/status",
            authHeader: MakeBasicHeader(ValidUsername, "wrong-pass"));

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UIPath_MissingAuth_Returns401()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/status");

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UIPath_DisabledUI_Returns503()
    {
        var opts = BuildOptions(username: "", password: "");
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/status");

        await mw.InvokeAsync(ctx);

        Assert.Equal(503, ctx.Response.StatusCode);
    }

    // ── health probe always passes without auth ───────────────────────────────

    [Fact]
    public async Task HealthProbe_NoAuth_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/api/agent/health");

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task HealthProbe_CaseInsensitive_Passes()
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext("/API/AGENT/HEALTH");

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── static asset paths bypass auth ───────────────────────────────────────

    [Theory]
    [InlineData("/css/app.css")]
    [InlineData("/js/bundle.js")]
    [InlineData("/images/logo.png")]
    [InlineData("/_framework/blazor.webassembly.js")]
    public async Task StaticPaths_NoAuth_Pass(string path)
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext(path);

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── other internal queue paths use token auth ────────────────────────────

    [Theory]
    [InlineData("/api/agent/pending-installs")]
    [InlineData("/api/agent/install-now")]
    [InlineData("/api/agent/unblock-queue")]
    public async Task OtherInternalPaths_CorrectToken_Pass(string path)
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext(path, internalTokenHeader: ValidToken);

        await mw.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/agent/pending-installs")]
    [InlineData("/api/agent/install-now")]
    [InlineData("/api/agent/unblock-queue")]
    public async Task OtherInternalPaths_MissingToken_Return401(string path)
    {
        var opts = BuildOptions();
        var mw = new BasicAuthMiddleware(NextDelegate, opts);
        var ctx = BuildContext(path);

        await mw.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }
}
