using EventForge.DTOs.Common;

namespace EventForge.Server.Extensions
{
    /// <summary>
    /// Extension methods to convert between entity enums and DTO enums.
    /// Note: These are now redundant since entities use shared DTO enums directly,
    /// but kept for backward compatibility.
    /// </summary>
    public static class EnumMappingExtensions
    {
        /// <summary>
        /// Convert entity AddressType to DTO AddressType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static AddressType ToDto(this AddressType entityEnum)
        {
            return entityEnum;
        }

        /// <summary>
        /// Convert DTO AddressType to entity AddressType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static AddressType ToEntity(this AddressType dtoEnum)
        {
            return dtoEnum;
        }

        /// <summary>
        /// Convert entity ContactType to DTO ContactType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ContactType ToDto(this ContactType entityEnum)
        {
            return entityEnum;
        }

        /// <summary>
        /// Convert DTO ContactType to entity ContactType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ContactType ToEntity(this ContactType dtoEnum)
        {
            return dtoEnum;
        }

        /// <summary>
        /// Convert entity ProductClassificationType to DTO ProductClassificationType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ProductClassificationType ToDto(this ProductClassificationType entityEnum)
        {
            return entityEnum;
        }

        /// <summary>
        /// Convert DTO ProductClassificationType to entity ProductClassificationType.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ProductClassificationType ToEntity(this ProductClassificationType dtoEnum)
        {
            return dtoEnum;
        }

        /// <summary>
        /// Convert entity ProductClassificationNodeStatus to DTO ProductClassificationNodeStatus.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ProductClassificationNodeStatus ToDto(this ProductClassificationNodeStatus entityEnum)
        {
            return entityEnum;
        }

        /// <summary>
        /// Convert DTO ProductClassificationNodeStatus to entity ProductClassificationNodeStatus.
        /// Note: Since entities now use DTO enums directly, this just returns the same value.
        /// </summary>
        public static ProductClassificationNodeStatus ToEntity(this ProductClassificationNodeStatus dtoEnum)
        {
            return dtoEnum;
        }
    }
}