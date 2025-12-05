using EventForge.DTOs.Sales;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Warehouse;
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
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IStockMovementService _stockMovementService;

    public SaleSessionService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<SaleSessionService> logger,
        IDocumentHeaderService documentHeaderService,
        IStockMovementService stockMovementService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
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

            // Verify session state immediately after loading
            _logger.LogInformation(
                "Session loaded - Id: {SessionId}, TenantId: {TenantId}, IsDeleted: {IsDeleted}, Status: {Status}, ItemsCount: {ItemCount}",
                session.Id,
                session.TenantId,
                session.IsDeleted,
                session.Status,
                session.Items.Count);

            // Verify tenant matches
            if (session.TenantId != currentTenantId.Value)
            {
                _logger.LogError(
                    "CRITICAL: TenantId mismatch! Session.TenantId={SessionTenantId}, CurrentTenantId={CurrentTenantId}",
                    session.TenantId,
                    currentTenantId.Value);
                throw new InvalidOperationException($"Tenant mismatch: session belongs to {session.TenantId} but current tenant is {currentTenantId.Value}");
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

            // Get VAT rate from product
            var taxRate = 0m;
            if (product.VatRateId.HasValue)
            {
                var vatRate = await _context.VatRates
                    .Where(vr => vr.Id == product.VatRateId.Value && !vr.IsDeleted)
                    .Select(vr => vr.Percentage)
                    .FirstOrDefaultAsync(cancellationToken);
                taxRate = vatRate;
            }
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

            // Log new item details
            _logger.LogInformation(
                "New SaleItem created - Id: {ItemId}, ProductId: {ProductId}, Quantity: {Quantity}, UnitPrice: {UnitPrice}, TenantId: {TenantId}",
                item.Id,
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.TenantId);

            session.Items.Add(item);
            
            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            CalculateTotalsInline(session);

            // Log calculated totals
            _logger.LogInformation(
                "Totals calculated - SessionId: {SessionId}, OriginalTotal: {OriginalTotal}, DiscountAmount: {DiscountAmount}, TaxAmount: {TaxAmount}, FinalTotal: {FinalTotal}",
                session.Id,
                session.OriginalTotal,
                session.DiscountAmount,
                session.TaxAmount,
                session.FinalTotal);
            
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // NEW CODE: Add comprehensive diagnostic logging
            try
            {
                _logger.LogInformation(
                    "SaveChanges attempt - SessionId: {SessionId}, TenantId: {TenantId}, ItemsCount: {ItemCount}, TrackedEntities: {TrackedCount}",
                    sessionId,
                    currentTenantId.Value,
                    session.Items.Count,
                    _context.ChangeTracker.Entries().Count());

                // Log all tracked entities and their states
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    var entityType = entry.Entity.GetType().Name;
                    var entityId = entry.Entity is AuditableEntity ae ? ae.Id.ToString() : "N/A";
                    var entityState = entry.State.ToString();
                    
                    _logger.LogDebug(
                        "Tracked entity: Type={EntityType}, Id={EntityId}, State={State}",
                        entityType,
                        entityId,
                        entityState);
                        
                    // Log IsDeleted for AuditableEntity
                    if (entry.Entity is AuditableEntity auditableEntity)
                    {
                        _logger.LogDebug(
                            "  -> IsDeleted={IsDeleted}, TenantId={TenantId}",
                            auditableEntity.IsDeleted,
                            auditableEntity.TenantId);
                    }
                }

                // Attempt SaveChanges
                var affectedRows = await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "SaveChanges completed successfully - SessionId: {SessionId}, AffectedRows: {RowCount}",
                    sessionId,
                    affectedRows);

                _ = await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Items", "AddItem", null, $"Added {product.Name}", currentUser, "Sale Session", cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex,
                    "DbUpdateConcurrencyException in AddItemAsync - SessionId: {SessionId}, TenantId: {TenantId}, ProductId: {ProductId}",
                    sessionId,
                    currentTenantId.Value,
                    addItemDto.ProductId);

                // Log detailed information about failed entities
                _logger.LogError("Failed entities count: {Count}", ex.Entries.Count);
                
                foreach (var entry in ex.Entries)
                {
                    var entityType = entry.Entity.GetType().Name;
                    var entityState = entry.State.ToString();
                    
                    _logger.LogError(
                        "Failed entity: Type={EntityType}, State={State}",
                        entityType,
                        entityState);
                    
                    if (entry.Entity is AuditableEntity failedEntity)
                    {
                        _logger.LogError(
                            "  -> Id={Id}, IsDeleted={IsDeleted}, TenantId={TenantId}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}",
                            failedEntity.Id,
                            failedEntity.IsDeleted,
                            failedEntity.TenantId,
                            failedEntity.CreatedAt,
                            failedEntity.ModifiedAt);
                    }
                    
                    // Log current and original values for modified entities
                    if (entry.State == EntityState.Modified)
                    {
                        foreach (var property in entry.Properties.Where(p => p.IsModified))
                        {
                            _logger.LogError(
                                "  -> Modified property: {PropertyName}, OriginalValue={OriginalValue}, CurrentValue={CurrentValue}",
                                property.Metadata.Name,
                                property.OriginalValue,
                                property.CurrentValue);
                        }
                    }
                }
                
                throw;
            }

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
            item.UnitPrice = updateItemDto.UnitPrice;
            item.DiscountPercent = updateItemDto.DiscountPercent;
            item.Notes = updateItemDto.Notes;

            var subtotal = item.UnitPrice * item.Quantity;
            var discountAmount = subtotal * (item.DiscountPercent / 100);
            item.TotalAmount = subtotal - discountAmount;
            item.TaxAmount = item.TotalAmount * (item.TaxRate / 100);

            item.ModifiedBy = currentUser;
            item.ModifiedAt = DateTime.UtcNow;

            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            CalculateTotalsInline(session);
            
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Single SaveChanges call to save both item and updated totals atomically
            _ = await _context.SaveChangesAsync(cancellationToken);

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

            // CRITICAL FIX: Calculate totals BEFORE saving to avoid multiple SaveChanges
            // This prevents concurrency conflicts in the ChangeTracker
            CalculateTotalsInline(session);
            
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            // Single SaveChanges call to save both item and updated totals atomically
            _ = await _context.SaveChangesAsync(cancellationToken);

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

            // Get user ID from username
            var userId = await GetUserIdFromUsernameAsync(currentUser, cancellationToken);

            var note = new SessionNote
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                SaleSessionId = sessionId,
                NoteFlagId = addNoteDto.NoteFlagId,
                Text = addNoteDto.Text,
                CreatedByUserId = userId,
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

            // Generate document (receipt) for the closed session
            try
            {
                var documentId = await GenerateReceiptDocumentAsync(session, currentUser, cancellationToken);
                
                if (documentId.HasValue)
                {
                    session.DocumentId = documentId.Value;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Generated receipt document {DocumentId} for sale session {SessionId}", documentId.Value, sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt document for sale session {SessionId}. Session is closed but document generation failed.", sessionId);
                // Session is already closed, we don't throw here to avoid rolling back the closure
            }

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
                .Where(s => s.TenantId == currentTenantId.Value && 
                           !s.IsDeleted && 
                           (s.Status == SaleSessionStatus.Open || s.Status == SaleSessionStatus.Suspended))
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

    /// <summary>
    /// Calculates session totals inline without calling SaveChanges.
    /// Used by Add/Update/Remove methods to avoid DbUpdateConcurrencyException.
    /// </summary>
    private void CalculateTotalsInline(SaleSession session)
    {
        CalculateTotals(session);
    }

    private async Task RecalculateTotalsAsync(SaleSession session, CancellationToken cancellationToken)
    {
        CalculateTotals(session);
        session.ModifiedAt = DateTime.UtcNow;
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Shared calculation logic for session totals.
    /// </summary>
    private void CalculateTotals(SaleSession session)
    {
        var activeItems = session.Items.Where(i => !i.IsDeleted).ToList();

        session.OriginalTotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
        var itemsTotal = activeItems.Sum(i => i.TotalAmount);
        session.DiscountAmount = session.OriginalTotal - itemsTotal;
        session.TaxAmount = activeItems.Sum(i => i.TaxAmount);
        session.FinalTotal = itemsTotal + session.TaxAmount;
    }

    private async Task<SaleSessionDto> MapToDtoAsync(SaleSession session, CancellationToken cancellationToken)
    {
        // Get product IDs from items
        var productIds = session.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId).Distinct().ToList();

        // Fetch product details including Brand, VatRate, ImageDocument for all items at once
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

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
            Items = session.Items.Where(i => !i.IsDeleted).Select(i => MapItemToDto(i, products)).ToList(),
            Payments = session.Payments.Where(p => !p.IsDeleted).Select(MapPaymentToDto).ToList(),
            Notes = session.Notes.Select(MapNoteToDto).ToList()
        };

        return dto;
    }

    private SaleItemDto MapItemToDto(SaleItem item, Dictionary<Guid, EventForge.Server.Data.Entities.Products.Product> products)
    {
        var dto = new SaleItemDto
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

        // Enrich with product details if available
        if (products.TryGetValue(item.ProductId, out var product))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            dto.ProductImageUrl = product.ImageUrl;
#pragma warning restore CS0618 // Type or member is obsolete
            // Use ImageDocument if available, fallback to deprecated ImageUrl
            if (product.ImageDocument != null)
            {
                dto.ProductThumbnailUrl = product.ImageDocument.ThumbnailStorageKey ?? product.ImageDocument.StorageKey ?? string.Empty;
                dto.ProductImageUrl = product.ImageDocument.Url ?? product.ImageDocument.StorageKey ?? string.Empty;
            }
            dto.BrandName = product.Brand?.Name;
            dto.VatRateId = product.VatRateId;
            dto.VatRateName = product.VatRate?.Name;
            // Note: UnitOfMeasureName would require additional context from ProductCode/ProductUnit
            // For now, we'll leave it null as it requires the specific code that was scanned
        }

        return dto;
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

    public async Task<SaleSessionDto?> VoidSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default)
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

            // Only closed sessions can be voided
            if (session.Status != SaleSessionStatus.Closed)
            {
                throw new InvalidOperationException("Only closed sessions can be voided.");
            }

            // Update session status to Cancelled
            session.Status = SaleSessionStatus.Cancelled;
            session.ModifiedBy = currentUser;
            session.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync("SaleSession", session.Id, "Status", "Void", "Closed", "Cancelled", currentUser, "Sale Session", cancellationToken);

            _logger.LogInformation("Voided sale session {SessionId}", sessionId);

            // Create inverse stock movements to restore inventory
            if (session.DocumentId.HasValue)
            {
                foreach (var item in session.Items.Where(i => !i.IsDeleted && !i.IsService))
                {
                    try
                    {
                        var voidMovementDto = new EventForge.DTOs.Warehouse.CreateStockMovementDto
                        {
                            MovementType = "VOID",
                            ProductId = item.ProductId,
                            Quantity = item.Quantity, // Positive to restore inventory
                            MovementDate = DateTime.UtcNow,
                            DocumentHeaderId = session.DocumentId.Value,
                            Reason = "Annullamento vendita",
                            Notes = $"Storno vendita da sessione {session.Id}",
                            Reference = $"VOID-{session.Id.ToString("N")[..8]}"
                        };

                        await _stockMovementService.CreateMovementAsync(voidMovementDto, currentUser, cancellationToken);
                        _logger.LogInformation("Created void stock movement for product {ProductId}, quantity {Quantity}", 
                            item.ProductId, item.Quantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating void stock movement for product {ProductId} in session {SessionId}", 
                            item.ProductId, session.Id);
                        // Continue with other items even if one fails
                    }
                }

                // Mark document as cancelled
                try
                {
                    var document = await _context.DocumentHeaders.FirstOrDefaultAsync(
                        d => d.Id == session.DocumentId.Value && d.TenantId == currentTenantId.Value && !d.IsDeleted, 
                        cancellationToken);

                    if (document != null)
                    {
                        document.Status = EventForge.Server.Data.Entities.Documents.DocumentStatus.Cancelled;
                        document.ModifiedBy = currentUser;
                        document.ModifiedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Marked document {DocumentId} as cancelled", session.DocumentId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error marking document {DocumentId} as cancelled", session.DocumentId.Value);
                }
            }

            return await MapToDtoAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding sale session {SessionId}.", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Gets user ID from username.
    /// </summary>
    private async Task<Guid> GetUserIdFromUsernameAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                return Guid.Empty;
            }

            var userId = await _context.Users
                .Where(u => u.Username == username && u.TenantId == currentTenantId.Value && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return userId != Guid.Empty ? userId : Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user ID for username {Username}, using Empty GUID", username);
            return Guid.Empty;
        }
    }

    /// <summary>
    /// Generates a receipt document for a closed sale session.
    /// </summary>
    private async Task<Guid?> GenerateReceiptDocumentAsync(SaleSession session, string currentUser, CancellationToken cancellationToken)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                _logger.LogWarning("Cannot generate receipt: No tenant context");
                return null;
            }

            // Find or get RECEIPT document type
            var receiptDocumentType = await _context.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Code == "RECEIPT" && dt.TenantId == currentTenantId.Value && !dt.IsDeleted, cancellationToken);

            if (receiptDocumentType == null)
            {
                _logger.LogWarning("RECEIPT document type not found. Document generation skipped for session {SessionId}", session.Id);
                return null;
            }

            // Create document header
            var createDocumentDto = new EventForge.DTOs.Documents.CreateDocumentHeaderDto
            {
                DocumentTypeId = receiptDocumentType.Id,
                Number = null, // Will be auto-generated
                Date = DateTime.UtcNow,
                BusinessPartyId = session.CustomerId ?? Guid.Empty,
                CashRegisterId = session.PosId,
                CashierId = session.OperatorId,
                Currency = session.Currency ?? "EUR",
                IsFiscal = true,
                TotalDiscountAmount = session.DiscountAmount,
                Notes = $"Generato dalla sessione di vendita {session.Id}",
                Rows = session.Items.Where(i => !i.IsDeleted).Select(item => new EventForge.DTOs.Documents.CreateDocumentRowDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Description = item.ProductName
                }).ToList()
            };

            var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(createDocumentDto, currentUser, cancellationToken);

            if (documentHeader != null)
            {
                // Create stock movements for each item (outbound)
                foreach (var item in session.Items.Where(i => !i.IsDeleted && !i.IsService))
                {
                    try
                    {
                        var movementDto = new EventForge.DTOs.Warehouse.CreateStockMovementDto
                        {
                            MovementType = "SALE",
                            ProductId = item.ProductId,
                            Quantity = -item.Quantity, // Negative for outbound
                            MovementDate = DateTime.UtcNow,
                            DocumentHeaderId = documentHeader.Id,
                            Reason = "Vendita",
                            Notes = $"Vendita da sessione {session.Id}",
                            Reference = $"SESS-{session.Id.ToString("N")[..8]}"
                        };

                        await _stockMovementService.CreateMovementAsync(movementDto, currentUser, cancellationToken);
                        _logger.LogInformation("Created stock movement for product {ProductId}, quantity {Quantity} for document {DocumentId}", 
                            item.ProductId, -item.Quantity, documentHeader.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating stock movement for product {ProductId} in session {SessionId}. Continuing with other items.", 
                            item.ProductId, session.Id);
                        // Continue with other items even if one fails
                    }
                }

                _logger.LogInformation("Document {DocumentId} created with stock movements for session {SessionId}", documentHeader.Id, session.Id);
                return documentHeader.Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt document for session {SessionId}", session.Id);
            throw;
        }
    }
}
