using EventForge.Client.Services;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services;

/// <summary>
/// Unit tests for BusinessPartyService to verify interface implementation and basic functionality.
/// </summary>
[Trait("Category", "Unit")]
public class BusinessPartyServiceTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<BusinessPartyService>> _mockLogger;
    private readonly Mock<ILoadingDialogService> _mockLoadingDialog;
    private readonly IBusinessPartyService _service;

    public BusinessPartyServiceTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<BusinessPartyService>>();
        _mockLoadingDialog = new Mock<ILoadingDialogService>();
        _service = new BusinessPartyService(_mockHttpClient.Object, _mockLogger.Object, _mockLoadingDialog.Object);
    }

    [Fact]
    public async Task GetBusinessPartiesAsync_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<BusinessPartyDto>
        {
            Items = new List<BusinessPartyDto>
            {
                new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Test Party" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<BusinessPartyDto>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetBusinessPartiesAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Test Party", result.Items.First().Name);
    }

    [Fact]
    public async Task GetBusinessPartyAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new BusinessPartyDto { Id = id, Name = "Test Party" };
        
        _mockHttpClient.Setup(x => x.GetAsync<BusinessPartyDto>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.GetBusinessPartyAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Party", result.Name);
    }

    [Fact]
    public async Task CreateBusinessPartyAsync_WithValidDto_ReturnsCreatedEntity()
    {
        // Arrange
        var createDto = new CreateBusinessPartyDto { Name = "New Party" };
        var expected = new BusinessPartyDto { Id = Guid.NewGuid(), Name = "New Party" };
        
        _mockHttpClient.Setup(x => x.PostAsync<CreateBusinessPartyDto, BusinessPartyDto>(
            It.IsAny<string>(),
            createDto,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.CreateBusinessPartyAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Party", result.Name);
    }

    [Fact]
    public async Task GetBusinessPartiesByTypeAsync_WithValidType_ReturnsEntities()
    {
        // Arrange
        var partyType = BusinessPartyType.Cliente;
        var expected = new List<BusinessPartyDto>
        {
            new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Customer 1", PartyType = partyType },
            new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Customer 2", PartyType = partyType }
        };
        
        _mockHttpClient.Setup(x => x.GetAsync<IEnumerable<BusinessPartyDto>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.GetBusinessPartiesByTypeAsync(partyType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, bp => Assert.Equal(partyType, bp.PartyType));
    }

    [Fact]
    public async Task SearchBusinessPartiesAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var searchTerm = "Test";
        var expected = new List<BusinessPartyDto>
        {
            new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Test Party 1" },
            new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Test Party 2" }
        };
        
        _mockHttpClient.Setup(x => x.GetAsync<IEnumerable<BusinessPartyDto>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.SearchBusinessPartiesAsync(searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateBusinessPartyAsync_WithValidDto_ReturnsUpdatedEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateBusinessPartyDto { Name = "Updated Party" };
        var expected = new BusinessPartyDto { Id = id, Name = "Updated Party" };
        
        _mockHttpClient.Setup(x => x.PutAsync<UpdateBusinessPartyDto, BusinessPartyDto>(
            It.IsAny<string>(),
            updateDto,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.UpdateBusinessPartyAsync(id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Updated Party", result.Name);
    }

    [Fact]
    public async Task DeleteBusinessPartyAsync_WithValidId_CompletesSuccessfully()
    {
        // Arrange
        var id = Guid.NewGuid();
        
        _mockHttpClient.Setup(x => x.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.CompletedTask);

        // Act & Assert
        await _service.DeleteBusinessPartyAsync(id);
        
        _mockHttpClient.Verify(x => x.DeleteAsync(
            It.Is<string>(url => url.Contains(id.ToString())),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetBusinessPartyAccountingByBusinessPartyIdAsync_WithValidId_ReturnsAccounting()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var expected = new BusinessPartyAccountingDto 
        { 
            Id = Guid.NewGuid(), 
            BusinessPartyId = businessPartyId 
        };
        
        _mockHttpClient.Setup(x => x.GetAsync<BusinessPartyAccountingDto>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(businessPartyId, result.BusinessPartyId);
    }
}
