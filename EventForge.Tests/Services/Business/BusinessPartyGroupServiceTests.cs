using Prym.DTOs.Business;
using Prym.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Business
{
    /// <summary>
    /// Unit tests for BusinessPartyGroupService.
    /// </summary>
    [Trait("Category", "Unit")]
    public class BusinessPartyGroupServiceTests : IDisposable
    {
        private readonly EventForgeDbContext _context;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<BusinessPartyGroupService>> _mockLogger;
        private readonly BusinessPartyGroupService _service;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly string _testUser = "test-user";

        public BusinessPartyGroupServiceTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<EventForgeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EventForgeDbContext(options);

            // Create mocks
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<BusinessPartyGroupService>>();

            // Setup tenant context
            _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

            // Create service
            _service = new BusinessPartyGroupService(
                _context,
                _mockAuditLogService.Object,
                _mockTenantContext.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateGroupAsync_WithValidDto_CreatesGroup()
        {
            // Arrange
            var createDto = new CreateBusinessPartyGroupDto
            {
                Name = "VIP Customers",
                Code = "VIP",
                Description = "VIP customer group",
                GroupType = BusinessPartyGroupType.Customer,
                ColorHex = "#FFD700",
                Icon = "Diamond",
                Priority = 80,
                IsActive = true
            };

            // Act
            var result = await _service.CreateGroupAsync(createDto, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VIP Customers", result.Name);
            Assert.Equal("VIP", result.Code);
            Assert.Equal(BusinessPartyGroupType.Customer, result.GroupType);
            Assert.Equal("#FFD700", result.ColorHex);
            Assert.Equal(80, result.Priority);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetGroupsAsync_ReturnsPagedResult()
        {
            // Arrange
            var group1 = new BusinessPartyGroup
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = "Group 1",
                GroupType = BusinessPartyGroupType.Customer,
                Priority = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _testUser
            };

            var group2 = new BusinessPartyGroup
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = "Group 2",
                GroupType = BusinessPartyGroupType.Supplier,
                Priority = 60,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _testUser
            };

            _context.BusinessPartyGroups.AddRange(group1, group2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetGroupsAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task UpdateGroupAsync_PersistsChangesToDatabase()
        {
            // Arrange — create a group directly in the DB
            var group = new BusinessPartyGroup
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = "Original Name",
                Code = "ORIG",
                Description = "Original description",
                GroupType = BusinessPartyGroupType.Customer,
                ColorHex = "#1976D2",
                Priority = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _testUser
            };

            _context.BusinessPartyGroups.Add(group);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateBusinessPartyGroupDto
            {
                Name = "Updated Name",
                Code = "UPD",
                Description = "Updated description",
                GroupType = BusinessPartyGroupType.Supplier,
                ColorHex = "#FF0000",
                Priority = 80,
                IsActive = true
            };

            // Act
            var result = await _service.UpdateGroupAsync(group.Id, updateDto, _testUser);

            // Assert — returned DTO reflects the update
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("UPD", result.Code);
            Assert.Equal("Updated description", result.Description);
            Assert.Equal(BusinessPartyGroupType.Supplier, result.GroupType);
            Assert.Equal("#FF0000", result.ColorHex);
            Assert.Equal(80, result.Priority);

            // Assert — changes are actually persisted in the database
            var persisted = await _context.BusinessPartyGroups.FindAsync(group.Id);
            Assert.NotNull(persisted);
            Assert.Equal("Updated Name", persisted!.Name);
            Assert.Equal("UPD", persisted.Code);
            Assert.Equal("#FF0000", persisted.ColorHex);
            Assert.Equal(BusinessPartyGroupType.Supplier, persisted.GroupType);
            Assert.Equal(80, persisted.Priority);
        }

        [Fact]
        public async Task DeleteGroupAsync_SoftDeletePersistsToDatabase()
        {
            // Arrange
            var group = new BusinessPartyGroup
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = "To Delete",
                GroupType = BusinessPartyGroupType.Customer,
                Priority = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _testUser
            };

            _context.BusinessPartyGroups.Add(group);
            await _context.SaveChangesAsync();

            // Act
            var deleted = await _service.DeleteGroupAsync(group.Id, _testUser);

            // Assert — operation reported success
            Assert.True(deleted);

            // Assert — record is soft-deleted in the database
            var persisted = await _context.BusinessPartyGroups.FindAsync(group.Id);
            Assert.NotNull(persisted);
            Assert.True(persisted!.IsDeleted);
            Assert.NotNull(persisted.DeletedAt);
            Assert.Equal(_testUser, persisted.DeletedBy);
        }

        public void Dispose()
        {
            _context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
