using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var members = await context.TeamMembers
            .AsNoTracking()
            .Where(m => m.TeamId == teamId && !m.IsDeleted)
            .Include(m => m.Team)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return members.Select(MapToTeamMemberDto);
    }

    public async Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var member = await context.TeamMembers
            .AsNoTracking()
            .Where(m => m.Id == id && m.TenantId == currentTenantId && !m.IsDeleted)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            logger.LogWarning("Team member con ID {MemberId} non trovato.", id);
            return null;
        }

        return MapToTeamMemberDto(member);
    }

    public async Task<TeamMemberDto> AddTeamMemberAsync(CreateTeamMemberDto createTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
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
            FiscalCode = createTeamMemberDto.FiscalCode,
            Status = ToEntityTeamMemberStatus(createTeamMemberDto.Status),
            Position = createTeamMemberDto.Position,
            JerseyNumber = createTeamMemberDto.JerseyNumber,
            EligibilityStatus = createTeamMemberDto.EligibilityStatus,
            PhotoDocumentId = createTeamMemberDto.PhotoDocumentId,
            PhotoConsent = createTeamMemberDto.PhotoConsent,
            PhotoConsentAt = createTeamMemberDto.PhotoConsentAt,
            TeamId = createTeamMemberDto.TeamId,
            TenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("TenantId is required but was not found in the current tenant context."),
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };

        _ = context.TeamMembers.Add(member);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log
        _ = await auditLogService.TrackEntityChangesAsync(member, "Insert", currentUser, null, cancellationToken);

        var createdMember = await context.TeamMembers
            .Include(m => m.Team)
            .FirstAsync(m => m.Id == member.Id, cancellationToken);

        return MapToTeamMemberDto(createdMember);
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberDto updateTeamMemberDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamMemberDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for team operations.");

            var member = await context.TeamMembers
                .Where(m => m.Id == id && m.TenantId == currentTenantId && !m.IsDeleted)
                .Include(m => m.Team)
                .FirstOrDefaultAsync(cancellationToken);

            if (member is null)
            {
                logger.LogWarning("Team member con ID {MemberId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            var originalMember = await context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && m.TenantId == currentTenantId && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.FirstName = updateTeamMemberDto.FirstName;
            member.LastName = updateTeamMemberDto.LastName;
            member.Email = updateTeamMemberDto.Email;
            member.Role = updateTeamMemberDto.Role;
            member.DateOfBirth = updateTeamMemberDto.DateOfBirth;
            member.FiscalCode = updateTeamMemberDto.FiscalCode;
            member.Status = ToEntityTeamMemberStatus(updateTeamMemberDto.Status);
            member.Position = updateTeamMemberDto.Position;
            member.JerseyNumber = updateTeamMemberDto.JerseyNumber;
            member.EligibilityStatus = updateTeamMemberDto.EligibilityStatus;
            member.PhotoDocumentId = updateTeamMemberDto.PhotoDocumentId;
            member.PhotoConsent = updateTeamMemberDto.PhotoConsent;
            member.PhotoConsentAt = updateTeamMemberDto.PhotoConsentAt;
            member.ModifiedBy = currentUser;
            member.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating team member {MemberId}.", id);
                throw new InvalidOperationException("Il membro del team è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log
            _ = await auditLogService.TrackEntityChangesAsync(member, "Update", currentUser, originalMember, cancellationToken);

            return MapToTeamMemberDto(member);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for team operations.");

            var member = await context.TeamMembers
                .Where(m => m.Id == id && m.TenantId == currentTenantId && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (member is null)
            {
                logger.LogWarning("Team member con ID {MemberId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            var originalMember = await context.TeamMembers
                .AsNoTracking()
                .Where(m => m.Id == id && m.TenantId == currentTenantId && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            member.IsDeleted = true;
            member.DeletedBy = currentUser;
            member.DeletedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict removing team member {MemberId}.", id);
                throw new InvalidOperationException("Il membro del team è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log
            _ = await auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await context.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == teamId && !t.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<TeamMemberDto>> GetMembersWithBirthdayAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue) return Enumerable.Empty<TeamMemberDto>();

        var members = await context.TeamMembers
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.DateOfBirth.HasValue && m.TenantId == tenantId.Value)
            .Include(m => m.Team)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return members.Select(MapToTeamMemberDto);
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await context.Events
            .AsNoTracking()
            .AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
    }

    // Document Reference operations

}
