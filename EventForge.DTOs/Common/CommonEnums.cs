namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Address type enumeration.
    /// </summary>
    public enum AddressType
    {
        Legal,
        Operational,
        Destination
    }

    /// <summary>
    /// Contact type enumeration.
    /// </summary>
    public enum ContactType
    {
        Email,
        Phone,
        Fax,
        PEC,
        Other
    }

    /// <summary>
    /// Product classification type enumeration.
    /// </summary>
    public enum ProductClassificationType
    {
        Category,
        Subcategory,
        Brand,
        Line
    }

    /// <summary>
    /// Product classification node status enumeration.
    /// </summary>
    public enum ProductClassificationNodeStatus
    {
        Active,
        Inactive,
        Pending
    }
}