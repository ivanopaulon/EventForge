using EventForge.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Address = EventForge.Server.Data.Entities.Common.Address;
using Contact = EventForge.Server.Data.Entities.Common.Contact;
using PriceList = EventForge.Server.Data.Entities.PriceList.PriceList;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;

namespace EventForge.Tests.Services.Business;

/// <summary>
/// Unit tests for BusinessPartyService GetFullDetailAsync method.
/// FASE 5: Tests for the aggregated full-detail endpoint.
/// </summary>
[Trait("Category", "Unit")]
public class BusinessPartyService_FullDetailTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<BusinessPartyService>> _mockLogger;
    private readonly BusinessPartyService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly string _testUser = "test-user";

    public BusinessPartyService_FullDetailTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<BusinessPartyService>>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create service
        _service = new BusinessPartyService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetFullDetailAsync_ValidId_ReturnsCompleteData()
    {
        // Arrange
        var businessPartyId = await SeedTestDataAsync();

        // Act
        var result = await _service.GetFullDetailAsync(businessPartyId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.BusinessParty);
        Assert.Equal(businessPartyId, result.BusinessParty.Id);
        Assert.Equal("Test Company", result.BusinessParty.Name);

        // Verify contacts are loaded
        Assert.NotEmpty(result.Contacts);
        Assert.Equal(2, result.Contacts.Count);

        // Verify addresses are loaded
        Assert.NotEmpty(result.Addresses);
        Assert.Single(result.Addresses);

        // Verify statistics are populated
        Assert.NotNull(result.Statistics);
        Assert.Equal(2, result.Statistics.TotalContacts);
        Assert.Equal(1, result.Statistics.TotalAddresses);
    }

    [Fact]
    public async Task GetFullDetailAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetFullDetailAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFullDetailAsync_Statistics_AreAccurate()
    {
        // Arrange
        var businessPartyId = await SeedTestDataWithKnownCountsAsync(
            contactCount: 3,
            addressCount: 2,
            priceListCount: 1,
            documentCount: 5);

        // Act
        var result = await _service.GetFullDetailAsync(businessPartyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Statistics.TotalContacts);
        Assert.Equal(2, result.Statistics.TotalAddresses);
        Assert.Equal(1, result.Statistics.TotalPriceLists);
        Assert.Equal(5, result.Statistics.TotalDocuments);
    }

    [Fact]
    public async Task GetFullDetailAsync_IncludeInactive_IncludesDeletedRecords()
    {
        // Arrange
        var businessPartyId = await SeedTestDataWithInactiveAsync();

        // Act - without inactive
        var resultWithoutInactive = await _service.GetFullDetailAsync(businessPartyId, includeInactive: false);

        // Act - with inactive
        var resultWithInactive = await _service.GetFullDetailAsync(businessPartyId, includeInactive: true);

        // Assert
        Assert.NotNull(resultWithoutInactive);
        Assert.NotNull(resultWithInactive);
        Assert.True(resultWithInactive.Contacts.Count > resultWithoutInactive.Contacts.Count);
    }

    [Fact]
    public async Task GetFullDetailAsync_PriceLists_OnlyActiveReturned()
    {
        // Arrange
        var businessPartyId = await SeedTestDataWithPriceListsAsync();

        // Act
        var result = await _service.GetFullDetailAsync(businessPartyId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.AssignedPriceLists); // Only active price list
        Assert.All(result.AssignedPriceLists, pl =>
            Assert.Equal(PriceListStatus.Active, pl.Status));
    }

    [Fact]
    public async Task GetFullDetailAsync_Contacts_OrderedByPrimary()
    {
        // Arrange
        var businessPartyId = await SeedTestDataAsync();

        // Act
        var result = await _service.GetFullDetailAsync(businessPartyId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Contacts);

        // First contact should be primary
        Assert.True(result.Contacts.First().IsPrimary);
    }

    #region Helper Methods

    /// <summary>
    /// Seeds basic test data with 2 contacts, 1 address
    /// </summary>
    private async Task<Guid> SeedTestDataAsync()
    {
        var businessPartyId = Guid.NewGuid();

        var businessParty = new BusinessParty
        {
            Id = businessPartyId,
            Name = "Test Company",
            TaxCode = "TC123456",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _context.BusinessParties.Add(businessParty);

        // Add contacts
        _context.Contacts.Add(new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = businessPartyId,
            OwnerType = "BusinessParty",
            ContactType = ContactType.Email,
            Value = "primary@test.com",
            IsPrimary = true,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _context.Contacts.Add(new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = businessPartyId,
            OwnerType = "BusinessParty",
            ContactType = ContactType.Phone,
            Value = "+1234567890",
            IsPrimary = false,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Add address
        _context.Addresses.Add(new Address
        {
            Id = Guid.NewGuid(),
            OwnerId = businessPartyId,
            OwnerType = "BusinessParty",
            AddressType = AddressType.Legal,
            Street = "123 Main St",
            City = "Test City",
            ZipCode = "12345",
            Country = "Test Country",
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();
        return businessPartyId;
    }

    /// <summary>
    /// Seeds test data with known counts for statistics verification
    /// </summary>
    private async Task<Guid> SeedTestDataWithKnownCountsAsync(
        int contactCount,
        int addressCount,
        int priceListCount,
        int documentCount)
    {
        var businessPartyId = Guid.NewGuid();

        var businessParty = new BusinessParty
        {
            Id = businessPartyId,
            Name = "Stats Test Company",
            TaxCode = "STC123456",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _context.BusinessParties.Add(businessParty);

        // Add contacts
        for (int i = 0; i < contactCount; i++)
        {
            _context.Contacts.Add(new Contact
            {
                Id = Guid.NewGuid(),
                OwnerId = businessPartyId,
                OwnerType = "BusinessParty",
                ContactType = ContactType.Email,
                Value = $"contact{i}@test.com",
                IsPrimary = i == 0,
                TenantId = _tenantId,
                CreatedBy = _testUser,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        // Add addresses
        for (int i = 0; i < addressCount; i++)
        {
            _context.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                OwnerId = businessPartyId,
                OwnerType = "BusinessParty",
                AddressType = AddressType.Legal,
                Street = $"{i} Main St",
                City = "Test City",
                TenantId = _tenantId,
                CreatedBy = _testUser,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        // Add price lists
        for (int i = 0; i < priceListCount; i++)
        {
            var priceListId = Guid.NewGuid();
            _context.PriceLists.Add(new PriceList
            {
                Id = priceListId,
                Name = $"Price List {i}",
                Code = $"PL{i}",
                Description = $"Test price list {i}",
                Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
                TenantId = _tenantId,
                CreatedBy = _testUser,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });

            _context.PriceListBusinessParties.Add(new PriceListBusinessParty
            {
                Id = Guid.NewGuid(),
                PriceListId = priceListId,
                BusinessPartyId = businessPartyId,
                Status = Server.Data.Entities.PriceList.PriceListBusinessPartyStatus.Active,
                TenantId = _tenantId,
                CreatedBy = _testUser,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        // Add documents
        var documentTypeId = Guid.NewGuid();
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = documentTypeId,
            Name = "Invoice",
            Code = "INV",
            IsStockIncrease = false,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        for (int i = 0; i < documentCount; i++)
        {
            _context.DocumentHeaders.Add(new DocumentHeader
            {
                Id = Guid.NewGuid(),
                DocumentTypeId = documentTypeId,
                BusinessPartyId = businessPartyId,
                Number = $"INV-{i:D5}",
                Date = DateTime.UtcNow.AddDays(-i),
                TotalGrossAmount = 100m * (i + 1),
                TenantId = _tenantId,
                CreatedBy = _testUser,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();
        return businessPartyId;
    }

    /// <summary>
    /// Seeds test data with inactive/deleted records
    /// </summary>
    private async Task<Guid> SeedTestDataWithInactiveAsync()
    {
        var businessPartyId = Guid.NewGuid();

        var businessParty = new BusinessParty
        {
            Id = businessPartyId,
            Name = "Inactive Test Company",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _context.BusinessParties.Add(businessParty);

        // Add active contact
        _context.Contacts.Add(new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = businessPartyId,
            OwnerType = "BusinessParty",
            ContactType = ContactType.Email,
            Value = "active@test.com",
            IsPrimary = true,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Add inactive contact
        _context.Contacts.Add(new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = businessPartyId,
            OwnerType = "BusinessParty",
            ContactType = ContactType.Email,
            Value = "inactive@test.com",
            IsPrimary = false,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true // Deleted/inactive
        });

        await _context.SaveChangesAsync();
        return businessPartyId;
    }

    /// <summary>
    /// Seeds test data with both active and inactive price lists
    /// </summary>
    private async Task<Guid> SeedTestDataWithPriceListsAsync()
    {
        var businessPartyId = Guid.NewGuid();

        var businessParty = new BusinessParty
        {
            Id = businessPartyId,
            Name = "PriceList Test Company",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _context.BusinessParties.Add(businessParty);

        // Add active price list
        var activePriceListId = Guid.NewGuid();
        _context.PriceLists.Add(new PriceList
        {
            Id = activePriceListId,
            Name = "Active Price List",
            Code = "ACTIVE",
            Description = "Active price list",
            Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            Id = Guid.NewGuid(),
            PriceListId = activePriceListId,
            BusinessPartyId = businessPartyId,
            Status = Server.Data.Entities.PriceList.PriceListBusinessPartyStatus.Active,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Add inactive price list
        var inactivePriceListId = Guid.NewGuid();
        _context.PriceLists.Add(new PriceList
        {
            Id = inactivePriceListId,
            Name = "Inactive Price List",
            Code = "INACTIVE",
            Description = "Inactive price list",
            Status = Server.Data.Entities.PriceList.PriceListStatus.Suspended,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            Id = Guid.NewGuid(),
            PriceListId = inactivePriceListId,
            BusinessPartyId = businessPartyId,
            Status = Server.Data.Entities.PriceList.PriceListBusinessPartyStatus.Active,
            TenantId = _tenantId,
            CreatedBy = _testUser,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();
        return businessPartyId;
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
