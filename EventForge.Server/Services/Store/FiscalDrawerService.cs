using EventForge.DTOs.Common;
using EventForge.DTOs.Store;
using EventForge.Server.Data.Entities.Store;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service implementation for managing fiscal drawers.
/// </summary>
public class FiscalDrawerService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<FiscalDrawerService> logger) : IFiscalDrawerService
{
    #region CRUD

    public async Task<PagedResult<FiscalDrawerDto>> GetFiscalDrawersAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        // WhereActiveTenant already filters by !IsDeleted, TenantId, and IsActive
        var query = context.FiscalDrawers
            .WhereActiveTenant(tenantId)
            .Include(d => d.Pos)
            .Include(d => d.Operator)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(term) ||
                (d.Code != null && d.Code.ToLower().Contains(term)) ||
                (d.Description != null && d.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Batch-load open sessions for all returned drawers to avoid N+1
        var drawerIds = items.Select(d => d.Id).ToList();
        var openSessions = await context.FiscalDrawerSessions
            .Where(s => drawerIds.Contains(s.FiscalDrawerId) &&
                        s.Status == FiscalDrawerSessionStatus.Open &&
                        !s.IsDeleted &&
                        s.TenantId == tenantId)
            .ToDictionaryAsync(s => s.FiscalDrawerId, ct);

        return new PagedResult<FiscalDrawerDto>
        {
            Items = items.Select(d => MapToDto(d, openSessions.GetValueOrDefault(d.Id))),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FiscalDrawerDto?> GetFiscalDrawerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.FiscalDrawers
            .Include(d => d.Pos)
            .Include(d => d.Operator)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId, ct);
        if (entity is null) return null;
        var openSession = await GetOpenSessionForDrawerAsync(id, tenantId, ct);
        return MapToDto(entity, openSession);
    }

    public async Task<FiscalDrawerDto?> GetFiscalDrawerByPosIdAsync(Guid posId, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.FiscalDrawers
            .Include(d => d.Pos)
            .Include(d => d.Operator)
            .FirstOrDefaultAsync(d => d.PosId == posId && !d.IsDeleted && d.TenantId == tenantId, ct);
        if (entity is null) return null;
        var openSession = await GetOpenSessionForDrawerAsync(entity.Id, tenantId, ct);
        return MapToDto(entity, openSession);
    }

    public async Task<FiscalDrawerDto?> GetFiscalDrawerByOperatorIdAsync(Guid operatorId, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.FiscalDrawers
            .Include(d => d.Pos)
            .Include(d => d.Operator)
            .FirstOrDefaultAsync(d => d.OperatorId == operatorId && !d.IsDeleted && d.TenantId == tenantId, ct);
        if (entity is null) return null;
        var openSession = await GetOpenSessionForDrawerAsync(entity.Id, tenantId, ct);
        return MapToDto(entity, openSession);
    }

    public async Task<FiscalDrawerDto> CreateFiscalDrawerAsync(CreateFiscalDrawerDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var entity = new FiscalDrawer
        {
            TenantId = tenantId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            AssignmentType = dto.AssignmentType,
            CurrencyCode = dto.CurrencyCode,
            Status = dto.Status,
            OpeningBalance = dto.OpeningBalance,
            CurrentBalance = dto.OpeningBalance,
            PosId = dto.PosId,
            OperatorId = dto.OperatorId,
            Notes = dto.Notes,
            CreatedBy = currentUser
        };

        context.FiscalDrawers.Add(entity);
        await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, ct);
        logger.LogInformation("FiscalDrawer {Id} created by {User}", entity.Id, currentUser);

