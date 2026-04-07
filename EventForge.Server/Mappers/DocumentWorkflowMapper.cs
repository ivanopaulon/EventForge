using EventForge.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentWorkflow entity to DTOs.
/// </summary>
public static class DocumentWorkflowMapper
{
    /// <summary>
    /// Maps DocumentWorkflow entity to DocumentWorkflowDto.
    /// </summary>
    public static DocumentWorkflowDto ToDto(DocumentWorkflow workflow)
    {
        return new DocumentWorkflowDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            DocumentTypeId = workflow.DocumentTypeId,
            DocumentTypeName = workflow.DocumentType?.Name,
            Category = workflow.Category,
            Priority = workflow.Priority,
            IsActive = workflow.IsActive,
            AutoApprovalThreshold = null, // Not directly available in entity
            EscalationTimeoutHours = workflow.MaxProcessingTimeHours,
            DefaultAssignee = null, // Could be derived from step definitions
            NotificationConfig = workflow.NotificationSettings,
            Version = workflow.WorkflowVersion,
            CreatedAt = workflow.CreatedAt,
            CreatedBy = workflow.CreatedBy,
            ModifiedAt = workflow.ModifiedAt,
            ModifiedBy = workflow.ModifiedBy,
            StepCount = workflow.StepDefinitions?.Count ?? 0,
            ActiveExecutions = workflow.WorkflowExecutions?.Count(e => e.Status == DTOs.Common.WorkflowExecutionStatus.InProgress) ?? 0
        };
    }

    /// <summary>
    /// Maps collection of DocumentWorkflow entities to DocumentWorkflowDto collection.
    /// </summary>
    public static IEnumerable<DocumentWorkflowDto> ToDtoCollection(IEnumerable<DocumentWorkflow> workflows)
    {
        return workflows.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of DocumentWorkflow entities to DocumentWorkflowDto list.
    /// </summary>
    public static List<DocumentWorkflowDto> ToDtoList(IEnumerable<DocumentWorkflow> workflows)
    {
        return workflows.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateDocumentWorkflowDto to DocumentWorkflow entity.
    /// </summary>
    public static DocumentWorkflow ToEntity(CreateDocumentWorkflowDto dto)
    {
        return new DocumentWorkflow
        {
            Name = dto.Name,
            Description = dto.Description,
            DocumentTypeId = dto.DocumentTypeId,
            Category = dto.Category,
            Priority = dto.Priority,
            MaxProcessingTimeHours = dto.EscalationTimeoutHours,
            NotificationSettings = dto.NotificationConfig,
            WorkflowVersion = 1 // Initial version
        };
    }

    /// <summary>
    /// Updates DocumentWorkflow entity from UpdateDocumentWorkflowDto.
    /// </summary>
    public static void UpdateEntity(DocumentWorkflow workflow, UpdateDocumentWorkflowDto dto)
    {
        workflow.Name = dto.Name;
        workflow.Description = dto.Description;
        workflow.DocumentTypeId = dto.DocumentTypeId;
        workflow.Category = dto.Category;
        workflow.Priority = dto.Priority;
        workflow.IsActive = dto.IsActive;
        workflow.MaxProcessingTimeHours = dto.EscalationTimeoutHours;
        workflow.NotificationSettings = dto.NotificationConfig;
        // Increment version on update
        workflow.WorkflowVersion++;
    }
}