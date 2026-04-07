using EventForge.DTOs.Documents;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Implementation of document retention policy service.
/// Manages document lifecycle for GDPR compliance.
/// </summary>
public class DocumentRetentionService(
    EventForgeDbContext context,
    ILogger<DocumentRetentionService> logger) : IDocumentRetentionService
{

    public async Task<IEnumerable<DocumentRetentionPolicyDto>> GetAllPoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        var policies = await context.Set<DocumentRetentionPolicy>()
            .Include(p => p.DocumentType)
            .OrderBy(p => p.DocumentType!.Name)
            .ToListAsync(cancellationToken);

        return policies.Select(MapToDto);
    }

    public async Task<DocumentRetentionPolicyDto?> GetPolicyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var policy = await context.Set<DocumentRetentionPolicy>()
            .Include(p => p.DocumentType)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return policy is not null ? MapToDto(policy) : null;
    }

    public async Task<DocumentRetentionPolicyDto?> GetPolicyByDocumentTypeAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default)
    {
        var policy = await context.Set<DocumentRetentionPolicy>()
            .Include(p => p.DocumentType)
            .FirstOrDefaultAsync(p => p.DocumentTypeId == documentTypeId && p.IsActive, cancellationToken);

        return policy is not null ? MapToDto(policy) : null;
    }

    public async Task<DocumentRetentionPolicyDto> CreatePolicyAsync(
        CreateDocumentRetentionPolicyDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Check if policy already exists for this document type
        var existingPolicy = await context.Set<DocumentRetentionPolicy>()
            .FirstOrDefaultAsync(p => p.DocumentTypeId == dto.DocumentTypeId, cancellationToken);

        if (existingPolicy is not null)
        {
            throw new InvalidOperationException(
                $"A retention policy already exists for document type {dto.DocumentTypeId}");
        }

        var policy = new DocumentRetentionPolicy
        {
            Id = Guid.NewGuid(),
            DocumentTypeId = dto.DocumentTypeId,
            RetentionDays = dto.RetentionDays,
            AutoDeleteEnabled = dto.AutoDeleteEnabled,
            GracePeriodDays = dto.GracePeriodDays,
            ArchiveInsteadOfDelete = dto.ArchiveInsteadOfDelete,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = currentUser,
            ModifiedAt = DateTime.UtcNow
        };

        _ = context.Set<DocumentRetentionPolicy>().Add(policy);
        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created retention policy {PolicyId} for document type {DocumentTypeId} by {User}",
            policy.Id, dto.DocumentTypeId, currentUser);

        // Reload with navigation properties
        await context.Entry(policy).Reference(p => p.DocumentType).LoadAsync(cancellationToken);

        return MapToDto(policy);
    }

    public async Task<DocumentRetentionPolicyDto?> UpdatePolicyAsync(
        Guid id,
        UpdateDocumentRetentionPolicyDto dto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var policy = await context.Set<DocumentRetentionPolicy>()
            .Include(p => p.DocumentType)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (policy is null)
        {
            return null;
        }

        // Update only provided fields
        if (dto.RetentionDays.HasValue)
            policy.RetentionDays = dto.RetentionDays.Value;

        if (dto.AutoDeleteEnabled.HasValue)
            policy.AutoDeleteEnabled = dto.AutoDeleteEnabled.Value;

        if (dto.GracePeriodDays.HasValue)
            policy.GracePeriodDays = dto.GracePeriodDays.Value;

        if (dto.ArchiveInsteadOfDelete.HasValue)
            policy.ArchiveInsteadOfDelete = dto.ArchiveInsteadOfDelete.Value;

        if (dto.IsActive.HasValue)
            policy.IsActive = dto.IsActive.Value;

        if (dto.Notes is not null)
            policy.Notes = dto.Notes;

        policy.ModifiedBy = currentUser;
        policy.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated retention policy {PolicyId} by {User}",
            id, currentUser);

        return MapToDto(policy);
    }

    public async Task<bool> DeletePolicyAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var policy = await context.Set<DocumentRetentionPolicy>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (policy is null)
        {
            return false;
        }

        _ = context.Set<DocumentRetentionPolicy>().Remove(policy);
        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted retention policy {PolicyId} by {User}",
            id, currentUser);

        return true;
    }

    public async Task<RetentionApplicationResultDto> ApplyRetentionPoliciesAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new RetentionApplicationResultDto
        {
            WasDryRun = dryRun,
            ExecutedAt = DateTime.UtcNow,
            Errors = new List<string>()
        };

        try
        {
            logger.LogInformation(
                "Starting retention policy application (DryRun: {DryRun})",
                dryRun);

            // Get all active policies
            var policies = await context.Set<DocumentRetentionPolicy>()
                .Where(p => p.IsActive && p.AutoDeleteEnabled && p.RetentionDays.HasValue)
                .ToListAsync(cancellationToken);

            logger.LogInformation(
                "Found {Count} active retention policies",
                policies.Count);

            foreach (var policy in policies)
            {
                try
                {
                    var (deleted, archived) = await ApplyPolicyAsync(policy, dryRun, cancellationToken);
                    result.DocumentsDeleted += deleted;
                    result.DocumentsArchived += archived;
                    result.PoliciesApplied++;

                    if (!dryRun)
                    {
                        policy.LastAppliedAt = DateTime.UtcNow;
                        policy.DocumentsDeleted += deleted;
                        policy.DocumentsArchived += archived;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error applying retention policy {PolicyId}",
                        policy.Id);
                    result.Errors.Add($"Policy {policy.Id}: {ex.Message}");
                }
            }

            if (!dryRun && result.PoliciesApplied > 0)
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            logger.LogInformation(
                "Completed retention policy application: {Deleted} deleted, {Archived} archived, {Duration}ms",
                result.DocumentsDeleted, result.DocumentsArchived, result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in retention policy application");
            result.Errors.Add($"Global error: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<Guid>> GetEligibleForDeletionAsync(
        Guid? policyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<DocumentRetentionPolicy>()
            .Where(p => p.IsActive && p.RetentionDays.HasValue);

        if (policyId.HasValue)
        {
            query = query.Where(p => p.Id == policyId.Value);
        }

        var policies = await query.ToListAsync(cancellationToken);
        var eligibleDocuments = new List<Guid>();

        foreach (var policy in policies)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-(policy.RetentionDays!.Value + policy.GracePeriodDays));

            var documents = await context.DocumentHeaders
                .Where(d => d.DocumentTypeId == policy.DocumentTypeId)
                .Where(d => d.CreatedAt <= cutoffDate)
                .Where(d => !d.IsDeleted)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            eligibleDocuments.AddRange(documents);
        }

        return eligibleDocuments.Distinct();
    }

    private async Task<(int deleted, int archived)> ApplyPolicyAsync(
        DocumentRetentionPolicy policy,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-(policy.RetentionDays!.Value + policy.GracePeriodDays));

        var documents = await context.DocumentHeaders
            .Where(d => d.DocumentTypeId == policy.DocumentTypeId)
            .Where(d => d.CreatedAt <= cutoffDate)
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Policy {PolicyId}: Found {Count} documents eligible for {Action}",
            policy.Id, documents.Count,
            policy.ArchiveInsteadOfDelete ? "archiving" : "deletion");

        if (dryRun || documents.Count == 0)
        {
            return policy.ArchiveInsteadOfDelete
                ? (0, documents.Count)
                : (documents.Count, 0);
        }

        int deleted = 0;
        int archived = 0;

        foreach (var doc in documents)
        {
            if (policy.ArchiveInsteadOfDelete)
            {
                // Mark as archived (soft delete with specific reason)
                doc.IsDeleted = true;
                doc.DeletedAt = DateTime.UtcNow;
                doc.DeletedBy = "System.RetentionPolicy";
                doc.Notes = (doc.Notes ?? "") + $" [Archived by retention policy {policy.Id}]";
                archived++;
            }
            else
            {
                // Soft delete
                doc.IsDeleted = true;
                doc.DeletedAt = DateTime.UtcNow;
                doc.DeletedBy = "System.RetentionPolicy";
                deleted++;
            }
        }

        return (deleted, archived);
    }

    private DocumentRetentionPolicyDto MapToDto(DocumentRetentionPolicy policy)
    {
        return new DocumentRetentionPolicyDto
        {
            Id = policy.Id,
            DocumentTypeId = policy.DocumentTypeId,
            DocumentTypeName = policy.DocumentType?.Name,
            RetentionDays = policy.RetentionDays,
            AutoDeleteEnabled = policy.AutoDeleteEnabled,
            GracePeriodDays = policy.GracePeriodDays,
            ArchiveInsteadOfDelete = policy.ArchiveInsteadOfDelete,
            IsActive = policy.IsActive,
            Notes = policy.Notes,
            CreatedBy = policy.CreatedBy,
            CreatedAt = policy.CreatedAt,
            UpdatedBy = policy.ModifiedBy,
            UpdatedAt = policy.ModifiedAt
        };
    }

}
