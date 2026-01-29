using EventForge.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing contacts.
/// </summary>
public class ContactService : IContactService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ContactService> _logger;

    public ContactService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<ContactService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ContactDto>> GetContactsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for contact operations.");
            }

            var query = _context.Contacts
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var contacts = await query
                .OrderBy(c => c.OwnerType)
                .ThenBy(c => c.ContactType)
                .ThenBy(c => c.Value)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var contactDtos = contacts.Select(MapToContactDto);

            return new PagedResult<ContactDto>
            {
                Items = contactDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts.");
            throw;
        }
    }

    public async Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for contact operations.");
            }

            var contacts = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && !c.IsDeleted && c.TenantId == currentTenantId.Value)
                .OrderBy(c => c.ContactType)
                .ThenBy(c => c.Value)
                .ToListAsync(cancellationToken);

            return contacts.Select(MapToContactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    public async Task<ContactDto?> GetContactByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return contact == null ? null : MapToContactDto(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact {ContactId}.", id);
            throw;
        }
    }

    public async Task<ContactDto> CreateContactAsync(CreateContactDto createContactDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createContactDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for contact operations.");
            }

            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                OwnerId = createContactDto.OwnerId,
                OwnerType = createContactDto.OwnerType,
                ContactType = createContactDto.ContactType.ToEntity(),
                Value = createContactDto.Value,
                Notes = createContactDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _ = _context.Contacts.Add(contact);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(contact, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Contact {ContactId} created by {User}.", contact.Id, currentUser);

            return MapToContactDto(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact.");
            throw;
        }
    }

    public async Task<ContactDto?> UpdateContactAsync(Guid id, UpdateContactDto updateContactDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateContactDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var contact = await _context.Contacts
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (contact == null) return null;

            // Create snapshot of original state before modifications
            var originalValues = _context.Entry(contact).CurrentValues.Clone();
            var originalContact = (Contact)originalValues.ToObject();

            contact.ContactType = updateContactDto.ContactType.ToEntity();
            contact.Value = updateContactDto.Value;
            contact.Notes = updateContactDto.Notes;
            contact.ModifiedAt = DateTime.UtcNow;
            contact.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(contact, "Update", currentUser, originalContact, cancellationToken);

            _logger.LogInformation("Contact {ContactId} updated by {User}.", contact.Id, currentUser);

            return MapToContactDto(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteContactAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var contact = await _context.Contacts
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (contact == null) return false;

            // Create snapshot of original state before modifications
            var originalValues = _context.Entry(contact).CurrentValues.Clone();
            var originalContact = (Contact)originalValues.ToObject();

            contact.IsDeleted = true;
            contact.DeletedAt = DateTime.UtcNow;
            contact.DeletedBy = currentUser;
            contact.ModifiedAt = DateTime.UtcNow;
            contact.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(contact, "Delete", currentUser, originalContact, cancellationToken);

            _logger.LogInformation("Contact {ContactId} deleted by {User}.", contact.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}.", id);
            throw;
        }
    }

    public async Task<bool> ContactExistsAsync(Guid contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contacts
                .AsNoTracking()
                .AnyAsync(c => c.Id == contactId && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if contact {ContactId} exists.", contactId);
            throw;
        }
    }

    public async Task<IEnumerable<ContactDto>> GetContactsByOwnerAndPurposeAsync(Guid ownerId, string ownerType, ContactPurpose purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && c.OwnerType == ownerType && c.Purpose == purpose && !c.IsDeleted)
                .OrderBy(c => c.ContactType)
                .ThenBy(c => c.Value)
                .ToListAsync(cancellationToken);

            return contacts.Select(MapToContactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts for owner {OwnerId} of type {OwnerType} with purpose {Purpose}.", ownerId, ownerType, purpose);
            throw;
        }
    }

    public async Task<ContactDto?> GetPrimaryContactAsync(Guid ownerId, string ownerType, ContactType contactType, CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && c.OwnerType == ownerType &&
                           c.ContactType == contactType.ToEntity() && c.IsPrimary && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return contact != null ? MapToContactDto(contact) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving primary contact for owner {OwnerId} of type {OwnerType} with contact type {ContactType}.", ownerId, ownerType, contactType);
            throw;
        }
    }

    public async Task<bool> ValidateEmergencyContactRequirementsAsync(Guid ownerId, bool isMinor, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!isMinor)
            {
                // Emergency contact not required for adults
                return true;
            }

            // Check if there's at least one emergency contact
            var hasEmergencyContact = await _context.Contacts
                .AsNoTracking()
                .AnyAsync(c => c.OwnerId == ownerId && c.Purpose == ContactPurpose.Emergency && !c.IsDeleted, cancellationToken);

            return hasEmergencyContact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating emergency contact requirements for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    private static ContactDto MapToContactDto(Contact contact)
    {
        return new ContactDto
        {
            Id = contact.Id,
            OwnerId = contact.OwnerId,
            OwnerType = contact.OwnerType,
            ContactType = contact.ContactType.ToDto(),
            Value = contact.Value,
            Notes = contact.Notes,
            CreatedAt = contact.CreatedAt,
            CreatedBy = contact.CreatedBy,
            ModifiedAt = contact.ModifiedAt,
            ModifiedBy = contact.ModifiedBy
        };
    }
}