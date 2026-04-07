using EventForge.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentRecurrence entity to DTOs.
/// </summary>
public static class DocumentRecurrenceMapper
{
    /// <summary>
    /// Maps DocumentRecurrence entity to DocumentRecurrenceDto.
    /// </summary>
    public static DocumentRecurrenceDto ToDto(DocumentRecurrence recurrence)
    {
        return new DocumentRecurrenceDto
        {
            Id = recurrence.Id,
            Name = recurrence.Name,
            Description = recurrence.Description,
            TemplateId = recurrence.TemplateId,
            TemplateName = recurrence.Template?.Name,
            Pattern = recurrence.Pattern,
            Interval = recurrence.Interval,
            DaysOfWeek = recurrence.DaysOfWeek,
            DayOfMonth = recurrence.DayOfMonth,
            StartDate = recurrence.StartDate,
            EndDate = recurrence.EndDate,
            MaxOccurrences = recurrence.MaxOccurrences,
            NextExecutionDate = recurrence.NextExecutionDate,
            LastExecutionDate = recurrence.LastExecutionDate,
            ExecutionCount = recurrence.ExecutionCount,
            IsEnabled = recurrence.IsEnabled,
            Status = recurrence.Status,
            BusinessPartyId = recurrence.BusinessPartyId,
            WarehouseId = recurrence.WarehouseId,
            LeadTimeDays = recurrence.LeadTimeDays,
            NotificationSettings = recurrence.NotificationSettings,
            AdditionalConfig = recurrence.AdditionalConfig,
            CreatedAt = recurrence.CreatedAt,
            CreatedBy = recurrence.CreatedBy,
            ModifiedAt = recurrence.ModifiedAt,
            ModifiedBy = recurrence.ModifiedBy,
            IsActive = recurrence.IsActive
        };
    }

    /// <summary>
    /// Maps collection of DocumentRecurrence entities to DocumentRecurrenceDto collection.
    /// </summary>
    public static IEnumerable<DocumentRecurrenceDto> ToDtoCollection(IEnumerable<DocumentRecurrence> recurrences)
    {
        return recurrences.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of DocumentRecurrence entities to DocumentRecurrenceDto list.
    /// </summary>
    public static List<DocumentRecurrenceDto> ToDtoList(IEnumerable<DocumentRecurrence> recurrences)
    {
        return recurrences.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateDocumentRecurrenceDto to DocumentRecurrence entity.
    /// </summary>
    public static DocumentRecurrence ToEntity(CreateDocumentRecurrenceDto dto)
    {
        return new DocumentRecurrence
        {
            Name = dto.Name,
            Description = dto.Description,
            TemplateId = dto.TemplateId,
            Pattern = dto.Pattern,
            Interval = dto.Interval,
            DaysOfWeek = dto.DaysOfWeek,
            DayOfMonth = dto.DayOfMonth,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            MaxOccurrences = dto.MaxOccurrences,
            BusinessPartyId = dto.BusinessPartyId,
            WarehouseId = dto.WarehouseId,
            LeadTimeDays = dto.LeadTimeDays,
            NotificationSettings = dto.NotificationSettings,
            AdditionalConfig = dto.AdditionalConfig,
            // Calculate initial next execution date
            NextExecutionDate = dto.StartDate
        };
    }

    /// <summary>
    /// Updates DocumentRecurrence entity from UpdateDocumentRecurrenceDto.
    /// </summary>
    public static void UpdateEntity(DocumentRecurrence recurrence, UpdateDocumentRecurrenceDto dto)
    {
        recurrence.Name = dto.Name;
        recurrence.Description = dto.Description;
        recurrence.Pattern = dto.Pattern;
        recurrence.Interval = dto.Interval;
        recurrence.DaysOfWeek = dto.DaysOfWeek;
        recurrence.DayOfMonth = dto.DayOfMonth;
        recurrence.StartDate = dto.StartDate;
        recurrence.EndDate = dto.EndDate;
        recurrence.MaxOccurrences = dto.MaxOccurrences;
        recurrence.IsEnabled = dto.IsEnabled;
        recurrence.Status = dto.Status;
        recurrence.BusinessPartyId = dto.BusinessPartyId;
        recurrence.WarehouseId = dto.WarehouseId;
        recurrence.LeadTimeDays = dto.LeadTimeDays;
        recurrence.NotificationSettings = dto.NotificationSettings;
        recurrence.AdditionalConfig = dto.AdditionalConfig;
        recurrence.IsActive = dto.IsActive;
    }
}