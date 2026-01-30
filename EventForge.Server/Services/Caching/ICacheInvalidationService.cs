namespace EventForge.Server.Services.Caching;

/// <summary>
/// Service for invalidating output cache by tags
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidate cache for static entities (VatRates, DocumentTypes, PaymentTerms, Banks)
    /// </summary>
    Task InvalidateStaticEntitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache for semi-static entities (Brands, Models, UnitOfMeasures, etc.)
    /// </summary>
    Task InvalidateSemiStaticEntitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache for real-time entities (POS sessions, Tables)
    /// </summary>
    Task InvalidateRealTimeEntitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache by custom tag
    /// </summary>
    Task InvalidateByTagAsync(string tag, CancellationToken ct = default);
}
