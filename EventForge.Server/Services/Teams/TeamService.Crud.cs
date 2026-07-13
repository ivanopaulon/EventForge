using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto createTeamDto, string currentUser, CancellationToken cancellationToken = default)
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

        _ = context.Teams.Add(team);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Audit log
        _ = await auditLogService.TrackEntityChangesAsync(team, "Insert", currentUser, null, cancellationToken);

        var createdTeam = await context.Teams
            .Include(t => t.Event)
            .Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id, cancellationToken);

        return MapToTeamDto(createdTeam);
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto updateTeamDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateTeamDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for team operations.");

            var team = await context.Teams
                .Where(t => t.Id == id && t.TenantId == currentTenantId && !t.IsDeleted)
                .Include(t => t.Event)
                .Include(t => t.Members.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(cancellationToken);

            if (team is null)
            {
                logger.LogWarning("Team con ID {TeamId} non trovato per update da parte di {User}.", id, currentUser);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(team).CurrentValues.Clone();
            var originalTeam = (Team)originalValues.ToObject();

            team.Name = updateTeamDto.Name;
            team.ShortDescription = updateTeamDto.ShortDescription;
            team.LongDescription = updateTeamDto.LongDescription;
            team.Email = updateTeamDto.Email;
            team.ModifiedBy = currentUser;
            team.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating team {TeamId}.", id);
                throw new InvalidOperationException("Il team è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log
            _ = await auditLogService.TrackEntityChangesAsync(team, "Update", currentUser, originalTeam, cancellationToken);

            return MapToTeamDto(team);
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

    public async Task<bool> DeleteTeamAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId
                ?? throw new InvalidOperationException("Tenant context is required for team operations.");

            var team = await context.Teams
                .Where(t => t.Id == id && t.TenantId == currentTenantId && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (team is null)
            {
                logger.LogWarning("Team con ID {TeamId} non trovato per cancellazione da parte di {User}.", id, currentUser);
                return false;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(team).CurrentValues.Clone();
            var originalTeam = (Team)originalValues.ToObject();

            team.IsDeleted = true;
            team.DeletedBy = currentUser;
            team.DeletedAt = DateTime.UtcNow;

            var members = await context.TeamMembers
                .Where(m => m.TeamId == id && !m.IsDeleted)
                .ToListAsync(cancellationToken);

            // Create snapshots of all members BEFORE modifying them
            var originalMembers = members.ToDictionary(
                m => m.Id,
                m =>
                {
                    var originalMemberValues = context.Entry(m).CurrentValues.Clone();
                    return (TeamMember)originalMemberValues.ToObject();
                }
            );

            foreach (var member in members)
            {
                var originalMember = originalMembers[member.Id];

                member.IsDeleted = true;
                member.DeletedBy = currentUser;
                member.DeletedAt = DateTime.UtcNow;

                // Audit log per ogni membro eliminato
                _ = await auditLogService.TrackEntityChangesAsync(member, "Delete", currentUser, originalMember, cancellationToken);
            }

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting team {TeamId}.", id);
                throw new InvalidOperationException("Il team è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log per il team eliminato
            _ = await auditLogService.TrackEntityChangesAsync(team, "Delete", currentUser, originalTeam, cancellationToken);

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

    // Team Member operations

}
