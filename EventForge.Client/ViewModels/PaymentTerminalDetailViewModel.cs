using EventForge.Client.Services.Store;
using EventForge.DTOs.PaymentTerminal;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for the PaymentTerminal detail dialog.
/// </summary>
public class PaymentTerminalDetailViewModel(
    IPaymentTerminalService paymentTerminalService,
    ILogger<PaymentTerminalDetailViewModel> logger)
    : BaseEntityDetailViewModel<PaymentTerminalDto, CreatePaymentTerminalDto, UpdatePaymentTerminalDto>(logger)
{
    protected override PaymentTerminalDto CreateNewEntity() => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        IsEnabled = true,
        ConnectionType = "Tcp",
        Port = 60000,
        TimeoutMs = 30000
    };

    protected override Task<PaymentTerminalDto?> LoadEntityFromServiceAsync(Guid entityId)
        => paymentTerminalService.GetByIdAsync(entityId);

    protected override CreatePaymentTerminalDto MapToCreateDto(PaymentTerminalDto entity) => new()
    {
        Name = entity.Name,
        Description = entity.Description,
        IsEnabled = entity.IsEnabled,
        ConnectionType = entity.ConnectionType,
        IpAddress = entity.IpAddress,
        Port = entity.Port,
        AgentId = entity.AgentId,
        TimeoutMs = entity.TimeoutMs,
        AmountConfirmationRequired = entity.AmountConfirmationRequired,
        TerminalId = entity.TerminalId
    };

    protected override UpdatePaymentTerminalDto MapToUpdateDto(PaymentTerminalDto entity) => new()
    {
        Name = entity.Name,
        Description = entity.Description,
        IsEnabled = entity.IsEnabled,
        ConnectionType = entity.ConnectionType,
        IpAddress = entity.IpAddress,
        Port = entity.Port,
        AgentId = entity.AgentId,
        TimeoutMs = entity.TimeoutMs,
        AmountConfirmationRequired = entity.AmountConfirmationRequired,
        TerminalId = entity.TerminalId
    };

    protected override async Task<PaymentTerminalDto?> CreateEntityAsync(CreatePaymentTerminalDto createDto)
        => await paymentTerminalService.CreateAsync(createDto);

    protected override async Task<PaymentTerminalDto?> UpdateEntityAsync(Guid entityId, UpdatePaymentTerminalDto updateDto)
        => await paymentTerminalService.UpdateAsync(entityId, updateDto);

    protected override Guid GetEntityId(PaymentTerminalDto entity) => entity.Id;

    /// <summary>
    /// Tests the saved terminal's connection. Only valid for persisted terminals (Id != Guid.Empty).
    /// </summary>
    public Task TestConnectionAsync(Guid terminalId, CancellationToken ct = default)
        => paymentTerminalService.TestConnectionAsync(terminalId, ct);
}
