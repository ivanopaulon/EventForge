namespace EventForge.Server.Extensions
{
    /// <summary>
    /// Extension methods to convert between entity enums and DTO enums.
    /// </summary>
    public static class EnumMappingExtensions
    {
        /// <summary>
        /// Convert entity AddressType to DTO AddressType.
        /// </summary>
        public static EventForge.DTOs.Common.AddressType ToDto(this Data.Entities.Common.AddressType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.AddressType.Legal => EventForge.DTOs.Common.AddressType.Legal,
                Data.Entities.Common.AddressType.Operational => EventForge.DTOs.Common.AddressType.Operational,
                Data.Entities.Common.AddressType.Destination => EventForge.DTOs.Common.AddressType.Destination,
                _ => EventForge.DTOs.Common.AddressType.Operational
            };
        }

        /// <summary>
        /// Convert DTO AddressType to entity AddressType.
        /// </summary>
        public static Data.Entities.Common.AddressType ToEntity(this EventForge.DTOs.Common.AddressType dtoEnum)
        {
            return dtoEnum switch
            {
                EventForge.DTOs.Common.AddressType.Legal => Data.Entities.Common.AddressType.Legal,
                EventForge.DTOs.Common.AddressType.Operational => Data.Entities.Common.AddressType.Operational,
                EventForge.DTOs.Common.AddressType.Destination => Data.Entities.Common.AddressType.Destination,
                _ => Data.Entities.Common.AddressType.Operational
            };
        }

        /// <summary>
        /// Convert entity ContactType to DTO ContactType.
        /// </summary>
        public static EventForge.DTOs.Common.ContactType ToDto(this Data.Entities.Common.ContactType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ContactType.Email => EventForge.DTOs.Common.ContactType.Email,
                Data.Entities.Common.ContactType.Phone => EventForge.DTOs.Common.ContactType.Phone,
                Data.Entities.Common.ContactType.Fax => EventForge.DTOs.Common.ContactType.Fax,
                Data.Entities.Common.ContactType.PEC => EventForge.DTOs.Common.ContactType.PEC,
                Data.Entities.Common.ContactType.Other => EventForge.DTOs.Common.ContactType.Other,
                _ => EventForge.DTOs.Common.ContactType.Other
            };
        }

        /// <summary>
        /// Convert DTO ContactType to entity ContactType.
        /// </summary>
        public static Data.Entities.Common.ContactType ToEntity(this EventForge.DTOs.Common.ContactType dtoEnum)
        {
            return dtoEnum switch
            {
                EventForge.DTOs.Common.ContactType.Email => Data.Entities.Common.ContactType.Email,
                EventForge.DTOs.Common.ContactType.Phone => Data.Entities.Common.ContactType.Phone,
                EventForge.DTOs.Common.ContactType.Fax => Data.Entities.Common.ContactType.Fax,
                EventForge.DTOs.Common.ContactType.PEC => Data.Entities.Common.ContactType.PEC,
                EventForge.DTOs.Common.ContactType.Other => Data.Entities.Common.ContactType.Other,
                _ => Data.Entities.Common.ContactType.Other
            };
        }

        /// <summary>
        /// Convert entity ProductClassificationType to DTO ProductClassificationType.
        /// </summary>
        public static EventForge.DTOs.Common.ProductClassificationType ToDto(this Data.Entities.Common.ProductClassificationType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ProductClassificationType.Category => EventForge.DTOs.Common.ProductClassificationType.Category,
                Data.Entities.Common.ProductClassificationType.Subcategory => EventForge.DTOs.Common.ProductClassificationType.Subcategory,
                Data.Entities.Common.ProductClassificationType.Brand => EventForge.DTOs.Common.ProductClassificationType.Brand,
                Data.Entities.Common.ProductClassificationType.Line => EventForge.DTOs.Common.ProductClassificationType.Line,
                _ => EventForge.DTOs.Common.ProductClassificationType.Category
            };
        }

        /// <summary>
        /// Convert DTO ProductClassificationType to entity ProductClassificationType.
        /// </summary>
        public static Data.Entities.Common.ProductClassificationType ToEntity(this EventForge.DTOs.Common.ProductClassificationType dtoEnum)
        {
            return dtoEnum switch
            {
                EventForge.DTOs.Common.ProductClassificationType.Category => Data.Entities.Common.ProductClassificationType.Category,
                EventForge.DTOs.Common.ProductClassificationType.Subcategory => Data.Entities.Common.ProductClassificationType.Subcategory,
                EventForge.DTOs.Common.ProductClassificationType.Brand => Data.Entities.Common.ProductClassificationType.Brand,
                EventForge.DTOs.Common.ProductClassificationType.Line => Data.Entities.Common.ProductClassificationType.Line,
                _ => Data.Entities.Common.ProductClassificationType.Category
            };
        }

        /// <summary>
        /// Convert entity ProductClassificationNodeStatus to DTO ProductClassificationNodeStatus.
        /// </summary>
        public static EventForge.DTOs.Common.ProductClassificationNodeStatus ToDto(this Data.Entities.Common.ProductClassificationNodeStatus entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ProductClassificationNodeStatus.Active => EventForge.DTOs.Common.ProductClassificationNodeStatus.Active,
                Data.Entities.Common.ProductClassificationNodeStatus.Inactive => EventForge.DTOs.Common.ProductClassificationNodeStatus.Inactive,
                Data.Entities.Common.ProductClassificationNodeStatus.Pending => EventForge.DTOs.Common.ProductClassificationNodeStatus.Pending,
                _ => EventForge.DTOs.Common.ProductClassificationNodeStatus.Active
            };
        }

        /// <summary>
        /// Convert DTO ProductClassificationNodeStatus to entity ProductClassificationNodeStatus.
        /// </summary>
        public static Data.Entities.Common.ProductClassificationNodeStatus ToEntity(this EventForge.DTOs.Common.ProductClassificationNodeStatus dtoEnum)
        {
            return dtoEnum switch
            {
                EventForge.DTOs.Common.ProductClassificationNodeStatus.Active => Data.Entities.Common.ProductClassificationNodeStatus.Active,
                EventForge.DTOs.Common.ProductClassificationNodeStatus.Inactive => Data.Entities.Common.ProductClassificationNodeStatus.Inactive,
                EventForge.DTOs.Common.ProductClassificationNodeStatus.Pending => Data.Entities.Common.ProductClassificationNodeStatus.Pending,
                _ => Data.Entities.Common.ProductClassificationNodeStatus.Active
            };
        }
    }
}