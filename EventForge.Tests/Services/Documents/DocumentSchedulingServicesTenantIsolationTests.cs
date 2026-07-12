using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Cross-tenant isolation tests for <see cref="DocumentRecurrenceService"/>, <see cref="DocumentReminderService"/>
/// and <see cref="DocumentRetentionService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate resources
/// belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 2).
/// </summary>
public class DocumentSchedulingServicesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _recurrenceAId;
    private readonly Guid _reminderAId;
    private readonly Guid _retentionPolicyAId;
    private readonly Guid _documentHeaderAId;

    public DocumentSchedulingServicesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _recurrenceAId = Guid.NewGuid();
        _reminderAId = Guid.NewGuid();
        _retentionPolicyAId = Guid.NewGuid();
        _documentHeaderAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.DocumentRecurrences.Add(new DocumentRecurrence
        {
            Id = _recurrenceAId,
            TenantId = _tenantAId,
            Name = "Recurrence A",
            TemplateId = Guid.NewGuid(),
            Pattern = RecurrencePattern.Monthly,
            StartDate = DateTime.UtcNow,
            IsActive = true
        });

        _context.DocumentReminders.Add(new DocumentReminder
        {
            Id = _reminderAId,
            TenantId = _tenantAId,
            DocumentHeaderId = _documentHeaderAId,
            ReminderType = ReminderType.Deadline,
            Title = "Reminder A"
        });

        _context.Set<DocumentRetentionPolicy>().Add(new DocumentRetentionPolicy
        {
            Id = _retentionPolicyAId,
            TenantId = _tenantAId,
            DocumentTypeId = Guid.NewGuid(),
            RetentionDays = 30
        });

        _context.SaveChanges();
    }

    private DocumentRecurrenceService CreateRecurrenceService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentRecurrenceService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentRecurrenceService>>().Object);
    }

    private DocumentReminderService CreateReminderService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentReminderService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentReminderService>>().Object);
    }

    private DocumentRetentionService CreateRetentionService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentRetentionService(
            _context,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentRetentionService>>().Object);
    }

    [Fact]
    public async Task GetByIdAsync_Recurrence_FromOtherTenant_ReturnsNull()
    {
        var service = CreateRecurrenceService(_tenantBId);

        var result = await service.GetByIdAsync(_recurrenceAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Recurrence_FromOtherTenant_ReturnsNull()
    {
        var service = CreateRecurrenceService(_tenantBId);

        var result = await service.UpdateAsync(_recurrenceAId, new UpdateDocumentRecurrenceDto
        {
            Name = "Hacked",
            Pattern = RecurrencePattern.Monthly,
            StartDate = DateTime.UtcNow
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentRecurrences.AsNoTracking().FirstAsync(r => r.Id == _recurrenceAId);
        Assert.Equal("Recurrence A", stillOriginal.Name);
    }

    [Fact]
    public async Task DeleteAsync_Recurrence_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateRecurrenceService(_tenantBId);

        var result = await service.DeleteAsync(_recurrenceAId, "attacker");

        Assert.False(result);

        var stillActive = await _context.DocumentRecurrences.AsNoTracking().FirstAsync(r => r.Id == _recurrenceAId);
        Assert.True(stillActive.IsActive);
    }

    [Fact]
    public async Task UpdateDocumentReminderAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateReminderService(_tenantBId);

        var result = await service.UpdateDocumentReminderAsync(_reminderAId, new UpdateDocumentReminderDto
        {
            Title = "Hacked"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentReminders.AsNoTracking().FirstAsync(r => r.Id == _reminderAId);
        Assert.Equal("Reminder A", stillOriginal.Title);
    }

    [Fact]
    public async Task DeleteDocumentReminderAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateReminderService(_tenantBId);

        var result = await service.DeleteDocumentReminderAsync(_reminderAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentReminders.AsNoTracking().FirstAsync(r => r.Id == _reminderAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task UpdatePolicyAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateRetentionService(_tenantBId);

        var result = await service.UpdatePolicyAsync(_retentionPolicyAId, new UpdateDocumentRetentionPolicyDto
        {
            RetentionDays = 999
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.Set<DocumentRetentionPolicy>().AsNoTracking().FirstAsync(p => p.Id == _retentionPolicyAId);
        Assert.Equal(30, stillOriginal.RetentionDays);
    }

    [Fact]
    public async Task DeletePolicyAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateRetentionService(_tenantBId);

        var result = await service.DeletePolicyAsync(_retentionPolicyAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.Set<DocumentRetentionPolicy>().AsNoTracking().AnyAsync(p => p.Id == _retentionPolicyAId);
        Assert.True(stillExists);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
