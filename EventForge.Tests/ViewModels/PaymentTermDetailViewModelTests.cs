using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for PaymentTermDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class PaymentTermDetailViewModelTests : IDisposable
{
    private readonly Mock<IFinancialService> _mockFinancialService;
    private readonly Mock<ILogger<PaymentTermDetailViewModel>> _mockLogger;
    private readonly PaymentTermDetailViewModel _viewModel;

    public PaymentTermDetailViewModelTests()
    {
        _mockFinancialService = new Mock<IFinancialService>();
        _mockLogger = new Mock<ILogger<PaymentTermDetailViewModel>>();
        _viewModel = new PaymentTermDetailViewModel(
            _mockFinancialService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var paymentTermId = Guid.NewGuid();
        var expectedPaymentTerm = new PaymentTermDto
        {
            Id = paymentTermId,
            Name = "Net 30",
            Description = "Payment due within 30 days",
            DueDays = 30,
            PaymentMethod = PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            ModifiedAt = null,
            ModifiedBy = null
        };

        _mockFinancialService.Setup(s => s.GetPaymentTermAsync(paymentTermId))
            .ReturnsAsync(expectedPaymentTerm);

        // Act
        await _viewModel.LoadEntityAsync(paymentTermId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(paymentTermId, _viewModel.Entity.Id);
        Assert.Equal("Net 30", _viewModel.Entity.Name);
        Assert.Equal("Payment due within 30 days", _viewModel.Entity.Description);
        Assert.Equal(30, _viewModel.Entity.DueDays);
        Assert.Equal(PaymentMethod.BankTransfer, _viewModel.Entity.PaymentMethod);
        Assert.False(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultPaymentTerm()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.Null(_viewModel.Entity.Description);
        Assert.Equal(0, _viewModel.Entity.DueDays);
        Assert.Equal(PaymentMethod.BankTransfer, _viewModel.Entity.PaymentMethod);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        _viewModel.Entity!.Name = "Net 60";
        _viewModel.Entity.Description = "Payment due within 60 days";
        _viewModel.Entity.DueDays = 60;
        _viewModel.Entity.PaymentMethod = PaymentMethod.Cash;

        var createdPaymentTerm = new PaymentTermDto
        {
            Id = Guid.NewGuid(),
            Name = "Net 60",
            Description = "Payment due within 60 days",
            DueDays = 60,
            PaymentMethod = PaymentMethod.Cash,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            ModifiedAt = null,
            ModifiedBy = null
        };

        _mockFinancialService.Setup(s => s.CreatePaymentTermAsync(
            It.IsAny<CreatePaymentTermDto>()))
            .ReturnsAsync(createdPaymentTerm);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdPaymentTerm.Id, _viewModel.Entity.Id);
        Assert.Equal("Net 60", _viewModel.Entity.Name);
        Assert.Equal("Payment due within 60 days", _viewModel.Entity.Description);
        Assert.Equal(60, _viewModel.Entity.DueDays);
        Assert.Equal(PaymentMethod.Cash, _viewModel.Entity.PaymentMethod);
        Assert.False(_viewModel.IsNewEntity);
        _mockFinancialService.Verify(s => s.CreatePaymentTermAsync(
            It.IsAny<CreatePaymentTermDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var paymentTermId = Guid.NewGuid();
        var existingPaymentTerm = new PaymentTermDto
        {
            Id = paymentTermId,
            Name = "Net 30",
            Description = "Payment due within 30 days",
            DueDays = 30,
            PaymentMethod = PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            CreatedBy = "admin",
            ModifiedAt = null,
            ModifiedBy = null
        };

        _mockFinancialService.Setup(s => s.GetPaymentTermAsync(paymentTermId))
            .ReturnsAsync(existingPaymentTerm);

        await _viewModel.LoadEntityAsync(paymentTermId);

        // Modify entity
        _viewModel.Entity!.Name = "Net 45";
        _viewModel.Entity.Description = "Updated payment terms";
        _viewModel.Entity.DueDays = 45;
        _viewModel.Entity.PaymentMethod = PaymentMethod.Card;

        var updatedPaymentTerm = new PaymentTermDto
        {
            Id = paymentTermId,
            Name = "Net 45",
            Description = "Updated payment terms",
            DueDays = 45,
            PaymentMethod = PaymentMethod.Card,
            CreatedAt = existingPaymentTerm.CreatedAt,
            CreatedBy = "admin",
            ModifiedAt = DateTime.UtcNow,
            ModifiedBy = "admin"
        };

        _mockFinancialService.Setup(s => s.UpdatePaymentTermAsync(
            paymentTermId,
            It.IsAny<UpdatePaymentTermDto>()))
            .ReturnsAsync(updatedPaymentTerm);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Net 45", _viewModel.Entity.Name);
        Assert.Equal("Updated payment terms", _viewModel.Entity.Description);
        Assert.Equal(45, _viewModel.Entity.DueDays);
        Assert.Equal(PaymentMethod.Card, _viewModel.Entity.PaymentMethod);
        _mockFinancialService.Verify(s => s.UpdatePaymentTermAsync(
            paymentTermId,
            It.IsAny<UpdatePaymentTermDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_NoRelatedEntities_CompletesSuccessfully()
    {
        // Arrange
        var paymentTermId = Guid.NewGuid();
        var existingPaymentTerm = new PaymentTermDto
        {
            Id = paymentTermId,
            Name = "Net 30",
            Description = "Payment due within 30 days",
            DueDays = 30,
            PaymentMethod = PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };

        _mockFinancialService.Setup(s => s.GetPaymentTermAsync(paymentTermId))
            .ReturnsAsync(existingPaymentTerm);

        // Act
        await _viewModel.LoadEntityAsync(paymentTermId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(paymentTermId, _viewModel.Entity.Id);
        // PaymentTerm is standalone - no related entities to verify
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
        var paymentTermId = Guid.NewGuid();
        var expectedPaymentTerm = new PaymentTermDto
        {
            Id = paymentTermId,
            Name = "Net 30",
            Description = "Payment due within 30 days",
            DueDays = 30,
            PaymentMethod = PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };

        _mockFinancialService.Setup(s => s.GetPaymentTermAsync(paymentTermId))
            .ReturnsAsync(expectedPaymentTerm);

        // Act
        await _viewModel.LoadEntityAsync(paymentTermId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(paymentTermId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
