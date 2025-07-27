using System.Net.Http.Json;

namespace EventForge.Client.Services;

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
    private readonly HttpClient _httpClient;
    private readonly ILogger<BackupService> _logger;

    public BackupService(HttpClient httpClient, ILogger<BackupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/SuperAdmin/backup", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BackupStatusDto>();
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting backup");
            throw;
        }
    }

    public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BackupStatusDto>($"api/SuperAdmin/backup/{backupId}");
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
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<BackupStatusDto>>($"api/SuperAdmin/backup?limit={limit}");
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
            var response = await _httpClient.PostAsync($"api/SuperAdmin/backup/{backupId}/cancel", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backup {BackupId}", backupId);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(Guid backupId)
    {
        return Task.FromResult($"{_httpClient.BaseAddress}api/SuperAdmin/backup/{backupId}/download");
    }

    public async Task DeleteBackupAsync(Guid backupId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/SuperAdmin/backup/{backupId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            throw;
        }
    }
}