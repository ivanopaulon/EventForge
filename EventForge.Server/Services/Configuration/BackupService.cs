using EventForge.Server.Mappers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing backup operations.
/// </summary>
public class BackupService : IBackupService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHubContext<AuditLogHub> _hubContext;
    private readonly ILogger<BackupService> _logger;
    private readonly IWebHostEnvironment _environment;

    public BackupService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IHubContext<AuditLogHub> hubContext,
        ILogger<BackupService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _tenantContext = tenantContext;
        _hubContext = hubContext;
        _logger = logger;
        _environment = environment;
    }

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request, CancellationToken ct = default)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can perform backup operations.");
        }

        var backup = new BackupOperation
        {
            Status = "Starting",
            StartedByUserId = _tenantContext.CurrentUserId ?? throw new InvalidOperationException("Current user ID not available"),
            Description = request.Description,
            IncludeAuditLogs = request.IncludeAuditLogs,
            IncludeUserData = request.IncludeUserData,
            IncludeConfiguration = request.IncludeConfiguration,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.CurrentUserId.ToString()
        };

        _ = _context.BackupOperations.Add(backup);
        _ = await _context.SaveChangesAsync(ct);

        // Start backup process in background
        _ = Task.Run(async () => await PerformBackupAsync(backup.Id, CancellationToken.None), CancellationToken.None);

        var result = BackupMapper.ToServerStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId, ct));

        return result;
    }

    public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId, CancellationToken ct = default)
    {
        var backup = await _context.BackupOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == backupId, ct);

        if (backup == null)
        {
            return null;
        }

        var result = BackupMapper.ToServerStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId, ct));

        return result;
    }

    public async Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50, CancellationToken ct = default)
    {
        var backups = await _context.BackupOperations
            .AsNoTracking()
            .OrderByDescending(b => b.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        var results = new List<BackupStatusDto>();

        foreach (var backup in backups)
        {
            var dto = BackupMapper.ToServerStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId, ct));
            results.Add(dto);
        }

        return results;
    }

    public async Task CancelBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId, ct);

        if (backup == null)
        {
            throw new InvalidOperationException($"Backup operation {backupId} not found.");
        }

        if (backup.Status == "Completed" || backup.Status == "Failed" || backup.Status == "Cancelled")
        {
            throw new InvalidOperationException($"Cannot cancel backup in {backup.Status} status.");
        }

        backup.Status = "Cancelled";
        backup.CompletedAt = DateTime.UtcNow;
        backup.ModifiedAt = DateTime.UtcNow;
        backup.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

        _ = await _context.SaveChangesAsync(ct);

        // Notify clients
        await NotifyBackupStatusChange(backup, ct);
    }

    public async Task<(Stream FileStream, string FileName)?> DownloadBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        var backup = await _context.BackupOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == backupId, ct);

        if (backup == null || backup.Status != "Completed" || string.IsNullOrEmpty(backup.FilePath))
        {
            return null;
        }

        if (!File.Exists(backup.FilePath))
        {
            return null;
        }

        var fileStream = new FileStream(backup.FilePath, FileMode.Open, FileAccess.Read);
        var fileName = $"EventForge_Backup_{backup.StartedAt:yyyyMMdd_HHmmss}.zip";

        return (fileStream, fileName);
    }

    public async Task DeleteBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId, ct);

        if (backup == null)
        {
            throw new InvalidOperationException($"Backup operation {backupId} not found.");
        }

        // Delete physical file if it exists
        if (!string.IsNullOrEmpty(backup.FilePath) && File.Exists(backup.FilePath))
        {
            try
            {
                File.Delete(backup.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete backup file: {FilePath}", backup.FilePath);
            }
        }

        // Delete database record
        _ = _context.BackupOperations.Remove(backup);
        _ = await _context.SaveChangesAsync(ct);
    }

    private async Task PerformBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        var backup = await _context.BackupOperations.FindAsync(new object[] { backupId }, ct);
        if (backup == null) return;

        try
        {
            backup.Status = "In Progress";
            backup.ProgressPercentage = 0;
            backup.CurrentOperation = "Preparing backup";
            _ = await _context.SaveChangesAsync(ct);
            await NotifyBackupStatusChange(backup, ct);

            // Create backup directory
            var backupDir = Path.Combine(_environment.ContentRootPath, "Backups");
            _ = Directory.CreateDirectory(backupDir);

            var fileName = $"EventForge_Backup_{backup.StartedAt:yyyyMMdd_HHmmss}.zip";
            var filePath = Path.Combine(backupDir, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // Backup configuration
                if (backup.IncludeConfiguration)
                {
                    backup.CurrentOperation = "Backing up configuration";
                    backup.ProgressPercentage = 10;
                    _ = await _context.SaveChangesAsync(ct);
                    await NotifyBackupStatusChange(backup, ct);

                    await BackupConfiguration(archive, ct);
                }

                // Backup user data
                if (backup.IncludeUserData)
                {
                    backup.CurrentOperation = "Backing up user data";
                    backup.ProgressPercentage = 40;
                    _ = await _context.SaveChangesAsync(ct);
                    await NotifyBackupStatusChange(backup, ct);

                    await BackupUserData(archive, ct);
                }

                // Backup audit logs
                if (backup.IncludeAuditLogs)
                {
                    backup.CurrentOperation = "Backing up audit logs";
                    backup.ProgressPercentage = 70;
                    _ = await _context.SaveChangesAsync(ct);
                    await NotifyBackupStatusChange(backup, ct);

                    await BackupAuditLogs(archive, ct);
                }

                backup.CurrentOperation = "Finalizing backup";
                backup.ProgressPercentage = 90;
                _ = await _context.SaveChangesAsync(ct);
                await NotifyBackupStatusChange(backup, ct);
            }

            // Get file size
            var fileInfo = new FileInfo(filePath);

            backup.Status = "Completed";
            backup.ProgressPercentage = 100;
            backup.CompletedAt = DateTime.UtcNow;
            backup.FilePath = filePath;
            backup.FileSizeBytes = fileInfo.Length;
            backup.CurrentOperation = null;
            backup.ModifiedAt = DateTime.UtcNow;
            backup.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(ct);
            await NotifyBackupStatusChange(backup, ct);

            _logger.LogInformation("Backup completed successfully: {BackupId}, File: {FilePath}, Size: {Size} bytes",
                backupId, filePath, fileInfo.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Backup operation {BackupId} was cancelled", backupId);
            backup.Status = "Cancelled";
            backup.CompletedAt = DateTime.UtcNow;
            backup.ModifiedAt = DateTime.UtcNow;
            backup.ModifiedBy = "System";
            _ = await _context.SaveChangesAsync(CancellationToken.None);
            await NotifyBackupStatusChange(backup, CancellationToken.None);
        }
        catch (Exception ex)
        {
            backup.Status = "Failed";
            backup.ErrorMessage = ex.Message;
            backup.CompletedAt = DateTime.UtcNow;
            backup.ModifiedAt = DateTime.UtcNow;
            backup.ModifiedBy = "System";

            _ = await _context.SaveChangesAsync(CancellationToken.None);
            await NotifyBackupStatusChange(backup, CancellationToken.None);

            _logger.LogError(ex, "Backup failed: {BackupId}", backupId);
        }
    }

    private async Task BackupConfiguration(ZipArchive archive, CancellationToken ct = default)
    {
        var configurations = await _context.SystemConfigurations
            .AsNoTracking()
            .ToListAsync(ct);
        var data = JsonSerializer.Serialize(configurations, new JsonSerializerOptions { WriteIndented = true });

        var entry = archive.CreateEntry("configuration.json");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(data);
    }

    private async Task BackupUserData(ZipArchive archive, CancellationToken ct = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync(ct);
        var tenants = await _context.Tenants
            .AsNoTracking()
            .ToListAsync(ct);

        var userData = new
        {
            Users = users,
            Tenants = tenants
        };

        var data = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });

        var entry = archive.CreateEntry("users.json");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(data);
    }

    private async Task BackupAuditLogs(ZipArchive archive, CancellationToken ct = default)
    {
        var auditLogs = await _context.EntityChangeLogs
            .AsNoTracking()
            .ToListAsync(ct);
        var auditTrails = await _context.AuditTrails
            .AsNoTracking()
            .ToListAsync(ct);

        var auditData = new
        {
            EntityChangeLogs = auditLogs,
            AuditTrails = auditTrails
        };

        var data = JsonSerializer.Serialize(auditData, new JsonSerializerOptions { WriteIndented = true });

        var entry = archive.CreateEntry("audit.json");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(data);
    }

    private async Task NotifyBackupStatusChange(BackupOperation backup, CancellationToken ct = default)
    {
        var dto = BackupMapper.ToServerStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId, ct));

        await _hubContext.Clients.Group("AuditLogUpdates")
            .SendAsync("BackupStatusChanged", dto, ct);
    }

    private async Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown User";
    }
}