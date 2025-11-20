using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for DocumentHeaderDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentHeaderDetailViewModelTests : IDisposable
{
    private readonly Mock<IDocumentHeaderService> _mockDocumentHeaderService;
    private readonly Mock<IDocumentTypeService> _mockDocumentTypeService;
    private readonly Mock<IBusinessPartyService> _mockBusinessPartyService;
    private readonly Mock<ILogger<DocumentHeaderDetailViewModel>> _mockLogger;
    private readonly DocumentHeaderDetailViewModel _viewModel;

    public DocumentHeaderDetailViewModelTests()
    {
        _mockDocumentHeaderService = new Mock<IDocumentHeaderService>();
        _mockDocumentTypeService = new Mock<IDocumentTypeService>();
        _mockBusinessPartyService = new Mock<IBusinessPartyService>();
        _mockLogger = new Mock<ILogger<DocumentHeaderDetailViewModel>>();
        _viewModel = new DocumentHeaderDetailViewModel(
            _mockDocumentHeaderService.Object,
            _mockDocumentTypeService.Object,
            _mockBusinessPartyService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var documentHeaderId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();
        var expectedDocumentHeader = new DocumentHeaderDto
        {
            Id = documentHeaderId,
            DocumentTypeId = documentTypeId,
            DocumentTypeName = "Invoice",
            Number = "INV-2024-001",
            Date = DateTime.UtcNow,
            BusinessPartyId = businessPartyId,
            BusinessPartyName = "Test Customer",
            TotalGrossAmount = 1000m,
            Status = DocumentStatus.Draft
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>
            {
                new DocumentTypeDto { Id = documentTypeId, Code = "INV", Name = "Invoice" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };

        var businessParties = new PagedResult<BusinessPartyDto>
        {
            Items = new List<BusinessPartyDto>
            {
                new BusinessPartyDto { Id = businessPartyId, Name = "Test Customer" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentHeaderService.Setup(s => s.GetDocumentHeaderByIdAsync(documentHeaderId, false))
            .ReturnsAsync(expectedDocumentHeader);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);
        _mockBusinessPartyService.Setup(s => s.GetBusinessPartiesAsync(1, 100))
            .ReturnsAsync(businessParties);

        // Act
        await _viewModel.LoadEntityAsync(documentHeaderId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentHeaderId, _viewModel.Entity.Id);
        Assert.Equal("INV-2024-001", _viewModel.Entity.Number);
        Assert.Equal(documentTypeId, _viewModel.Entity.DocumentTypeId);
        Assert.Equal(businessPartyId, _viewModel.Entity.BusinessPartyId);
        Assert.Equal(1000m, _viewModel.Entity.TotalGrossAmount);
        Assert.Equal(DocumentStatus.Draft, _viewModel.Entity.Status);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.DocumentTypes);
        Assert.NotNull(_viewModel.BusinessParties);
        Assert.Single(_viewModel.DocumentTypes);
        Assert.Single(_viewModel.BusinessParties);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultDocumentHeader()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Number);
        Assert.Equal(Guid.Empty, _viewModel.Entity.DocumentTypeId);
        Assert.Equal(Guid.Empty, _viewModel.Entity.BusinessPartyId);
        Assert.True(_viewModel.Entity.IsFiscal);
        Assert.Equal(PaymentStatus.Pending, _viewModel.Entity.PaymentStatus);
        Assert.Equal(ApprovalStatus.Pending, _viewModel.Entity.ApprovalStatus);
        Assert.Equal(DocumentStatus.Draft, _viewModel.Entity.Status);
        Assert.Equal("EUR", _viewModel.Entity.Currency);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var documentTypeId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();
        _viewModel.Entity!.DocumentTypeId = documentTypeId;
        _viewModel.Entity.BusinessPartyId = businessPartyId;
        _viewModel.Entity.Number = "INV-2024-001";
        _viewModel.Entity.Date = DateTime.UtcNow;
        _viewModel.Entity.TotalGrossAmount = 1500m;
        
        var createdDocumentHeader = new DocumentHeaderDto
        {
            Id = Guid.NewGuid(),
            DocumentTypeId = documentTypeId,
            DocumentTypeName = "Invoice",
            BusinessPartyId = businessPartyId,
            BusinessPartyName = "Test Customer",
            Number = "INV-2024-001",
            Date = _viewModel.Entity.Date,
            TotalGrossAmount = 1500m,
            Status = DocumentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _mockDocumentHeaderService.Setup(s => s.CreateDocumentHeaderAsync(
            It.IsAny<CreateDocumentHeaderDto>()))
            .ReturnsAsync(createdDocumentHeader);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdDocumentHeader.Id, _viewModel.Entity.Id);
        Assert.Equal("INV-2024-001", _viewModel.Entity.Number);
        Assert.Equal(documentTypeId, _viewModel.Entity.DocumentTypeId);
        Assert.Equal(businessPartyId, _viewModel.Entity.BusinessPartyId);
        Assert.Equal(1500m, _viewModel.Entity.TotalGrossAmount);
        Assert.False(_viewModel.IsNewEntity);
        _mockDocumentHeaderService.Verify(s => s.CreateDocumentHeaderAsync(
            It.IsAny<CreateDocumentHeaderDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var documentHeaderId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();
        var existingDocumentHeader = new DocumentHeaderDto
        {
            Id = documentHeaderId,
            DocumentTypeId = documentTypeId,
            BusinessPartyId = businessPartyId,
            Number = "INV-2024-001",
            Date = DateTime.UtcNow,
            TotalGrossAmount = 1000m,
            Status = DocumentStatus.Draft,
            Notes = "Original notes"
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        var businessParties = new PagedResult<BusinessPartyDto>
        {
            Items = new List<BusinessPartyDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentHeaderService.Setup(s => s.GetDocumentHeaderByIdAsync(documentHeaderId, false))
            .ReturnsAsync(existingDocumentHeader);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);
        _mockBusinessPartyService.Setup(s => s.GetBusinessPartiesAsync(1, 100))
            .ReturnsAsync(businessParties);

        await _viewModel.LoadEntityAsync(documentHeaderId);

        // Modify entity
        _viewModel.Entity!.TotalGrossAmount = 2000m;
        _viewModel.Entity.Notes = "Updated notes";
        _viewModel.Entity.Status = DocumentStatus.Approved;

        var updatedDocumentHeader = new DocumentHeaderDto
        {
            Id = documentHeaderId,
            DocumentTypeId = documentTypeId,
            BusinessPartyId = businessPartyId,
            Number = "INV-2024-001",
            Date = existingDocumentHeader.Date,
            TotalGrossAmount = 2000m,
            Status = DocumentStatus.Approved,
            Notes = "Updated notes"
        };

        _mockDocumentHeaderService.Setup(s => s.UpdateDocumentHeaderAsync(
            documentHeaderId,
            It.IsAny<UpdateDocumentHeaderDto>()))
            .ReturnsAsync(updatedDocumentHeader);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal(2000m, _viewModel.Entity.TotalGrossAmount);
        Assert.Equal("Updated notes", _viewModel.Entity.Notes);
        Assert.Equal(DocumentStatus.Approved, _viewModel.Entity.Status);
        _mockDocumentHeaderService.Verify(s => s.UpdateDocumentHeaderAsync(
            documentHeaderId,
            It.IsAny<UpdateDocumentHeaderDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsDocumentTypesAndBusinessParties()
    {
        // Arrange
        var documentHeaderId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();
        var existingDocumentHeader = new DocumentHeaderDto
        {
            Id = documentHeaderId,
            DocumentTypeId = documentTypeId,
            BusinessPartyId = businessPartyId,
            Number = "INV-2024-001",
            Date = DateTime.UtcNow,
            TotalGrossAmount = 1000m,
            Status = DocumentStatus.Draft
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>
            {
                new DocumentTypeDto { Id = documentTypeId, Code = "INV", Name = "Invoice" },
                new DocumentTypeDto { Id = Guid.NewGuid(), Code = "DDT", Name = "Delivery Note" },
                new DocumentTypeDto { Id = Guid.NewGuid(), Code = "ORD", Name = "Order" }
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 100
        };

        var businessParties = new PagedResult<BusinessPartyDto>
        {
            Items = new List<BusinessPartyDto>
            {
                new BusinessPartyDto { Id = businessPartyId, Name = "Test Customer" },
                new BusinessPartyDto { Id = Guid.NewGuid(), Name = "Test Customer 2" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentHeaderService.Setup(s => s.GetDocumentHeaderByIdAsync(documentHeaderId, false))
            .ReturnsAsync(existingDocumentHeader);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);
        _mockBusinessPartyService.Setup(s => s.GetBusinessPartiesAsync(1, 100))
            .ReturnsAsync(businessParties);

        // Act
        await _viewModel.LoadEntityAsync(documentHeaderId);

        // Assert
        Assert.NotNull(_viewModel.DocumentTypes);
        Assert.NotNull(_viewModel.BusinessParties);
        Assert.Equal(3, _viewModel.DocumentTypes.Count());
        Assert.Equal(2, _viewModel.BusinessParties.Count());
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "INV");
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "DDT");
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "ORD");
        Assert.Contains(_viewModel.BusinessParties, bp => bp.Name == "Test Customer");
        Assert.Contains(_viewModel.BusinessParties, bp => bp.Name == "Test Customer 2");
        _mockDocumentTypeService.Verify(s => s.GetAllDocumentTypesAsync(), Times.Once);
        _mockBusinessPartyService.Verify(s => s.GetBusinessPartiesAsync(1, 100), Times.Once);
    }

    [Fact]
    public async Task IsNewEntity_WithEmptyId_ReturnsTrue()
    {
        // Arrange & Act
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Assert
        Assert.True(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
    }

    [Fact]
    public async Task GetEntityId_ReturnsCorrectId()
    {
        // Arrange
        var documentHeaderId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();
        var expectedDocumentHeader = new DocumentHeaderDto
        {
            Id = documentHeaderId,
            DocumentTypeId = documentTypeId,
            BusinessPartyId = businessPartyId,
            Number = "INV-2024-001",
            Date = DateTime.UtcNow,
            TotalGrossAmount = 1000m,
            Status = DocumentStatus.Draft
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        var businessParties = new PagedResult<BusinessPartyDto>
        {
            Items = new List<BusinessPartyDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentHeaderService.Setup(s => s.GetDocumentHeaderByIdAsync(documentHeaderId, false))
            .ReturnsAsync(expectedDocumentHeader);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);
        _mockBusinessPartyService.Setup(s => s.GetBusinessPartiesAsync(1, 100))
            .ReturnsAsync(businessParties);

        // Act
        await _viewModel.LoadEntityAsync(documentHeaderId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentHeaderId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
