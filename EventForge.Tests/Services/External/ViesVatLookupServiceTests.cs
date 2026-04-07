using EventForge.Server.Services.External;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace EventForge.Tests.Services.External;

/// <summary>
/// Unit tests for ViesVatLookupService.
/// </summary>
[Trait("Category", "Unit")]
public class ViesVatLookupServiceTests
{
    private readonly Mock<ILogger<ViesVatLookupService>> _mockLogger;
    private readonly IMemoryCache _cache;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ViesVatLookupService _service;

    public ViesVatLookupServiceTests()
    {
        _mockLogger = new Mock<ILogger<ViesVatLookupService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new ViesVatLookupService(_httpClient, _cache, _mockLogger.Object);
    }

    [Fact]
    public async Task LookupAsync_WithEmptyVatNumber_ReturnsInvalidResult()
    {
        // Act
        var result = await _service.LookupAsync(string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("VAT number is required", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupAsync_WithValidVatNumber_ReturnsValidResult()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = true,
            countryCode = "IT",
            vatNumber = "12345678901",
            name = "MARIO ROSSI SRL",
            address = "VIA ROMA 123\n00100 ROMA RM"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupAsync("IT12345678901");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("IT", result.CountryCode);
        Assert.Equal("12345678901", result.VatNumber);
        Assert.Equal("MARIO ROSSI SRL", result.Name);
        Assert.NotNull(result.ParsedAddress);
        Assert.Equal("VIA ROMA 123", result.ParsedAddress.Street);
        Assert.Equal("ROMA", result.ParsedAddress.City);
        Assert.Equal("00100", result.ParsedAddress.PostalCode);
        Assert.Equal("RM", result.ParsedAddress.Province);
    }

    [Fact]
    public async Task LookupAsync_WithInvalidVatNumber_ReturnsInvalidResult()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = false,
            countryCode = "IT",
            vatNumber = "99999999999"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupAsync("IT99999999999");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task LookupAsync_WithoutCountryCode_DefaultsToIT()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = true,
            countryCode = "IT",
            vatNumber = "12345678901",
            name = "TEST COMPANY",
            address = "TEST ADDRESS"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupAsync("12345678901");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("IT", result.CountryCode);
    }

    [Fact]
    public async Task LookupAsync_WithServiceError_ReturnsErrorResult()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupAsync("IT12345678901");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("temporarily unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupAsync_CachesSuccessfulResults()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = true,
            countryCode = "IT",
            vatNumber = "12345678901",
            name = "CACHED COMPANY",
            address = "CACHED ADDRESS"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act - First call should hit the API
        var result1 = await _service.LookupAsync("IT12345678901");

        // Act - Second call should return cached result
        var result2 = await _service.LookupAsync("IT12345678901");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Name, result2.Name);

        // Verify HTTP was called only once (second call was cached)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task LookupAsync_ParsesItalianAddressCorrectly()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = true,
            countryCode = "IT",
            vatNumber = "12345678901",
            name = "TEST SRL",
            address = "VIA GIUSEPPE VERDI 456\n20100 MILANO MI"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.LookupAsync("IT12345678901");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ParsedAddress);
        Assert.Equal("VIA GIUSEPPE VERDI 456", result.ParsedAddress.Street);
        Assert.Equal("MILANO", result.ParsedAddress.City);
        Assert.Equal("20100", result.ParsedAddress.PostalCode);
        Assert.Equal("MI", result.ParsedAddress.Province);
    }
}
