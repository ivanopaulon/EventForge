using EventForge.DTOs.PaymentTerminal;
using EventForge.Hardware.Interfaces;
using EventForge.Server.Services.PaymentTerminal.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventForge.Server.Services.PaymentTerminal;

public class PaymentTerminalService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    ILogger<PaymentTerminalService> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IPaymentTerminalService
{
    public async Task<List<PaymentTerminalDto>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var tenantId = RequireTenantId();
            var items = await context.PaymentTerminals
                .AsNoTracking()
                .WhereActiveTenant(tenantId)
                .OrderBy(t => t.Name)
                .ToListAsync(ct);
            return items.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment terminals.");
            throw;
        }
    }

    public async Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var tenantId = RequireTenantId();
            var entity = await context.PaymentTerminals
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId, ct);
            return entity is null ? null : MapToDto(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment terminal {Id}.", id);
            throw;
        }
    }

    public async Task<PaymentTerminalDto> CreateAsync(CreatePaymentTerminalDto dto, string currentUser, CancellationToken ct = default)
    {
        try
        {
            var tenantId = RequireTenantId();
            var entity = new EventForge.Server.Data.Entities.Store.PaymentTerminal
            {
                TenantId = tenantId,
                Name = dto.Name,
                Description = dto.Description,
                IsEnabled = dto.IsEnabled,
                ConnectionType = dto.ConnectionType,
                IpAddress = dto.IpAddress,
                Port = dto.Port,
                AgentId = dto.AgentId,
                TimeoutMs = dto.TimeoutMs,
                AmountConfirmationRequired = dto.AmountConfirmationRequired,
                TerminalId = dto.TerminalId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };
            context.PaymentTerminals.Add(entity);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Payment terminal {Name} created by {User}.", entity.Name, currentUser);
            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment terminal.");
            throw;
        }
    }

    public async Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto, string currentUser, CancellationToken ct = default)
    {
        try
        {
            var tenantId = RequireTenantId();
            var entity = await context.PaymentTerminals
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId, ct);
            if (entity is null) return null;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.IsEnabled = dto.IsEnabled;
            entity.ConnectionType = dto.ConnectionType;
            entity.IpAddress = dto.IpAddress;
            entity.Port = dto.Port;
            entity.AgentId = dto.AgentId;
            entity.TimeoutMs = dto.TimeoutMs;
            entity.AmountConfirmationRequired = dto.AmountConfirmationRequired;
            entity.TerminalId = dto.TerminalId;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Payment terminal {Id} updated by {User}.", id, currentUser);
            return MapToDto(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment terminal {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        try
        {
            var tenantId = RequireTenantId();
            var entity = await context.PaymentTerminals
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId, ct);
            if (entity is null) return false;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = currentUser;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Payment terminal {Id} deleted by {User}.", id, currentUser);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting payment terminal {Id}.", id);
            throw;
        }
    }

    public async Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            await using var channel = await CreateChannelAsync(terminalId, ct);
            var result = await channel.SendPaymentAsync(request.Amount, ct);
            return MapResult(result, request.Amount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending payment to terminal {TerminalId}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendVoidAsync(Guid terminalId, CancellationToken ct = default)
    {
        try
        {
            await using var channel = await CreateChannelAsync(terminalId, ct);
            var result = await channel.SendVoidAsync(ct);
            return MapResult(result, 0m);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending void to terminal {TerminalId}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            await using var channel = await CreateChannelAsync(terminalId, ct);
            var result = await channel.SendRefundAsync(request.Amount, ct);
            return MapResult(result, request.Amount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending refund to terminal {TerminalId}.", terminalId);
            return new PaymentResultDto { Success = false, Approved = false, ErrorMessage = ex.Message };
        }
    }

    public async Task TestConnectionAsync(Guid terminalId, CancellationToken ct = default)
    {
        await using var channel = await CreateChannelAsync(terminalId, ct);
        await channel.TestConnectionAsync(ct);
    }

    public async Task TestTcpConnectionAsync(string host, int port, int timeoutMs, CancellationToken ct = default)
    {
        await using var channel = new Protocol17TcpChannel(host, port, timeoutMs);
        await channel.TestConnectionAsync(ct);
    }

    public async Task TestTcpViaAgentAsync(string agentBaseUrl, string host, int port, int timeoutMs, CancellationToken ct = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        await using var channel = new Protocol17AgentChannel(httpClient, agentBaseUrl, host, port, timeoutMs);
        await channel.TestConnectionAsync(ct);
    }

    private async Task<IPaymentTerminalChannel> CreateChannelAsync(Guid terminalId, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var terminal = await context.PaymentTerminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId && !t.IsDeleted && t.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Payment terminal {terminalId} not found.");

        if (string.IsNullOrEmpty(terminal.IpAddress))
            throw new InvalidOperationException("Il terminale non ha un indirizzo IP configurato.");

        if (terminal.ConnectionType == "TcpViaAgent")
        {
            if (!terminal.AgentId.HasValue)
                throw new InvalidOperationException("Il terminale è configurato per proxy agente ma AgentId non è impostato.");
            var agentBaseUrl = configuration[$"AgentProxies:{terminal.AgentId.Value}"]
                ?? throw new InvalidOperationException($"URL agente '{terminal.AgentId.Value}' non configurato. Aggiungere 'AgentProxies:{terminal.AgentId.Value}' alla configurazione.");
            var httpClient = httpClientFactory.CreateClient();
            return new Protocol17AgentChannel(httpClient, agentBaseUrl, terminal.IpAddress, terminal.Port, terminal.TimeoutMs);
        }

        return new Protocol17TcpChannel(terminal.IpAddress, terminal.Port, terminal.TimeoutMs);
    }

    private static PaymentResultDto MapResult(Protocol17Response r, decimal requestedAmount) => new()
    {
        Success = r.Approved,
        Approved = r.Approved,
        ResponseCode = r.ResponseCode,
        AuthorizationCode = r.AuthorizationCode,
        Amount = r.Amount > 0 ? r.Amount : requestedAmount,
        ErrorMessage = r.ErrorMessage,
        TransactionAt = DateTime.UtcNow
    };

    private static PaymentTerminalDto MapToDto(EventForge.Server.Data.Entities.Store.PaymentTerminal t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        IsEnabled = t.IsEnabled,
        ConnectionType = t.ConnectionType,
        IpAddress = t.IpAddress,
        Port = t.Port,
        AgentId = t.AgentId,
        TimeoutMs = t.TimeoutMs,
        AmountConfirmationRequired = t.AmountConfirmationRequired,
        TerminalId = t.TerminalId,
        CreatedAt = t.CreatedAt,
        CreatedBy = t.CreatedBy,
        ModifiedAt = t.ModifiedAt,
        ModifiedBy = t.ModifiedBy
    };

    private Guid RequireTenantId()
    {
        if (!tenantContext.CurrentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");
        return tenantContext.CurrentTenantId.Value;
    }
}
