using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;

namespace EventForge.Tests.Services.Common;

/// <summary>
/// Cross-tenant isolation tests for <see cref="AddressService"/>, <see cref="ContactService"/>
/// and <see cref="ReferenceService"/>, verifying that single-record get/update/delete operations
/// cannot read or mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class CommonServicesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _addressAId;
    private readonly Guid _contactAId;
    private readonly Guid _referenceAId;

    public CommonServicesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _addressAId = Guid.NewGuid();
        _contactAId = Guid.NewGuid();
        _referenceAId = Guid.NewGuid();

        _context.Addresses.Add(new Address
        {
            Id = _addressAId,
            TenantId = _tenantAId,
            OwnerId = Guid.NewGuid(),
            OwnerType = "BusinessParty",
            AddressType = AddressType.Legal,
            City = "Milano"
        });

        _context.Contacts.Add(new Contact
        {
            Id = _contactAId,
            TenantId = _tenantAId,
            OwnerId = Guid.NewGuid(),
            OwnerType = "BusinessParty",
            ContactType = ContactType.Email,
            Value = "a@example.com"
        });

        _context.References.Add(new Reference
        {
            Id = _referenceAId,
            TenantId = _tenantAId,
            OwnerId = Guid.NewGuid(),
            FirstName = "Mario",
            LastName = "Rossi"
        });

        _context.SaveChanges();
    }

    private ITenantContext CreateTenantContext(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);
        return mock.Object;
    }

    private AddressService CreateAddressService(Guid? currentTenantId) =>
        new(_context, new Mock<IAuditLogService>().Object, CreateTenantContext(currentTenantId), new Mock<ILogger<AddressService>>().Object);

    private ContactService CreateContactService(Guid? currentTenantId) =>
        new(_context, new Mock<IAuditLogService>().Object, CreateTenantContext(currentTenantId), new Mock<ILogger<ContactService>>().Object);

    private ReferenceService CreateReferenceService(Guid? currentTenantId) =>
        new(_context, new Mock<IAuditLogService>().Object, CreateTenantContext(currentTenantId), new Mock<ILogger<ReferenceService>>().Object);

    // ---- AddressService ----

    [Fact]
    public async Task GetAddressByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateAddressService(_tenantBId);
        Assert.Null(await service.GetAddressByIdAsync(_addressAId));
    }

    [Fact]
    public async Task UpdateAddressAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateAddressService(_tenantBId);
        var dto = new UpdateAddressDto { AddressType = AddressType.Legal, City = "Hacked" };

        var result = await service.UpdateAddressAsync(_addressAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.Addresses.AsNoTracking().FirstAsync(a => a.Id == _addressAId);
        Assert.Equal("Milano", unchanged.City);
    }

    [Fact]
    public async Task DeleteAddressAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateAddressService(_tenantBId);

        var result = await service.DeleteAddressAsync(_addressAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.Addresses.AsNoTracking().FirstAsync(a => a.Id == _addressAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task GetAddressByIdAsync_MissingTenant_Throws()
    {
        var service = CreateAddressService(null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAddressByIdAsync(_addressAId));
    }

    // ---- ContactService ----

    [Fact]
    public async Task GetContactByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateContactService(_tenantBId);
        Assert.Null(await service.GetContactByIdAsync(_contactAId));
    }

    [Fact]
    public async Task UpdateContactAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateContactService(_tenantBId);
        var dto = new UpdateContactDto { ContactType = ContactType.Email, Value = "hacked@example.com" };

        var result = await service.UpdateContactAsync(_contactAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.Contacts.AsNoTracking().FirstAsync(c => c.Id == _contactAId);
        Assert.Equal("a@example.com", unchanged.Value);
    }

    [Fact]
    public async Task DeleteContactAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateContactService(_tenantBId);

        var result = await service.DeleteContactAsync(_contactAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.Contacts.AsNoTracking().FirstAsync(c => c.Id == _contactAId);
        Assert.False(unchanged.IsDeleted);
    }

    // ---- ReferenceService ----

    [Fact]
    public async Task GetReferenceByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateReferenceService(_tenantBId);
        Assert.Null(await service.GetReferenceByIdAsync(_referenceAId));
    }

    [Fact]
    public async Task UpdateReferenceAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateReferenceService(_tenantBId);
        var dto = new UpdateReferenceDto { FirstName = "Hacked", LastName = "Hacked" };

        var result = await service.UpdateReferenceAsync(_referenceAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.References.AsNoTracking().FirstAsync(r => r.Id == _referenceAId);
        Assert.Equal("Mario", unchanged.FirstName);
    }

    [Fact]
    public async Task DeleteReferenceAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateReferenceService(_tenantBId);

        var result = await service.DeleteReferenceAsync(_referenceAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.References.AsNoTracking().FirstAsync(r => r.Id == _referenceAId);
        Assert.False(unchanged.IsDeleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
