using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.IO.Compression;
using System.Text.Json;
using EventForge.Server.Mappers;

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

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request)
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

        _context.BackupOperations.Add(backup);
        await _context.SaveChangesAsync();

        // Start backup process in background
        _ = Task.Run(async () => await PerformBackupAsync(backup.Id));

        var result = BackupMapper.ToStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId));

        return result;
    }

    public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId);

        if (backup == null)
        {
            return null;
        }

        var result = BackupMapper.ToStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId));

        return result;
    }

    public async Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50)
    {
        var backups = await _context.BackupOperations
            .OrderByDescending(b => b.StartedAt)
            .Take(limit)
            .ToListAsync();

        var results = new List<BackupStatusDto>();
        
        foreach (var backup in backups)
        {
            var dto = BackupMapper.ToStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId));
            results.Add(dto);
        }

        return results;
    }

    public async Task CancelBackupAsync(Guid backupId)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId);

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

        await _context.SaveChangesAsync();
        
        // Notify clients
        await NotifyBackupStatusChange(backup);
    }

    public async Task<(Stream FileStream, string FileName)?> DownloadBackupAsync(Guid backupId)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId);

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

    public async Task DeleteBackupAsync(Guid backupId)
    {
        var backup = await _context.BackupOperations
            .FirstOrDefaultAsync(b => b.Id == backupId);

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
        _context.BackupOperations.Remove(backup);
        await _context.SaveChangesAsync();
    }

    private async Task PerformBackupAsync(Guid backupId)
    {
        var backup = await _context.BackupOperations.FindAsync(backupId);
        if (backup == null) return;

        try
        {
            backup.Status = "In Progress";
            backup.ProgressPercentage = 0;
            backup.CurrentOperation = "Preparing backup";
            await _context.SaveChangesAsync();
            await NotifyBackupStatusChange(backup);

            // Create backup directory
            var backupDir = Path.Combine(_environment.ContentRootPath, "Backups");
            Directory.CreateDirectory(backupDir);

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
                    await _context.SaveChangesAsync();
                    await NotifyBackupStatusChange(backup);

                    await BackupConfiguration(archive);
                }

                // Backup user data
                if (backup.IncludeUserData)
                {
                    backup.CurrentOperation = "Backing up user data";
                    backup.ProgressPercentage = 40;
                    await _context.SaveChangesAsync();
                    await NotifyBackupStatusChange(backup);

                    await BackupUserData(archive);
                }

                // Backup audit logs
                if (backup.IncludeAuditLogs)
                {
                    backup.CurrentOperation = "Backing up audit logs";
                    backup.ProgressPercentage = 70;
                    await _context.SaveChangesAsync();
                    await NotifyBackupStatusChange(backup);

                    await BackupAuditLogs(archive);
                }

                backup.CurrentOperation = "Finalizing backup";
                backup.ProgressPercentage = 90;
                await _context.SaveChangesAsync();
                await NotifyBackupStatusChange(backup);
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

            await _context.SaveChangesAsync();
            await NotifyBackupStatusChange(backup);

            _logger.LogInformation("Backup completed successfully: {BackupId}, File: {FilePath}, Size: {Size} bytes",
                backupId, filePath, fileInfo.Length);
        }
        catch (Exception ex)
        {
            backup.Status = "Failed";
            backup.ErrorMessage = ex.Message;
            backup.CompletedAt = DateTime.UtcNow;
            backup.ModifiedAt = DateTime.UtcNow;
            backup.ModifiedBy = "System";

            await _context.SaveChangesAsync();
            await NotifyBackupStatusChange(backup);

            _logger.LogError(ex, "Backup failed: {BackupId}", backupId);
        }
    }

    private async Task BackupConfiguration(ZipArchive archive)
    {
        var configurations = await _context.SystemConfigurations.ToListAsync();
        var data = JsonSerializer.Serialize(configurations, new JsonSerializerOptions { WriteIndented = true });
        
        var entry = archive.CreateEntry("configuration.json");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(data);
    }

    private async Task BackupUserData(ZipArchive archive)
    {
        var users = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ToListAsync();
        var tenants = await _context.Tenants.ToListAsync();
        
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

    private async Task BackupAuditLogs(ZipArchive archive)
    {
        var auditLogs = await _context.EntityChangeLogs.ToListAsync();
        var auditTrails = await _context.AuditTrails.ToListAsync();
        
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

    private async Task NotifyBackupStatusChange(BackupOperation backup)
    {
        var dto = BackupMapper.ToStatusDto(backup, await GetUserDisplayNameAsync(backup.StartedByUserId));
        
        await _hubContext.Clients.Group("AuditLogUpdates")
            .SendAsync("BackupStatusChanged", dto);
    }

    private async Task<string> GetUserDisplayNameAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown User";
    }
}