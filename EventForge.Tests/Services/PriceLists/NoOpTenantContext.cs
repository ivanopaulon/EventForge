using EventForge.Server.Services.Tenants;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Minimal <see cref="ITenantContext"/> stub used by existing PriceLists tests that do not
/// exercise tenant isolation directly. Returns <see cref="Guid.Empty"/> to match the default
/// (unset) TenantId of entities created in these tests, so pre-existing test scenarios keep
/// passing after tenant filtering was added to single-record operations
/// (see PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md).
/// </summary>
internal class NoOpTenantContext(Guid? tenantId = null) : ITenantContext
{
    public Guid? CurrentTenantId => tenantId ?? Guid.Empty;

    public Guid? CurrentUserId => Guid.Empty;

    public bool IsSuperAdmin => false;

    public bool IsImpersonating => false;

    public Task SetTenantContextAsync(Guid tenantId, string auditReason, CancellationToken ct = default) => Task.CompletedTask;

    public Task StartImpersonationAsync(Guid userId, string auditReason, CancellationToken ct = default) => Task.CompletedTask;

    public Task EndImpersonationAsync(string auditReason, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IEnumerable<Guid>> GetManageableTenantsAsync(CancellationToken ct = default) => Task.FromResult(Enumerable.Empty<Guid>());

    public Task<bool> CanAccessTenantAsync(Guid tenantId, CancellationToken ct = default) => Task.FromResult(true);
}
