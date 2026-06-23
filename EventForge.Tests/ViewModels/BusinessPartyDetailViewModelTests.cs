using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Business;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using Prym.Web.Services;
using Prym.Web.ViewModels;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for BusinessPartyDetailViewModel — load, save, and related-data tabs.
/// </summary>
[Trait("Category", "Unit")]
public class BusinessPartyDetailViewModelTests
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

    #region Load — new entity (Guid.Empty)

    [Fact]
    public async Task LoadEntityAsync_WithEmptyGuid_CreatesNewEntity()
    {
        await _viewModel.LoadEntityAsync(Guid.Empty);

        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task LoadEntityAsync_NewEntity_HasDefaultPartyTypeCliente()
    {
        await _viewModel.LoadEntityAsync(Guid.Empty);

        Assert.Equal(BusinessPartyType.Cliente, _viewModel.Entity!.PartyType);
    }

    [Fact]
    public async Task LoadEntityAsync_NewEntity_IsActiveByDefault()
    {
        await _viewModel.LoadEntityAsync(Guid.Empty);

        Assert.True(_viewModel.Entity!.IsActive);
    }

    [Fact]
    public async Task LoadEntityAsync_NewEntity_HasEmptyDocumentsCollection()
    {
        await _viewModel.LoadEntityAsync(Guid.Empty);

        Assert.NotNull(_viewModel.Documents);
        Assert.Empty(_viewModel.Documents);
    }

    #endregion

    #region Load — existing entity

    [Fact]
    public async Task LoadEntityAsync_WithValidId_LoadsEntity()
    {
        var partyId = Guid.NewGuid();
        var expectedParty = new BusinessPartyDto
        {
            Id = partyId,
            Name = "Acme Srl",
            PartyType = BusinessPartyType.Cliente,
            IsActive = true,
            HasAccountingData = false,
            AddressCount = 0,
            ContactCount = 0,
            ReferenceCount = 0,
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };

        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedParty);

        await _viewModel.LoadEntityAsync(partyId);

        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(partyId, _viewModel.Entity.Id);
        Assert.Equal("Acme Srl", _viewModel.Entity.Name);
        Assert.False(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task LoadEntityAsync_WithValidId_ResetsTabLoadedFlags()
    {
        var partyId = Guid.NewGuid();
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusinessPartyDto
            {
                Id = partyId,
                Name = "Test Party",
                Contacts = new List<ContactDto>(),
                CreatedAt = DateTime.UtcNow
            });

        await _viewModel.LoadEntityAsync(partyId);

        Assert.False(_viewModel.IsAccountingLoaded);
        Assert.False(_viewModel.IsDocumentsLoaded);
        Assert.False(_viewModel.IsProductAnalysisLoaded);
    }

    [Fact]
    public async Task LoadEntityAsync_ServiceReturnsNull_EntityRemainsNull()
    {
        var missingId = Guid.NewGuid();
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessPartyDto?)null);

        await _viewModel.LoadEntityAsync(missingId);

        Assert.Null(_viewModel.Entity);
    }

    #endregion

    #region Save — create

    [Fact]
    public async Task SaveEntityAsync_NewEntity_CallsCreateBusinessPartyAsync()
    {
        await _viewModel.LoadEntityAsync(Guid.Empty);
        _viewModel.Entity!.Name = "New Party";

        var createdDto = new BusinessPartyDto
        {
            Id = Guid.NewGuid(),
            Name = "New Party",
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };
        _mockBusinessPartyService
            .Setup(s => s.CreateBusinessPartyAsync(It.IsAny<CreateBusinessPartyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var success = await _viewModel.SaveEntityAsync();

        Assert.True(success);
        _mockBusinessPartyService.Verify(
            s => s.CreateBusinessPartyAsync(It.IsAny<CreateBusinessPartyDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Save — update

    [Fact]
    public async Task SaveEntityAsync_ExistingEntity_CallsUpdateBusinessPartyAsync()
    {
        var partyId = Guid.NewGuid();
        var existing = new BusinessPartyDto
        {
            Id = partyId,
            Name = "Existing Party",
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockBusinessPartyService
            .Setup(s => s.UpdateBusinessPartyAsync(partyId, It.IsAny<UpdateBusinessPartyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _viewModel.LoadEntityAsync(partyId);
        _viewModel.Entity!.Name = "Updated Party";

        var success = await _viewModel.SaveEntityAsync();

        Assert.True(success);
        _mockBusinessPartyService.Verify(
            s => s.UpdateBusinessPartyAsync(partyId, It.IsAny<UpdateBusinessPartyDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region LoadAccountingAsync

    [Fact]
    public async Task LoadAccountingAsync_CallsService_SetsIsAccountingLoaded()
    {
        var partyId = Guid.NewGuid();
        var existing = new BusinessPartyDto
        {
            Id = partyId,
            Name = "Party",
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAccountingByBusinessPartyIdAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessPartyAccountingDto?)null);

        await _viewModel.LoadEntityAsync(partyId);
        await _viewModel.LoadAccountingAsync();

        Assert.True(_viewModel.IsAccountingLoaded);
    }

    #endregion

    #region LoadDocumentsAsync

    [Fact]
    public async Task LoadDocumentsAsync_CallsService_SetsIsDocumentsLoaded()
    {
        var partyId = Guid.NewGuid();
        var existing = new BusinessPartyDto
        {
            Id = partyId,
            Name = "Party",
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockBusinessPartyService
            .Setup(s => s.GetBusinessPartyDocumentsAsync(
                partyId,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<DocumentHeaderDto>
            {
                Items = new List<DocumentHeaderDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });

        await _viewModel.LoadEntityAsync(partyId);
        await _viewModel.LoadDocumentsAsync();

        Assert.True(_viewModel.IsDocumentsLoaded);
    }

    #endregion
}
