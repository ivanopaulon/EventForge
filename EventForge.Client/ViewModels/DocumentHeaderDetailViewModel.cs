using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for DocumentHeader detail page
/// </summary>
public class DocumentHeaderDetailViewModel : BaseEntityDetailViewModel<DocumentHeaderDto, CreateDocumentHeaderDto, UpdateDocumentHeaderDto>
{
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IDocumentTypeService _documentTypeService;
    private readonly IBusinessPartyService _businessPartyService;

    public DocumentHeaderDetailViewModel(
        IDocumentHeaderService documentHeaderService,
        IDocumentTypeService documentTypeService,
        IBusinessPartyService businessPartyService,
        ILogger<DocumentHeaderDetailViewModel> logger) 
        : base(logger)
    {
        _documentHeaderService = documentHeaderService;
        _documentTypeService = documentTypeService;
        _businessPartyService = businessPartyService;
    }

    // Related entity collections
    public IEnumerable<DocumentTypeDto>? DocumentTypes { get; private set; }
    public IEnumerable<EventForge.DTOs.Business.BusinessPartyDto>? BusinessParties { get; private set; }

    protected override DocumentHeaderDto CreateNewEntity()
    {
        return new DocumentHeaderDto
        {
            Id = Guid.Empty,
            DocumentTypeId = Guid.Empty,
            DocumentTypeName = null,
            Series = null,
            Number = string.Empty,
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.Empty,
            BusinessPartyName = null,
            BusinessPartyAddressId = null,
            CustomerName = null,
            SourceWarehouseId = null,
            SourceWarehouseName = null,
            DestinationWarehouseId = null,
            DestinationWarehouseName = null,
            ShippingDate = null,
            CarrierName = null,
            TrackingNumber = null,
            ShippingNotes = null,
            TeamMemberId = null,
            TeamMemberName = null,
            TeamId = null,
            TeamName = null,
            EventId = null,
            EventName = null,
            CashRegisterId = null,
            CashierId = null,
            CashierName = null,
            ExternalDocumentNumber = null,
            ExternalDocumentSeries = null,
            ExternalDocumentDate = null,
            DocumentReason = null,
            IsProforma = false,
            IsFiscal = true,
            FiscalDocumentNumber = null,
            FiscalDate = null,
            VatAmount = 0m,
            TotalNetAmount = 0m,
            TotalGrossAmount = 0m,
            Currency = "EUR",
            ExchangeRate = null,
            BaseCurrencyAmount = null,
            DueDate = null,
            PaymentStatus = PaymentStatus.Pending,
            AmountPaid = 0m,
            PaymentMethod = null,
            PaymentReference = null,
            TotalDiscount = 0m,
            TotalDiscountAmount = 0m,
            ApprovalStatus = ApprovalStatus.Pending,
            ApprovedBy = null,
            ApprovedAt = null,
            ClosedAt = null,
            Status = DocumentStatus.Draft,
            ReferenceDocumentId = null,
            Notes = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null,
            Rows = null,
            TotalBeforeDiscount = 0m,
            TotalAfterDiscount = 0m
        };
    }

