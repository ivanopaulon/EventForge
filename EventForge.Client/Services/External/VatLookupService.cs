using EventForge.DTOs.External;

namespace EventForge.Client.Services.External;

/// <summary>
/// Implementation of VAT lookup client service.
/// </summary>
public class VatLookupService : IVatLookupService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<VatLookupService> _logger;

    public VatLookupService(
        IHttpClientService httpClientService,
        ILogger<VatLookupService> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    public async Task<VatLookupResultDto?> LookupAsync(string vatNumber)
    {
        try
        {
            _logger.LogInformation("Looking up VAT number: {VatNumber}", vatNumber);

            var result = await _httpClientService.GetAsync<VatLookupResultDto>(
                $"api/v1/vat-lookup/{Uri.EscapeDataString(vatNumber)}");

            if (result != null)
            {
                _logger.LogInformation(
                    "VAT lookup completed: {VatNumber} - Valid: {IsValid}",
                    vatNumber, result.IsValid);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up VAT number {VatNumber}", vatNumber);
            return null;
        }
    }
}
