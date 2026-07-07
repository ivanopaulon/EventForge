using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Moq.Protected;
using Prym.DTOs.Tenants;
using Prym.Web.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace EventForge.Tests.Services.Auth;

/// <summary>
/// Unit tests per GetAvailableTenantsWithStatusAsync — verifica che IsServerUnreachable
/// sia true solo per errori di rete/timeout e false per risposte 2xx (anche lista vuota).
/// </summary>
[Trait("Category", "Unit")]
public class AuthServiceTenantsTests
{
    private static (AuthService service, Mock<HttpMessageHandler> handler) CreateService(
        HttpResponseMessage? response = null,
        Exception? throwException = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        var setup = mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        if (throwException != null)
            setup.ThrowsAsync(throwException);
        else
            setup.ReturnsAsync(response!);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://test-server/")
        };

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("ApiClient")).Returns(httpClient);

        var mockServerConfig = new Mock<IServerConfigService>();
        mockServerConfig
            .Setup(s => s.GetServerUrlAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://test-server/");

        var mockJsRuntime = new Mock<IJSRuntime>();
        var mockLogger = new Mock<ILogger<AuthService>>();

        var service = new AuthService(
            mockFactory.Object,
            mockJsRuntime.Object,
            mockLogger.Object,
            mockServerConfig.Object);

        return (service, mockHandler);
    }

    private static HttpResponseMessage JsonResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    // (a) Server risponde con lista vuota → IsServerUnreachable == false
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_EmptyList_IsServerUnreachableFalse()
    {
        var (service, _) = CreateService(JsonResponse(new List<TenantResponseDto>()));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.False(result.IsServerUnreachable);
        Assert.Empty(result.Tenants);
    }

    // (a) Server risponde con lista di tenant → IsServerUnreachable == false
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_WithTenants_IsServerUnreachableFalse()
    {
        var tenants = new List<TenantResponseDto>
        {
            new() { Id = Guid.NewGuid(), Name = "tenant1", DisplayName = "Tenant 1" }
        };
        var (service, _) = CreateService(JsonResponse(tenants));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.False(result.IsServerUnreachable);
        Assert.Single(result.Tenants);
        Assert.Equal("tenant1", result.Tenants[0].Name);
    }

    // (b) HttpRequestException → IsServerUnreachable == true, Tenants vuoto
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_NetworkException_IsServerUnreachableTrue()
    {
        var (service, _) = CreateService(throwException: new HttpRequestException("Connection refused"));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.True(result.IsServerUnreachable);
        Assert.Empty(result.Tenants);
    }

    // (b) Timeout (TaskCanceledException) → IsServerUnreachable == true, Tenants vuoto
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_Timeout_IsServerUnreachableTrue()
    {
        var (service, _) = CreateService(throwException: new TaskCanceledException("Timeout"));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.True(result.IsServerUnreachable);
        Assert.Empty(result.Tenants);
    }

    // (b) 5xx dal server → IsServerUnreachable == true (server non disponibile/guasto → retry sensato)
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_ServerError500_IsServerUnreachableTrue()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.True(result.IsServerUnreachable);
        Assert.Empty(result.Tenants);
    }

    // 4xx dal server → IsServerUnreachable == false (server raggiungibile, errore applicativo)
    [Fact]
    public async Task GetAvailableTenantsWithStatusAsync_ClientError401_IsServerUnreachableFalse()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await service.GetAvailableTenantsWithStatusAsync();

        Assert.False(result.IsServerUnreachable);
        Assert.Empty(result.Tenants);
    }

    // GetAvailableTenantsAsync (wrapper retrocompatibile) restituisce lista vuota senza eccezione
    [Fact]
    public async Task GetAvailableTenantsAsync_WrapperRetrocompatible_ReturnsEmpty()
    {
        var (service, _) = CreateService(throwException: new HttpRequestException("Connection refused"));

        var result = await service.GetAvailableTenantsAsync();

        Assert.Empty(result);
    }
}
