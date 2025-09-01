using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Result of stock movement validation.
    /// </summary>
    public class MovementValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public decimal? AvailableQuantity { get; set; }
        public decimal? RequiredQuantity { get; set; }
        public decimal? ShortfallQuantity
        {
            get
            {
                if (RequiredQuantity.HasValue && AvailableQuantity.HasValue)
                    return Math.Max(0, RequiredQuantity.Value - AvailableQuantity.Value);
                return null;
            }
        }

        public bool HasSufficientStock => ShortfallQuantity == 0;
        public bool LocationExists { get; set; }
        public bool ProductExists { get; set; }
        public bool LotExists { get; set; }
        public bool SerialExists { get; set; }

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}