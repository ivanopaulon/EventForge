namespace EventForge.Server.Data.Entities
{
    /// <summary>
    /// Entity class for Serilog logs table
    /// </summary>
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
    }
}