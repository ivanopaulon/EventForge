using EventForge.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Mapper for DocumentAnalytics entity and DTO conversions
/// </summary>
public static class DocumentAnalyticsMapper
{
    /// <summary>
    /// Maps DocumentAnalytics entity to DTO
    /// </summary>
    /// <param name="entity">DocumentAnalytics entity</param>
    /// <returns>DocumentAnalyticsDto</returns>
    public static DocumentAnalyticsDto ToDto(DocumentAnalytics entity)
    {
        return new DocumentAnalyticsDto
        {
            Id = entity.Id,
            DocumentHeaderId = entity.DocumentHeaderId,
            AnalyticsDate = entity.AnalyticsDate,
            DocumentTypeId = entity.DocumentTypeId,
            BusinessPartyId = entity.BusinessPartyId,
            CreatedByUser = entity.DocumentCreator,
            Department = entity.Department,
            Status = null, // Not directly available on entity
            Priority = null, // Not directly available on entity  
            Category = entity.AnalyticsCategory,
            Tags = entity.Tags,
            TimeToCompletionHours = entity.TimeToClosureHours,
            TimeToFirstApprovalHours = entity.TimeToFirstApprovalHours,
            TimeToClosureHours = entity.TimeToClosureHours,
            ProcessingTimeHours = entity.TotalProcessingTimeHours,
            ApprovalStepsRequired = entity.ApprovalStepsRequired,
            ApprovalStepsCompleted = entity.ApprovalStepsCompleted,
            ApprovalsReceived = entity.ApprovalsReceived,
            Rejections = entity.Rejections,
            AverageApprovalTimeHours = entity.AverageApprovalTimeHours,
            Escalations = entity.Escalations,
            ErrorCount = entity.Errors,
            RevisionCount = entity.Revisions,
            ReworkIterations = 0, // Not directly available, could be calculated
            DocumentValue = entity.DocumentValue,
            ProcessingCost = entity.ProcessingCost,
            EfficiencyScore = entity.EfficiencyScore,
            FinalStatus = entity.FinalStatus?.ToString(),
            QualityScore = entity.QualityScore,
            ComplianceScore = entity.ComplianceScore,
            SatisfactionRating = entity.SatisfactionScore,
            AdditionalData = entity.AdditionalData,
            DataSource = "workflow", // Default value since not stored on entity
            LastUpdatedAt = entity.ModifiedAt ?? entity.CreatedAt,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            TenantId = entity.TenantId
        };
    }

    /// <summary>
    /// Maps DTO to DocumentAnalytics entity (for create operations)
    /// </summary>
    /// <param name="dto">DocumentAnalyticsDto</param>
    /// <returns>DocumentAnalytics entity</returns>
    public static DocumentAnalytics ToEntity(DocumentAnalyticsDto dto)
    {
        return new DocumentAnalytics
        {
            Id = dto.Id,
            DocumentHeaderId = dto.DocumentHeaderId,
            AnalyticsDate = dto.AnalyticsDate,
            DocumentTypeId = dto.DocumentTypeId,
            BusinessPartyId = dto.BusinessPartyId,
            DocumentCreator = dto.CreatedByUser,
            Department = dto.Department,
            Tags = dto.Tags,
            TimeToFirstApprovalHours = dto.TimeToFirstApprovalHours,
            TimeToClosureHours = dto.TimeToClosureHours,
            TotalProcessingTimeHours = dto.ProcessingTimeHours,
            ApprovalStepsRequired = dto.ApprovalStepsRequired,
            ApprovalStepsCompleted = dto.ApprovalStepsCompleted,
            ApprovalsReceived = dto.ApprovalsReceived,
            Rejections = dto.Rejections,
            AverageApprovalTimeHours = dto.AverageApprovalTimeHours,
            Escalations = dto.Escalations,
            Errors = dto.ErrorCount,
            Revisions = dto.RevisionCount,
            DocumentValue = dto.DocumentValue,
            ProcessingCost = dto.ProcessingCost,
            EfficiencyScore = dto.EfficiencyScore,
            QualityScore = dto.QualityScore,
            ComplianceScore = dto.ComplianceScore,
            SatisfactionScore = dto.SatisfactionRating,
            AdditionalData = dto.AdditionalData,
            AnalyticsCategory = dto.Category
        };
    }

    /// <summary>
    /// Updates existing entity with DTO values
    /// </summary>
    /// <param name="entity">Existing entity</param>
    /// <param name="dto">DTO with new values</param>
    public static void UpdateEntity(DocumentAnalytics entity, DocumentAnalyticsDto dto)
    {
        entity.AnalyticsDate = dto.AnalyticsDate;
        entity.DocumentTypeId = dto.DocumentTypeId;
        entity.BusinessPartyId = dto.BusinessPartyId;
        entity.DocumentCreator = dto.CreatedByUser;
        entity.Department = dto.Department;
        entity.Tags = dto.Tags;
        entity.TimeToFirstApprovalHours = dto.TimeToFirstApprovalHours;
        entity.TimeToClosureHours = dto.TimeToClosureHours;
        entity.TotalProcessingTimeHours = dto.ProcessingTimeHours;
        entity.ApprovalStepsRequired = dto.ApprovalStepsRequired;
        entity.ApprovalStepsCompleted = dto.ApprovalStepsCompleted;
        entity.ApprovalsReceived = dto.ApprovalsReceived;
        entity.Rejections = dto.Rejections;
        entity.AverageApprovalTimeHours = dto.AverageApprovalTimeHours;
        entity.Escalations = dto.Escalations;
        entity.Errors = dto.ErrorCount;
        entity.Revisions = dto.RevisionCount;
        entity.DocumentValue = dto.DocumentValue;
        entity.ProcessingCost = dto.ProcessingCost;
        entity.EfficiencyScore = dto.EfficiencyScore;
        entity.QualityScore = dto.QualityScore;
        entity.ComplianceScore = dto.ComplianceScore;
        entity.SatisfactionScore = dto.SatisfactionRating;
        entity.AdditionalData = dto.AdditionalData;
        entity.AnalyticsCategory = dto.Category;
    }
}