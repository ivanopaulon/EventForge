namespace EventForge.DTOs.Logs
{
    public class ApplicationLogQueryParameters
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Level { get; set; }
        public string? User { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        // Altri parametri di ricerca se necessario
    }
}