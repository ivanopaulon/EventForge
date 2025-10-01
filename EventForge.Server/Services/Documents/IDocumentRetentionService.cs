using EventForge.DTOs.Documents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for document retention policy management.
/// Supports GDPR compliance through automated document lifecycle management.
/// </summary>
public interface IDocumentRetentionService
{
    /// <summary>
    /// Gets all retention policies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of retention policies</returns>
    Task<IEnumerable<DocumentRetentionPolicyDto>> GetAllPoliciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a retention policy by ID.
    /// </summary>
    /// <param name="id">Policy ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Retention policy or null if not found</returns>
    Task<DocumentRetentionPolicyDto?> GetPolicyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets retention policy for a specific document type.
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Retention policy or null if not found</returns>
    Task<DocumentRetentionPolicyDto?> GetPolicyByDocumentTypeAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new retention policy.
    /// </summary>
    /// <param name="dto">Policy data</param>
    /// <param name="currentUser">User creating the policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created retention policy</returns>
    Task<DocumentRetentionPolicyDto> CreatePolicyAsync(
        CreateDocumentRetentionPolicyDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing retention policy.
    /// </summary>
    /// <param name="id">Policy ID</param>
    /// <param name="dto">Updated policy data</param>
    /// <param name="currentUser">User updating the policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated retention policy or null if not found</returns>
    Task<DocumentRetentionPolicyDto?> UpdatePolicyAsync(
        Guid id,
        UpdateDocumentRetentionPolicyDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a retention policy.
    /// </summary>
    /// <param name="id">Policy ID</param>
    /// <param name="currentUser">User deleting the policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePolicyAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies retention policies and deletes/archives expired documents.
    /// This method is called by a background job.
    /// </summary>
    /// <param name="dryRun">If true, only simulates deletion without actually deleting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary of documents processed (deleted/archived)</returns>
    Task<RetentionApplicationResultDto> ApplyRetentionPoliciesAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents that are eligible for deletion based on retention policies.
    /// </summary>
    /// <param name="policyId">Optional policy ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document IDs eligible for deletion</returns>
    Task<IEnumerable<Guid>> GetEligibleForDeletionAsync(
        Guid? policyId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for retention policy application results.
/// </summary>
public class RetentionApplicationResultDto
{
    /// <summary>
    /// Whether this was a dry run (simulation only).
    /// </summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Number of documents deleted.
    /// </summary>
    public int DocumentsDeleted { get; set; }

    /// <summary>
    /// Number of documents archived.
    /// </summary>
    public int DocumentsArchived { get; set; }

    /// <summary>
    /// Number of policies applied.
    /// </summary>
    public int PoliciesApplied { get; set; }

    /// <summary>
    /// Execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Any errors encountered.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
