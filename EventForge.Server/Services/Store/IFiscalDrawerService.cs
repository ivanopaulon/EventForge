using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service interface for managing fiscal drawers (cassetti fiscali).
/// </summary>
public interface IFiscalDrawerService
{
    // CRUD
    Task<PagedResult<FiscalDrawerDto>> GetFiscalDrawersAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetFiscalDrawerByIdAsync(Guid id, CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetFiscalDrawerByPosIdAsync(Guid posId, CancellationToken ct = default);
    Task<FiscalDrawerDto?> GetFiscalDrawerByOperatorIdAsync(Guid operatorId, CancellationToken ct = default);
    Task<FiscalDrawerDto> CreateFiscalDrawerAsync(CreateFiscalDrawerDto dto, string currentUser, CancellationToken ct = default);
    Task<FiscalDrawerDto?> UpdateFiscalDrawerAsync(Guid id, UpdateFiscalDrawerDto dto, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteFiscalDrawerAsync(Guid id, string currentUser, CancellationToken ct = default);

    // Sessions
    Task<FiscalDrawerSessionDto?> GetCurrentSessionAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<PagedResult<FiscalDrawerSessionDto>> GetSessionsAsync(Guid fiscalDrawerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<FiscalDrawerSessionDto> OpenSessionAsync(Guid fiscalDrawerId, OpenFiscalDrawerSessionDto dto, string currentUser, CancellationToken ct = default);
    Task<FiscalDrawerSessionDto> CloseSessionAsync(Guid fiscalDrawerId, CloseFiscalDrawerSessionDto dto, string currentUser, CancellationToken ct = default);

    // Transactions
    Task<PagedResult<FiscalDrawerTransactionDto>> GetTransactionsAsync(Guid fiscalDrawerId, Guid? sessionId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<FiscalDrawerTransactionDto> CreateTransactionAsync(Guid fiscalDrawerId, CreateFiscalDrawerTransactionDto dto, string currentUser, CancellationToken ct = default);
    Task RecordSaleTransactionAsync(Guid fiscalDrawerId, decimal cashAmount, decimal cardAmount, decimal otherAmount, Guid saleSessionId, string operatorName, CancellationToken ct = default);

    // Cash Denominations
    Task<List<CashDenominationDto>> GetCashDenominationsAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<List<CashDenominationDto>> InitializeDenominationsAsync(Guid fiscalDrawerId, string currencyCode, string currentUser, CancellationToken ct = default);
    Task<CashDenominationDto?> UpdateDenominationQuantityAsync(Guid denominationId, UpdateCashDenominationDto dto, string currentUser, CancellationToken ct = default);

    // Change Calculation
    Task<CalculateChangeResponseDto> CalculateChangeAsync(Guid fiscalDrawerId, CalculateChangeRequestDto request, CancellationToken ct = default);

    // Summary / Dashboard
    Task<FiscalDrawerSummaryDto?> GetDrawerSummaryAsync(Guid fiscalDrawerId, CancellationToken ct = default);
    Task<SalesDashboardDto> GetSalesDashboardAsync(CancellationToken ct = default);
}
