using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service interface for managing fiscal drawers.
/// </summary>
public interface IFiscalDrawerService
{
    // CRUD
    Task<PagedResult<FiscalDrawerDto>?> GetPagedAsync(int page = 1, int pageSize = 20);
    Task<List<FiscalDrawerDto>> GetAllAsync();
    Task<FiscalDrawerDto?> GetByIdAsync(Guid id);
    Task<FiscalDrawerDto?> GetByPosIdAsync(Guid posId);
    Task<FiscalDrawerDto?> GetByOperatorIdAsync(Guid operatorId);
    Task<FiscalDrawerDto?> CreateAsync(CreateFiscalDrawerDto dto);
    Task<FiscalDrawerDto?> UpdateAsync(Guid id, UpdateFiscalDrawerDto dto);
    Task<bool> DeleteAsync(Guid id);

    // Sessions
    Task<FiscalDrawerSessionDto?> GetCurrentSessionAsync(Guid fiscalDrawerId);
    Task<PagedResult<FiscalDrawerSessionDto>?> GetSessionsAsync(Guid fiscalDrawerId, int page = 1, int pageSize = 20);
    Task<FiscalDrawerSessionDto?> OpenSessionAsync(Guid fiscalDrawerId, OpenFiscalDrawerSessionDto dto);
    Task<FiscalDrawerSessionDto?> CloseSessionAsync(Guid fiscalDrawerId, CloseFiscalDrawerSessionDto dto);

    // Transactions
    Task<PagedResult<FiscalDrawerTransactionDto>?> GetTransactionsAsync(Guid fiscalDrawerId, Guid? sessionId = null, int page = 1, int pageSize = 50);
    Task<FiscalDrawerTransactionDto?> CreateTransactionAsync(Guid fiscalDrawerId, CreateFiscalDrawerTransactionDto dto);

    // Cash Denominations
    Task<List<CashDenominationDto>> GetDenominationsAsync(Guid fiscalDrawerId);
    Task<List<CashDenominationDto>> InitializeDenominationsAsync(Guid fiscalDrawerId, string currencyCode = "EUR");
    Task<CashDenominationDto?> UpdateDenominationAsync(Guid denominationId, UpdateCashDenominationDto dto);

    // Change Calculation
    Task<CalculateChangeResponseDto?> CalculateChangeAsync(Guid fiscalDrawerId, CalculateChangeRequestDto request);

    // Summary / Dashboard
    Task<FiscalDrawerSummaryDto?> GetSummaryAsync(Guid fiscalDrawerId);
    Task<SalesDashboardDto?> GetSalesDashboardAsync();
}
