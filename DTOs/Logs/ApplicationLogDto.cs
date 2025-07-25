namespace EventForge.DTOs.Logs
{
    public class ApplicationLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? User { get; set; }
        // Altri campi se necessario
    }
}