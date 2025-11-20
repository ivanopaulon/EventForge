using EventForge.Client.Services;
using EventForge.DTOs.Documents;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for DocumentCounter detail page
/// </summary>
public class DocumentCounterDetailViewModel : BaseEntityDetailViewModel<DocumentCounterDto, CreateDocumentCounterDto, UpdateDocumentCounterDto>
{
    private readonly IDocumentCounterService _documentCounterService;
    private readonly IDocumentTypeService _documentTypeService;

    public DocumentCounterDetailViewModel(
        IDocumentCounterService documentCounterService,
        IDocumentTypeService documentTypeService,
        ILogger<DocumentCounterDetailViewModel> logger) 
        : base(logger)
    {
        _documentCounterService = documentCounterService;
        _documentTypeService = documentTypeService;
    }

    // Related entity collections
    public IEnumerable<DocumentTypeDto>? DocumentTypes { get; private set; }

    protected override DocumentCounterDto CreateNewEntity()
    {
        return new DocumentCounterDto
        {
            Id = Guid.Empty,
            DocumentTypeId = Guid.Empty,
            DocumentTypeName = null,
            Series = string.Empty,
            CurrentValue = 0,
            Year = null,
            Prefix = null,
            PaddingLength = 5,
            FormatPattern = null,
            ResetOnYearChange = true,
            Notes = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<DocumentCounterDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _documentCounterService.GetDocumentCounterByIdAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            DocumentTypes = new List<DocumentTypeDto>();
            return;
        }

        try
        {
            // Load document types for dropdown
            var documentTypesResult = await _documentTypeService.GetAllDocumentTypesAsync();
            DocumentTypes = documentTypesResult ?? new List<DocumentTypeDto>();
            
            Logger.LogInformation("Loaded {Count} document types for document counter {Id}", 
                DocumentTypes.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for document counter {Id}", entityId);
            DocumentTypes = new List<DocumentTypeDto>();
        }
    }

    protected override CreateDocumentCounterDto MapToCreateDto(DocumentCounterDto entity)
    {
        return new CreateDocumentCounterDto
        {
            DocumentTypeId = entity.DocumentTypeId,
            Series = entity.Series,
            Year = entity.Year,
            Prefix = entity.Prefix,
            PaddingLength = entity.PaddingLength,
            FormatPattern = entity.FormatPattern,
            ResetOnYearChange = entity.ResetOnYearChange,
            Notes = entity.Notes
        };
    }

    protected override UpdateDocumentCounterDto MapToUpdateDto(DocumentCounterDto entity)
    {
        return new UpdateDocumentCounterDto
        {
            CurrentValue = entity.CurrentValue,
            Prefix = entity.Prefix,
            PaddingLength = entity.PaddingLength,
            FormatPattern = entity.FormatPattern,
            ResetOnYearChange = entity.ResetOnYearChange,
            Notes = entity.Notes
        };
    }

    protected override Task<DocumentCounterDto?> CreateEntityAsync(CreateDocumentCounterDto createDto)
    {
        return _documentCounterService.CreateDocumentCounterAsync(createDto);
    }

    protected override Task<DocumentCounterDto?> UpdateEntityAsync(Guid entityId, UpdateDocumentCounterDto updateDto)
    {
        return _documentCounterService.UpdateDocumentCounterAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(DocumentCounterDto entity)
    {
        return entity.Id;
    }
}
