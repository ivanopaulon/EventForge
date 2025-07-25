using EventForge.Server.DTOs.Common;
using EventForge.Server.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing contacts.
/// </summary>
public class ContactService : IContactService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ContactService> _logger;

    public ContactService(EventForgeDbContext context, IAuditLogService auditLogService, ILogger<ContactService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ContactDto>> GetContactsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Contacts
                .Where(c => !c.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var contacts = await query
                .OrderBy(c => c.OwnerType)
                .ThenBy(c => c.ContactType)
                .ThenBy(c => c.Value)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var contactDtos = contacts.Select(MapToContactDto);

            return new PagedResult<ContactDto>
            {
                Items = contactDtos,
                Page = page,
                PageSize = pageSize,
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
            var contacts = await _context.Contacts
                .Where(c => c.OwnerId == ownerId && !c.IsDeleted)
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

            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                OwnerId = createContactDto.OwnerId,
                OwnerType = createContactDto.OwnerType,
                ContactType = createContactDto.ContactType,
                Value = createContactDto.Value,
                Notes = createContactDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(contact, "Create", currentUser, null, cancellationToken);

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

            var originalContact = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalContact == null) return null;

            var contact = await _context.Contacts
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (contact == null) return null;

            contact.ContactType = updateContactDto.ContactType;
            contact.Value = updateContactDto.Value;
            contact.Notes = updateContactDto.Notes;
            contact.ModifiedAt = DateTime.UtcNow;
            contact.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(contact, "Update", currentUser, originalContact, cancellationToken);

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

            var originalContact = await _context.Contacts
                .AsNoTracking()
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalContact == null) return false;

            var contact = await _context.Contacts
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (contact == null) return false;

            contact.IsDeleted = true;
            contact.DeletedAt = DateTime.UtcNow;
            contact.DeletedBy = currentUser;
            contact.ModifiedAt = DateTime.UtcNow;
            contact.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(contact, "Delete", currentUser, originalContact, cancellationToken);

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
                .AnyAsync(c => c.Id == contactId && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if contact {ContactId} exists.", contactId);
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
            ContactType = contact.ContactType,
            Value = contact.Value,
            Notes = contact.Notes,
            CreatedAt = contact.CreatedAt,
            CreatedBy = contact.CreatedBy,
            ModifiedAt = contact.ModifiedAt,
            ModifiedBy = contact.ModifiedBy
        };
    }
}