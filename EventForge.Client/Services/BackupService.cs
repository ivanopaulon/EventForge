using System.Net.Http.Json;
using EventForge.DTOs.SuperAdmin;

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
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IHttpClientService httpClientService, ILogger<BackupService> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    public async Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request)
    {
        try
        {
            var result = await _httpClientService.PostAsync<BackupRequestDto, BackupStatusDto>("api/SuperAdmin/backup", request);
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
            return await _httpClientService.GetAsync<BackupStatusDto>($"api/SuperAdmin/backup/{backupId}");
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
            var response = await _httpClientService.GetAsync<IEnumerable<BackupStatusDto>>($"api/SuperAdmin/backup?limit={limit}");
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
            await _httpClientService.PostAsync<object?>($"api/SuperAdmin/backup/{backupId}/cancel", null);
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
        return Task.FromResult($"api/SuperAdmin/backup/{backupId}/download");
    }

    public async Task DeleteBackupAsync(Guid backupId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/SuperAdmin/backup/{backupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            throw;
        }
    }
}