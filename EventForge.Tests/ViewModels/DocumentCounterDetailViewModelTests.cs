using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for DocumentCounterDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentCounterDetailViewModelTests : IDisposable
{
    private readonly Mock<IDocumentCounterService> _mockDocumentCounterService;
    private readonly Mock<IDocumentTypeService> _mockDocumentTypeService;
    private readonly Mock<ILogger<DocumentCounterDetailViewModel>> _mockLogger;
    private readonly DocumentCounterDetailViewModel _viewModel;

    public DocumentCounterDetailViewModelTests()
    {
        _mockDocumentCounterService = new Mock<IDocumentCounterService>();
        _mockDocumentTypeService = new Mock<IDocumentTypeService>();
        _mockLogger = new Mock<ILogger<DocumentCounterDetailViewModel>>();
        _viewModel = new DocumentCounterDetailViewModel(
            _mockDocumentCounterService.Object,
            _mockDocumentTypeService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var documentCounterId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var expectedDocumentCounter = new DocumentCounterDto
        {
            Id = documentCounterId,
            DocumentTypeId = documentTypeId,
            DocumentTypeName = "Invoice",
            Series = "A",
            CurrentValue = 100,
            Year = 2024,
            Prefix = "INV-",
            PaddingLength = 5,
            FormatPattern = "{PREFIX}{SERIES}/{YEAR}/{NUMBER}",
            ResetOnYearChange = true,
            Notes = "Invoice counter for 2024"
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>
            {
                new DocumentTypeDto { Id = documentTypeId, Code = "INV", Name = "Invoice" },
                new DocumentTypeDto { Id = Guid.NewGuid(), Code = "DDT", Name = "Delivery Note" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentCounterService.Setup(s => s.GetDocumentCounterByIdAsync(documentCounterId))
            .ReturnsAsync(expectedDocumentCounter);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);

        // Act
        await _viewModel.LoadEntityAsync(documentCounterId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentCounterId, _viewModel.Entity.Id);
        Assert.Equal("A", _viewModel.Entity.Series);
        Assert.Equal(100, _viewModel.Entity.CurrentValue);
        Assert.Equal(2024, _viewModel.Entity.Year);
        Assert.Equal("INV-", _viewModel.Entity.Prefix);
        Assert.Equal(5, _viewModel.Entity.PaddingLength);
        Assert.True(_viewModel.Entity.ResetOnYearChange);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.DocumentTypes);
        Assert.Equal(2, _viewModel.DocumentTypes.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultDocumentCounter()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Series);
        Assert.Equal(0, _viewModel.Entity.CurrentValue);
        Assert.Null(_viewModel.Entity.Year);
        Assert.Null(_viewModel.Entity.Prefix);
        Assert.Equal(5, _viewModel.Entity.PaddingLength);
        Assert.True(_viewModel.Entity.ResetOnYearChange);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var documentTypeId = Guid.NewGuid();
        _viewModel.Entity!.DocumentTypeId = documentTypeId;
        _viewModel.Entity.Series = "B";
        _viewModel.Entity.Year = 2024;
        _viewModel.Entity.Prefix = "DDT-";
        _viewModel.Entity.PaddingLength = 6;
        _viewModel.Entity.ResetOnYearChange = false;
        _viewModel.Entity.Notes = "Delivery note counter";
        
        var createdDocumentCounter = new DocumentCounterDto
        {
            Id = Guid.NewGuid(),
            DocumentTypeId = documentTypeId,
            DocumentTypeName = "Delivery Note",
            Series = "B",
            CurrentValue = 0,
            Year = 2024,
            Prefix = "DDT-",
            PaddingLength = 6,
            FormatPattern = null,
            ResetOnYearChange = false,
            Notes = "Delivery note counter",
            CreatedAt = DateTime.UtcNow
        };

        _mockDocumentCounterService.Setup(s => s.CreateDocumentCounterAsync(
            It.IsAny<CreateDocumentCounterDto>()))
            .ReturnsAsync(createdDocumentCounter);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdDocumentCounter.Id, _viewModel.Entity.Id);
        Assert.Equal("B", _viewModel.Entity.Series);
        Assert.Equal(documentTypeId, _viewModel.Entity.DocumentTypeId);
        Assert.Equal("DDT-", _viewModel.Entity.Prefix);
        Assert.Equal(6, _viewModel.Entity.PaddingLength);
        Assert.False(_viewModel.Entity.ResetOnYearChange);
        Assert.False(_viewModel.IsNewEntity);
        _mockDocumentCounterService.Verify(s => s.CreateDocumentCounterAsync(
            It.IsAny<CreateDocumentCounterDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var documentCounterId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var existingDocumentCounter = new DocumentCounterDto
        {
            Id = documentCounterId,
            DocumentTypeId = documentTypeId,
            Series = "A",
            CurrentValue = 100,
            Year = 2024,
            Prefix = "INV-",
            PaddingLength = 5,
            ResetOnYearChange = true,
            Notes = "Original notes"
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentCounterService.Setup(s => s.GetDocumentCounterByIdAsync(documentCounterId))
            .ReturnsAsync(existingDocumentCounter);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);

        await _viewModel.LoadEntityAsync(documentCounterId);

        // Modify entity
        _viewModel.Entity!.CurrentValue = 150;
        _viewModel.Entity.Prefix = "INV2-";
        _viewModel.Entity.Notes = "Updated notes";

        var updatedDocumentCounter = new DocumentCounterDto
        {
            Id = documentCounterId,
            DocumentTypeId = documentTypeId,
            Series = "A",
            CurrentValue = 150,
            Year = 2024,
            Prefix = "INV2-",
            PaddingLength = 5,
            ResetOnYearChange = true,
            Notes = "Updated notes"
        };

        _mockDocumentCounterService.Setup(s => s.UpdateDocumentCounterAsync(
            documentCounterId,
            It.IsAny<UpdateDocumentCounterDto>()))
            .ReturnsAsync(updatedDocumentCounter);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal(150, _viewModel.Entity.CurrentValue);
        Assert.Equal("INV2-", _viewModel.Entity.Prefix);
        Assert.Equal("Updated notes", _viewModel.Entity.Notes);
        _mockDocumentCounterService.Verify(s => s.UpdateDocumentCounterAsync(
            documentCounterId,
            It.IsAny<UpdateDocumentCounterDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsDocumentTypes()
    {
        // Arrange
        var documentCounterId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var existingDocumentCounter = new DocumentCounterDto
        {
            Id = documentCounterId,
            DocumentTypeId = documentTypeId,
            Series = "A",
            CurrentValue = 100,
            Year = 2024,
            Prefix = "INV-",
            PaddingLength = 5,
            ResetOnYearChange = true
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

        _mockDocumentCounterService.Setup(s => s.GetDocumentCounterByIdAsync(documentCounterId))
            .ReturnsAsync(existingDocumentCounter);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);

        // Act
        await _viewModel.LoadEntityAsync(documentCounterId);

        // Assert
        Assert.NotNull(_viewModel.DocumentTypes);
        Assert.Equal(3, _viewModel.DocumentTypes.Count());
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "INV");
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "DDT");
        Assert.Contains(_viewModel.DocumentTypes, dt => dt.Code == "ORD");
        _mockDocumentTypeService.Verify(s => s.GetAllDocumentTypesAsync(), Times.Once);
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
        var documentCounterId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var expectedDocumentCounter = new DocumentCounterDto
        {
            Id = documentCounterId,
            DocumentTypeId = documentTypeId,
            Series = "A",
            CurrentValue = 100,
            Year = 2024,
            Prefix = "INV-",
            PaddingLength = 5,
            ResetOnYearChange = true
        };

        var documentTypes = new PagedResult<DocumentTypeDto>
        {
            Items = new List<DocumentTypeDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentCounterService.Setup(s => s.GetDocumentCounterByIdAsync(documentCounterId))
            .ReturnsAsync(expectedDocumentCounter);
        _mockDocumentTypeService.Setup(s => s.GetAllDocumentTypesAsync())
            .ReturnsAsync(documentTypes.Items);

        // Act
        await _viewModel.LoadEntityAsync(documentCounterId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentCounterId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
