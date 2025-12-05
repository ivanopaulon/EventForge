using EventForge.DTOs.External;
using EventForge.Server.Services.External;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace EventForge.Tests.Services.External;

/// <summary>
/// Unit tests for simplified VatLookupService using VIES REST API.
/// </summary>
[Trait("Category", "Unit")]
public class VatLookupServiceTests
{
    private readonly Mock<ILogger<VatLookupService>> _mockLogger;
    private readonly IMemoryCache _cache;
    private readonly Mock<IViesValidationService> _mockViesService;
    private readonly VatLookupService _service;

    public VatLookupServiceTests()
    {
        _mockLogger = new Mock<ILogger<VatLookupService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockViesService = new Mock<IViesValidationService>();

        _service = new VatLookupService(_mockViesService.Object, _cache, _mockLogger.Object);
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
        var viesResponse = new ViesValidationResponseDto
        {
            IsValid = true,
            Name = "FRENCH COMPANY SARL",
            Address = "123 RUE DE LA PAIX\n75001 PARIS"
        };

        _mockViesService
            .Setup(x => x.ValidateVatAsync("FR", "12345678901", It.IsAny<CancellationToken>()))
            .ReturnsAsync(viesResponse);

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
    public async Task LookupAsync_WithInvalidVatNumber_ReturnsInvalidResult()
    {
        // Arrange
        var viesResponse = new ViesValidationResponseDto
        {
            IsValid = false,
            Name = string.Empty,
            Address = string.Empty
        };

        _mockViesService
            .Setup(x => x.ValidateVatAsync("FR", "99999999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(viesResponse);

        // Act
        var result = await _service.LookupAsync("FR99999999999");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("FR", result.CountryCode);
    }

    [Fact]
    public async Task LookupAsync_ServiceReturnsNull_ReturnsErrorResult()
    {
        // Arrange
        _mockViesService
            .Setup(x => x.ValidateVatAsync("ES", "12345678Z", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ViesValidationResponseDto?)null);

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
        var viesResponse = new ViesValidationResponseDto
        {
            IsValid = true,
            Name = "CACHED COMPANY",
            Address = "CACHED ADDRESS"
        };

        _mockViesService
            .Setup(x => x.ValidateVatAsync("FR", "12345678901", It.IsAny<CancellationToken>()))
            .ReturnsAsync(viesResponse);

        // Act - First call should hit the service
        var result1 = await _service.LookupAsync("FR12345678901");
        
        // Act - Second call should return cached result
        var result2 = await _service.LookupAsync("FR12345678901");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Name, result2.Name);
        
        // Verify service was called only once (second call was cached)
        _mockViesService.Verify(
            x => x.ValidateVatAsync("FR", "12345678901", It.IsAny<CancellationToken>()),
            Times.Once()
        );
    }

    [Fact]
    public async Task LookupAsync_ParsesItalianAddressCorrectly()
    {
        // Arrange
        var viesResponse = new ViesValidationResponseDto
        {
            IsValid = true,
            Name = "TEST COMPANY",
            Address = "VIA GIUSEPPE VERDI 456\n20100 MILANO MI"
        };

        _mockViesService
            .Setup(x => x.ValidateVatAsync("IT", "123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(viesResponse);

        // Act
        var result = await _service.LookupAsync("IT123456789");

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
        // Arrange
        var viesResponse = new ViesValidationResponseDto
        {
            IsValid = true,
            Name = "DEFAULT IT COMPANY",
            Address = "VIA ROMA 1"
        };

        _mockViesService
            .Setup(x => x.ValidateVatAsync("IT", "00000000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(viesResponse);

        // Act
        var result = await _service.LookupAsync("00000000000");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IT", result.CountryCode);
    }
}
