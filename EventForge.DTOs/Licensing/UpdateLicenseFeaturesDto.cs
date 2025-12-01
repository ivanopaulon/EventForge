using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// DTO for updating license features.
    /// </summary>
    public class UpdateLicenseFeaturesDto
    {
        [Required]
        public List<string> FeatureNames { get; set; } = new();
    }
}
