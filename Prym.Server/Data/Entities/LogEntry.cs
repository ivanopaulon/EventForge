using System.ComponentModel.DataAnnotations;

namespace Prym.Server.Data.Entities
{
    /// <summary>
    /// Entity class for Serilog logs table
    /// </summary>
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        [MaxLength(50)]
        public string Level { get; set; } = string.Empty;
        [MaxLength(8000)]
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        [MaxLength(256)]
        public string? MachineName { get; set; }
        [MaxLength(256)]
        public string? UserName { get; set; }
    }
}