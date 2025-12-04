using EventForge.DTOs.External;
using EventForge.Server.Controllers;
using EventForge.Server.Services.External;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for VatLookupController.
/// </summary>
[Trait("Category", "Unit")]
public class VatLookupControllerTests
{
    private readonly Mock<IVatLookupService> _mockVatLookupService;
    private readonly Mock<ILogger<VatLookupController>> _mockLogger;
    private readonly VatLookupController _controller;

    public VatLookupControllerTests()
    {
        _mockVatLookupService = new Mock<IVatLookupService>();
        _mockLogger = new Mock<ILogger<VatLookupController>>();
        _controller = new VatLookupController(_mockVatLookupService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Lookup_WithEmptyVatNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Lookup(string.Empty, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Lookup_WithValidVatNumber_ReturnsOkResult()
    {
        // Arrange
        var vatLookupResult = new VatLookupResult
        {
            IsValid = true,
            CountryCode = "IT",
            VatNumber = "12345678901",
            Name = "TEST COMPANY SRL",
            Address = "VIA ROMA 123\n00100 ROMA RM",
            ParsedAddress = new ParsedAddress
            {
                Street = "VIA ROMA 123",
                City = "ROMA",
                PostalCode = "00100",
                Province = "RM"
            }
        };

        _mockVatLookupService
            .Setup(x => x.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vatLookupResult);

        // Act
        var result = await _controller.Lookup("IT12345678901", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VatLookupResultDto>(okResult.Value);
        Assert.True(dto.IsValid);
        Assert.Equal("IT", dto.CountryCode);
        Assert.Equal("12345678901", dto.VatNumber);
        Assert.Equal("TEST COMPANY SRL", dto.Name);
        Assert.Equal("VIA ROMA 123", dto.Street);
        Assert.Equal("ROMA", dto.City);
        Assert.Equal("00100", dto.PostalCode);
        Assert.Equal("RM", dto.Province);
    }

    [Fact]
    public async Task Lookup_WithInvalidVatNumber_ReturnsOkWithInvalidResult()
    {
        // Arrange
        var vatLookupResult = new VatLookupResult
        {
            IsValid = false,
            CountryCode = "IT",
            VatNumber = "99999999999",
            ErrorMessage = "Invalid VAT number"
        };

        _mockVatLookupService
            .Setup(x => x.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vatLookupResult);

        // Act
        var result = await _controller.Lookup("IT99999999999", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VatLookupResultDto>(okResult.Value);
        Assert.False(dto.IsValid);
        Assert.Equal("Invalid VAT number", dto.ErrorMessage);
    }

    [Fact]
    public async Task Lookup_WhenServiceReturnsNull_ReturnsServiceUnavailable()
    {
        // Arrange
        _mockVatLookupService
            .Setup(x => x.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VatLookupResult?)null);

        // Act
        var result = await _controller.Lookup("IT12345678901", CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Lookup_CallsServiceWithCorrectVatNumber()
    {
        // Arrange
        var vatNumber = "IT12345678901";
        var vatLookupResult = new VatLookupResult { IsValid = true };

        _mockVatLookupService
            .Setup(x => x.LookupAsync(vatNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vatLookupResult);

        // Act
        await _controller.Lookup(vatNumber, CancellationToken.None);

        // Assert
        _mockVatLookupService.Verify(
            x => x.LookupAsync(vatNumber, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
