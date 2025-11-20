using EventForge.DTOs.Sales;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for note flags.
/// </summary>
public class NoteFlagService : INoteFlagService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<NoteFlagService> _logger;
    private const string BaseUrl = "api/v1/note-flags";

    public NoteFlagService(IHttpClientService httpClientService, ILogger<NoteFlagService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<NoteFlagDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<NoteFlagDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all note flags");
            return null;
        }
    }

    public async Task<List<NoteFlagDto>?> GetActiveAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<NoteFlagDto>>($"{BaseUrl}/active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active note flags");
            return null;
        }
    }

    public async Task<NoteFlagDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<NoteFlagDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving note flag {Id}", id);
            return null;
        }
    }

    public async Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateNoteFlagDto, NoteFlagDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note flag");
            return null;
        }
    }

    public async Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateNoteFlagDto, NoteFlagDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note flag {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note flag {Id}", id);
            return false;
        }
    }
}
