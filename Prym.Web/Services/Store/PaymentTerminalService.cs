using Prym.DTOs.PaymentTerminal;
using System.Net.Http.Json;

namespace Prym.Web.Services.Store;

public class PaymentTerminalService(
    HttpClient httpClient,
    ILogger<PaymentTerminalService> logger) : IPaymentTerminalService
{
    private const string ApiBase = "api/v1/payment-terminals";

    public async Task<List<PaymentTerminalDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<PaymentTerminalDto>>(ApiBase, ct) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nel recupero dei terminali di pagamento.");
            return [];
        }
    }

    public async Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PaymentTerminalDto>($"{ApiBase}/{id}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nel recupero del terminale di pagamento {Id}.", id);
            throw;
        }
    }

    public async Task<PaymentTerminalDto?> CreateAsync(CreatePaymentTerminalDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiBase, dto, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentTerminalDto>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nella creazione del terminale di pagamento.");
            throw new InvalidOperationException("Errore nella creazione del terminale POS.", ex);
        }
    }

    public async Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{ApiBase}/{id}", dto, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentTerminalDto>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'aggiornamento del terminale di pagamento {Id}.", id);
            throw new InvalidOperationException("Errore nell'aggiornamento del terminale POS.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"{ApiBase}/{id}", ct);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'eliminazione del terminale di pagamento {Id}.", id);
            throw new InvalidOperationException("Errore nell'eliminazione del terminale POS.", ex);
        }
    }

    public async Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{terminalId}/pay", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>(ct)
                ?? new PaymentResultDto { Success = false, ErrorMessage = "Risposta vuota dal server." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'invio del pagamento al terminale {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendVoidAsync(Guid terminalId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"{ApiBase}/{terminalId}/void", null, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>(ct)
                ?? new PaymentResultDto { Success = false, ErrorMessage = "Risposta vuota dal server." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'invio dello storno al terminale {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ApiBase}/{terminalId}/refund", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResultDto>(ct)
                ?? new PaymentResultDto { Success = false, ErrorMessage = "Risposta vuota dal server." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'invio del rimborso al terminale {Id}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task TestConnectionAsync(Guid terminalId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"{ApiBase}/{terminalId}/test-connection", null, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nel test di connessione al terminale {Id}.", terminalId);
            throw;
        }
    }
}
