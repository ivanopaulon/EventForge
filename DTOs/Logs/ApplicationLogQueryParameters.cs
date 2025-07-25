namespace EventForge.DTOs.Logs
{
    public class ApplicationLogQueryParameters
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }

        public int? Skip { get; set; }
        public string? Level { get; set; }
        public string? User { get; set; }
        public string? Message { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool? HasException { get; set; }

        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }
}