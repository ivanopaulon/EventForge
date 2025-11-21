using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for BusinessPartyDetailViewModel following Onda 3 pattern
/// </summary>
[Trait("Category", "Unit")]
public class BusinessPartyDetailViewModelTests : IDisposable
{
    private readonly Mock<IBusinessPartyService> _mockBusinessPartyService;
    private readonly Mock<ILookupCacheService> _mockLookupCacheService;
    private readonly Mock<ILogger<BusinessPartyDetailViewModel>> _mockLogger;
    private readonly BusinessPartyDetailViewModel _viewModel;

    public BusinessPartyDetailViewModelTests()
    {
        _mockBusinessPartyService = new Mock<IBusinessPartyService>();
        _mockLookupCacheService = new Mock<ILookupCacheService>();
        _mockLogger = new Mock<ILogger<BusinessPartyDetailViewModel>>();
        _viewModel = new BusinessPartyDetailViewModel(
            _mockBusinessPartyService.Object,
            _mockLookupCacheService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadEntityAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var expectedBusinessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            PartyType = BusinessPartyType.Cliente,
            Name = "Test Customer",
            TaxCode = "TESTCF123456",
            VatNumber = "IT12345678901",
            SdiCode = "ABCDEFG",
            Pec = "test@pec.it",
            IsActive = true,
            HasAccountingData = true,
            AddressCount = 1,
            ContactCount = 2,
            ReferenceCount = 1,
            CreatedAt = DateTime.UtcNow
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(expectedBusinessParty);

        // Act
        await _viewModel.LoadEntityAsync(businessPartyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(businessPartyId, _viewModel.Entity.Id);
        Assert.Equal("Test Customer", _viewModel.Entity.Name);
        Assert.Equal(BusinessPartyType.Cliente, _viewModel.Entity.PartyType);
        Assert.Equal("TESTCF123456", _viewModel.Entity.TaxCode);
        Assert.Equal("IT12345678901", _viewModel.Entity.VatNumber);
        Assert.False(_viewModel.IsNewEntity);
        Assert.False(_viewModel.IsAccountingLoaded);
        Assert.False(_viewModel.IsDocumentsLoaded);
        Assert.False(_viewModel.IsProductAnalysisLoaded);
    }

    [Fact]
    public async Task LoadEntityAsync_WithEmptyId_CreatesNewEntity()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.Equal(BusinessPartyType.Cliente, _viewModel.Entity.PartyType);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.False(_viewModel.Entity.HasAccountingData);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task LoadAccountingAsync_WithValidEntity_LoadsAccounting()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        var expectedAccounting = new BusinessPartyAccountingDto
        {
            Id = Guid.NewGuid(),
            BusinessPartyId = businessPartyId
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId))
            .ReturnsAsync(expectedAccounting);

        await _viewModel.LoadEntityAsync(businessPartyId);

        // Act
        await _viewModel.LoadAccountingAsync();

        // Assert
        Assert.NotNull(_viewModel.Accounting);
        Assert.Equal(expectedAccounting.Id, _viewModel.Accounting.Id);
        Assert.Equal(businessPartyId, _viewModel.Accounting.BusinessPartyId);
        Assert.True(_viewModel.IsAccountingLoaded);
    }

