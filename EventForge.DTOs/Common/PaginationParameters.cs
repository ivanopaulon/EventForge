using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventForge.DTOs.Common;

public class PaginationParameters
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;
    
    [Range(1, 10000, ErrorMessage = "PageSize must be between 1 and 10000")]
    public int PageSize { get; set; } = 20;
    
    [JsonIgnore]
    public bool WasCapped { get; set; }
    
    [JsonIgnore]
    public int AppliedMaxPageSize { get; set; }
    
    public int CalculateSkip() => (Page - 1) * PageSize;
    
    public PaginationParameters() { }
    
    public PaginationParameters(int page, int pageSize)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Max(1, pageSize);
    }
}
