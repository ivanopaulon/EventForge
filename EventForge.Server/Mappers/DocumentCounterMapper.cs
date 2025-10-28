using EventForge.DTOs.Documents;
using EventForge.Server.Data.Entities.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentCounter entity to DTOs.
/// </summary>
public static class DocumentCounterMapper
{
    /// <summary>
    /// Maps DocumentCounter entity to DocumentCounterDto.
    /// </summary>
    public static DocumentCounterDto ToDto(this DocumentCounter counter)
    {
        return new DocumentCounterDto
        {
            Id = counter.Id,
            DocumentTypeId = counter.DocumentTypeId,
            DocumentTypeName = counter.DocumentType?.Name,
            Series = counter.Series,
            CurrentValue = counter.CurrentValue,
            Year = counter.Year,
            Prefix = counter.Prefix,
            PaddingLength = counter.PaddingLength,
            FormatPattern = counter.FormatPattern,
            ResetOnYearChange = counter.ResetOnYearChange,
            Notes = counter.Notes,
            CreatedAt = counter.CreatedAt,
            CreatedBy = counter.CreatedBy,
            ModifiedAt = counter.ModifiedAt,
            ModifiedBy = counter.ModifiedBy
        };
    }

    /// <summary>
    /// Maps collection of DocumentCounter entities to DocumentCounterDto collection.
    /// </summary>
    public static IEnumerable<DocumentCounterDto> ToDtoCollection(this IEnumerable<DocumentCounter> counters)
    {
        return counters.Select(ToDto);
    }

    /// <summary>
    /// Maps CreateDocumentCounterDto to DocumentCounter entity.
    /// </summary>
    public static DocumentCounter ToEntity(this CreateDocumentCounterDto dto)
    {
        return new DocumentCounter
        {
            DocumentTypeId = dto.DocumentTypeId,
            Series = dto.Series,
            CurrentValue = 0, // Start at 0
            Year = dto.Year,
            Prefix = dto.Prefix,
            PaddingLength = dto.PaddingLength,
            FormatPattern = dto.FormatPattern,
            ResetOnYearChange = dto.ResetOnYearChange,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates DocumentCounter entity from UpdateDocumentCounterDto.
    /// </summary>
    public static void UpdateFromDto(this DocumentCounter entity, UpdateDocumentCounterDto dto)
    {
        entity.CurrentValue = dto.CurrentValue;
        entity.Prefix = dto.Prefix;
        entity.PaddingLength = dto.PaddingLength;
        entity.FormatPattern = dto.FormatPattern;
        entity.ResetOnYearChange = dto.ResetOnYearChange;
        entity.Notes = dto.Notes;
    }
}
