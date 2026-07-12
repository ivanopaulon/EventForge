using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Teams;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Teams;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.Teams;

namespace EventForge.Tests.Services.Teams;

/// <summary>
/// Cross-tenant isolation tests for <see cref="TeamService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate
/// resources belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 1).
/// </summary>
public class TeamServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _teamAId;
    private readonly Guid _teamMemberAId;
    private readonly Guid _documentReferenceAId;
    private readonly Guid _membershipCardAId;
    private readonly Guid _insurancePolicyAId;

    public TeamServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _teamAId = Guid.NewGuid();
        _teamMemberAId = Guid.NewGuid();
        _documentReferenceAId = Guid.NewGuid();
        _membershipCardAId = Guid.NewGuid();
        _insurancePolicyAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.Teams.Add(new Team
        {
            Id = _teamAId,
            TenantId = _tenantAId,
            Name = "Team A",
            Email = "team-a@example.com"
        });

        _context.TeamMembers.Add(new TeamMember
        {
            Id = _teamMemberAId,
            TenantId = _tenantAId,
            TeamId = _teamAId,
            FirstName = "Alice",
            LastName = "Anderson",
            FiscalCode = "AAAAAA00A00A000A"
        });

        _context.DocumentReferences.Add(new DocumentReference
        {
            Id = _documentReferenceAId,
            TenantId = _tenantAId,
            OwnerId = _teamMemberAId,
            OwnerType = "TeamMember",
            FileName = "doc-a.pdf",
            Type = DocumentReferenceType.IdentityDocument
        });

        _context.MembershipCards.Add(new MembershipCard
        {
            Id = _membershipCardAId,
            TenantId = _tenantAId,
            TeamMemberId = _teamMemberAId,
            CardNumber = "CARD-A-001",
            ValidFrom = DateTime.UtcNow
        });

        _context.InsurancePolicies.Add(new InsurancePolicy
        {
            Id = _insurancePolicyAId,
            TenantId = _tenantAId,
            TeamMemberId = _teamMemberAId,
            Provider = "Provider A",
            PolicyNumber = "POLICY-A-001",
            ValidFrom = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    private TeamService CreateService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new TeamService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<TeamService>>().Object);
    }

    [Fact]
    public async Task GetTeamByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetTeamByIdAsync(_teamAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTeamDetailAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetTeamDetailAsync(_teamAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTeamAsync_FromOtherTenant_ReturnsNullAndDoesNotModify()
    {
        var service = CreateService(_tenantBId);

        var updateDto = new UpdateTeamDto { Name = "Hijacked Name" };
        var result = await service.UpdateTeamAsync(_teamAId, updateDto, "attacker");

        Assert.Null(result);

        var untouched = await _context.Teams.AsNoTracking().FirstAsync(t => t.Id == _teamAId);
        Assert.Equal("Team A", untouched.Name);
    }

    [Fact]
    public async Task DeleteTeamAsync_FromOtherTenant_ReturnsFalseAndDoesNotDelete()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteTeamAsync(_teamAId, "attacker");

        Assert.False(result);

        var untouched = await _context.Teams.AsNoTracking().FirstAsync(t => t.Id == _teamAId);
        Assert.False(untouched.IsDeleted);
    }

    [Fact]
    public async Task GetTeamMemberByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetTeamMemberByIdAsync(_teamMemberAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTeamMemberAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var updateDto = new UpdateTeamMemberDto { FirstName = "Hacked", LastName = "Anderson" };
        var result = await service.UpdateTeamMemberAsync(_teamMemberAId, updateDto, "attacker");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveTeamMemberAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.RemoveTeamMemberAsync(_teamMemberAId, "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task GetDocumentReferenceByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetDocumentReferenceByIdAsync(_documentReferenceAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteDocumentReferenceAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteDocumentReferenceAsync(_documentReferenceAId, "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task GetMembershipCardByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetMembershipCardByIdAsync(_membershipCardAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteMembershipCardAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteMembershipCardAsync(_membershipCardAId, "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task GetInsurancePolicyByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetInsurancePolicyByIdAsync(_insurancePolicyAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteInsurancePolicyAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteInsurancePolicyAsync(_insurancePolicyAId, "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task GetOtherActiveTeamsForFiscalCodeAsync_WithoutTenantContext_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetOtherActiveTeamsForFiscalCodeAsync("AAAAAA00A00A000A", Guid.NewGuid()));
    }

    [Fact]
    public async Task GetOtherActiveTeamsForFiscalCodeAsync_FromOtherTenant_DoesNotLeakOtherTenantMembers()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetOtherActiveTeamsForFiscalCodeAsync("AAAAAA00A00A000A", Guid.NewGuid());

        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
