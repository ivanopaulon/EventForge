using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Mappers;
using EventForge.Server.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for document analytics and KPI tracking
/// </summary>
public class DocumentAnalyticsService : IDocumentAnalyticsService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentAnalyticsService> _logger;

    public DocumentAnalyticsService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentAnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentAnalyticsDto> CreateOrUpdateAnalyticsAsync(
        Guid documentHeaderId,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get or create analytics record
            var analytics = await _context.DocumentAnalytics
                .FirstOrDefaultAsync(a => a.DocumentHeaderId == documentHeaderId, cancellationToken);

            if (analytics == null)
            {
                // Create new analytics record
                analytics = new DocumentAnalytics
                {
                    DocumentHeaderId = documentHeaderId,
                    AnalyticsDate = DateTime.UtcNow.Date,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DocumentAnalytics.Add(analytics);
            }

            // Calculate analytics from document data
            await CalculateAnalyticsMetrics(analytics, cancellationToken);

            analytics.ModifiedBy = currentUser;
            analytics.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogEntityChangeAsync(
                "DocumentAnalytics",
                analytics.Id,
                "Analytics",
                analytics == null ? "Created" : "Updated",
                null,
                analytics.Id.ToString(),
                currentUser,
                "Document Analytics",
                cancellationToken);

            _logger.LogInformation("Analytics updated for document {DocumentHeaderId} by user {User}", 
                documentHeaderId, currentUser);

            return DocumentAnalyticsMapper.ToDto(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating analytics for document {DocumentHeaderId}", documentHeaderId);
            throw;
        }
    }

    public async Task<DocumentAnalyticsDto?> GetDocumentAnalyticsAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _context.DocumentAnalytics
                .Include(a => a.DocumentHeader)
                .Include(a => a.DocumentType)
                .FirstOrDefaultAsync(a => a.DocumentHeaderId == documentHeaderId, cancellationToken);

            return analytics != null ? DocumentAnalyticsMapper.ToDto(analytics) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for document {DocumentHeaderId}", documentHeaderId);
            throw;
        }
    }

    public async Task<DocumentAnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.DocumentAnalytics.AsQueryable();

            // Apply date filters
            if (from.HasValue)
                query = query.Where(a => a.AnalyticsDate >= from.Value.Date);
            
            if (to.HasValue)
                query = query.Where(a => a.AnalyticsDate <= to.Value.Date);

            var analytics = await query
                .Include(a => a.DocumentType)
                .ToListAsync(cancellationToken);

            var summary = new DocumentAnalyticsSummaryDto
            {
                PeriodStart = from,
                PeriodEnd = to,
                GroupBy = groupBy,
                TotalDocuments = analytics.Count(),
                CompletedDocuments = analytics.Count(a => !string.IsNullOrEmpty(a.FinalStatus?.ToString())),
                PendingDocuments = analytics.Count(a => string.IsNullOrEmpty(a.FinalStatus?.ToString())),
                AverageCompletionTimeHours = analytics.Where(a => a.TimeToClosureHours.HasValue)
                    .Average(a => a.TimeToClosureHours),
                AverageQualityScore = analytics.Where(a => a.QualityScore.HasValue)
                    .Average(a => a.QualityScore),
                TotalDocumentValue = analytics.Where(a => a.DocumentValue.HasValue)
                    .Sum(a => a.DocumentValue)
            };

            // Create groups based on groupBy parameter
            summary.Groups = CreateAnalyticsGroups(analytics, groupBy ?? "documentType");

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics summary from {From} to {To} grouped by {GroupBy}", 
                from, to, groupBy);
            throw;
        }
    }

    public async Task<DocumentAnalyticsDto> HandleWorkflowEventAsync(
        Guid documentHeaderId,
        string eventType,
        object? eventData,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _context.DocumentAnalytics
                .FirstOrDefaultAsync(a => a.DocumentHeaderId == documentHeaderId, cancellationToken);

            if (analytics == null)
            {
                analytics = new DocumentAnalytics
                {
                    DocumentHeaderId = documentHeaderId,
                    AnalyticsDate = DateTime.UtcNow.Date,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentAnalytics.Add(analytics);
            }

            // Update analytics based on event type
            UpdateAnalyticsForWorkflowEvent(analytics, eventType, eventData);

            analytics.ModifiedBy = currentUser;
            analytics.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Analytics updated for workflow event {EventType} on document {DocumentHeaderId}", 
                eventType, documentHeaderId);

            return DocumentAnalyticsMapper.ToDto(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling workflow event {EventType} for document {DocumentHeaderId}", 
                eventType, documentHeaderId);
            throw;
        }
    }

    public async Task<DocumentKpiSummaryDto> CalculateKpiSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _context.DocumentAnalytics
                .Where(a => a.AnalyticsDate >= from.Date && a.AnalyticsDate <= to.Date)
                .ToListAsync(cancellationToken);

            var totalDocuments = analytics.Count;
            var completedDocuments = analytics.Count(a => !string.IsNullOrEmpty(a.FinalStatus?.ToString()));

            return new DocumentKpiSummaryDto
            {
                PeriodStart = from,
                PeriodEnd = to,
                TotalDocuments = totalDocuments,
                CompletionRate = totalDocuments > 0 ? (decimal)completedDocuments / totalDocuments * 100 : 0,
                AverageProcessingTime = analytics.Where(a => a.TotalProcessingTimeHours.HasValue)
                    .Average(a => a.TotalProcessingTimeHours) ?? 0,
                AverageQualityScore = analytics.Where(a => a.QualityScore.HasValue)
                    .Average(a => a.QualityScore) ?? 0,
                ErrorRate = totalDocuments > 0 ? 
                    (decimal)(analytics.Average(a => a.Errors) / Math.Max(totalDocuments, 1) * 100) : 0,
                EscalationRate = totalDocuments > 0 ? 
                    (decimal)analytics.Sum(a => a.Escalations) / totalDocuments * 100 : 0,
                AverageCustomerSatisfaction = analytics.Where(a => a.SatisfactionScore.HasValue)
                    .Average(a => a.SatisfactionScore),
                TotalBusinessValue = analytics.Where(a => a.DocumentValue.HasValue)
                    .Sum(a => a.DocumentValue),
                CostEfficiencyRatio = CalculateCostEfficiencyRatio(analytics)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating KPI summary from {From} to {To}", from, to);
            throw;
        }
    }

    private async Task CalculateAnalyticsMetrics(DocumentAnalytics analytics, CancellationToken cancellationToken)
    {
        // Get document header with related data
        var document = await _context.DocumentHeaders
            .Include(d => d.DocumentType)
            .Include(d => d.WorkflowExecutions)
                .ThenInclude(we => we.WorkflowSteps)
            .FirstOrDefaultAsync(d => d.Id == analytics.DocumentHeaderId, cancellationToken);

        if (document == null) return;

        // Update basic information
        analytics.DocumentTypeId = document.DocumentTypeId;
        analytics.BusinessPartyId = document.BusinessPartyId;
        analytics.DocumentCreator = document.CreatedBy;

        // Calculate workflow metrics
        var latestWorkflow = document.WorkflowExecutions?.OrderByDescending(we => we.CreatedAt).FirstOrDefault();
        if (latestWorkflow != null)
        {
            analytics.ApprovalStepsRequired = latestWorkflow.WorkflowSteps?.Count ?? 0;
            analytics.ApprovalStepsCompleted = latestWorkflow.WorkflowSteps?.Count(s => s.CompletedAt.HasValue) ?? 0;
            
            // Calculate time metrics
            if (latestWorkflow.StartedAt.HasValue)
            {
                var completedAt = latestWorkflow.CompletedAt ?? DateTime.UtcNow;
                analytics.TimeToClosureHours = (decimal)(completedAt - latestWorkflow.StartedAt.Value).TotalHours;
            }

            if (latestWorkflow.ProcessingTimeHours.HasValue)
            {
                analytics.TotalProcessingTimeHours = latestWorkflow.ProcessingTimeHours;
            }

            analytics.Escalations = latestWorkflow.EscalationLevel;
        }

        // Calculate quality score based on errors and completion time
        analytics.QualityScore = CalculateQualityScore(analytics);
    }

    private void UpdateAnalyticsForWorkflowEvent(DocumentAnalytics analytics, string eventType, object? eventData)
    {
        switch (eventType.ToLowerInvariant())
        {
            case "approved":
                analytics.ApprovalsReceived++;
                break;
            case "rejected":
                analytics.Rejections++;
                break;
            case "escalated":
                analytics.Escalations++;
                break;
            case "error":
                analytics.Errors++;
                break;
            case "revision":
                analytics.Revisions++;
                break;
        }

        // Recalculate quality score
        analytics.QualityScore = CalculateQualityScore(analytics);
    }

    private List<AnalyticsGroupDto> CreateAnalyticsGroups(List<DocumentAnalytics> analytics, string groupBy)
    {
        return groupBy.ToLowerInvariant() switch
        {
            "documenttype" => analytics
                .GroupBy(a => a.DocumentType?.Name ?? "Unknown")
                .Select(g => new AnalyticsGroupDto
                {
                    GroupKey = g.Key,
                    GroupLabel = g.Key,
                    DocumentCount = g.Count(),
                    AverageCompletionTime = g.Where(a => a.TimeToClosureHours.HasValue)
                        .Average(a => a.TimeToClosureHours),
                    TotalValue = g.Where(a => a.DocumentValue.HasValue).Sum(a => a.DocumentValue),
                    AverageQuality = g.Where(a => a.QualityScore.HasValue).Average(a => a.QualityScore)
                }).ToList(),
            
            "department" => analytics
                .GroupBy(a => a.Department ?? "Unknown")
                .Select(g => new AnalyticsGroupDto
                {
                    GroupKey = g.Key,
                    GroupLabel = g.Key,
                    DocumentCount = g.Count(),
                    AverageCompletionTime = g.Where(a => a.TimeToClosureHours.HasValue)
                        .Average(a => a.TimeToClosureHours),
                    TotalValue = g.Where(a => a.DocumentValue.HasValue).Sum(a => a.DocumentValue),
                    AverageQuality = g.Where(a => a.QualityScore.HasValue).Average(a => a.QualityScore)
                }).ToList(),
            
            "time" => analytics
                .GroupBy(a => a.AnalyticsDate.ToString("yyyy-MM"))
                .Select(g => new AnalyticsGroupDto
                {
                    GroupKey = g.Key,
                    GroupLabel = DateTime.ParseExact(g.Key + "-01", "yyyy-MM-dd", null).ToString("MMM yyyy"),
                    DocumentCount = g.Count(),
                    AverageCompletionTime = g.Where(a => a.TimeToClosureHours.HasValue)
                        .Average(a => a.TimeToClosureHours),
                    TotalValue = g.Where(a => a.DocumentValue.HasValue).Sum(a => a.DocumentValue),
                    AverageQuality = g.Where(a => a.QualityScore.HasValue).Average(a => a.QualityScore)
                }).ToList(),
            
            _ => new List<AnalyticsGroupDto>()
        };
    }

    private decimal? CalculateQualityScore(DocumentAnalytics analytics)
    {
        // Simple quality score calculation
        // Start with 100 and subtract points for issues
        decimal score = 100;
        
        score -= analytics.Errors * 10; // -10 points per error
        score -= analytics.Rejections * 5;   // -5 points per rejection
        score -= analytics.Revisions * 3; // -3 points per revision
        
        // Bonus for fast completion (if under average)
        if (analytics.TimeToClosureHours.HasValue && analytics.TimeToClosureHours.Value < 24)
        {
            score += 5;
        }

        return Math.Max(0, Math.Min(100, score));
    }

    private decimal? CalculateCostEfficiencyRatio(List<DocumentAnalytics> analytics)
    {
        var totalValue = analytics.Where(a => a.DocumentValue.HasValue).Sum(a => a.DocumentValue);
        var totalCost = analytics.Where(a => a.ProcessingCost.HasValue).Sum(a => a.ProcessingCost);

        return totalCost > 0 ? totalValue / totalCost : null;
    }
}