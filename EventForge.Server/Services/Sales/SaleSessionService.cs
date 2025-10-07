using EventForge.DTOs.Sales;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing sales sessions.
/// </summary>
public class SaleSessionService : ISaleSessionService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SaleSessionService> _logger;

    public SaleSessionService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<SaleSessionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SaleSessionDto> CreateSessionAsync(CreateSaleSessionDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = new SaleSession
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                OperatorId = createDto.OperatorId,
                PosId = createDto.PosId,
                CustomerId = createDto.CustomerId,
                SaleType = createDto.SaleType,
                TableId = createDto.TableId,
                Currency = createDto.Currency,
                Status = SaleSessionStatus.Open,
                OriginalTotal = 0,
                DiscountAmount = 0,
                FinalTotal = 0,
                TaxAmount = 0,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            _ = _context.SaleSessions.Add(session);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Create", null, "Open", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Created sale session {SessionId} for operator {OperatorId} at POS {PosId}", session.Id, createDto.OperatorId, createDto.PosId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale session.");
            throw;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            session.CustomerId = updateDto.CustomerId ?? session.CustomerId;
            session.SaleType = updateDto.SaleType ?? session.SaleType;

            if (updateDto.Status.HasValue)
            {
                session.Status = (SaleSessionStatus)updateDto.Status.Value;
            }

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Update", null, session.Status.ToString(), currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Updated sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return false;
            }

            session.IsDeleted = true;
            session.DeletedAt = DateTime.UtcNow;
            session.DeletedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "IsDeleted", "Delete", "false", "true", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Deleted sale session {SessionId}", sessionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto addItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            // Fetch product details
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == addItemDto.ProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken);

            if (product == null)
            {
                throw new InvalidOperationException($"Product {addItemDto.ProductId} not found.");
            }

            var unitPrice = addItemDto.UnitPrice ?? product.DefaultPrice ?? 0;
            var subtotal = unitPrice * addItemDto.Quantity;
            var discountAmount = subtotal * (addItemDto.DiscountPercent / 100);
            var totalAmount = subtotal - discountAmount;

            // Get VAT rate from product (simplified - in real scenario would fetch from VatRate entity)
            var taxRate = 0m; // TODO: Fetch from product.VatRate
            var taxAmount = totalAmount * (taxRate / 100);

            var item = new SaleItem
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                ProductId = addItemDto.ProductId,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitPrice = unitPrice,
                Quantity = addItemDto.Quantity,
                DiscountPercent = addItemDto.DiscountPercent,
                TotalAmount = totalAmount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Notes = addItemDto.Notes,
                IsService = addItemDto.IsService,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            session.Items.Add(item);
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Recalculate totals
            await RecalculateTotalsAsync(session, cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "AddItem", null, $"Added {product.Name}", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Added item {ProductId} to sale session {SessionId}", addItemDto.ProductId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateItemDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            var item = session.Items.FirstOrDefault(i => i.Id == itemId && !i.IsDeleted);
            if (item == null)
            {
                throw new InvalidOperationException($"Item {itemId} not found in session {sessionId}.");
            }

            item.Quantity = updateItemDto.Quantity;
            item.DiscountPercent = updateItemDto.DiscountPercent;
            item.Notes = updateItemDto.Notes;

            var subtotal = item.UnitPrice * item.Quantity;
            var discountAmount = subtotal * (item.DiscountPercent / 100);
            item.TotalAmount = subtotal - discountAmount;
            item.TaxAmount = item.TotalAmount * (item.TaxRate / 100);

            item.ModifiedBy = currentUser;
            item.ModifiedAt = DateTime.UtcNow;

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Recalculate totals
            await RecalculateTotalsAsync(session, cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "UpdateItem", item.Quantity.ToString(), updateItemDto.Quantity.ToString(), currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Updated item {ItemId} in sale session {SessionId}", itemId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} in sale session {SessionId}.", itemId, sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            var item = session.Items.FirstOrDefault(i => i.Id == itemId && !i.IsDeleted);
            if (item == null)
            {
                throw new InvalidOperationException($"Item {itemId} not found in session {sessionId}.");
            }

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.DeletedBy = currentUser;

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Recalculate totals
            await RecalculateTotalsAsync(session, cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "RemoveItem", item.ProductName, "Removed", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Removed item {ItemId} from sale session {SessionId}", itemId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from sale session {SessionId}.", itemId, sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto addPaymentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            var payment = new SalePayment
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                PaymentMethodId = addPaymentDto.PaymentMethodId,
                Amount = addPaymentDto.Amount,
                Status = Data.Entities.Sales.PaymentStatus.Completed,
                Notes = addPaymentDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            session.Payments.Add(payment);
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Payments", "AddPayment", null, $"Payment: {addPaymentDto.Amount}", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Added payment of {Amount} to sale session {SessionId}", addPaymentDto.Amount, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            var payment = session.Payments.FirstOrDefault(p => p.Id == paymentId && !p.IsDeleted);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {paymentId} not found in session {sessionId}.");
            }

            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
            payment.DeletedBy = currentUser;

            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Payments", "RemovePayment", payment.Amount.ToString(), "Removed", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Removed payment {PaymentId} from sale session {SessionId}", paymentId, sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment {PaymentId} from sale session {SessionId}.", paymentId, sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto addNoteDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            var note = new SessionNote
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                NoteFlagId = addNoteDto.NoteFlagId,
                Text = addNoteDto.Text,
                CreatedByUserId = Guid.Empty, // TODO: Get actual user ID
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = currentUser,
                ModifiedAt = DateTime.UtcNow
            };

            session.Notes.Add(note);
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Notes", "AddNote", null, "Note added", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Added note to sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            await RecalculateTotalsAsync(session, cancellationToken);

            _logger.LogInformation("Recalculated totals for sale session {SessionId}", sessionId);

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating totals for sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var session = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (session == null)
            {
                return null;
            }

            // Validate that session is fully paid
            var completedPayments = session.Payments.Where(p => !p.IsDeleted && p.Status == Data.Entities.Sales.PaymentStatus.Completed).Sum(p => p.Amount);
            if (completedPayments < session.FinalTotal)
            {
                throw new InvalidOperationException($"Session cannot be closed. Total paid ({completedPayments}) is less than final total ({session.FinalTotal}).");
            }

            session.Status = SaleSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Close", "Open", "Closed", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Closed sale session {SessionId}", sessionId);

            // TODO: Generate document (invoice/receipt) - requires DocumentService integration

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing sale session {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<List<SaleSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var sessions = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted && s.Status == SaleSessionStatus.Open)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                dtos.Add(await MapToDtoAsync(session, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sale sessions.");
            throw;
        }
    }

    public async Task<List<SaleSessionDto>> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for sale session operations.");
            }

            var sessions = await _context.SaleSessions
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted && s.OperatorId == operatorId && s.Status == SaleSessionStatus.Open)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = new List<SaleSessionDto>();
            foreach (var session in sessions)
            {
                dtos.Add(await MapToDtoAsync(session, cancellationToken));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sale sessions for operator {OperatorId}.", operatorId);
            throw;
        }
    }

    private async Task RecalculateTotalsAsync(SaleSession session, CancellationToken cancellationToken)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();

        session.OriginalTotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
        var itemsTotal = activeItems.Sum(i => i.TotalAmount);
        session.DiscountAmount = session.OriginalTotal - itemsTotal;
        session.TaxAmount = activeItems.Sum(i => i.TaxAmount);
        session.FinalTotal = itemsTotal + session.TaxAmount;

        session.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<SaleSessionDto> MapToDtoAsync(SaleSession session, CancellationToken cancellationToken)
    {
        var dto = new SaleSessionDto
        {
            Id = session.Id,
            OperatorId = session.OperatorId,
            PosId = session.PosId,
            CustomerId = session.CustomerId,
            SaleType = session.SaleType,
            Status = (SaleSessionStatusDto)session.Status,
            OriginalTotal = session.OriginalTotal,
            DiscountAmount = session.DiscountAmount,
            FinalTotal = session.FinalTotal,
            TaxAmount = session.TaxAmount,
            Currency = session.Currency,
            TableId = session.TableId,
            DocumentId = session.DocumentId,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.ModifiedAt ?? session.CreatedAt,
            ClosedAt = session.ClosedAt,
            CouponCodes = session.CouponCodes,
            Items = session.Items.Where(i => !i.IsDeleted).Select(MapItemToDto).ToList(),
            Payments = session.Payments.Where(p => !p.IsDeleted).Select(MapPaymentToDto).ToList(),
            Notes = session.Notes.Select(MapNoteToDto).ToList()
        };

        return dto;
    }

    private SaleItemDto MapItemToDto(SaleItem item)
    {
        return new SaleItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            DiscountPercent = item.DiscountPercent,
            TotalAmount = item.TotalAmount,
            TaxRate = item.TaxRate,
            TaxAmount = item.TaxAmount,
            Notes = item.Notes,
            IsService = item.IsService,
            PromotionId = item.PromotionId
        };
    }

    private SalePaymentDto MapPaymentToDto(SalePayment payment)
    {
        return new SalePaymentDto
        {
            Id = payment.Id,
            PaymentMethodId = payment.PaymentMethodId,
            Amount = payment.Amount,
            Status = (PaymentStatusDto)payment.Status,
            TransactionReference = payment.TransactionReference,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }

    private SessionNoteDto MapNoteToDto(SessionNote note)
    {
        return new SessionNoteDto
        {
            Id = note.Id,
            NoteFlagId = note.NoteFlagId,
            NoteFlagName = note.NoteFlag?.Name,
            NoteFlagColor = note.NoteFlag?.Color,
            NoteFlagIcon = note.NoteFlag?.Icon,
            Text = note.Text ?? string.Empty,
            CreatedByUserName = note.CreatedBy,
            CreatedAt = note.CreatedAt
        };
    }
}
