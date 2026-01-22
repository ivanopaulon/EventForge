using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing price lists.
/// </summary>
public class PriceListService : IPriceListService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PriceListService> _logger;
    private const string BaseUrl = "api/v1/product-management/price-lists";

    public PriceListService(IHttpClientService httpClientService, ILogger<PriceListService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PriceListDto>> GetPagedAsync(int page = 1, int pageSize = 1000, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<PriceListDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
            return result ?? new PagedResult<PriceListDto> { Items = new List<PriceListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price lists");
            throw;
        }
    }

    public async Task<PriceListDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<PriceListDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list with ID {Id}", id);
            throw;
        }
    }

    public async Task<PriceListDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<PriceListDetailDto>($"{BaseUrl}/{id}/detail", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price list detail with ID {Id}", id);
            throw;
        }
    }

    public async Task<PriceListDto> CreateAsync(CreatePriceListDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreatePriceListDto, PriceListDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create price list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price list");
            throw;
        }
    }

    public async Task<PriceListDto?> UpdateAsync(Guid id, UpdatePriceListDto dto, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdatePriceListDto, PriceListDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price list with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting price list with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.GetAsync<IEnumerable<PriceListDto>>($"{BaseUrl}/event/{eventId}", ct);
            return result ?? new List<PriceListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price lists for event {EventId}", eventId);
            throw;
        }
    }

    public async Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<GeneratePriceListFromPurchasesDto, GeneratePriceListPreviewDto>($"{BaseUrl}/generate-from-purchases/preview", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to preview price list generation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing price list generation from purchases");
            throw;
        }
    }

    public async Task<Guid> GenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<GeneratePriceListFromPurchasesDto, Guid>($"{BaseUrl}/generate-from-purchases", dto, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating price list from purchases");
            throw;
        }
    }

    public async Task<PriceListEntryDto> AddEntryAsync(CreatePriceListEntryDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreatePriceListEntryDto, PriceListEntryDto>($"{BaseUrl}/{dto.PriceListId}/entries", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to add entry to price list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entry to price list");
            throw;
        }
    }

    public async Task<PriceListEntryDto> UpdateEntryAsync(Guid id, UpdatePriceListEntryDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdatePriceListEntryDto, PriceListEntryDto>($"{BaseUrl}/entries/{id}", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to update price list entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price list entry with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteEntryAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/entries/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting price list entry with ID {Id}", id);
            throw;
        }
    }

    public async Task<int> AddEntriesBulkAsync(List<CreatePriceListEntryDto> entries, CancellationToken ct = default)
    {
        try
        {
            if (entries == null || !entries.Any())
                return 0;

            // Safely get the first entry for priceListId
            var firstEntry = entries.FirstOrDefault();
            if (firstEntry == null)
                return 0;

            var priceListId = firstEntry.PriceListId;
            var result = await _httpClientService.PostAsync<List<CreatePriceListEntryDto>, int>($"{BaseUrl}/{priceListId}/entries/bulk", entries, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk adding entries to price list");
            throw;
        }
    }

    public async Task AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.PostAsync($"{BaseUrl}/{priceListId}/business-parties", dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning business party to price list {PriceListId}", priceListId);
            throw;
        }
    }

    public async Task UnassignBusinessPartyAsync(Guid priceListId, Guid businessPartyId, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{priceListId}/business-parties/{businessPartyId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning business party {BusinessPartyId} from price list {PriceListId}", businessPartyId, priceListId);
            throw;
        }
    }

    public async Task<IEnumerable<PriceListBusinessPartyDto>> GetAssignedBusinessPartiesAsync(Guid priceListId, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.GetAsync<IEnumerable<PriceListBusinessPartyDto>>($"{BaseUrl}/{priceListId}/business-parties", ct);
            return result ?? new List<PriceListBusinessPartyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned business parties for price list {PriceListId}", priceListId);
            throw;
        }
    }

    public async Task<PriceListBusinessPartyDto> UpdateBusinessPartyAssignmentAsync(Guid priceListId, Guid businessPartyId, UpdateBusinessPartyAssignmentDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateBusinessPartyAssignmentDto, PriceListBusinessPartyDto>($"{BaseUrl}/{priceListId}/business-parties/{businessPartyId}", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to update business party assignment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business party assignment for price list {PriceListId}, business party {BusinessPartyId}", priceListId, businessPartyId);
            throw;
        }
    }
}
