namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for file storage operations with multi-tenant support
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage with tenant-aware path
    /// </summary>
    /// <param name="tenantId">Tenant ID for path isolation</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage path and metadata</returns>
    Task<FileStorageResult> SaveFileAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file from storage
    /// </summary>
    /// <param name="tenantId">Tenant ID for security validation</param>
    /// <param name="storagePath">Storage path returned from SaveFileAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream and metadata</returns>
    Task<FileRetrievalResult?> GetFileAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="tenantId">Tenant ID for security validation</param>
    /// <param name="storagePath">Storage path to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteFileAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file information without downloading content
    /// </summary>
    /// <param name="tenantId">Tenant ID for security validation</param>
    /// <param name="storagePath">Storage path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata?> GetFileMetadataAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of file storage operation
/// </summary>
public class FileStorageResult
{
    public required string StoragePath { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long FileSize { get; set; }
    public required DateTime StoredAt { get; set; }
    public string? FileHash { get; set; }
}

/// <summary>
/// Result of file retrieval operation
/// </summary>
public class FileRetrievalResult : IDisposable
{
    public required Stream FileStream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long FileSize { get; set; }
    public DateTime LastModified { get; set; }

    public void Dispose()
    {
        FileStream?.Dispose();
    }
}

/// <summary>
/// File metadata information
/// </summary>
public class FileMetadata
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long FileSize { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string? FileHash { get; set; }
}