        return MapToDto(entity, null);
    }

    public async Task<FiscalDrawerDto?> UpdateFiscalDrawerAsync(Guid id, UpdateFiscalDrawerDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.FiscalDrawers
            .Include(d => d.Pos)
            .Include(d => d.Operator)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId, ct);

        if (entity is null) return null;

        // Prevent currency change if cash denominations already exist for this drawer
        if (entity.CurrencyCode != dto.CurrencyCode)
        {
            var hasDenominations = await context.CashDenominations
                .AnyAsync(d => d.FiscalDrawerId == id && !d.IsDeleted && d.TenantId == tenantId, ct);
            if (hasDenominations)
                throw new InvalidOperationException(
                    "Cannot change currency code when cash denominations are already configured. " +
                    "Reset denominations first via the 'Initialize' button.");
        }

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.Description = dto.Description;
        entity.AssignmentType = dto.AssignmentType;
        entity.CurrencyCode = dto.CurrencyCode;
        entity.Status = dto.Status;
        entity.PosId = dto.PosId;
        entity.OperatorId = dto.OperatorId;
        entity.Notes = dto.Notes;
        entity.ModifiedBy = currentUser;
        entity.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, null, ct);

        var openSession = await GetOpenSessionForDrawerAsync(id, tenantId, ct);
        return MapToDto(entity, openSession);
    }

    public async Task<bool> DeleteFiscalDrawerAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.FiscalDrawers
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.TenantId == tenantId, ct);

        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = currentUser;
        entity.ModifiedBy = currentUser;
        entity.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, null, ct);
        return true;
    }

    #endregion

    #region Sessions

    public async Task<FiscalDrawerSessionDto?> GetCurrentSessionAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var session = await context.FiscalDrawerSessions
            .Include(s => s.OpenedByOperator)
            .Include(s => s.ClosedByOperator)
            .Include(s => s.FiscalDrawer)
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

        return session is null ? null : MapSessionToDto(session);
    }

    public async Task<PagedResult<FiscalDrawerSessionDto>> GetSessionsAsync(Guid fiscalDrawerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var query = context.FiscalDrawerSessions
            .Include(s => s.OpenedByOperator)
            .Include(s => s.ClosedByOperator)
            .Include(s => s.FiscalDrawer)
            .Where(s => s.FiscalDrawerId == fiscalDrawerId && !s.IsDeleted && s.TenantId == tenantId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.SessionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<FiscalDrawerSessionDto>
        {
            Items = items.Select(MapSessionToDto),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FiscalDrawerSessionDto> OpenSessionAsync(Guid fiscalDrawerId, OpenFiscalDrawerSessionDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var existingSession = await context.FiscalDrawerSessions
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

        if (existingSession is not null)
            throw new InvalidOperationException("A session is already open for this fiscal drawer.");

        var drawer = await context.FiscalDrawers
            .FirstOrDefaultAsync(d => d.Id == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Fiscal drawer {fiscalDrawerId} not found.");

        var now = DateTime.UtcNow;
        var session = new FiscalDrawerSession
        {
            TenantId = tenantId,
            FiscalDrawerId = fiscalDrawerId,
            SessionDate = now.Date,
            OpenedAt = now,
            OpeningBalance = dto.OpeningBalance,
            ClosingBalance = dto.OpeningBalance,
            OpenedByOperatorId = dto.OperatorId,
            Status = FiscalDrawerSessionStatus.Open,
            Notes = dto.Notes,
            CreatedBy = currentUser
        };

        context.FiscalDrawerSessions.Add(session);

        context.FiscalDrawerTransactions.Add(new FiscalDrawerTransaction
        {
            TenantId = tenantId,
            FiscalDrawerId = fiscalDrawerId,
            FiscalDrawerSessionId = session.Id,
            TransactionType = FiscalDrawerTransactionType.OpeningBalance,
            PaymentType = FiscalDrawerPaymentType.Cash,
            Amount = dto.OpeningBalance,
            Description = "Fondo cassa apertura",
            TransactionAt = now,
            OperatorName = currentUser,
            CreatedBy = currentUser
        });

        drawer.CurrentBalance = dto.OpeningBalance;
        drawer.ModifiedBy = currentUser;
        drawer.ModifiedAt = now;

        await context.SaveChangesAsync(ct);

        session = await context.FiscalDrawerSessions
            .Include(s => s.OpenedByOperator)
            .Include(s => s.FiscalDrawer)
            .FirstAsync(s => s.Id == session.Id, ct);

        logger.LogInformation("FiscalDrawer session opened for {DrawerId} by {User}", fiscalDrawerId, currentUser);
        return MapSessionToDto(session);
    }

    public async Task<FiscalDrawerSessionDto> CloseSessionAsync(Guid fiscalDrawerId, CloseFiscalDrawerSessionDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var session = await context.FiscalDrawerSessions
            .Include(s => s.OpenedByOperator)
            .Include(s => s.FiscalDrawer)
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("No open session found for this fiscal drawer.");

        var now = DateTime.UtcNow;

        context.FiscalDrawerTransactions.Add(new FiscalDrawerTransaction
        {
            TenantId = tenantId,
            FiscalDrawerId = fiscalDrawerId,
            FiscalDrawerSessionId = session.Id,
            TransactionType = FiscalDrawerTransactionType.ClosingBalance,
            PaymentType = FiscalDrawerPaymentType.Cash,
            Amount = dto.ClosingBalance,
            Description = "Fondo cassa chiusura",
            TransactionAt = now,
            OperatorName = currentUser,
            CreatedBy = currentUser
        });

        session.ClosedAt = now;
        session.ClosingBalance = dto.ClosingBalance;
        session.ClosedByOperatorId = dto.OperatorId;
        session.Status = FiscalDrawerSessionStatus.Closed;
        if (!string.IsNullOrEmpty(dto.Notes)) session.Notes = dto.Notes;
        session.ModifiedBy = currentUser;
        session.ModifiedAt = now;

        session.FiscalDrawer.CurrentBalance = dto.ClosingBalance;
        session.FiscalDrawer.ModifiedBy = currentUser;
        session.FiscalDrawer.ModifiedAt = now;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("FiscalDrawer session closed for {DrawerId} by {User}", fiscalDrawerId, currentUser);

        return MapSessionToDto(session);
    }

    #endregion

    #region Transactions

    public async Task<PagedResult<FiscalDrawerTransactionDto>> GetTransactionsAsync(Guid fiscalDrawerId, Guid? sessionId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var query = context.FiscalDrawerTransactions
            .Where(t => t.FiscalDrawerId == fiscalDrawerId && !t.IsDeleted && t.TenantId == tenantId);

        if (sessionId.HasValue)
            query = query.Where(t => t.FiscalDrawerSessionId == sessionId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.TransactionAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<FiscalDrawerTransactionDto>
        {
            Items = items.Select(MapTransactionToDto),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FiscalDrawerTransactionDto> CreateTransactionAsync(Guid fiscalDrawerId, CreateFiscalDrawerTransactionDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var drawer = await context.FiscalDrawers
            .FirstOrDefaultAsync(d => d.Id == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Fiscal drawer {fiscalDrawerId} not found.");

        var session = await context.FiscalDrawerSessions
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

        var now = DateTime.UtcNow;
        // Deposits are positive, withdrawals are negative for balance tracking
        var signedAmount = dto.TransactionType == FiscalDrawerTransactionType.Withdrawal
            ? -Math.Abs(dto.Amount)
            : Math.Abs(dto.Amount);

        var tx = new FiscalDrawerTransaction
        {
            TenantId = tenantId,
            FiscalDrawerId = fiscalDrawerId,
            FiscalDrawerSessionId = session?.Id,
            TransactionType = dto.TransactionType,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            Description = dto.Description,
            TransactionAt = now,
            OperatorName = currentUser,
            CreatedBy = currentUser
        };

        context.FiscalDrawerTransactions.Add(tx);

        if (session is not null)
        {
            if (dto.TransactionType == FiscalDrawerTransactionType.Deposit)
            {
                session.TotalDeposits += dto.Amount;
                session.TotalCashIn += dto.Amount;
            }
            else if (dto.TransactionType == FiscalDrawerTransactionType.Withdrawal)
            {
                session.TotalWithdrawals += dto.Amount;
                session.TotalCashOut += dto.Amount;
            }
            session.TransactionCount++;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = now;
        }

        drawer.CurrentBalance += signedAmount;
        drawer.ModifiedBy = currentUser;
        drawer.ModifiedAt = now;

        await context.SaveChangesAsync(ct);
        return MapTransactionToDto(tx);
    }

    public async Task RecordSaleTransactionAsync(Guid fiscalDrawerId, decimal cashAmount, decimal cardAmount, decimal otherAmount, Guid saleSessionId, string operatorName, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var now = DateTime.UtcNow;

        var drawer = await context.FiscalDrawers
            .FirstOrDefaultAsync(d => d.Id == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId, ct);
        if (drawer is null) return;

        var session = await context.FiscalDrawerSessions
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

        var totalSale = cashAmount + cardAmount + otherAmount;

        void AddTx(decimal amount, FiscalDrawerPaymentType payType)
        {
            if (amount <= 0) return;
            context.FiscalDrawerTransactions.Add(new FiscalDrawerTransaction
            {
                TenantId = tenantId,
                FiscalDrawerId = fiscalDrawerId,
                FiscalDrawerSessionId = session?.Id,
                TransactionType = FiscalDrawerTransactionType.Sale,
                PaymentType = payType,
                Amount = amount,
                SaleSessionId = saleSessionId,
                TransactionAt = now,
                OperatorName = operatorName,
                CreatedBy = operatorName
            });
        }

        AddTx(cashAmount, FiscalDrawerPaymentType.Cash);
        AddTx(cardAmount, FiscalDrawerPaymentType.Card);
        AddTx(otherAmount, FiscalDrawerPaymentType.Other);

        if (session is not null)
        {
            session.TotalSales += totalSale;
            session.TotalCashIn += totalSale;
            session.TransactionCount++;
            session.ClosingBalance = session.OpeningBalance + session.TotalCashIn - session.TotalCashOut;
            session.ModifiedBy = operatorName;
            session.ModifiedAt = now;
        }

        drawer.CurrentBalance += cashAmount; // Only cash affects physical drawer balance
        drawer.ModifiedBy = operatorName;
        drawer.ModifiedAt = now;

        await context.SaveChangesAsync(ct);
    }

    #endregion

    #region Cash Denominations

    private static readonly Dictionary<string, List<(decimal Value, DenominationType Type)>> CurrencyDenominations = new()
    {
        ["EUR"] = new()
        {
            (0.01m, DenominationType.Coin), (0.02m, DenominationType.Coin), (0.05m, DenominationType.Coin),
            (0.10m, DenominationType.Coin), (0.20m, DenominationType.Coin), (0.50m, DenominationType.Coin),
            (1.00m, DenominationType.Coin), (2.00m, DenominationType.Coin),
            (5m, DenominationType.Banknote), (10m, DenominationType.Banknote), (20m, DenominationType.Banknote),
            (50m, DenominationType.Banknote), (100m, DenominationType.Banknote), (200m, DenominationType.Banknote),
            (500m, DenominationType.Banknote)
        },
        ["USD"] = new()
        {
            (0.01m, DenominationType.Coin), (0.05m, DenominationType.Coin), (0.10m, DenominationType.Coin),
            (0.25m, DenominationType.Coin), (0.50m, DenominationType.Coin), (1.00m, DenominationType.Coin),
            (1m, DenominationType.Banknote), (2m, DenominationType.Banknote), (5m, DenominationType.Banknote),
            (10m, DenominationType.Banknote), (20m, DenominationType.Banknote), (50m, DenominationType.Banknote),
            (100m, DenominationType.Banknote)
        },
        ["GBP"] = new()
        {
            (0.01m, DenominationType.Coin), (0.02m, DenominationType.Coin), (0.05m, DenominationType.Coin),
            (0.10m, DenominationType.Coin), (0.20m, DenominationType.Coin), (0.50m, DenominationType.Coin),
            (1.00m, DenominationType.Coin), (2.00m, DenominationType.Coin),
            (5m, DenominationType.Banknote), (10m, DenominationType.Banknote), (20m, DenominationType.Banknote),
            (50m, DenominationType.Banknote)
        },
        ["JPY"] = new()
        {
            (1m, DenominationType.Coin), (5m, DenominationType.Coin), (10m, DenominationType.Coin),
            (50m, DenominationType.Coin), (100m, DenominationType.Coin), (500m, DenominationType.Coin),
            (1000m, DenominationType.Banknote), (2000m, DenominationType.Banknote),
            (5000m, DenominationType.Banknote), (10000m, DenominationType.Banknote)
        },
        ["CNY"] = new()
        {
            (0.1m, DenominationType.Coin), (0.5m, DenominationType.Coin), (1.0m, DenominationType.Coin),
            (1m, DenominationType.Banknote), (5m, DenominationType.Banknote), (10m, DenominationType.Banknote),
            (20m, DenominationType.Banknote), (50m, DenominationType.Banknote), (100m, DenominationType.Banknote)
        }
    };

    public async Task<List<CashDenominationDto>> GetCashDenominationsAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var items = await context.CashDenominations
            .Where(d => d.FiscalDrawerId == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Value)
            .ToListAsync(ct);

        return items.Select(MapDenominationToDto).ToList();
    }

    public async Task<List<CashDenominationDto>> InitializeDenominationsAsync(Guid fiscalDrawerId, string currencyCode, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();

        var existing = await context.CashDenominations
            .Where(d => d.FiscalDrawerId == fiscalDrawerId && d.TenantId == tenantId && !d.IsDeleted)
            .ToListAsync(ct);
        foreach (var d in existing)
        {
            d.IsDeleted = true;
            d.DeletedAt = DateTime.UtcNow;
            d.DeletedBy = currentUser;
        }

        if (!CurrencyDenominations.TryGetValue(currencyCode, out var denomList))
            denomList = CurrencyDenominations["EUR"]; // fallback

        var result = new List<CashDenomination>();
        for (int i = 0; i < denomList.Count; i++)
        {
            var (value, type) = denomList[i];
            var denom = new CashDenomination
            {
                TenantId = tenantId,
                FiscalDrawerId = fiscalDrawerId,
                CurrencyCode = currencyCode,
                Value = value,
                DenominationType = type,
                Quantity = 0,
                SortOrder = i,
                CreatedBy = currentUser
            };
            context.CashDenominations.Add(denom);
            result.Add(denom);
        }

        await context.SaveChangesAsync(ct);
        return result.Select(MapDenominationToDto).ToList();
    }

    public async Task<CashDenominationDto?> UpdateDenominationQuantityAsync(Guid denominationId, UpdateCashDenominationDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var entity = await context.CashDenominations
            .FirstOrDefaultAsync(d => d.Id == denominationId && !d.IsDeleted && d.TenantId == tenantId, ct);

        if (entity is null) return null;

        entity.Quantity = dto.Quantity;
        entity.ModifiedBy = currentUser;
        entity.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return MapDenominationToDto(entity);
    }

    #endregion

    #region Change Calculation

    public async Task<CalculateChangeResponseDto> CalculateChangeAsync(Guid fiscalDrawerId, CalculateChangeRequestDto request, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var changeAmount = request.ReceivedAmount - request.TotalAmount;

        var response = new CalculateChangeResponseDto
        {
            TotalAmount = request.TotalAmount,
            ReceivedAmount = request.ReceivedAmount,
            ChangeAmount = changeAmount,
            HasSufficientFunds = request.ReceivedAmount >= request.TotalAmount
        };

        if (!response.HasSufficientFunds)
        {
            response.Message = "Importo ricevuto insufficiente.";
            return response;
        }

        if (changeAmount == 0)
        {
            response.IsExact = true;
            response.Message = "Pagamento esatto.";
            return response;
        }

        var denominations = await context.CashDenominations
            .Where(d => d.FiscalDrawerId == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId && d.Quantity > 0)
            .OrderByDescending(d => d.Value)
            .ToListAsync(ct);

        // Greedy change-making algorithm
        var remaining = changeAmount;
        var breakdown = new List<ChangeItem>();

        foreach (var denom in denominations)
        {
            if (remaining <= 0) break;
            var useCount = (int)Math.Min(Math.Floor(remaining / denom.Value), denom.Quantity);
            if (useCount > 0)
            {
                breakdown.Add(new ChangeItem
                {
                    Value = denom.Value,
                    DenominationType = denom.DenominationType,
                    Quantity = useCount
                });
                remaining -= denom.Value * useCount;
                remaining = Math.Round(remaining, 4); // avoid floating-point drift
            }
        }

        response.ChangeBreakdown = breakdown;
        response.IsExact = remaining == 0;
        response.Message = response.IsExact
            ? $"Resto: {changeAmount:C}"
            : $"Resto parziale: {changeAmount - remaining:C} (mancano {remaining:C} in contanti)";

        return response;
    }

    #endregion

    #region Summary / Dashboard

    public async Task<FiscalDrawerSummaryDto?> GetDrawerSummaryAsync(Guid fiscalDrawerId, CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var drawer = await context.FiscalDrawers
            .FirstOrDefaultAsync(d => d.Id == fiscalDrawerId && !d.IsDeleted && d.TenantId == tenantId, ct);
        if (drawer is null) return null;

        var session = await context.FiscalDrawerSessions
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == fiscalDrawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

        return new FiscalDrawerSummaryDto
        {
            Id = drawer.Id,
            Name = drawer.Name,
            CurrencyCode = drawer.CurrencyCode,
            CurrentBalance = drawer.CurrentBalance,
            Status = drawer.Status,
            HasOpenSession = session is not null,
            CurrentSessionId = session?.Id,
            SessionOpenedAt = session?.OpenedAt,
            SessionOpeningBalance = session?.OpeningBalance ?? 0,
            SessionTotalSales = session?.TotalSales ?? 0,
            SessionTotalDeposits = session?.TotalDeposits ?? 0,
            SessionTotalWithdrawals = session?.TotalWithdrawals ?? 0
        };
    }

    public async Task<SalesDashboardDto> GetSalesDashboardAsync(CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var drawers = await context.FiscalDrawers
            .Where(d => !d.IsDeleted && d.TenantId == tenantId && d.Status == FiscalDrawerStatus.Active)
            .ToListAsync(ct);

        var todaySessions = await context.FiscalDrawerSessions
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.SessionDate == today)
            .ToListAsync(ct);

        var todayTx = await context.FiscalDrawerTransactions
            .Where(t => !t.IsDeleted && t.TenantId == tenantId &&
                        t.TransactionType == FiscalDrawerTransactionType.Sale &&
                        t.TransactionAt >= today)
            .ToListAsync(ct);

        var weekSessions = await context.FiscalDrawerSessions
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.SessionDate >= weekStart)
            .ToListAsync(ct);

        var monthSessions = await context.FiscalDrawerSessions
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.SessionDate >= monthStart)
            .ToListAsync(ct);

        var openSessions = await context.FiscalDrawerSessions
            .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.Status == FiscalDrawerSessionStatus.Open)
            .ToListAsync(ct);

        var drawerSummaries = new List<FiscalDrawerSummaryDto>();
        foreach (var d in drawers)
        {
            var openSession = openSessions.FirstOrDefault(s => s.FiscalDrawerId == d.Id);
            drawerSummaries.Add(new FiscalDrawerSummaryDto
            {
                Id = d.Id,
                Name = d.Name,
                CurrencyCode = d.CurrencyCode,
                CurrentBalance = d.CurrentBalance,
                Status = d.Status,
                HasOpenSession = openSession is not null,
                CurrentSessionId = openSession?.Id,
                SessionOpenedAt = openSession?.OpenedAt,
                SessionOpeningBalance = openSession?.OpeningBalance ?? 0,
                SessionTotalSales = openSession?.TotalSales ?? 0,
                SessionTotalDeposits = openSession?.TotalDeposits ?? 0,
                SessionTotalWithdrawals = openSession?.TotalWithdrawals ?? 0
            });
        }

        var weeklyTrend = weekSessions
            .GroupBy(s => s.SessionDate.Date)
            .Select(g => new DailySalesPointDto
            {
                Date = g.Key,
                TotalSales = g.Sum(s => s.TotalSales),
                TransactionCount = g.Sum(s => s.TransactionCount)
            })
            .OrderBy(p => p.Date)
            .ToList();

        return new SalesDashboardDto
        {
            TodayTotalSales = todayTx.Sum(t => t.Amount),
            TodayCashSales = todayTx.Where(t => t.PaymentType == FiscalDrawerPaymentType.Cash).Sum(t => t.Amount),
            TodayCardSales = todayTx.Where(t => t.PaymentType == FiscalDrawerPaymentType.Card).Sum(t => t.Amount),
            TodayOtherSales = todayTx.Where(t => t.PaymentType != FiscalDrawerPaymentType.Cash && t.PaymentType != FiscalDrawerPaymentType.Card).Sum(t => t.Amount),
            TodayTransactionCount = todaySessions.Sum(s => s.TransactionCount),
            WeekTotalSales = weekSessions.Sum(s => s.TotalSales),
            MonthTotalSales = monthSessions.Sum(s => s.TotalSales),
            TotalDrawerBalance = drawers.Sum(d => d.CurrentBalance),
            ActiveDrawersCount = drawers.Count,
            OpenSessionsCount = openSessions.Count,
            DrawerSummaries = drawerSummaries,
            WeeklySalesTrend = weeklyTrend
        };
    }

    #endregion

    #region Mapping

    private static FiscalDrawerDto MapToDto(FiscalDrawer d, FiscalDrawerSession? openSession) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Code = d.Code,
        Description = d.Description,
        AssignmentType = d.AssignmentType,
        CurrencyCode = d.CurrencyCode,
        Status = d.Status,
        OpeningBalance = d.OpeningBalance,
        CurrentBalance = d.CurrentBalance,
        PosId = d.PosId,
        PosName = d.Pos?.Name,
        OperatorId = d.OperatorId,
        OperatorName = d.Operator?.Name,
        Notes = d.Notes,
        HasOpenSession = openSession is not null,
        CurrentSessionId = openSession?.Id,
        CreatedAt = d.CreatedAt,
        CreatedBy = d.CreatedBy,
        ModifiedAt = d.ModifiedAt,
        ModifiedBy = d.ModifiedBy
    };

    private static FiscalDrawerSessionDto MapSessionToDto(FiscalDrawerSession s) => new()
    {
        Id = s.Id,
        FiscalDrawerId = s.FiscalDrawerId,
        FiscalDrawerName = s.FiscalDrawer?.Name ?? string.Empty,
        SessionDate = s.SessionDate,
        OpenedAt = s.OpenedAt,
        ClosedAt = s.ClosedAt,
        OpeningBalance = s.OpeningBalance,
        ClosingBalance = s.ClosingBalance,
        TotalCashIn = s.TotalCashIn,
        TotalCashOut = s.TotalCashOut,
        TotalSales = s.TotalSales,
        TotalDeposits = s.TotalDeposits,
        TotalWithdrawals = s.TotalWithdrawals,
        TransactionCount = s.TransactionCount,
        OpenedByOperatorId = s.OpenedByOperatorId,
        OpenedByOperatorName = s.OpenedByOperator?.Name,
        ClosedByOperatorId = s.ClosedByOperatorId,
        ClosedByOperatorName = s.ClosedByOperator?.Name,
        Status = s.Status,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt
    };

    private static FiscalDrawerTransactionDto MapTransactionToDto(FiscalDrawerTransaction t) => new()
    {
        Id = t.Id,
        FiscalDrawerId = t.FiscalDrawerId,
        FiscalDrawerSessionId = t.FiscalDrawerSessionId,
        TransactionType = t.TransactionType,
        PaymentType = t.PaymentType,
        Amount = t.Amount,
        Description = t.Description,
        SaleSessionId = t.SaleSessionId,
        TransactionAt = t.TransactionAt,
        OperatorName = t.OperatorName,
        CreatedAt = t.CreatedAt
    };

    private static CashDenominationDto MapDenominationToDto(CashDenomination d) => new()
    {
        Id = d.Id,
        FiscalDrawerId = d.FiscalDrawerId,
        CurrencyCode = d.CurrencyCode,
        Value = d.Value,
        DenominationType = d.DenominationType,
        Quantity = d.Quantity,
        SortOrder = d.SortOrder
    };

    #endregion

    #region Helpers

    private Guid RequireTenantId()
    {
        if (!tenantContext.CurrentTenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");
        return tenantContext.CurrentTenantId.Value;
    }

    /// <summary>Loads the currently open session for a drawer, if any.</summary>
    private Task<FiscalDrawerSession?> GetOpenSessionForDrawerAsync(Guid drawerId, Guid tenantId, CancellationToken ct) =>
        context.FiscalDrawerSessions
            .FirstOrDefaultAsync(s =>
                s.FiscalDrawerId == drawerId &&
                s.Status == FiscalDrawerSessionStatus.Open &&
                !s.IsDeleted &&
                s.TenantId == tenantId, ct);

    #endregion
}
