namespace EventForge.Server.Configuration;

public class PaginationSettings
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 1000;
    public int MaxExportPageSize { get; set; } = 10000;
    public int RecommendedPageSize { get; set; } = 100;

    public Dictionary<string, int> EndpointOverrides { get; set; } = new();
    public Dictionary<string, int> RoleBasedLimits { get; set; } = new();
}
