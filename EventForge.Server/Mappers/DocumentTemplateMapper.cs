using EventForge.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentTemplate entity to DTOs.
/// </summary>
public static class DocumentTemplateMapper
{
    /// <summary>
    /// Maps DocumentTemplate entity to DocumentTemplateDto.
    /// </summary>
    public static DocumentTemplateDto ToDto(DocumentTemplate template)
    {
        return new DocumentTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            DocumentTypeId = template.DocumentTypeId,
            DocumentTypeName = template.DocumentType?.Name,
            Category = template.Category,
            IsPublic = template.IsPublic,
            Owner = template.Owner,
            TemplateConfiguration = template.TemplateConfiguration,
            DefaultBusinessPartyId = template.DefaultBusinessPartyId,
            DefaultWarehouseId = template.DefaultWarehouseId,
            DefaultPaymentMethod = template.DefaultPaymentMethod,
            DefaultDueDateDays = template.DefaultDueDateDays,
            DefaultNotes = template.DefaultNotes,
            UsageCount = template.UsageCount,
            LastUsedAt = template.LastUsedAt,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            ModifiedAt = template.ModifiedAt,
            ModifiedBy = template.ModifiedBy,
            IsActive = template.IsActive
        };
    }

    /// <summary>
    /// Maps collection of DocumentTemplate entities to DocumentTemplateDto collection.
    /// </summary>
    public static IEnumerable<DocumentTemplateDto> ToDtoCollection(IEnumerable<DocumentTemplate> templates)
    {
        return templates.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of DocumentTemplate entities to DocumentTemplateDto list.
    /// </summary>
    public static List<DocumentTemplateDto> ToDtoList(IEnumerable<DocumentTemplate> templates)
    {
        return templates.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateDocumentTemplateDto to DocumentTemplate entity.
    /// </summary>
    public static DocumentTemplate ToEntity(CreateDocumentTemplateDto dto)
    {
        return new DocumentTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            DocumentTypeId = dto.DocumentTypeId,
            Category = dto.Category,
            IsPublic = dto.IsPublic,
            Owner = dto.Owner,
            TemplateConfiguration = dto.TemplateConfiguration,
            DefaultBusinessPartyId = dto.DefaultBusinessPartyId,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            DefaultPaymentMethod = dto.DefaultPaymentMethod,
            DefaultDueDateDays = dto.DefaultDueDateDays,
            DefaultNotes = dto.DefaultNotes
        };
    }

    /// <summary>
    /// Updates DocumentTemplate entity from UpdateDocumentTemplateDto.
    /// </summary>
    public static void UpdateEntity(DocumentTemplate template, UpdateDocumentTemplateDto dto)
    {
        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Category = dto.Category;
        template.IsPublic = dto.IsPublic;
        template.Owner = dto.Owner;
        template.TemplateConfiguration = dto.TemplateConfiguration;
        template.DefaultBusinessPartyId = dto.DefaultBusinessPartyId;
        template.DefaultWarehouseId = dto.DefaultWarehouseId;
        template.DefaultPaymentMethod = dto.DefaultPaymentMethod;
        template.DefaultDueDateDays = dto.DefaultDueDateDays;
        template.DefaultNotes = dto.DefaultNotes;
        template.IsActive = dto.IsActive;
    }
}