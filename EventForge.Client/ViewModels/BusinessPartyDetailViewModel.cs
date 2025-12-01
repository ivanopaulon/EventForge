using EventForge.Client.Services;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for BusinessParty detail page following Onda 3 pattern
/// </summary>
public class BusinessPartyDetailViewModel : BaseEntityDetailViewModel<BusinessPartyDto, CreateBusinessPartyDto, UpdateBusinessPartyDto>
{
    private readonly IBusinessPartyService _businessPartyService;
    private readonly ILookupCacheService _lookupCacheService;

    public BusinessPartyDetailViewModel(
        IBusinessPartyService businessPartyService,
        ILookupCacheService lookupCacheService,
        ILogger<BusinessPartyDetailViewModel> logger)
        : base(logger)
    {
        _businessPartyService = businessPartyService;
        _lookupCacheService = lookupCacheService;
    }

    // Related entity collections - lazy loaded per tab
    public BusinessPartyAccountingDto? Accounting { get; private set; }
    public IEnumerable<DocumentHeaderDto>? Documents { get; private set; }
    public IEnumerable<BusinessPartyProductAnalysisDto>? ProductAnalysis { get; private set; }

    // Tab state tracking
    public bool IsAccountingLoaded { get; private set; }
    public bool IsDocumentsLoaded { get; private set; }
    public bool IsProductAnalysisLoaded { get; private set; }

    protected override BusinessPartyDto CreateNewEntity()
    {
        // Initialize empty collections for new entity
        Documents = new List<DocumentHeaderDto>();
        ProductAnalysis = new List<BusinessPartyProductAnalysisDto>();
        Accounting = null;

        return new BusinessPartyDto
        {
            Id = Guid.Empty,
            PartyType = BusinessPartyType.Cliente,
            Name = string.Empty,
            TaxCode = null,
            VatNumber = null,
            SdiCode = null,
            Pec = null,
            Notes = null,
            IsActive = true,
            HasAccountingData = false,
            AddressCount = 0,
            ContactCount = 0,
            ReferenceCount = 0,
            Contacts = new List<ContactDto>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    protected override async Task<BusinessPartyDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _businessPartyService.GetBusinessPartyAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        // Related entities are loaded lazily per tab
        // Reset tab loaded states
        IsAccountingLoaded = false;
        IsDocumentsLoaded = false;
        IsProductAnalysisLoaded = false;

        // Initialize empty collections
        Accounting = null;
        Documents = new List<DocumentHeaderDto>();
        ProductAnalysis = new List<BusinessPartyProductAnalysisDto>();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Lazy load accounting data when Accounting tab is activated
    /// </summary>
    public async Task LoadAccountingAsync()
    {
        if (IsAccountingLoaded || Entity == null || IsNewEntity)
        {
            return;
        }

        try
        {
            Accounting = await _businessPartyService.GetBusinessPartyAccountingByBusinessPartyIdAsync(Entity.Id);
            IsAccountingLoaded = true;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading accounting for business party {BusinessPartyId}", Entity.Id);
            Accounting = null;
        }
    }

    /// <summary>
    /// Lazy load documents when Documents tab is activated
    /// </summary>
    public async Task LoadDocumentsAsync(int page = 1, int pageSize = 20)
    {
        if (Entity == null || IsNewEntity)
        {
            return;
        }

        try
        {
            var result = await _businessPartyService.GetBusinessPartyDocumentsAsync(
                Entity.Id,
                page: page,
                pageSize: pageSize);

            Documents = result?.Items ?? new List<DocumentHeaderDto>();
            IsDocumentsLoaded = true;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading documents for business party {BusinessPartyId}", Entity.Id);
            Documents = new List<DocumentHeaderDto>();
        }
    }

    /// <summary>
    /// Lazy load product analysis when Product Analysis tab is activated
    /// </summary>
    public async Task LoadProductAnalysisAsync(int page = 1, int pageSize = 20)
    {
        if (Entity == null || IsNewEntity)
        {
            return;
        }

        try
        {
            var result = await _businessPartyService.GetBusinessPartyProductAnalysisAsync(
                Entity.Id,
                page: page,
                pageSize: pageSize);

            ProductAnalysis = result?.Items ?? new List<BusinessPartyProductAnalysisDto>();
            IsProductAnalysisLoaded = true;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading product analysis for business party {BusinessPartyId}", Entity.Id);
            ProductAnalysis = new List<BusinessPartyProductAnalysisDto>();
        }
    }

    protected override CreateBusinessPartyDto MapToCreateDto(BusinessPartyDto entity)
    {
        return new CreateBusinessPartyDto
        {
            PartyType = entity.PartyType,
            Name = entity.Name,
            TaxCode = entity.TaxCode,
            VatNumber = entity.VatNumber,
            SdiCode = entity.SdiCode,
            Pec = entity.Pec,
            Notes = entity.Notes
        };
    }

    protected override UpdateBusinessPartyDto MapToUpdateDto(BusinessPartyDto entity)
    {
        return new UpdateBusinessPartyDto
        {
            PartyType = entity.PartyType,
            Name = entity.Name,
            TaxCode = entity.TaxCode,
            VatNumber = entity.VatNumber,
            SdiCode = entity.SdiCode,
            Pec = entity.Pec,
            Notes = entity.Notes
        };
    }

    protected override async Task<BusinessPartyDto?> CreateEntityAsync(CreateBusinessPartyDto createDto)
    {
        return await _businessPartyService.CreateBusinessPartyAsync(createDto);
    }

    protected override async Task<BusinessPartyDto?> UpdateEntityAsync(Guid entityId, UpdateBusinessPartyDto updateDto)
    {
        return await _businessPartyService.UpdateBusinessPartyAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(BusinessPartyDto entity)
    {
        return entity.Id;
    }
}
