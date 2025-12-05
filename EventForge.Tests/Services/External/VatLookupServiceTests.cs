using EventForge.Server.Services.External;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace EventForge.Tests.Services.External;

/// <summary>
/// Unit tests for VatLookupService with REST provider and SOAP fallback.
/// </summary>
[Trait("Category", "Unit")]
public class VatLookupServiceTests
{
    private readonly Mock<ILogger<VatLookupService>> _mockLogger;
    private readonly IMemoryCache _cache;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly VatLookupService _service;

    public VatLookupServiceTests()
    {
        _mockLogger = new Mock<ILogger<VatLookupService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();

        // Default configuration
        _mockConfiguration.Setup(x => x["VatLookup:RestProviderUrlTemplate"])
            .Returns("https://api.vatcomply.com/vat?vat={country}{vat}");
        _mockConfiguration.Setup(x => x["VatLookup:RestTimeoutSeconds"])
            .Returns("10");
        _mockConfiguration.Setup(x => x.GetSection("VatLookup:RestTimeoutSeconds").Value)
            .Returns("10");

        _service = new VatLookupService(_httpClient, _cache, _mockLogger.Object, _mockConfiguration.Object);
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

    [Theory]
    [InlineData("IT00000000097")] // Invalid checksum
    [InlineData("IT12345678902")] // Invalid checksum
    [InlineData("IT99999999998")] // Invalid checksum
    public async Task LookupAsync_WithInvalidItalianChecksum_ReturnsInvalidResult(string vatNumber)
    {
        // Act
        var result = await _service.LookupAsync(vatNumber);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("checksum failed", result.ErrorMessage);
        Assert.Equal("IT", result.CountryCode);
    }

    [Theory]
    [InlineData("IT00000000000")] // Valid checksum
    [InlineData("IT12345678903")] // Valid checksum
    public async Task LookupAsync_WithValidItalianChecksum_ContinuesWithProviderLookup(string vatNumber)
    {
        // Arrange - Mock REST provider response
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": true,
                ""country_code"": ""IT"",
                ""vat_number"": ""00000000000"",
                ""company_name"": ""TEST COMPANY SRL"",
                ""company_address"": ""VIA ROMA 123\n00100 ROMA RM""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act
        var result = await _service.LookupAsync(vatNumber);

        // Assert
        Assert.NotNull(result);
        // The checksum passed, so it should have queried the provider
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task LookupAsync_WithRestProviderSuccess_ReturnsValidResult()
    {
        // Arrange
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": true,
                ""country_code"": ""FR"",
                ""vat_number"": ""12345678901"",
                ""company_name"": ""FRENCH COMPANY SARL"",
                ""company_address"": ""123 RUE DE LA PAIX\n75001 PARIS""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act
        var result = await _service.LookupAsync("FR12345678901");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("FR", result.CountryCode);
        Assert.Equal("12345678901", result.VatNumber);
        Assert.Equal("FRENCH COMPANY SARL", result.Name);
        Assert.NotNull(result.ParsedAddress);
        Assert.Equal("123 RUE DE LA PAIX", result.ParsedAddress.Street);
    }

    [Fact]
    public async Task LookupAsync_WithRestProviderInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": false,
                ""country_code"": ""FR"",
                ""vat_number"": ""99999999999""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act
        var result = await _service.LookupAsync("FR99999999999");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("FR", result.CountryCode);
    }

    [Fact]
    public async Task LookupAsync_RestProviderFails_FallsBackToViesSoap()
    {
        // Arrange
        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                callCount++;
                if (req.Method == HttpMethod.Get)
                {
                    // REST provider fails
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
                else if (req.Method == HttpMethod.Post)
                {
                    // SOAP fallback succeeds
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <checkVatResponse xmlns=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
      <countryCode>DE</countryCode>
      <vatNumber>123456789</vatNumber>
      <valid>true</valid>
      <name>GERMAN COMPANY GMBH</name>
      <address>BERLIN STRASSE 123</address>
    </checkVatResponse>
  </soap:Body>
</soap:Envelope>")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

        // Act
        var result = await _service.LookupAsync("DE123456789");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("DE", result.CountryCode);
        Assert.Equal("123456789", result.VatNumber);
        Assert.Equal("GERMAN COMPANY GMBH", result.Name);
        Assert.Equal("BERLIN STRASSE 123", result.Address);

        // Verify both REST and SOAP were called
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task LookupAsync_ViesSoapSuccess_ReturnsValidResult()
    {
        // Arrange - No REST provider configured, should go straight to SOAP
        _mockConfiguration.Setup(x => x["VatLookup:RestProviderUrlTemplate"])
            .Returns((string?)null);

        var service = new VatLookupService(_httpClient, _cache, _mockLogger.Object, _mockConfiguration.Object);

        var soapResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <checkVatResponse xmlns=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
      <countryCode>NL</countryCode>
      <vatNumber>123456789B01</vatNumber>
      <valid>true</valid>
      <name>DUTCH COMPANY BV</name>
      <address>AMSTERDAM STREET 456</address>
    </checkVatResponse>
  </soap:Body>
</soap:Envelope>")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(soapResponse);

        // Act
        var result = await service.LookupAsync("NL123456789B01");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("NL", result.CountryCode);
        Assert.Equal("123456789B01", result.VatNumber);
        Assert.Equal("DUTCH COMPANY BV", result.Name);
    }

    [Fact]
    public async Task LookupAsync_ViesSoapMissingNameAddress_ReturnsPlaceholders()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["VatLookup:RestProviderUrlTemplate"])
            .Returns((string?)null);

        var service = new VatLookupService(_httpClient, _cache, _mockLogger.Object, _mockConfiguration.Object);

        var soapResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <checkVatResponse xmlns=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
      <countryCode>BE</countryCode>
      <vatNumber>0123456789</vatNumber>
      <valid>true</valid>
      <name></name>
      <address></address>
    </checkVatResponse>
  </soap:Body>
</soap:Envelope>")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(soapResponse);

        // Act
        var result = await service.LookupAsync("BE0123456789");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("---", result.Name);
        Assert.Equal("---", result.Address);
    }

    [Fact]
    public async Task LookupAsync_AllProvidersFail_ReturnsErrorResult()
    {
        // Arrange - Both REST and SOAP fail
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        // Act
        var result = await _service.LookupAsync("ES12345678Z");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains("temporarily unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task LookupAsync_CachesValidResults()
    {
        // Arrange
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": true,
                ""country_code"": ""FR"",
                ""vat_number"": ""12345678901"",
                ""company_name"": ""CACHED COMPANY"",
                ""company_address"": ""CACHED ADDRESS""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act - First call should hit the API
        var result1 = await _service.LookupAsync("FR12345678901");
        
        // Act - Second call should return cached result
        var result2 = await _service.LookupAsync("FR12345678901");

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
    public async Task LookupAsync_CachesInvalidResults()
    {
        // Arrange - Invalid Italian checksum should be cached
        var vatNumber = "IT00000000097"; // Invalid checksum

        // Act - First call
        var result1 = await _service.LookupAsync(vatNumber);
        
        // Act - Second call should return cached result
        var result2 = await _service.LookupAsync(vatNumber);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.False(result1.IsValid);
        Assert.False(result2.IsValid);
        Assert.Equal(result1.ErrorMessage, result2.ErrorMessage);

        // Verify HTTP was never called (checksum validation prevented it)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task LookupAsync_ParsesItalianAddressCorrectly()
    {
        // Arrange
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": true,
                ""country_code"": ""DE"",
                ""vat_number"": ""123456789"",
                ""company_name"": ""TEST COMPANY"",
                ""company_address"": ""VIA GIUSEPPE VERDI 456\n20100 MILANO MI""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act
        var result = await _service.LookupAsync("DE123456789");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ParsedAddress);
        Assert.Equal("VIA GIUSEPPE VERDI 456", result.ParsedAddress.Street);
        Assert.Equal("MILANO", result.ParsedAddress.City);
        Assert.Equal("20100", result.ParsedAddress.PostalCode);
        Assert.Equal("MI", result.ParsedAddress.Province);
    }

    [Fact]
    public async Task LookupAsync_WithoutCountryCode_DefaultsToIT()
    {
        // Arrange - Valid Italian VAT with valid checksum
        var restResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""valid"": true,
                ""country_code"": ""IT"",
                ""vat_number"": ""00000000000"",
                ""company_name"": ""DEFAULT IT COMPANY"",
                ""company_address"": ""VIA ROMA 1""
            }")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(restResponse);

        // Act
        var result = await _service.LookupAsync("00000000000");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IT", result.CountryCode);
    }

    [Fact]
    public async Task LookupAsync_TemplateReplacement_ReplacesAllPlaceholders()
    {
        // Arrange
        string? capturedUrl = null;
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
                    Content = new StringContent(@"{""valid"": true, ""country_code"": ""FR"", ""vat_number"": ""12345678901""}")
                };
            });

        // Act
        await _service.LookupAsync("FR12345678901");

        // Assert
        Assert.NotNull(capturedUrl);
        Assert.Contains("FR", capturedUrl);
        Assert.Contains("12345678901", capturedUrl);
    }
}
