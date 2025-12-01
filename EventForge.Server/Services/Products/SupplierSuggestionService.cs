using EventForge.DTOs.Products.SupplierSuggestion;
using EventForge.Server.Services.Alerts;
using EventForge.Server.Services.PriceHistory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for intelligent supplier recommendations with multi-factor scoring.
/// </summary>
public class SupplierSuggestionService : ISupplierSuggestionService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ISupplierProductPriceHistoryService _priceHistoryService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupplierSuggestionService> _logger;
    private readonly Lazy<ISupplierPriceAlertService>? _alertService;

    // Configuration values
    private readonly decimal _priceWeight;
    private readonly decimal _leadTimeWeight;
    private readonly decimal _reliabilityWeight;
    private readonly decimal _trendWeight;
    private readonly int _minDataPointsForTrend;
    private readonly int _trendAnalysisPeriodDays;
    private readonly int _cacheScoresDurationMinutes;
    private readonly int _lowConfidenceThreshold;
    private readonly int _highConfidenceThreshold;
    private readonly decimal _alertScoreDifferenceThreshold;

    public SupplierSuggestionService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        ISupplierProductPriceHistoryService priceHistoryService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<SupplierSuggestionService> logger,
        Lazy<ISupplierPriceAlertService>? alertService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _priceHistoryService = priceHistoryService ?? throw new ArgumentNullException(nameof(priceHistoryService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alertService = alertService;

        // Load configuration
        _priceWeight = _configuration.GetValue<decimal>("SupplierSuggestion:Weights:Price", 0.4m);
        _leadTimeWeight = _configuration.GetValue<decimal>("SupplierSuggestion:Weights:LeadTime", 0.25m);
        _reliabilityWeight = _configuration.GetValue<decimal>("SupplierSuggestion:Weights:Reliability", 0.2m);
        _trendWeight = _configuration.GetValue<decimal>("SupplierSuggestion:Weights:Trend", 0.15m);
        _minDataPointsForTrend = _configuration.GetValue<int>("SupplierSuggestion:MinDataPointsForTrend", 3);
        _trendAnalysisPeriodDays = _configuration.GetValue<int>("SupplierSuggestion:TrendAnalysisPeriodDays", 180);
        _cacheScoresDurationMinutes = _configuration.GetValue<int>("SupplierSuggestion:CacheScoresDurationMinutes", 5);
        _lowConfidenceThreshold = _configuration.GetValue<int>("SupplierSuggestion:ConfidenceThresholds:Low", 60);
        _highConfidenceThreshold = _configuration.GetValue<int>("SupplierSuggestion:ConfidenceThresholds:High", 80);
        _alertScoreDifferenceThreshold = _configuration.GetValue<decimal>("SupplierSuggestion:AlertScoreDifferenceThreshold", 10m);
    }

    public async Task<SupplierSuggestionResponse> GetSupplierSuggestionsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not available.");
        }

        // Get product details
        var product = await _context.Products
            .Where(p => p.Id == productId && p.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        // Calculate suggestions
        var suggestions = await CalculateSuggestionsAsync(productId, cancellationToken);

        if (suggestions.Count == 0)
        {
            return new SupplierSuggestionResponse
            {
                ProductId = productId,
                ProductCode = product.Code,
                ProductName = product.Name,
                Suggestions = suggestions,
                RecommendedSupplier = null,
                PotentialSavings = 0,
                RecommendationExplanation = "No suppliers available for comparison."
            };
        }

        // Get the recommended supplier (highest score)
        var recommended = suggestions.OrderByDescending(s => s.TotalScore).First();

        // Calculate potential savings compared to current preferred
        var currentPreferred = suggestions.FirstOrDefault(s => s.IsCurrentPreferred);
        var potentialSavings = 0m;
        if (currentPreferred != null && currentPreferred.UnitCost.HasValue && recommended.UnitCost.HasValue)
        {
            potentialSavings = currentPreferred.UnitCost.Value - recommended.UnitCost.Value;
        }

        // Generate explanation
        var explanation = await GenerateRecommendationExplanationAsync(recommended, product);

        // Generate alert if there's a significantly better supplier (FASE 5 integration)
        if (_alertService != null && currentPreferred != null && recommended != null &&
            recommended.SupplierId != currentPreferred.SupplierId &&
            recommended.TotalScore > currentPreferred.TotalScore + _alertScoreDifferenceThreshold)
        {
            try
            {
                await _alertService.Value.GenerateAlertsForBetterSupplierAsync(
                    productId,
                    currentPreferred.SupplierId,
                    recommended.SupplierId,
                    cancellationToken);
            }
            catch (Exception alertEx)
            {
                _logger.LogWarning(alertEx, "Failed to generate better supplier alert for product {ProductId}", productId);
                // Don't throw - alerts are not critical to suggestions
            }
        }

        return new SupplierSuggestionResponse
        {
            ProductId = productId,
            ProductCode = product.Code,
            ProductName = product.Name,
            Suggestions = suggestions,
            RecommendedSupplier = recommended,
            PotentialSavings = potentialSavings,
            RecommendationExplanation = explanation
        };
    }

    public async Task<List<SupplierSuggestion>> CalculateSuggestionsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        var cacheKey = $"SupplierSuggestions_{productId}_{tenantId}";

        // Try to get from cache
        if (_cache.TryGetValue<List<SupplierSuggestion>>(cacheKey, out var cachedSuggestions) && cachedSuggestions != null)
        {
            return cachedSuggestions;
        }

        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not available.");
        }

        // Get all suppliers for this product
        var productSuppliers = await _context.ProductSuppliers
            .Include(ps => ps.Supplier)
            .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (productSuppliers.Count < 2)
        {
            // Need at least 2 suppliers for comparison
            return new List<SupplierSuggestion>();
        }

        var suggestions = new List<SupplierSuggestion>();

        foreach (var ps in productSuppliers)
        {
            if (ps.Supplier == null) continue;

            var suggestion = new SupplierSuggestion
            {
                SupplierId = ps.SupplierId,
                SupplierName = ps.Supplier.Name,
                IsCurrentPreferred = ps.Preferred,
                UnitCost = ps.UnitCost,
                Currency = ps.Currency ?? "EUR",
                LeadTimeDays = ps.LeadTimeDays
            };

            // Calculate individual scores
            suggestion.ScoreBreakdown.PriceScore = await CalculatePriceScoreAsync(productId, ps, productSuppliers, cancellationToken);
            suggestion.ScoreBreakdown.LeadTimeScore = CalculateLeadTimeScore(ps, productSuppliers);
            suggestion.ScoreBreakdown.ReliabilityScore = await CalculateReliabilityScoreAsync(ps.SupplierId, cancellationToken);
            suggestion.ScoreBreakdown.TrendScore = await CalculateTrendScoreAsync(ps.SupplierId, productId, cancellationToken);

            // Calculate total weighted score
            suggestion.TotalScore =
                (suggestion.ScoreBreakdown.PriceScore * _priceWeight) +
                (suggestion.ScoreBreakdown.LeadTimeScore * _leadTimeWeight) +
                (suggestion.ScoreBreakdown.ReliabilityScore * _reliabilityWeight) +
                (suggestion.ScoreBreakdown.TrendScore * _trendWeight);

            // Determine confidence level
            suggestion.Confidence = DetermineConfidenceLevel(suggestion.TotalScore);

            // Generate recommendation reason
            suggestion.RecommendationReason = GenerateRecommendationReason(suggestion);

            // Add explanations
            suggestion.ScoreBreakdown.Explanations = GenerateScoreExplanations(suggestion, ps, productSuppliers);

            suggestions.Add(suggestion);
        }

        // Sort by total score descending
        suggestions = suggestions.OrderByDescending(s => s.TotalScore).ToList();

        // Cache the results
        _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(_cacheScoresDurationMinutes));

        return suggestions;
    }

    public async Task<bool> ApplySuggestedSupplierAsync(Guid productId, Guid supplierId, string? reason, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not available.");
        }

        try
        {
            // Get all suppliers for this product
            var productSuppliers = await _context.ProductSuppliers
                .Where(ps => ps.ProductId == productId && ps.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            // Unset current preferred
            foreach (var ps in productSuppliers)
            {
                ps.Preferred = false;
            }

            // Set new preferred
            var targetSupplier = productSuppliers.FirstOrDefault(ps => ps.SupplierId == supplierId);
            if (targetSupplier == null)
            {
                return false;
            }

            targetSupplier.Preferred = true;

            // Add note about the change
            if (!string.IsNullOrWhiteSpace(reason))
            {
                targetSupplier.Notes = $"Applied suggestion: {reason}. Previous notes: {targetSupplier.Notes}";
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = $"SupplierSuggestions_{productId}_{tenantId}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Applied suggested supplier {SupplierId} for product {ProductId}. Reason: {Reason}",
                supplierId, productId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying suggested supplier {SupplierId} for product {ProductId}",
                supplierId, productId);
            return false;
        }
    }

    public async Task<SupplierReliabilityResponse> GetSupplierReliabilityAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not available.");
        }

        var supplier = await _context.BusinessParties
            .Where(bp => bp.Id == supplierId && bp.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier == null)
        {
            throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");
        }

        var metrics = await CalculateReliabilityMetricsAsync(supplierId, cancellationToken);
        var score = CalculateReliabilityScoreFromMetrics(metrics);

        return new SupplierReliabilityResponse
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            OverallReliabilityScore = score,
            Metrics = metrics
        };
    }

    public async Task<string> GenerateRecommendationExplanationAsync(SupplierSuggestion suggestion, Product product)
    {
        await Task.CompletedTask; // Make async to allow for future AI integration

        var explanation = $"We recommend {suggestion.SupplierName} for {product.Name}";

        var reasons = new List<string>();

        // Price reason
        if (suggestion.ScoreBreakdown.PriceScore > 80)
        {
            reasons.Add($"they offer the best price ({suggestion.Currency} {suggestion.UnitCost:N2})");
        }
        else if (suggestion.ScoreBreakdown.PriceScore > 60)
        {
            reasons.Add($"they offer competitive pricing ({suggestion.Currency} {suggestion.UnitCost:N2})");
        }

        // Lead time reason
        if (suggestion.ScoreBreakdown.LeadTimeScore > 80 && suggestion.LeadTimeDays.HasValue)
        {
            reasons.Add($"excellent delivery time ({suggestion.LeadTimeDays} days)");
        }
        else if (suggestion.ScoreBreakdown.LeadTimeScore > 60 && suggestion.LeadTimeDays.HasValue)
        {
            reasons.Add($"good delivery time ({suggestion.LeadTimeDays} days)");
        }

        // Reliability reason
        if (suggestion.ScoreBreakdown.ReliabilityScore > 80)
        {
            reasons.Add("high reliability");
        }
        else if (suggestion.ScoreBreakdown.ReliabilityScore > 60)
        {
            reasons.Add("good reliability");
        }

        // Trend reason
        if (suggestion.ScoreBreakdown.TrendScore > 80)
        {
            reasons.Add("stable or decreasing pricing trend");
        }
        else if (suggestion.ScoreBreakdown.TrendScore < 40)
        {
            reasons.Add("but note: prices have been increasing");
        }

        if (reasons.Any())
        {
            explanation += $" because {string.Join(", ", reasons)}.";
        }
        else
        {
            explanation += " based on overall scoring.";
        }

        return explanation;
    }

    #region Private Helper Methods

    private async Task<decimal> CalculatePriceScoreAsync(Guid productId, ProductSupplier currentSupplier,
        List<ProductSupplier> allSuppliers, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // For async consistency

        var suppliersWithPrices = allSuppliers.Where(ps => ps.UnitCost.HasValue).ToList();
        if (suppliersWithPrices.Count < 2 || !currentSupplier.UnitCost.HasValue)
        {
            return 50m; // Neutral score if insufficient data
        }

        var prices = suppliersWithPrices.Select(ps => ps.UnitCost!.Value).ToList();
        var maxPrice = prices.Max();
        var minPrice = prices.Min();
        var currentPrice = currentSupplier.UnitCost!.Value;

        if (maxPrice == minPrice)
        {
            return 100m; // All same price
        }

        // Formula: ((MaxPrice - CurrentPrice) / (MaxPrice - MinPrice)) * 100
        var score = ((maxPrice - currentPrice) / (maxPrice - minPrice)) * 100m;
        return Math.Max(0m, Math.Min(100m, score));
    }

    private decimal CalculateLeadTimeScore(ProductSupplier currentSupplier, List<ProductSupplier> allSuppliers)
    {
        var suppliersWithLeadTime = allSuppliers.Where(ps => ps.LeadTimeDays.HasValue).ToList();
        if (suppliersWithLeadTime.Count < 2 || !currentSupplier.LeadTimeDays.HasValue)
        {
            return 50m; // Neutral score if insufficient data
        }

        var leadTimes = suppliersWithLeadTime.Select(ps => ps.LeadTimeDays!.Value).ToList();
        var maxLeadTime = leadTimes.Max();
        var minLeadTime = leadTimes.Min();
        var currentLeadTime = currentSupplier.LeadTimeDays!.Value;

        if (maxLeadTime == minLeadTime)
        {
            return 100m; // All same lead time
        }

        // Formula: ((MaxLeadTime - CurrentLeadTime) / (MaxLeadTime - MinLeadTime)) * 100
        var score = ((decimal)(maxLeadTime - currentLeadTime) / (maxLeadTime - minLeadTime)) * 100m;
        return Math.Max(0m, Math.Min(100m, score));
    }

    private async Task<decimal> CalculateReliabilityScoreAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var metrics = await CalculateReliabilityMetricsAsync(supplierId, cancellationToken);
        return CalculateReliabilityScoreFromMetrics(metrics);
    }

    private async Task<ReliabilityMetrics> CalculateReliabilityMetricsAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;

        // Simplified reliability based on available data
        // In future: integrate with actual order/delivery data

        var productCount = await _context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && ps.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var priceHistoryCount = await _context.SupplierProductPriceHistories
            .Where(ph => ph.SupplierId == supplierId && ph.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var supplierAge = await _context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && ps.TenantId == tenantId)
            .MinAsync(ps => (DateTime?)ps.CreatedAt, cancellationToken);

        var ageInDays = supplierAge.HasValue ? (DateTime.UtcNow - supplierAge.Value).Days : 0;

        // Calculate simplified metrics
        var totalOrders = Math.Max(1, priceHistoryCount); // Use price history as proxy for orders
        var onTimeRate = 85m + (ageInDays > 365 ? 10m : 0); // Older suppliers get bonus
        var accuracyRate = 90m + (productCount > 10 ? 5m : 0); // More products = more reliable
        var defectRate = Math.Max(0m, 10m - (productCount * 0.5m)); // Fewer defects with more products

        return new ReliabilityMetrics
        {
            TotalOrders = totalOrders,
            OnTimeDeliveryRate = Math.Min(100m, onTimeRate),
            OrderAccuracyRate = Math.Min(100m, accuracyRate),
            DefectRate = Math.Max(0m, defectRate),
            AverageResponseTimeHours = 24 - Math.Min(12, ageInDays / 30)
        };
    }

    private decimal CalculateReliabilityScoreFromMetrics(ReliabilityMetrics metrics)
    {
        // Weighted average of reliability factors
        var score = (metrics.OnTimeDeliveryRate * 0.4m) +
                    (metrics.OrderAccuracyRate * 0.3m) +
                    ((100m - metrics.DefectRate) * 0.2m) +
                    (Math.Max(0m, 100m - (metrics.AverageResponseTimeHours * 2)) * 0.1m);

        return Math.Max(0m, Math.Min(100m, score));
    }

    private async Task<decimal> CalculateTrendScoreAsync(Guid supplierId, Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddDays(-_trendAnalysisPeriodDays);

            var trendData = await _priceHistoryService.GetPriceTrendDataAsync(
                supplierId, productId, fromDate, toDate, cancellationToken);

            if (trendData == null || trendData.Count < _minDataPointsForTrend)
            {
                return 50m; // Neutral score if insufficient data
            }

            // Calculate price change percentage
            var firstPrice = trendData.First().Price;
            var lastPrice = trendData.Last().Price;

            if (firstPrice <= 0)
            {
                return 50m;
            }

            var changePercent = ((lastPrice - firstPrice) / firstPrice) * 100m;

            // Score based on price trend:
            // Decreasing prices = high score (100)
            // Stable prices = medium score (70)
            // Increasing prices = low score (proportional to increase)
            if (changePercent <= -5m) // Significant decrease
            {
                return 100m;
            }
            else if (changePercent <= 0m) // Slight decrease or stable
            {
                return 85m + (changePercent * 3m); // 85-100 range
            }
            else if (changePercent <= 5m) // Slight increase
            {
                return 70m - (changePercent * 3m); // 55-70 range
            }
            else // Significant increase
            {
                return Math.Max(0m, 50m - (changePercent * 2m));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating trend score for supplier {SupplierId} and product {ProductId}",
                supplierId, productId);
            return 50m; // Neutral score on error
        }
    }

    private ConfidenceLevel DetermineConfidenceLevel(decimal totalScore)
    {
        if (totalScore < _lowConfidenceThreshold)
        {
            return ConfidenceLevel.Low;
        }
        else if (totalScore < _highConfidenceThreshold)
        {
            return ConfidenceLevel.Medium;
        }
        else
        {
            return ConfidenceLevel.High;
        }
    }

    private string GenerateRecommendationReason(SupplierSuggestion suggestion)
    {
        var breakdown = suggestion.ScoreBreakdown;
        var strengths = new List<string>();

        if (breakdown.PriceScore > 80) strengths.Add("best price");
        if (breakdown.LeadTimeScore > 80) strengths.Add("fast delivery");
        if (breakdown.ReliabilityScore > 80) strengths.Add("high reliability");
        if (breakdown.TrendScore > 80) strengths.Add("stable pricing");

        if (strengths.Any())
        {
            return $"Recommended for: {string.Join(", ", strengths)}";
        }

        return "Balanced overall performance";
    }

    private Dictionary<string, string> GenerateScoreExplanations(SupplierSuggestion suggestion,
        ProductSupplier currentSupplier, List<ProductSupplier> allSuppliers)
    {
        var explanations = new Dictionary<string, string>();

        // Price explanation
        if (currentSupplier.UnitCost.HasValue)
        {
            var avgPrice = allSuppliers.Where(ps => ps.UnitCost.HasValue)
                .Average(ps => ps.UnitCost!.Value);
            var diff = currentSupplier.UnitCost.Value - avgPrice;
            var diffPercent = (diff / avgPrice) * 100m;

            if (Math.Abs(diffPercent) < 5m)
            {
                explanations["Price"] = "Price is competitive with market average";
            }
            else if (diffPercent < 0)
            {
                explanations["Price"] = $"Price is {Math.Abs(diffPercent):N1}% below average - excellent value";
            }
            else
            {
                explanations["Price"] = $"Price is {diffPercent:N1}% above average";
            }
        }
        else
        {
            explanations["Price"] = "No price data available";
        }

        // Lead time explanation
        if (currentSupplier.LeadTimeDays.HasValue)
        {
            var avgLeadTime = allSuppliers.Where(ps => ps.LeadTimeDays.HasValue)
                .Average(ps => ps.LeadTimeDays!.Value);
            var diff = currentSupplier.LeadTimeDays.Value - avgLeadTime;

            if (Math.Abs(diff) <= 1)
            {
                explanations["LeadTime"] = "Lead time is average for this product";
            }
            else if (diff < 0)
            {
                explanations["LeadTime"] = $"Delivers {Math.Abs(diff):N0} days faster than average";
            }
            else
            {
                explanations["LeadTime"] = $"Lead time is {diff:N0} days longer than average";
            }
        }
        else
        {
            explanations["LeadTime"] = "No lead time data available";
        }

        // Reliability explanation
        var score = suggestion.ScoreBreakdown.ReliabilityScore;
        if (score > 80)
        {
            explanations["Reliability"] = "Highly reliable supplier with good track record";
        }
        else if (score > 60)
        {
            explanations["Reliability"] = "Reliable supplier with adequate performance";
        }
        else
        {
            explanations["Reliability"] = "Limited reliability data available";
        }

        // Trend explanation
        var trendScore = suggestion.ScoreBreakdown.TrendScore;
        if (trendScore > 80)
        {
            explanations["Trend"] = "Pricing is stable or decreasing over time";
        }
        else if (trendScore > 60)
        {
            explanations["Trend"] = "Pricing shows moderate stability";
        }
        else if (trendScore > 40)
        {
            explanations["Trend"] = "Pricing has increased moderately";
        }
        else
        {
            explanations["Trend"] = "Pricing has increased significantly";
        }

        return explanations;
    }

    #endregion
}