    [Fact]
    public async Task LoadAccountingAsync_WhenAlreadyLoaded_DoesNotReload()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        var expectedAccounting = new BusinessPartyAccountingDto
        {
            Id = Guid.NewGuid(),
            BusinessPartyId = businessPartyId
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId))
            .ReturnsAsync(expectedAccounting);

        await _viewModel.LoadEntityAsync(businessPartyId);
        await _viewModel.LoadAccountingAsync();

        // Act
        await _viewModel.LoadAccountingAsync();

        // Assert
        _mockBusinessPartyService.Verify(
            s => s.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId),
            Times.Once);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithValidEntity_LoadsDocuments()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        var expectedDocuments = new PagedResult<DocumentHeaderDto>
        {
            Items = new List<DocumentHeaderDto>
            {
                new DocumentHeaderDto { Id = Guid.NewGuid(), Number = "DOC-001" },
                new DocumentHeaderDto { Id = Guid.NewGuid(), Number = "DOC-002" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyDocumentsAsync(
                businessPartyId, null, null, null, null, null, 1, 20))
            .ReturnsAsync(expectedDocuments);

        await _viewModel.LoadEntityAsync(businessPartyId);

        // Act
        await _viewModel.LoadDocumentsAsync();

        // Assert
        Assert.NotNull(_viewModel.Documents);
        Assert.Equal(2, _viewModel.Documents.Count());
        Assert.True(_viewModel.IsDocumentsLoaded);
    }

    [Fact]
    public async Task LoadProductAnalysisAsync_WithValidEntity_LoadsAnalysis()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        var expectedAnalysis = new PagedResult<BusinessPartyProductAnalysisDto>
        {
            Items = new List<BusinessPartyProductAnalysisDto>
            {
                new BusinessPartyProductAnalysisDto { ProductId = Guid.NewGuid(), ProductName = "Product 1" },
                new BusinessPartyProductAnalysisDto { ProductId = Guid.NewGuid(), ProductName = "Product 2" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyProductAnalysisAsync(
                businessPartyId, null, null, null, null, 1, 20, null, true))
            .ReturnsAsync(expectedAnalysis);

        await _viewModel.LoadEntityAsync(businessPartyId);

        // Act
        await _viewModel.LoadProductAnalysisAsync();

        // Assert
        Assert.NotNull(_viewModel.ProductAnalysis);
        Assert.Equal(2, _viewModel.ProductAnalysis.Count());
        Assert.True(_viewModel.IsProductAnalysisLoaded);
    }

    [Fact]
    public async Task SaveEntityAsync_NewEntity_CreatesEntity()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        _viewModel.Entity!.Name = "New Customer";
        _viewModel.Entity.PartyType = BusinessPartyType.Supplier;
        _viewModel.Entity.TaxCode = "NEWCF123456";

        var createdEntity = new BusinessPartyDto
        {
            Id = Guid.NewGuid(),
            Name = "New Customer",
            PartyType = BusinessPartyType.Supplier,
            TaxCode = "NEWCF123456"
        };

        _mockBusinessPartyService.Setup(s => s.CreateBusinessPartyAsync(It.IsAny<CreateBusinessPartyDto>()))
            .ReturnsAsync(createdEntity);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.NotEqual(Guid.Empty, _viewModel.Entity.Id);
        Assert.False(_viewModel.IsNewEntity);
        _mockBusinessPartyService.Verify(
            s => s.CreateBusinessPartyAsync(It.Is<CreateBusinessPartyDto>(dto => 
                dto.Name == "New Customer" && 
                dto.PartyType == BusinessPartyType.Supplier)),
            Times.Once);
    }

    [Fact]
    public async Task SaveEntityAsync_ExistingEntity_UpdatesEntity()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        await _viewModel.LoadEntityAsync(businessPartyId);

        _viewModel.Entity!.Name = "Updated Customer";

        var updatedEntity = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Updated Customer",
            PartyType = BusinessPartyType.Cliente
        };

        _mockBusinessPartyService.Setup(s => s.UpdateBusinessPartyAsync(
                businessPartyId, It.IsAny<UpdateBusinessPartyDto>()))
            .ReturnsAsync(updatedEntity);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Customer", _viewModel.Entity.Name);
        _mockBusinessPartyService.Verify(
            s => s.UpdateBusinessPartyAsync(
                businessPartyId,
                It.Is<UpdateBusinessPartyDto>(dto => dto.Name == "Updated Customer")),
            Times.Once);
    }

    [Fact]
    public async Task HasUnsavedChanges_WithModifiedEntity_ReturnsTrue()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        await _viewModel.LoadEntityAsync(businessPartyId);

        // Act
        _viewModel.Entity!.Name = "Modified Name";

        // Assert
        Assert.True(_viewModel.HasUnsavedChanges());
    }

    [Fact]
    public async Task HasUnsavedChanges_WithUnmodifiedEntity_ReturnsFalse()
    {
        // Arrange
        var businessPartyId = Guid.NewGuid();
        var businessParty = new BusinessPartyDto
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente
        };

        _mockBusinessPartyService.Setup(s => s.GetBusinessPartyAsync(businessPartyId))
            .ReturnsAsync(businessParty);

        await _viewModel.LoadEntityAsync(businessPartyId);

        // Act & Assert
        Assert.False(_viewModel.HasUnsavedChanges());
    }

    [Fact]
    public async Task LoadAccountingAsync_WithNewEntity_DoesNotLoadAccounting()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Act
        await _viewModel.LoadAccountingAsync();

        // Assert
        Assert.Null(_viewModel.Accounting);
        Assert.False(_viewModel.IsAccountingLoaded);
        _mockBusinessPartyService.Verify(
            s => s.GetBusinessPartyAccountingByBusinessPartyIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithNewEntity_DoesNotLoadDocuments()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Act
        await _viewModel.LoadDocumentsAsync();

        // Assert
        Assert.NotNull(_viewModel.Documents);
        Assert.Empty(_viewModel.Documents);
        Assert.False(_viewModel.IsDocumentsLoaded);
        _mockBusinessPartyService.Verify(
            s => s.GetBusinessPartyDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<ApprovalStatus?>(),
                It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadProductAnalysisAsync_WithNewEntity_DoesNotLoadAnalysis()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Act
        await _viewModel.LoadProductAnalysisAsync();

        // Assert
        Assert.NotNull(_viewModel.ProductAnalysis);
        Assert.Empty(_viewModel.ProductAnalysis);
        Assert.False(_viewModel.IsProductAnalysisLoaded);
        _mockBusinessPartyService.Verify(
            s => s.GetBusinessPartyProductAnalysisAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }
}