    protected override async Task<DocumentHeaderDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _documentHeaderService.GetDocumentHeaderByIdAsync(entityId, false);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            DocumentTypes = new List<DocumentTypeDto>();
            BusinessParties = new List<EventForge.DTOs.Business.BusinessPartyDto>();
            return;
        }

        try
        {
            // Load document types for dropdown
            var documentTypesResult = await _documentTypeService.GetAllDocumentTypesAsync();
            DocumentTypes = documentTypesResult ?? new List<DocumentTypeDto>();
            
            // Load business parties for dropdown
            var businessPartiesResult = await _businessPartyService.GetBusinessPartiesAsync(1, 100);
            BusinessParties = businessPartiesResult?.Items ?? new List<EventForge.DTOs.Business.BusinessPartyDto>();
            
            Logger.LogInformation("Loaded {DocumentTypeCount} document types and {BusinessPartyCount} business parties for document header {Id}", 
                DocumentTypes.Count(), BusinessParties.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for document header {Id}", entityId);
            DocumentTypes = new List<DocumentTypeDto>();
            BusinessParties = new List<EventForge.DTOs.Business.BusinessPartyDto>();
        }
    }

    protected override CreateDocumentHeaderDto MapToCreateDto(DocumentHeaderDto entity)
    {
        return new CreateDocumentHeaderDto
        {
            DocumentTypeId = entity.DocumentTypeId,
            Series = entity.Series,
            Number = entity.Number,
            Date = entity.Date,
            BusinessPartyId = entity.BusinessPartyId,
            BusinessPartyAddressId = entity.BusinessPartyAddressId,
            CustomerName = entity.CustomerName,
            SourceWarehouseId = entity.SourceWarehouseId,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            ShippingDate = entity.ShippingDate,
            CarrierName = entity.CarrierName,
            TrackingNumber = entity.TrackingNumber,
            ShippingNotes = entity.ShippingNotes,
            TeamMemberId = entity.TeamMemberId,
            TeamId = entity.TeamId,
            EventId = entity.EventId,
            CashRegisterId = entity.CashRegisterId,
            CashierId = entity.CashierId,
            ExternalDocumentNumber = entity.ExternalDocumentNumber,
            ExternalDocumentSeries = entity.ExternalDocumentSeries,
            ExternalDocumentDate = entity.ExternalDocumentDate,
            DocumentReason = entity.DocumentReason,
            IsProforma = entity.IsProforma,
            IsFiscal = entity.IsFiscal,
            FiscalDocumentNumber = entity.FiscalDocumentNumber,
            FiscalDate = entity.FiscalDate,
            Currency = entity.Currency,
            ExchangeRate = entity.ExchangeRate,
            DueDate = entity.DueDate,
            PaymentMethod = entity.PaymentMethod,
            PaymentReference = entity.PaymentReference,
            TotalDiscount = entity.TotalDiscount,
            TotalDiscountAmount = entity.TotalDiscountAmount,
            ReferenceDocumentId = entity.ReferenceDocumentId,
            Notes = entity.Notes
        };
    }

    protected override UpdateDocumentHeaderDto MapToUpdateDto(DocumentHeaderDto entity)
    {
        return new UpdateDocumentHeaderDto
        {
            DocumentTypeId = entity.DocumentTypeId,
            Series = entity.Series,
            Number = entity.Number,
            Date = entity.Date,
            BusinessPartyId = entity.BusinessPartyId,
            BusinessPartyAddressId = entity.BusinessPartyAddressId,
            CustomerName = entity.CustomerName,
            SourceWarehouseId = entity.SourceWarehouseId,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            ShippingDate = entity.ShippingDate,
            CarrierName = entity.CarrierName,
            TrackingNumber = entity.TrackingNumber,
            ShippingNotes = entity.ShippingNotes,
            TeamMemberId = entity.TeamMemberId,
            TeamId = entity.TeamId,
            EventId = entity.EventId,
            CashRegisterId = entity.CashRegisterId,
            CashierId = entity.CashierId,
            ExternalDocumentNumber = entity.ExternalDocumentNumber,
            ExternalDocumentSeries = entity.ExternalDocumentSeries,
            ExternalDocumentDate = entity.ExternalDocumentDate,
            DocumentReason = entity.DocumentReason,
            IsProforma = entity.IsProforma,
            IsFiscal = entity.IsFiscal,
            FiscalDocumentNumber = entity.FiscalDocumentNumber,
            FiscalDate = entity.FiscalDate,
            Currency = entity.Currency,
            ExchangeRate = entity.ExchangeRate,
            DueDate = entity.DueDate,
            PaymentStatus = entity.PaymentStatus,
            AmountPaid = entity.AmountPaid,
            PaymentMethod = entity.PaymentMethod,
            PaymentReference = entity.PaymentReference,
            TotalDiscount = entity.TotalDiscount,
            TotalDiscountAmount = entity.TotalDiscountAmount,
            ApprovalStatus = entity.ApprovalStatus,
            Status = entity.Status,
            ReferenceDocumentId = entity.ReferenceDocumentId,
            Notes = entity.Notes
        };
    }

    protected override Task<DocumentHeaderDto?> CreateEntityAsync(CreateDocumentHeaderDto createDto)
    {
        return _documentHeaderService.CreateDocumentHeaderAsync(createDto);
    }

    protected override Task<DocumentHeaderDto?> UpdateEntityAsync(Guid entityId, UpdateDocumentHeaderDto updateDto)
    {
        return _documentHeaderService.UpdateDocumentHeaderAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(DocumentHeaderDto entity)
    {
        return entity.Id;
    }
}
