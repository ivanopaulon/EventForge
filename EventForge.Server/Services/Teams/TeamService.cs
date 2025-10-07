using EventForge.DTOs.Teams;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Teams;

/// <summary>
/// Service implementation for managing teams and team members.
/// </summary>
public class TeamService : ITeamService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TeamService> _logger;

    public TeamService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<TeamService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Team CRUD operations

    public async Task<PagedResult<TeamDto>> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for team operations.");
            }

            var query = _context.Teams
                .WhereActiveTenant(currentTenantId.Value)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted && m.TenantId == currentTenantId.Value));

            var totalCount = await query.CountAsync(cancellationToken);
            var teams = await query
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var teamDtos = teams.Select(MapToTeamDto);

            return new PagedResult<TeamDto>
            {
                Items = teamDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei team.");
            throw;
        }
    }

    public async Task<IEnumerable<TeamDto>> GetTeamsByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _context.Teams
                .Where(t => t.EventId == eventId && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            return teams.Select(MapToTeamDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei team per l'evento {EventId}.", eventId);
            throw;
        }
    }

    public async Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato.", id);
                return null;
            }

            return MapToTeamDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per dettagli.", id);
                return null;
            }

            return MapToTeamDetailDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei dettagli del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto createTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createTeamDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var eventExists = await EventExistsAsync(createTeamDto.EventId, cancellationToken);
            if (!eventExists)
                throw new ArgumentException($"Event with ID {createTeamDto.EventId} does not exist.", nameof(createTeamDto));

            var team = new Team
            {
                Name = createTeamDto.Name,
                ShortDescription = createTeamDto.ShortDescription,
                LongDescription = createTeamDto.LongDescription,
                Email = createTeamDto.Email,
                EventId = createTeamDto.EventId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.Teams.Add(team);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            _ = await _auditLogService.TrackEntityChangesAsync(team, "Insert", currentUser, null, cancellationToken);

            var createdTeam = await _context.Teams
                .Include(t => t.Event)
                .Include(t => t.Members)
                .FirstAsync(t => t.Id == team.Id, cancellationToken);

            return MapToTeamDto(createdTeam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione del team.");
            throw;
        }
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto updateTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            // Recupera i valori originali per l'audit (preferibilmente AsNoTracking)
            var originalTeam = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            team.Name = updateTeamDto.Name;
            team.ShortDescription = updateTeamDto.ShortDescription;
            team.LongDescription = updateTeamDto.LongDescription;
            team.Email = updateTeamDto.Email;
            team.ModifiedBy = currentUser;
            team.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            _ = await _auditLogService.TrackEntityChangesAsync(team, "Update", currentUser, originalTeam, cancellationToken);

            return MapToTeamDto(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del team {TeamId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteTeamAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var team = await _context.Teams
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (team == null)
            {
                _logger.LogWarning("Team con ID {TeamId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            var originalTeam = await _context.Teams
                .AsNoTracking()
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            team.IsDeleted = true;
            team.DeletedBy = currentUser;
            team.DeletedAt = DateTime.UtcNow;

            var members = await _context.TeamMembers
                .Where(m => m.TeamId == id && !m.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var member in members)
            {
                member.IsDeleted = true;
                member.DeletedBy = currentUser;
                member.DeletedAt = DateTime.UtcNow;

                // Audit log per ogni membro eliminato
                _ = await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, null, cancellationToken);
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log per il team eliminato
            _ = await _auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la cancellazione del team {TeamId}.", id);
            throw;
        }
    }

    // Team Member operations

    public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == teamId && !m.IsDeleted)
            .Include(m => m.Team)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return members.Select(MapToTeamMemberDto);
    }

    public async Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await _context.TeamMembers
            .Where(m => m.Id == id && !m.IsDeleted)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            _logger.LogWarning("Team member con ID {MemberId} non trovato.", id);
            return null;
        }

        return MapToTeamMemberDto(member);
    }

    public async Task<TeamMemberDto> AddTeamMemberAsync(CreateTeamMemberDto createTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createTeamMemberDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var teamExists = await TeamExistsAsync(createTeamMemberDto.TeamId, cancellationToken);
            if (!teamExists)
                throw new ArgumentException($"Team with ID {createTeamMemberDto.TeamId} does not exist.", nameof(createTeamMemberDto));

            var member = new TeamMember
            {
                FirstName = createTeamMemberDto.FirstName,
                LastName = createTeamMemberDto.LastName,
                Email = createTeamMemberDto.Email,
                Role = createTeamMemberDto.Role,
                DateOfBirth = createTeamMemberDto.DateOfBirth,
                TeamId = createTeamMemberDto.TeamId,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = _context.TeamMembers.Add(member);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            _ = await _auditLogService.TrackEntityChangesAsync(member, "Insert", currentUser, null, cancellationToken);

            var createdMember = await _context.TeamMembers
                .Include(m => m.Team)
                .FirstAsync(m => m.Id == member.Id, cancellationToken);

            return MapToTeamMemberDto(createdMember);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta di un membro al team.");
            throw;
        }
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberDto updateTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamMemberDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var member = await _context.TeamMembers
                .Where(m => m.Id == id && !m.IsDeleted)
                .Include(m => m.Team)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Team member con ID {MemberId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            var originalMember = await _context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.FirstName = updateTeamMemberDto.FirstName;
            member.LastName = updateTeamMemberDto.LastName;
            member.Email = updateTeamMemberDto.Email;
            member.Role = updateTeamMemberDto.Role;
            member.DateOfBirth = updateTeamMemberDto.DateOfBirth;
            member.ModifiedBy = currentUser;
            member.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            _ = await _auditLogService.TrackEntityChangesAsync(member, "Update", currentUser, originalMember, cancellationToken);

            return MapToTeamMemberDto(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del membro {MemberId}.", id);
            throw;
        }
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var member = await _context.TeamMembers
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                _logger.LogWarning("Team member con ID {MemberId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            var originalMember = await _context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.IsDeleted = true;
            member.DeletedBy = currentUser;
            member.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            _ = await _auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la rimozione del membro {MemberId}.", id);
            throw;
        }
    }

    public async Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AnyAsync(t => t.Id == teamId && !t.IsDeleted, cancellationToken);
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    // Document Reference operations

    public async Task<IEnumerable<DocumentReferenceDto>> GetDocumentsByOwnerAsync(Guid ownerId, string ownerType, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _context.DocumentReferences
                .Where(d => d.OwnerId == ownerId && d.OwnerType == ownerType && !d.IsDeleted)
                .OrderBy(d => d.Type)
                .ThenBy(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            return documents.Select(MapToDocumentReferenceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for owner {OwnerId} of type {OwnerType}", ownerId, ownerType);
            throw;
        }
    }

    public async Task<DocumentReferenceDto?> GetDocumentReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _context.DocumentReferences
                .Where(d => d.Id == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return document != null ? MapToDocumentReferenceDto(document) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document reference {DocumentId}", id);
            throw;
        }
    }

    public async Task<DocumentReferenceDto> CreateDocumentReferenceAsync(CreateDocumentReferenceDto createDocumentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDocumentDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var document = new DocumentReference
            {
                OwnerId = createDocumentDto.OwnerId,
                OwnerType = createDocumentDto.OwnerType,
                FileName = createDocumentDto.FileName,
                Type = createDocumentDto.Type,
                SubType = createDocumentDto.SubType,
                MimeType = createDocumentDto.MimeType,
                StorageKey = createDocumentDto.StorageKey,
                Url = createDocumentDto.Url,
                ThumbnailStorageKey = createDocumentDto.ThumbnailStorageKey,
                Expiry = createDocumentDto.Expiry,
                FileSizeBytes = createDocumentDto.FileSizeBytes,
                Title = createDocumentDto.Title,
                Notes = createDocumentDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
            };

            _ = _context.DocumentReferences.Add(document);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(document, "Insert", currentUser, null, cancellationToken);

            return MapToDocumentReferenceDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document reference");
            throw;
        }
    }

    public async Task<DocumentReferenceDto?> UpdateDocumentReferenceAsync(Guid id, UpdateDocumentReferenceDto updateDocumentDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDocumentDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var document = await _context.DocumentReferences
                .Where(d => d.Id == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document reference {DocumentId} not found for update", id);
                return null;
            }

            var originalDocument = await _context.DocumentReferences
                .AsNoTracking()
                .Where(d => d.Id == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            document.FileName = updateDocumentDto.FileName ?? document.FileName;
            document.Type = updateDocumentDto.Type ?? document.Type;
            document.SubType = updateDocumentDto.SubType ?? document.SubType;
            document.Url = updateDocumentDto.Url;
            document.ThumbnailStorageKey = updateDocumentDto.ThumbnailStorageKey;
            document.Expiry = updateDocumentDto.Expiry;
            document.Title = updateDocumentDto.Title;
            document.Notes = updateDocumentDto.Notes;
            document.ModifiedBy = currentUser;
            document.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(document, "Update", currentUser, originalDocument, cancellationToken);

            return MapToDocumentReferenceDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document reference {DocumentId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var document = await _context.DocumentReferences
                .Where(d => d.Id == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document reference {DocumentId} not found for deletion", id);
                return false;
            }

            var originalDocument = await _context.DocumentReferences
                .AsNoTracking()
                .Where(d => d.Id == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            document.IsDeleted = true;
            document.DeletedBy = currentUser;
            document.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(document, "Delete", currentUser, originalDocument, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document reference {DocumentId}", id);
            throw;
        }
    }

    // Membership Card operations

    public async Task<IEnumerable<MembershipCardDto>> GetMembershipCardsByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cards = await _context.MembershipCards
                .Where(mc => mc.TeamMemberId == teamMemberId && !mc.IsDeleted)
                .Include(mc => mc.DocumentReference)
                .OrderBy(mc => mc.ValidFrom)
                .ToListAsync(cancellationToken);

            return cards.Select(MapToMembershipCardDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership cards for team member {TeamMemberId}", teamMemberId);
            throw;
        }
    }

    public async Task<MembershipCardDto?> GetMembershipCardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var card = await _context.MembershipCards
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .Include(mc => mc.DocumentReference)
                .Include(mc => mc.TeamMember)
                .FirstOrDefaultAsync(cancellationToken);

            return card != null ? MapToMembershipCardDto(card) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving membership card {CardId}", id);
            throw;
        }
    }

    public async Task<MembershipCardDto> CreateMembershipCardAsync(CreateMembershipCardDto createMembershipCardDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createMembershipCardDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var card = new MembershipCard
            {
                TeamMemberId = createMembershipCardDto.TeamMemberId,
                CardNumber = createMembershipCardDto.CardNumber,
                Federation = createMembershipCardDto.Federation,
                ValidFrom = createMembershipCardDto.ValidFrom,
                ValidTo = createMembershipCardDto.ValidTo,
                DocumentReferenceId = createMembershipCardDto.DocumentReferenceId,
                Category = createMembershipCardDto.Category,
                Notes = createMembershipCardDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
            };

            _ = _context.MembershipCards.Add(card);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(card, "Insert", currentUser, null, cancellationToken);

            var createdCard = await _context.MembershipCards
                .Include(mc => mc.DocumentReference)
                .Include(mc => mc.TeamMember)
                .FirstAsync(mc => mc.Id == card.Id, cancellationToken);

            return MapToMembershipCardDto(createdCard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating membership card");
            throw;
        }
    }

    public async Task<MembershipCardDto?> UpdateMembershipCardAsync(Guid id, UpdateMembershipCardDto updateMembershipCardDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateMembershipCardDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var card = await _context.MembershipCards
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .Include(mc => mc.DocumentReference)
                .Include(mc => mc.TeamMember)
                .FirstOrDefaultAsync(cancellationToken);

            if (card == null)
            {
                _logger.LogWarning("Membership card {CardId} not found for update", id);
                return null;
            }

            var originalCard = await _context.MembershipCards
                .AsNoTracking()
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            card.CardNumber = updateMembershipCardDto.CardNumber ?? card.CardNumber;
            card.Federation = updateMembershipCardDto.Federation ?? card.Federation;
            card.ValidFrom = updateMembershipCardDto.ValidFrom ?? card.ValidFrom;
            card.ValidTo = updateMembershipCardDto.ValidTo ?? card.ValidTo;
            card.DocumentReferenceId = updateMembershipCardDto.DocumentReferenceId;
            card.Category = updateMembershipCardDto.Category;
            card.Notes = updateMembershipCardDto.Notes;
            card.ModifiedBy = currentUser;
            card.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, cancellationToken);

            return MapToMembershipCardDto(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership card {CardId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteMembershipCardAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var card = await _context.MembershipCards
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (card == null)
            {
                _logger.LogWarning("Membership card {CardId} not found for deletion", id);
                return false;
            }

            var originalCard = await _context.MembershipCards
                .AsNoTracking()
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            card.IsDeleted = true;
            card.DeletedBy = currentUser;
            card.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(card, "Delete", currentUser, originalCard, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting membership card {CardId}", id);
            throw;
        }
    }

    // Insurance Policy operations

    public async Task<IEnumerable<InsurancePolicyDto>> GetInsurancePoliciesByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var policies = await _context.InsurancePolicies
                .Where(ip => ip.TeamMemberId == teamMemberId && !ip.IsDeleted)
                .Include(ip => ip.DocumentReference)
                .OrderBy(ip => ip.ValidFrom)
                .ToListAsync(cancellationToken);

            return policies.Select(MapToInsurancePolicyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policies for team member {TeamMemberId}", teamMemberId);
            throw;
        }
    }

    public async Task<InsurancePolicyDto?> GetInsurancePolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _context.InsurancePolicies
                .Where(ip => ip.Id == id && !ip.IsDeleted)
                .Include(ip => ip.DocumentReference)
                .Include(ip => ip.TeamMember)
                .FirstOrDefaultAsync(cancellationToken);

            return policy != null ? MapToInsurancePolicyDto(policy) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance policy {PolicyId}", id);
            throw;
        }
    }

    public async Task<InsurancePolicyDto> CreateInsurancePolicyAsync(CreateInsurancePolicyDto createInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createInsurancePolicyDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var policy = new InsurancePolicy
            {
                TeamMemberId = createInsurancePolicyDto.TeamMemberId,
                Provider = createInsurancePolicyDto.Provider,
                PolicyNumber = createInsurancePolicyDto.PolicyNumber,
                ValidFrom = createInsurancePolicyDto.ValidFrom,
                ValidTo = createInsurancePolicyDto.ValidTo,
                CoverageType = createInsurancePolicyDto.CoverageType,
                CoverageAmount = createInsurancePolicyDto.CoverageAmount,
                Currency = createInsurancePolicyDto.Currency,
                DocumentReferenceId = createInsurancePolicyDto.DocumentReferenceId,
                Notes = createInsurancePolicyDto.Notes,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
            };

            _ = _context.InsurancePolicies.Add(policy);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(policy, "Insert", currentUser, null, cancellationToken);

            var createdPolicy = await _context.InsurancePolicies
                .Include(ip => ip.DocumentReference)
                .Include(ip => ip.TeamMember)
                .FirstAsync(ip => ip.Id == policy.Id, cancellationToken);

            return MapToInsurancePolicyDto(createdPolicy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating insurance policy");
            throw;
        }
    }

    public async Task<InsurancePolicyDto?> UpdateInsurancePolicyAsync(Guid id, UpdateInsurancePolicyDto updateInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateInsurancePolicyDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var policy = await _context.InsurancePolicies
                .Where(ip => ip.Id == id && !ip.IsDeleted)
                .Include(ip => ip.DocumentReference)
                .Include(ip => ip.TeamMember)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy == null)
            {
                _logger.LogWarning("Insurance policy {PolicyId} not found for update", id);
                return null;
            }

            var originalPolicy = await _context.InsurancePolicies
                .AsNoTracking()
                .Where(ip => ip.Id == id && !ip.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            policy.Provider = updateInsurancePolicyDto.Provider ?? policy.Provider;
            policy.PolicyNumber = updateInsurancePolicyDto.PolicyNumber ?? policy.PolicyNumber;
            policy.ValidFrom = updateInsurancePolicyDto.ValidFrom ?? policy.ValidFrom;
            policy.ValidTo = updateInsurancePolicyDto.ValidTo ?? policy.ValidTo;
            policy.CoverageType = updateInsurancePolicyDto.CoverageType;
            policy.CoverageAmount = updateInsurancePolicyDto.CoverageAmount;
            policy.Currency = updateInsurancePolicyDto.Currency;
            policy.DocumentReferenceId = updateInsurancePolicyDto.DocumentReferenceId;
            policy.Notes = updateInsurancePolicyDto.Notes;
            policy.ModifiedBy = currentUser;
            policy.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(policy, "Update", currentUser, originalPolicy, cancellationToken);

            return MapToInsurancePolicyDto(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating insurance policy {PolicyId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteInsurancePolicyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var policy = await _context.InsurancePolicies
                .Where(ip => ip.Id == id && !ip.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy == null)
            {
                _logger.LogWarning("Insurance policy {PolicyId} not found for deletion", id);
                return false;
            }

            var originalPolicy = await _context.InsurancePolicies
                .AsNoTracking()
                .Where(ip => ip.Id == id && !ip.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            policy.IsDeleted = true;
            policy.DeletedBy = currentUser;
            policy.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(policy, "Delete", currentUser, originalPolicy, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting insurance policy {PolicyId}", id);
            throw;
        }
    }

    // Business Logic Methods

    public async Task<bool> ValidateJerseyNumberAsync(Guid teamId, int jerseyNumber, Guid? excludeTeamMemberId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.TeamMembers
                .Where(tm => tm.TeamId == teamId && tm.JerseyNumber == jerseyNumber && !tm.IsDeleted);

            if (excludeTeamMemberId.HasValue)
            {
                query = query.Where(tm => tm.Id != excludeTeamMemberId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);
            return !exists; // Return true if number is available (not taken)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating jersey number {JerseyNumber} for team {TeamId}", jerseyNumber, teamId);
            throw;
        }
    }

    public async Task<IEnumerable<TeamMemberDto>> GetMembersWithExpiringDocumentsAsync(int daysBeforeExpiry = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiryDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

            var membersWithExpiringDocs = await _context.TeamMembers
                .Where(tm => !tm.IsDeleted)
                .Include(tm => tm.Team)
                .Include(tm => tm.MembershipCards.Where(mc => !mc.IsDeleted && mc.ValidTo <= expiryDate))
                .Include(tm => tm.InsurancePolicies.Where(ip => !ip.IsDeleted && ip.ValidTo <= expiryDate))
                .Where(tm => tm.MembershipCards.Any(mc => !mc.IsDeleted && mc.ValidTo <= expiryDate) ||
                            tm.InsurancePolicies.Any(ip => !ip.IsDeleted && ip.ValidTo <= expiryDate))
                .ToListAsync(cancellationToken);

            return membersWithExpiringDocs.Select(MapToTeamMemberDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members with expiring documents");
            throw;
        }
    }

    public async Task<EligibilityValidationResult> ValidateTeamMemberEligibilityAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new EligibilityValidationResult
            {
                IsEligible = true,
                ValidatedAt = DateTime.UtcNow
            };

            var member = await _context.TeamMembers
                .Where(tm => tm.Id == teamMemberId && !tm.IsDeleted)
                .Include(tm => tm.MembershipCards.Where(mc => !mc.IsDeleted))
                .Include(tm => tm.InsurancePolicies.Where(ip => !ip.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
            {
                result.IsEligible = false;
                result.Issues.Add(new EligibilityIssue
                {
                    Type = EligibilityIssueType.InvalidData,
                    Severity = EligibilityIssueSeverity.Critical,
                    Description = "Team member not found",
                    Field = "TeamMemberId"
                });
                return result;
            }

            // Check for valid membership card
            var validMembershipCard = member.MembershipCards
                .FirstOrDefault(mc => mc.IsValid);

            if (validMembershipCard == null)
            {
                result.IsEligible = false;
                result.Issues.Add(new EligibilityIssue
                {
                    Type = EligibilityIssueType.MissingDocument,
                    Severity = EligibilityIssueSeverity.Error,
                    Description = "No valid membership card found",
                    Field = "MembershipCard",
                    SuggestedAction = "Add a valid membership card"
                });
            }

            // Check for valid insurance policy
            var validInsurancePolicy = member.InsurancePolicies
                .FirstOrDefault(ip => ip.IsValid);

            if (validInsurancePolicy == null)
            {
                result.Issues.Add(new EligibilityIssue
                {
                    Type = EligibilityIssueType.MissingDocument,
                    Severity = EligibilityIssueSeverity.Warning,
                    Description = "No valid insurance policy found",
                    Field = "InsurancePolicy",
                    SuggestedAction = "Add a valid insurance policy"
                });
            }

            // Check for expiring documents
            var expiringCards = member.MembershipCards
                .Where(mc => mc.DaysUntilExpiration <= 30 && mc.DaysUntilExpiration > 0);

            foreach (var card in expiringCards)
            {
                result.Warnings.Add(new EligibilityIssue
                {
                    Type = EligibilityIssueType.ExpiredDocument,
                    Severity = EligibilityIssueSeverity.Warning,
                    Description = $"Membership card expires in {card.DaysUntilExpiration} days",
                    Field = "MembershipCard",
                    DueDate = card.ValidTo,
                    SuggestedAction = "Renew membership card before expiry"
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating team member eligibility for member {TeamMemberId}", teamMemberId);
            throw;
        }
    }

    // Private mapping methods

    private static TeamDto MapToTeamDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            MemberCount = team.Members.Count(m => !m.IsDeleted),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    private static TeamDetailDto MapToTeamDetailDto(Team team)
    {
        return new TeamDetailDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            Members = team.Members.Where(m => !m.IsDeleted).Select(MapToTeamMemberDto).ToList(),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    private static TeamMemberDto MapToTeamMemberDto(TeamMember member)
    {
        return new TeamMemberDto
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Role = member.Role,
            DateOfBirth = member.DateOfBirth,
            TeamId = member.TeamId,
            TeamName = member.Team?.Name,
            CreatedAt = member.CreatedAt,
            CreatedBy = member.CreatedBy,
            ModifiedAt = member.ModifiedAt,
            ModifiedBy = member.ModifiedBy
        };
    }

    private static DocumentReferenceDto MapToDocumentReferenceDto(DocumentReference document)
    {
        return new DocumentReferenceDto
        {
            Id = document.Id,
            OwnerId = document.OwnerId,
            OwnerType = document.OwnerType,
            FileName = document.FileName,
            Type = document.Type,
            SubType = document.SubType,
            MimeType = document.MimeType,
            StorageKey = document.StorageKey,
            Url = document.Url,
            ThumbnailStorageKey = document.ThumbnailStorageKey,
            Expiry = document.Expiry,
            FileSizeBytes = document.FileSizeBytes,
            Title = document.Title,
            Notes = document.Notes,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy,
            ModifiedAt = document.ModifiedAt,
            ModifiedBy = document.ModifiedBy
        };
    }

    private static MembershipCardDto MapToMembershipCardDto(MembershipCard card)
    {
        return new MembershipCardDto
        {
            Id = card.Id,
            TeamMemberId = card.TeamMemberId,
            TeamMemberName = card.TeamMember != null ? $"{card.TeamMember.FirstName} {card.TeamMember.LastName}" : null,
            CardNumber = card.CardNumber,
            Federation = card.Federation,
            ValidFrom = card.ValidFrom,
            ValidTo = card.ValidTo,
            DocumentReferenceId = card.DocumentReferenceId,
            Category = card.Category,
            Notes = card.Notes,
            CreatedAt = card.CreatedAt,
            CreatedBy = card.CreatedBy,
            ModifiedAt = card.ModifiedAt,
            ModifiedBy = card.ModifiedBy
        };
    }

    private static InsurancePolicyDto MapToInsurancePolicyDto(InsurancePolicy policy)
    {
        return new InsurancePolicyDto
        {
            Id = policy.Id,
            TeamMemberId = policy.TeamMemberId,
            TeamMemberName = policy.TeamMember != null ? $"{policy.TeamMember.FirstName} {policy.TeamMember.LastName}" : null,
            Provider = policy.Provider,
            PolicyNumber = policy.PolicyNumber,
            ValidFrom = policy.ValidFrom,
            ValidTo = policy.ValidTo,
            CoverageType = policy.CoverageType,
            CoverageAmount = policy.CoverageAmount,
            Currency = policy.Currency,
            DocumentReferenceId = policy.DocumentReferenceId,
            Notes = policy.Notes,
            CreatedAt = policy.CreatedAt,
            CreatedBy = policy.CreatedBy,
            ModifiedAt = policy.ModifiedAt,
            ModifiedBy = policy.ModifiedBy
        };
    }
}