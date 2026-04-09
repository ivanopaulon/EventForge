using EventForge.DTOs.SuperAdmin;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing backup operations from the client.
/// </summary>
public interface IBackupService
{
    Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request, CancellationToken ct = default);
    Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId, CancellationToken ct = default);
    Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50, CancellationToken ct = default);
    Task CancelBackupAsync(Guid backupId, CancellationToken ct = default);
    Task<string> GetDownloadUrlAsync(Guid backupId, CancellationToken ct = default);
    Task DeleteBackupAsync(Guid backupId, CancellationToken ct = default);
}

public class BackupService(
    IHttpClientService httpClientService,
    ILogger<BackupService> logger,
    ILoadingDialogService loadingDialogService) : IBackupService
{
    private const string BaseUrl = "api/v1/backup";

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request, CancellationToken ct = default)
    {
        try
        {
            await loadingDialogService.ShowAsync("Avvio Backup", "Inizializzazione backup...", true);
            await loadingDialogService.UpdateProgressAsync(20);

            await loadingDialogService.UpdateOperationAsync("Invio richiesta di backup al server...");
            await loadingDialogService.UpdateProgressAsync(50);

            var result = await httpClientService.PostAsync<BackupRequestDto, BackupStatusDto>("api/v1/super-admin/backup", request, ct);

            await loadingDialogService.UpdateOperationAsync("Backup avviato con successo");
            await loadingDialogService.UpdateProgressAsync(100);

            await Task.Delay(1000);
            await loadingDialogService.HideAsync();

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            await loadingDialogService.HideAsync();
            logger.LogError(ex, "Error starting backup");
            throw;
        }
    }

    public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<BackupStatusDto>($"api/v1/super-admin/backup/{backupId}", ct);

        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving backup status {BackupId}", backupId);
            throw;
        }
    }

    public async Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<BackupStatusDto>>($"api/v1/super-admin/backup?limit={limit}", ct);

            return response ?? Enumerable.Empty<BackupStatusDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving backups");
            throw;
        }
    }

    public async Task CancelBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync<object?>($"api/v1/super-admin/backup/{backupId}/cancel", null, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling backup {BackupId}", backupId);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(Guid backupId, CancellationToken ct = default)
    {
        // This method constructs a URL and doesn't make HTTP call, so it doesn't need to be changed
        // However, I should get the base address from the HttpClientService somehow
        // For now, keeping the existing logic but this could be improved
        return Task.FromResult($"api/v1/super-admin/backup/{backupId}/download");
    }

    public async Task DeleteBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"api/v1/super-admin/backup/{backupId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            throw;
        }
    }
}