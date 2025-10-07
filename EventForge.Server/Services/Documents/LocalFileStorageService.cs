using System.Security.Cryptography;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Local file system implementation of file storage service with tenant isolation
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _baseStoragePath;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseStoragePath = _configuration["FileStorage:BasePath"] ?? "App_Data/Files";

        // Ensure base storage directory exists
        _ = Directory.CreateDirectory(_baseStoragePath);
    }

    public async Task<FileStorageResult> SaveFileAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        try
        {
            // Create tenant-specific directory
            var tenantPath = Path.Combine(_baseStoragePath, tenantId.ToString());
            _ = Directory.CreateDirectory(tenantPath);

            // Generate unique file name to avoid conflicts
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(tenantPath, uniqueFileName);

            // Calculate file hash and size
            string fileHash;
            long fileSize;

            using (var sha256 = SHA256.Create())
            {
                var originalPosition = fileStream.Position;
                fileStream.Position = 0;

                var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
                fileHash = Convert.ToHexString(hashBytes);

                fileSize = fileStream.Length;
                fileStream.Position = originalPosition;
            }

            // Save file to disk
            using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Position = 0;
                await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
            }

            var storagePath = Path.Combine(tenantId.ToString(), uniqueFileName);

            _logger.LogInformation("File saved successfully: {FileName} -> {StoragePath} (Size: {FileSize} bytes)",
                fileName, storagePath, fileSize);

            return new FileStorageResult
            {
                StoragePath = storagePath,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                StoredAt = DateTime.UtcNow,
                FileHash = fileHash
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileName} for tenant {TenantId}", fileName, tenantId);
            throw;
        }
    }

    public Task<FileRetrievalResult?> GetFileAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return Task.FromResult<FileRetrievalResult?>(null);

        try
        {
            // Validate path is within tenant scope
            if (!storagePath.StartsWith(tenantId.ToString()))
            {
                _logger.LogWarning("Attempted to access file outside tenant scope: {StoragePath} for tenant {TenantId}",
                    storagePath, tenantId);
                return Task.FromResult<FileRetrievalResult?>(null);
            }

            var fullPath = Path.Combine(_baseStoragePath, storagePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {StoragePath}", storagePath);
                return Task.FromResult<FileRetrievalResult?>(null);
            }

            var fileInfo = new FileInfo(fullPath);
            var fileName = Path.GetFileName(storagePath);

            // Determine content type based on file extension
            var contentType = GetContentType(Path.GetExtension(fileName));

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var result = new FileRetrievalResult
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc
            };
            return Task.FromResult<FileRetrievalResult?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {StoragePath} for tenant {TenantId}", storagePath, tenantId);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return Task.FromResult(false);

        try
        {
            // Validate path is within tenant scope
            if (!storagePath.StartsWith(tenantId.ToString()))
            {
                _logger.LogWarning("Attempted to delete file outside tenant scope: {StoragePath} for tenant {TenantId}",
                    storagePath, tenantId);
                return Task.FromResult(false);
            }

            var fullPath = Path.Combine(_baseStoragePath, storagePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }

            File.Delete(fullPath);

            _logger.LogInformation("File deleted successfully: {StoragePath} for tenant {TenantId}",
                storagePath, tenantId);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {StoragePath} for tenant {TenantId}", storagePath, tenantId);
            throw;
        }
    }

    public Task<FileMetadata?> GetFileMetadataAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return Task.FromResult<FileMetadata?>(null);

        try
        {
            // Validate path is within tenant scope
            if (!storagePath.StartsWith(tenantId.ToString()))
            {
                _logger.LogWarning("Attempted to access file metadata outside tenant scope: {StoragePath} for tenant {TenantId}",
                    storagePath, tenantId);
                return Task.FromResult<FileMetadata?>(null);
            }

            var fullPath = Path.Combine(_baseStoragePath, storagePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<FileMetadata?>(null);
            }

            var fileInfo = new FileInfo(fullPath);
            var fileName = Path.GetFileName(storagePath);
            var contentType = GetContentType(Path.GetExtension(fileName));

            var result = new FileMetadata
            {
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileInfo.Length,
                CreatedAt = fileInfo.CreationTimeUtc,
                LastModified = fileInfo.LastWriteTimeUtc
            };
            return Task.FromResult<FileMetadata?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata {StoragePath} for tenant {TenantId}", storagePath, tenantId);
            throw;
        }
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".xml" => "application/xml",
            ".json" => "application/json",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}