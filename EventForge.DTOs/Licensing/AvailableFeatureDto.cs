namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// DTO for available license features.
    /// </summary>
    public class AvailableFeatureDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
