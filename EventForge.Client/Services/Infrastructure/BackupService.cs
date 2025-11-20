using EventForge.DTOs.SuperAdmin;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Infrastructure;

/// <summary>
/// Service for managing backup operations from the client.
/// </summary>
public interface IBackupService
{
    Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request);
    Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId);
    Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50);
    Task CancelBackupAsync(Guid backupId);
    Task<string> GetDownloadUrlAsync(Guid backupId);
    Task DeleteBackupAsync(Guid backupId);
}

public class BackupService : IBackupService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BackupService> _logger;
    private readonly ILoadingDialogService _loadingDialogService;

    public BackupService(IHttpClientService httpClientService, ILogger<BackupService> logger, ILoadingDialogService loadingDialogService)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _loadingDialogService = loadingDialogService;
    }

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request)
    {
        try
        {
            await _loadingDialogService.ShowAsync("Avvio Backup", "Inizializzazione backup...", true);
            await _loadingDialogService.UpdateProgressAsync(20);

            await _loadingDialogService.UpdateOperationAsync("Invio richiesta di backup al server...");
            await _loadingDialogService.UpdateProgressAsync(50);

            var result = await _httpClientService.PostAsync<BackupRequestDto, BackupStatusDto>("api/v1/super-admin/backup", request);

            await _loadingDialogService.UpdateOperationAsync("Backup avviato con successo");
            await _loadingDialogService.UpdateProgressAsync(100);

            await Task.Delay(1000);
            await _loadingDialogService.HideAsync();

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            await _loadingDialogService.HideAsync();
            _logger.LogError(ex, "Error starting backup");
            throw;
        }
    }

    public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId)
    {
        try
        {
            return await _httpClientService.GetAsync<BackupStatusDto>($"api/v1/super-admin/backup/{backupId}");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup status {BackupId}", backupId);
            throw;
        }
    }

    public async Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50)
    {
        try
        {
            var response = await _httpClientService.GetAsync<IEnumerable<BackupStatusDto>>($"api/v1/super-admin/backup?limit={limit}");
            return response ?? Enumerable.Empty<BackupStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backups");
            throw;
        }
    }

    public async Task CancelBackupAsync(Guid backupId)
    {
        try
        {
            await _httpClientService.PostAsync<object?>($"api/v1/super-admin/backup/{backupId}/cancel", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backup {BackupId}", backupId);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(Guid backupId)
    {
        // This method constructs a URL and doesn't make HTTP call, so it doesn't need to be changed
        // However, I should get the base address from the HttpClientService somehow
        // For now, keeping the existing logic but this could be improved
        return Task.FromResult($"api/v1/super-admin/backup/{backupId}/download");
    }

    public async Task DeleteBackupAsync(Guid backupId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/super-admin/backup/{backupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            throw;
        }
    }
}