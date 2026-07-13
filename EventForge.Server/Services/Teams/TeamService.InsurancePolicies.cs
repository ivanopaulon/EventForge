using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<IEnumerable<InsurancePolicyDto>> GetInsurancePoliciesByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var policies = await context.InsurancePolicies
            .AsNoTracking()
            .Where(ip => ip.TeamMemberId == teamMemberId && ip.TenantId == currentTenantId && !ip.IsDeleted)
            .Include(ip => ip.DocumentReference)
            .OrderBy(ip => ip.ValidFrom)
            .ToListAsync(cancellationToken);

        return policies.Select(MapToInsurancePolicyDto);
    }

    public async Task<InsurancePolicyDto?> GetInsurancePolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var policy = await context.InsurancePolicies
            .AsNoTracking()
            .Where(ip => ip.Id == id && ip.TenantId == currentTenantId && !ip.IsDeleted)
            .Include(ip => ip.DocumentReference)
            .Include(ip => ip.TeamMember)
            .FirstOrDefaultAsync(cancellationToken);

        return policy is not null ? MapToInsurancePolicyDto(policy) : null;
    }

    public async Task<InsurancePolicyDto> CreateInsurancePolicyAsync(CreateInsurancePolicyDto createInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default)
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
            TenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
        };

        _ = context.InsurancePolicies.Add(policy);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(policy, "Insert", currentUser, null, cancellationToken);

        var createdPolicy = await context.InsurancePolicies
            .Include(ip => ip.DocumentReference)
            .Include(ip => ip.TeamMember)
            .FirstAsync(ip => ip.Id == policy.Id, cancellationToken);

        return MapToInsurancePolicyDto(createdPolicy);
    }

    public async Task<InsurancePolicyDto?> UpdateInsurancePolicyAsync(Guid id, UpdateInsurancePolicyDto updateInsurancePolicyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInsurancePolicyDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var policy = await context.InsurancePolicies
            .Where(ip => ip.Id == id && ip.TenantId == currentTenantId && !ip.IsDeleted)
            .Include(ip => ip.DocumentReference)
            .Include(ip => ip.TeamMember)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            logger.LogWarning("Insurance policy {PolicyId} not found for update", id);
            return null;
        }

        var originalPolicy = await context.InsurancePolicies
            .AsNoTracking()
            .Where(ip => ip.Id == id && ip.TenantId == currentTenantId && !ip.IsDeleted)
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

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(policy, "Update", currentUser, originalPolicy, cancellationToken);

        return MapToInsurancePolicyDto(policy);
    }

    public async Task<bool> DeleteInsurancePolicyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var policy = await context.InsurancePolicies
            .Where(ip => ip.Id == id && ip.TenantId == currentTenantId && !ip.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            logger.LogWarning("Insurance policy {PolicyId} not found for deletion", id);
            return false;
        }

        var originalPolicy = await context.InsurancePolicies
            .AsNoTracking()
            .Where(ip => ip.Id == id && ip.TenantId == currentTenantId && !ip.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        policy.IsDeleted = true;
        policy.DeletedBy = currentUser;
        policy.DeletedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(policy, "Delete", currentUser, originalPolicy, cancellationToken);

        return true;
    }

    // Business Logic Methods

}
