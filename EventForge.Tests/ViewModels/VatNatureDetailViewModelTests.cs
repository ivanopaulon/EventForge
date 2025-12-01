using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for VatNatureDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class VatNatureDetailViewModelTests : IDisposable
{
    private readonly Mock<IFinancialService> _mockFinancialService;
    private readonly Mock<ILogger<VatNatureDetailViewModel>> _mockLogger;
    private readonly VatNatureDetailViewModel _viewModel;

    public VatNatureDetailViewModelTests()
    {
        _mockFinancialService = new Mock<IFinancialService>();
        _mockLogger = new Mock<ILogger<VatNatureDetailViewModel>>();
        _viewModel = new VatNatureDetailViewModel(
            _mockFinancialService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var vatNatureId = Guid.NewGuid();
        var expectedVatNature = new VatNatureDto
        {
            Id = vatNatureId,
            Code = "N1",
            Name = "Excluded from VAT",
            Description = "Operations excluded from VAT according to article 15",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };

        _mockFinancialService.Setup(s => s.GetVatNatureAsync(vatNatureId))
            .ReturnsAsync(expectedVatNature);

        // Act
        await _viewModel.LoadEntityAsync(vatNatureId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(vatNatureId, _viewModel.Entity.Id);
        Assert.Equal("N1", _viewModel.Entity.Code);
        Assert.Equal("Excluded from VAT", _viewModel.Entity.Name);
        Assert.Equal("Operations excluded from VAT according to article 15", _viewModel.Entity.Description);
        Assert.False(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultVatNature()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Code);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.Null(_viewModel.Entity.Description);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        _viewModel.Entity!.Code = "N2";
        _viewModel.Entity.Name = "Not Subject to VAT";
        _viewModel.Entity.Description = "Operations not subject to VAT";

        var createdVatNature = new VatNatureDto
        {
            Id = Guid.NewGuid(),
            Code = "N2",
            Name = "Not Subject to VAT",
            Description = "Operations not subject to VAT",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };

        _mockFinancialService.Setup(s => s.CreateVatNatureAsync(
            It.IsAny<CreateVatNatureDto>()))
            .ReturnsAsync(createdVatNature);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdVatNature.Id, _viewModel.Entity.Id);
        Assert.Equal("N2", _viewModel.Entity.Code);
        Assert.Equal("Not Subject to VAT", _viewModel.Entity.Name);
        Assert.Equal("Operations not subject to VAT", _viewModel.Entity.Description);
        Assert.False(_viewModel.IsNewEntity);
        _mockFinancialService.Verify(s => s.CreateVatNatureAsync(
            It.IsAny<CreateVatNatureDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var vatNatureId = Guid.NewGuid();
        var existingVatNature = new VatNatureDto
        {
            Id = vatNatureId,
            Code = "N1",
            Name = "Excluded from VAT",
            Description = "Original description",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        _mockFinancialService.Setup(s => s.GetVatNatureAsync(vatNatureId))
            .ReturnsAsync(existingVatNature);

        await _viewModel.LoadEntityAsync(vatNatureId);

        // Modify entity
        _viewModel.Entity!.Name = "Updated Excluded from VAT";
        _viewModel.Entity.Description = "Updated description";

        var updatedVatNature = new VatNatureDto
        {
            Id = vatNatureId,
            Code = "N1",
            Name = "Updated Excluded from VAT",
            Description = "Updated description",
            CreatedAt = existingVatNature.CreatedAt,
            ModifiedAt = DateTime.UtcNow
        };

        _mockFinancialService.Setup(s => s.UpdateVatNatureAsync(
            vatNatureId,
            It.IsAny<UpdateVatNatureDto>()))
            .ReturnsAsync(updatedVatNature);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Excluded from VAT", _viewModel.Entity.Name);
        Assert.Equal("Updated description", _viewModel.Entity.Description);
        _mockFinancialService.Verify(s => s.UpdateVatNatureAsync(
            vatNatureId,
            It.IsAny<UpdateVatNatureDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_NoRelatedEntitiesNeeded()
    {
        // Arrange
        var vatNatureId = Guid.NewGuid();
        var existingVatNature = new VatNatureDto
        {
            Id = vatNatureId,
            Code = "N1",
            Name = "Excluded from VAT",
            Description = "Test description"
        };

        _mockFinancialService.Setup(s => s.GetVatNatureAsync(vatNatureId))
            .ReturnsAsync(existingVatNature);

        // Act
        await _viewModel.LoadEntityAsync(vatNatureId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(vatNatureId, _viewModel.Entity.Id);
        // VatNature has no related entities to load, so no additional assertions needed
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
        var vatNatureId = Guid.NewGuid();
        var expectedVatNature = new VatNatureDto
        {
            Id = vatNatureId,
            Code = "N1",
            Name = "Excluded from VAT",
            Description = "Test description"
        };

        _mockFinancialService.Setup(s => s.GetVatNatureAsync(vatNatureId))
            .ReturnsAsync(expectedVatNature);

        // Act
        await _viewModel.LoadEntityAsync(vatNatureId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(vatNatureId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
