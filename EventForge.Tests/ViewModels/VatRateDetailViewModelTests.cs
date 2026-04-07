using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for VatRateDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class VatRateDetailViewModelTests : IDisposable
{
    private readonly Mock<IFinancialService> _mockFinancialService;
    private readonly Mock<ILogger<VatRateDetailViewModel>> _mockLogger;
    private readonly VatRateDetailViewModel _viewModel;

    public VatRateDetailViewModelTests()
    {
        _mockFinancialService = new Mock<IFinancialService>();
        _mockLogger = new Mock<ILogger<VatRateDetailViewModel>>();
        _viewModel = new VatRateDetailViewModel(
            _mockFinancialService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var vatRateId = Guid.NewGuid();
        var vatNatureId = Guid.NewGuid();
        var expectedVatRate = new VatRateDto
        {
            Id = vatRateId,
            Name = "VAT 22%",
            Percentage = 22m,
            Status = VatRateStatus.Active,
            ValidFrom = DateTime.UtcNow.AddMonths(-1),
            ValidTo = null,
            Notes = "Standard VAT rate",
            VatNatureId = vatNatureId,
            VatNatureCode = "N1",
            VatNatureName = "Excluded",
            IsActive = true
        };

        var vatNatures = new PagedResult<VatNatureDto>
        {
            Items = new List<VatNatureDto>
            {
                new VatNatureDto { Id = vatNatureId, Code = "N1", Name = "Excluded" },
                new VatNatureDto { Id = Guid.NewGuid(), Code = "N2", Name = "Not Subject" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockFinancialService.Setup(s => s.GetVatRateAsync(vatRateId))
            .ReturnsAsync(expectedVatRate);
        _mockFinancialService.Setup(s => s.GetVatNaturesAsync(1, 100))
            .ReturnsAsync(vatNatures);

        // Act
        await _viewModel.LoadEntityAsync(vatRateId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(vatRateId, _viewModel.Entity.Id);
        Assert.Equal("VAT 22%", _viewModel.Entity.Name);
        Assert.Equal(22m, _viewModel.Entity.Percentage);
        Assert.Equal(VatRateStatus.Active, _viewModel.Entity.Status);
        Assert.Equal(vatNatureId, _viewModel.Entity.VatNatureId);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.VatNatures);
        Assert.Equal(2, _viewModel.VatNatures.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultVatRate()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.Equal(0m, _viewModel.Entity.Percentage);
        Assert.Equal(VatRateStatus.Active, _viewModel.Entity.Status);
        Assert.Null(_viewModel.Entity.VatNatureId);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        var vatNatureId = Guid.NewGuid();
        _viewModel.Entity!.Name = "VAT 10%";
        _viewModel.Entity.Percentage = 10m;
        _viewModel.Entity.Status = VatRateStatus.Active;
        _viewModel.Entity.ValidFrom = DateTime.UtcNow;
        _viewModel.Entity.Notes = "Reduced rate";
        _viewModel.Entity.VatNatureId = vatNatureId;

        var createdVatRate = new VatRateDto
        {
            Id = Guid.NewGuid(),
            Name = "VAT 10%",
            Percentage = 10m,
            Status = VatRateStatus.Active,
            ValidFrom = _viewModel.Entity.ValidFrom,
            ValidTo = null,
            Notes = "Reduced rate",
            VatNatureId = vatNatureId,
            VatNatureCode = "N1",
            VatNatureName = "Excluded",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockFinancialService.Setup(s => s.CreateVatRateAsync(
            It.IsAny<CreateVatRateDto>()))
            .ReturnsAsync(createdVatRate);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdVatRate.Id, _viewModel.Entity.Id);
        Assert.Equal("VAT 10%", _viewModel.Entity.Name);
        Assert.Equal(10m, _viewModel.Entity.Percentage);
        Assert.Equal(VatRateStatus.Active, _viewModel.Entity.Status);
        Assert.Equal(vatNatureId, _viewModel.Entity.VatNatureId);
        Assert.False(_viewModel.IsNewEntity);
        _mockFinancialService.Verify(s => s.CreateVatRateAsync(
            It.IsAny<CreateVatRateDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var vatRateId = Guid.NewGuid();
        var vatNatureId = Guid.NewGuid();
        var existingVatRate = new VatRateDto
        {
            Id = vatRateId,
            Name = "VAT 22%",
            Percentage = 22m,
            Status = VatRateStatus.Active,
            ValidFrom = DateTime.UtcNow.AddMonths(-1),
            VatNatureId = vatNatureId,
            IsActive = true,
            Notes = "Original notes"
        };

        var vatNatures = new PagedResult<VatNatureDto>
        {
            Items = new List<VatNatureDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockFinancialService.Setup(s => s.GetVatRateAsync(vatRateId))
            .ReturnsAsync(existingVatRate);
        _mockFinancialService.Setup(s => s.GetVatNaturesAsync(1, 100))
            .ReturnsAsync(vatNatures);

        await _viewModel.LoadEntityAsync(vatRateId);

        // Modify entity
        _viewModel.Entity!.Name = "Updated VAT 22%";
        _viewModel.Entity.Notes = "Updated notes";
        _viewModel.Entity.Status = VatRateStatus.Suspended;

        var updatedVatRate = new VatRateDto
        {
            Id = vatRateId,
            Name = "Updated VAT 22%",
            Percentage = 22m,
            Status = VatRateStatus.Suspended,
            ValidFrom = existingVatRate.ValidFrom,
            VatNatureId = vatNatureId,
            IsActive = true,
            Notes = "Updated notes"
        };

        _mockFinancialService.Setup(s => s.UpdateVatRateAsync(
            vatRateId,
            It.IsAny<UpdateVatRateDto>()))
            .ReturnsAsync(updatedVatRate);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated VAT 22%", _viewModel.Entity.Name);
        Assert.Equal("Updated notes", _viewModel.Entity.Notes);
        Assert.Equal(VatRateStatus.Suspended, _viewModel.Entity.Status);
        _mockFinancialService.Verify(s => s.UpdateVatRateAsync(
            vatRateId,
            It.IsAny<UpdateVatRateDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsVatNatures()
    {
        // Arrange
        var vatRateId = Guid.NewGuid();
        var vatNatureId = Guid.NewGuid();
        var existingVatRate = new VatRateDto
        {
            Id = vatRateId,
            Name = "VAT 22%",
            Percentage = 22m,
            Status = VatRateStatus.Active,
            VatNatureId = vatNatureId,
            IsActive = true
        };

        var vatNatures = new PagedResult<VatNatureDto>
        {
            Items = new List<VatNatureDto>
            {
                new VatNatureDto { Id = vatNatureId, Code = "N1", Name = "Excluded" },
                new VatNatureDto { Id = Guid.NewGuid(), Code = "N2", Name = "Not Subject" },
                new VatNatureDto { Id = Guid.NewGuid(), Code = "N3", Name = "Non-taxable" }
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 100
        };

        _mockFinancialService.Setup(s => s.GetVatRateAsync(vatRateId))
            .ReturnsAsync(existingVatRate);
        _mockFinancialService.Setup(s => s.GetVatNaturesAsync(1, 100))
            .ReturnsAsync(vatNatures);

        // Act
        await _viewModel.LoadEntityAsync(vatRateId);

        // Assert
        Assert.NotNull(_viewModel.VatNatures);
        Assert.Equal(3, _viewModel.VatNatures.Count());
        Assert.Contains(_viewModel.VatNatures, v => v.Code == "N1");
        Assert.Contains(_viewModel.VatNatures, v => v.Code == "N2");
        Assert.Contains(_viewModel.VatNatures, v => v.Code == "N3");
        _mockFinancialService.Verify(s => s.GetVatNaturesAsync(1, 100), Times.Once);
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
        var vatRateId = Guid.NewGuid();
        var expectedVatRate = new VatRateDto
        {
            Id = vatRateId,
            Name = "VAT 22%",
            Percentage = 22m,
            Status = VatRateStatus.Active,
            IsActive = true
        };

        var vatNatures = new PagedResult<VatNatureDto>
        {
            Items = new List<VatNatureDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockFinancialService.Setup(s => s.GetVatRateAsync(vatRateId))
            .ReturnsAsync(expectedVatRate);
        _mockFinancialService.Setup(s => s.GetVatNaturesAsync(1, 100))
            .ReturnsAsync(vatNatures);

        // Act
        await _viewModel.LoadEntityAsync(vatRateId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(vatRateId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
