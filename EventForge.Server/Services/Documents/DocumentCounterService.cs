using EventForge.DTOs.Documents;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service for managing document counters and generating document numbers.
/// </summary>
public class DocumentCounterService : IDocumentCounterService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentCounterService> _logger;

    public DocumentCounterService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<DocumentCounterService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DocumentCounterDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var counters = await _context.DocumentCounters
                .Include(dc => dc.DocumentType)
                .Where(dc => !dc.IsDeleted)
                .OrderBy(dc => dc.DocumentType!.Name)
                .ThenBy(dc => dc.Series)
                .ToListAsync(cancellationToken);

            return counters.Select(c => c.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all document counters.");
            throw;
        }
    }

    public async Task<IEnumerable<DocumentCounterDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var counters = await _context.DocumentCounters
                .Include(dc => dc.DocumentType)
                .Where(dc => dc.DocumentTypeId == documentTypeId && !dc.IsDeleted)
                .OrderBy(dc => dc.Series)
                .ToListAsync(cancellationToken);

            return counters.Select(c => c.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document counters for document type {DocumentTypeId}.", documentTypeId);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var counter = await _context.DocumentCounters
                .Include(dc => dc.DocumentType)
                .FirstOrDefaultAsync(dc => dc.Id == id && !dc.IsDeleted, cancellationToken);

            return counter?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document counter {Id}.", id);
            throw;
        }
    }

    public async Task<DocumentCounterDto?> GetByDocumentTypeSeriesYearAsync(
        Guid documentTypeId,
        string series,
        int? year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var counter = await _context.DocumentCounters
                .Include(dc => dc.DocumentType)
                .FirstOrDefaultAsync(dc =>
                    dc.DocumentTypeId == documentTypeId &&
                    dc.Series == series &&
                    dc.Year == year &&
                    !dc.IsDeleted,
                    cancellationToken);

            return counter?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document counter for type {DocumentTypeId}, series {Series}, year {Year}.",
                documentTypeId, series, year);
            throw;
        }
    }

    public async Task<DocumentCounterDto> CreateAsync(CreateDocumentCounterDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot create document counter without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            // Check if counter already exists
            var existingCounter = await _context.DocumentCounters
                .FirstOrDefaultAsync(dc =>
                    dc.DocumentTypeId == createDto.DocumentTypeId &&
                    dc.Series == createDto.Series &&
                    dc.Year == createDto.Year &&
                    !dc.IsDeleted,
                    cancellationToken);

            if (existingCounter != null)
            {
                throw new InvalidOperationException($"A counter already exists for document type {createDto.DocumentTypeId}, series '{createDto.Series}', year {createDto.Year}.");
            }

            var counter = createDto.ToEntity();
            counter.TenantId = tenantId.Value;
            counter.CreatedBy = currentUser;
            counter.CreatedAt = DateTime.UtcNow;

            _ = _context.DocumentCounters.Add(counter);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(counter, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Document counter {CounterId} created by {User}.", counter.Id, currentUser);

            return counter.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document counter.");
            throw;
        }
    }

    public async Task<DocumentCounterDto?> UpdateAsync(Guid id, UpdateDocumentCounterDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalCounter = await _context.DocumentCounters
                .AsNoTracking()
                .FirstOrDefaultAsync(dc => dc.Id == id && !dc.IsDeleted, cancellationToken);

            if (originalCounter == null)
            {
                _logger.LogWarning("Document counter with ID {Id} not found for update.", id);
                return null;
            }

            var counter = await _context.DocumentCounters
                .FirstOrDefaultAsync(dc => dc.Id == id && !dc.IsDeleted, cancellationToken);

            if (counter == null)
            {
                _logger.LogWarning("Document counter with ID {Id} not found for update.", id);
                return null;
            }

            counter.UpdateFromDto(updateDto);
            counter.ModifiedBy = currentUser;
            counter.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(counter, "Update", currentUser, originalCounter, cancellationToken);

            _logger.LogInformation("Document counter {CounterId} updated by {User}.", id, currentUser);

            return counter.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document counter {Id}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalCounter = await _context.DocumentCounters
                .AsNoTracking()
                .FirstOrDefaultAsync(dc => dc.Id == id && !dc.IsDeleted, cancellationToken);

            if (originalCounter == null)
            {
                _logger.LogWarning("Document counter with ID {Id} not found for deletion.", id);
                return false;
            }

            var counter = await _context.DocumentCounters
                .FirstOrDefaultAsync(dc => dc.Id == id && !dc.IsDeleted, cancellationToken);

            if (counter == null)
            {
                _logger.LogWarning("Document counter with ID {Id} not found for deletion.", id);
                return false;
            }

            counter.IsDeleted = true;
            counter.DeletedBy = currentUser;
            counter.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(counter, "Delete", currentUser, originalCounter, cancellationToken);

            _logger.LogInformation("Document counter {CounterId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document counter {Id}.", id);
            throw;
        }
    }

    public async Task<string> GenerateDocumentNumberAsync(
        Guid documentTypeId,
        string series,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required.");
            }

            var currentYear = DateTime.UtcNow.Year;

            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Find or create counter for this document type, series, and year
                var counter = await _context.DocumentCounters
                    .FirstOrDefaultAsync(dc =>
                        dc.DocumentTypeId == documentTypeId &&
                        dc.Series == series &&
                        dc.TenantId == tenantId.Value &&
                        !dc.IsDeleted &&
                        (!dc.ResetOnYearChange || dc.Year == currentYear),
                        cancellationToken);

                if (counter == null)
                {
                    // Create a new counter if it doesn't exist
                    counter = new DocumentCounter
                    {
                        DocumentTypeId = documentTypeId,
                        Series = series,
                        CurrentValue = 0,
                        Year = currentYear,
                        PaddingLength = 5,
                        ResetOnYearChange = true,
                        TenantId = tenantId.Value,
                        CreatedBy = currentUser,
                        CreatedAt = DateTime.UtcNow
                    };

                    _ = _context.DocumentCounters.Add(counter);
                    _logger.LogInformation("Created new document counter for type {DocumentTypeId}, series '{Series}', year {Year}.",
                        documentTypeId, series, currentYear);
                }
                else if (counter.ResetOnYearChange && counter.Year != currentYear)
                {
                    // Reset counter for new year
                    counter.Year = currentYear;
                    counter.CurrentValue = 0;
                    counter.ModifiedBy = currentUser;
                    counter.ModifiedAt = DateTime.UtcNow;
                    _logger.LogInformation("Reset document counter {CounterId} for new year {Year}.", counter.Id, currentYear);
                }

                // Increment counter
                counter.CurrentValue++;

                // Save changes to get the new counter value
                _ = await _context.SaveChangesAsync(cancellationToken);

                // Generate document number using format pattern or default format
                var documentNumber = FormatDocumentNumber(counter);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Generated document number '{DocumentNumber}' for type {DocumentTypeId}, series '{Series}'.",
                    documentNumber, documentTypeId, series);

                return documentNumber;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document number for type {DocumentTypeId}, series '{Series}'.",
                documentTypeId, series);
            throw;
        }
    }

    private static string FormatDocumentNumber(DocumentCounter counter)
    {
        var number = counter.CurrentValue.ToString().PadLeft(counter.PaddingLength, '0');

        if (!string.IsNullOrWhiteSpace(counter.FormatPattern))
        {
            // Replace placeholders in format pattern
            var result = counter.FormatPattern
                .Replace("{PREFIX}", counter.Prefix ?? string.Empty)
                .Replace("{SERIES}", counter.Series)
                .Replace("{YEAR}", counter.Year?.ToString() ?? string.Empty)
                .Replace("{NUMBER}", number);

            return result;
        }

        // Default format: [PREFIX][SERIES]/[YEAR]/[NUMBER]
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(counter.Prefix))
        {
            parts.Add(counter.Prefix);
        }

        if (!string.IsNullOrWhiteSpace(counter.Series))
        {
            parts.Add(counter.Series);
        }

        if (counter.Year.HasValue)
        {
            parts.Add(counter.Year.Value.ToString());
        }

        parts.Add(number);

        return string.Join("/", parts);
    }
}
