using Prym.DTOs.External;

namespace Prym.Client.Services.External;

/// <summary>
/// Implementation of VAT lookup client service.
/// </summary>
public class VatLookupService(
    IHttpClientService httpClientService,
    ILogger<VatLookupService> logger) : IVatLookupService
{
    public async Task<VatLookupResultDto?> LookupAsync(string vatNumber)
    {
        try
        {
            logger.LogInformation("Looking up VAT number: {VatNumber}", vatNumber);

            var result = await httpClientService.GetAsync<VatLookupResultDto>(
                $"api/v1/vat-lookup/{Uri.EscapeDataString(vatNumber)}");

            if (result is not null)
            {
                logger.LogInformation(
                    "VAT lookup completed: {VatNumber} - Valid: {IsValid}",
                    vatNumber, result.IsValid);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up VAT number {VatNumber}", vatNumber);
            return null;
        }
    }
}
