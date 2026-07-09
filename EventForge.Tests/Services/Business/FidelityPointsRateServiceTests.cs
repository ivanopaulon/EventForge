using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Configuration;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EventForge.Tests.Services.Business
{
    /// <summary>
    /// Unit tests for FidelityPointsRateService.
    /// </summary>
    [Trait("Category", "Unit")]
    public class FidelityPointsRateServiceTests : IDisposable
    {
        private readonly EventForgeDbContext _context;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly FidelityPointsRateService _service;
        private readonly Guid _tenantId = Guid.NewGuid();

        public FidelityPointsRateServiceTests()
        {
            var options = new DbContextOptionsBuilder<EventForgeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new EventForgeDbContext(options);

            _mockTenantContext = new Mock<ITenantContext>();
            _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

            _service = new FidelityPointsRateService(_context, _mockTenantContext.Object);
        }

        [Fact]
        public async Task GetEffectiveRateAsync_NoConfiguration_ReturnsDefaultRate()
        {
            var rate = await _service.GetEffectiveRateAsync(FidelityCardType.Gold);

            Assert.Equal(1m, rate);
        }

        [Fact]
        public async Task GetEffectiveRateAsync_WithTenantConfiguration_CombinesBaseRateAndMultiplier()
        {
            _context.SystemConfigurations.AddRange(
                new SystemConfiguration
                {
                    Key = "FidelityPoints.BaseRate",
                    Value = "2",
                    Category = "FidelityPoints",
                    TenantId = _tenantId
                },
                new SystemConfiguration
                {
                    Key = "FidelityPoints.Multiplier.Gold",
                    Value = "1.5",
                    Category = "FidelityPoints",
                    TenantId = _tenantId
                });
            _ = await _context.SaveChangesAsync();

            var rate = await _service.GetEffectiveRateAsync(FidelityCardType.Gold);

            Assert.Equal(3m, rate);
        }

        [Fact]
        public async Task GetEffectiveRateAsync_ConfigurationForOtherTenant_IsIgnored()
        {
            _context.SystemConfigurations.Add(new SystemConfiguration
            {
                Key = "FidelityPoints.BaseRate",
                Value = "5",
                Category = "FidelityPoints",
                TenantId = Guid.NewGuid()
            });
            _ = await _context.SaveChangesAsync();

            var rate = await _service.GetEffectiveRateAsync(FidelityCardType.Bronze);

            Assert.Equal(1m, rate);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
