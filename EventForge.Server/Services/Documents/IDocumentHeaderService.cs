using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for managing document headers.
/// </summary>
public interface IDocumentHeaderService
{
    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated document headers</returns>
    Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document header by ID.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document headers by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document headers</returns>
    Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document header DTO</returns>
    Task<DocumentHeaderDto> CreateDocumentHeaderAsync(
        CreateDocumentHeaderDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="updateDto">Document header update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> UpdateDocumentHeaderAsync(
        Guid id,
        UpdateDocumentHeaderDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document header (soft delete).
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentHeaderAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates document totals (net, VAT, gross) for a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header with updated totals or null if not found</returns>
    Task<DocumentHeaderDto?> CalculateDocumentTotalsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> ApproveDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO or null if not found</returns>
    Task<DocumentHeaderDto?> CloseDocumentAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document header exists.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> DocumentHeaderExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates an inventory document type.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory document type</returns>
    Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a receipt document type for sales.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Receipt document type</returns>
    Task<DocumentTypeDto> GetOrCreateReceiptDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a system business party for internal operations.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System business party ID</returns>
    Task<Guid> GetOrCreateSystemBusinessPartyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a row to an existing document header.
    /// </summary>
    /// <param name="createDto">Document row creation data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document row DTO</returns>
    Task<DocumentRowDto> AddDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="updateDto">Document row update data</param>
    /// <param name="currentUser">Current user for auditing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document row DTO or null if not found</returns>
    Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid rowId,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document row (soft delete).
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDocumentRowAsync(
        Guid rowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires an exclusive edit lock for a document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="userName">User acquiring the lock</param>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <returns>True if lock acquired, false if already locked by another user</returns>
    Task<bool> AcquireLockAsync(Guid documentId, string userName, string connectionId);

    /// <summary>
    /// Releases an edit lock for a document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="userName">User releasing the lock</param>
    /// <returns>True if lock released, false if user doesn't hold the lock</returns>
    Task<bool> ReleaseLockAsync(Guid documentId, string userName);

    /// <summary>
    /// Releases all locks held by a specific SignalR connection.
    /// Called when a user disconnects.
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    Task ReleaseAllLocksForConnectionAsync(string connectionId);

    /// <summary>
    /// Gets lock information for a document.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Lock information or null if document not found</returns>
    Task<DocumentLockInfo?> GetLockInfoAsync(Guid documentId);

    /// <summary>
    /// Get documents for export with batch processing support
    /// </summary>
    Task<IEnumerable<EventForge.DTOs.Export.DocumentExportDto>> GetDocumentsForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);
}