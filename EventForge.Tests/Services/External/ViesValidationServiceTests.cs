using EventForge.DTOs.External;
using EventForge.Server.Services.External;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace EventForge.Tests.Services.External;

/// <summary>
/// Unit tests for ViesValidationService using the simplified VIES REST API.
/// </summary>
[Trait("Category", "Unit")]
public class ViesValidationServiceTests
{
    private readonly Mock<ILogger<ViesValidationService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ViesValidationService _service;

    public ViesValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ViesValidationService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new ViesValidationService(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateVatAsync_WithValidVatNumber_ReturnsValidResult()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = true,
            requestDate = "2025-12-05T16:22:05.386Z",
            userError = "VALID",
            name = "SOLIDATA SRL",
            address = "VIA A DE GASPERI 5/A \n31039 RIESE PIO X TV\n",
            requestIdentifier = "",
            originalVatNumber = "03640560268",
            vatNumber = "03640560268",
            viesApproximate = new
            {
                name = "---",
                street = "---",
                postalCode = "---",
                city = "---",
                companyType = "---",
                matchName = 3,
                matchStreet = 3,
                matchPostalCode = 3,
                matchCity = 3,
                matchCompanyType = 3
            }
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri!.ToString().Contains("/IT/vat/03640560268")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.ValidateVatAsync("IT", "03640560268");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("SOLIDATA SRL", result.Name);
        Assert.Contains("VIA A DE GASPERI", result.Address);
    }

    [Fact]
    public async Task ValidateVatAsync_WithInvalidVatNumber_ReturnsInvalidResult()
    {
        // Arrange
        var viesResponse = new
        {
            isValid = false,
            requestDate = "2025-12-05T16:22:05.386Z",
            userError = "INVALID",
            name = "",
            address = "",
            requestIdentifier = "",
            originalVatNumber = "99999999999",
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
        var result = await _service.ValidateVatAsync("IT", "99999999999");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateVatAsync_WithServiceError_ReturnsNull()
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
        var result = await _service.ValidateVatAsync("IT", "12345678901");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateVatAsync_CleansVatNumber_RemovesSpacesAndDashes()
    {
        // Arrange
        string? capturedUrl = null;
        
        var viesResponse = new
        {
            isValid = true,
            requestDate = "2025-12-05T16:22:05.386Z",
            userError = "VALID",
            name = "TEST COMPANY",
            address = "TEST ADDRESS",
            vatNumber = "12345678901"
        };

        var responseContent = JsonSerializer.Serialize(viesResponse);
        
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent)
                };
            });

        // Act
        await _service.ValidateVatAsync("IT", "IT 123-456.789 01");

        // Assert
        Assert.NotNull(capturedUrl);
        Assert.Contains("/IT/vat/12345678901", capturedUrl);
    }

    [Fact]
    public async Task ValidateVatAsync_WithHttpRequestException_ReturnsNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.ValidateVatAsync("IT", "12345678901");

        // Assert
        Assert.Null(result);
    }
}
