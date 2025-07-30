using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Common
{
    /// <summary>
    /// DTO for handling ProblemDetails responses.
    /// </summary>
    public class ProblemDetailsDto
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public string? CorrelationId { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
    }
}