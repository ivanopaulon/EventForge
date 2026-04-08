using EventForge.DTOs.PaymentTerminal;
using System.Net.Http.Json;

namespace EventForge.Client.Services.Store;

public class PaymentTerminalService(
    HttpClient httpClient,
    ILogger<PaymentTerminalService> logger) : IPaymentTerminalService
{
    private const string ApiBase = "api/v1/payment-terminals";

    public async Task<List<PaymentTerminalDto>> GetAllAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<PaymentTerminalDto>>(ApiBase) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment terminals.");
            return [];
        }
    }

    public async Task<PaymentTerminalDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PaymentTerminalDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment terminal {Id}.", id);
            throw;
        }
    }

    public async Task<PaymentTerminalDto?> CreateAsync(CreatePaymentTerminalDto dto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentTerminalDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment terminal.");
            throw new InvalidOperationException("Errore nella creazione del terminale POS.", ex);
        }
    }

    public async Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentTerminalDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment terminal {Id}.", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del terminale POS.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting payment terminal {Id}.", id);
            throw new InvalidOperationException("Errore nell'eliminazione del terminale POS.", ex);
        }
    }

    public async Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{terminalId}/pay", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>()
                ?? new PaymentResultDto { Success = false, ErrorMessage = "Risposta vuota dal server." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending payment to terminal {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendVoidAsync(Guid terminalId)
    {
        try
        {
            var response = await httpClient.PostAsync($"{ApiBase}/{terminalId}/void", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>()
                ?? new PaymentResultDto { Success = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending void to terminal {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{terminalId}/refund", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>()
                ?? new PaymentResultDto { Success = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending refund to terminal {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task TestConnectionAsync(Guid terminalId)
    {
        var response = await httpClient.PostAsync($"{ApiBase}/{terminalId}/test-connection", null);
        response.EnsureSuccessStatusCode();
    }
}
