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

    /// <summary>
    /// Event status enumeration.
    /// </summary>
    public enum EventStatus
    {
        Planned,    // Planned
        Ongoing,    // Ongoing
        Completed,  // Completed
        Cancelled   // Cancelled
    }

    /// <summary>
    /// Team status enumeration.
    /// </summary>
    public enum TeamStatus
    {
        Active,     // The team is active and participating
        Suspended,  // The team is temporarily suspended
        Retired,    // The team has withdrawn from the event
        Deleted     // The team has been eliminated
    }

    /// <summary>
    /// Team member status enumeration.
    /// </summary>
    public enum TeamMemberStatus
    {
        Active,     // The member is actively participating
        Suspended,  // The member is temporarily suspended
        Inactive    // The member is inactive
    }

    /// <summary>
    /// Product status enumeration.
    /// </summary>
    public enum ProductStatus
    {
        Active,     // Product is active and available
        Suspended,  // Product is temporarily suspended
        OutOfStock, // Product is out of stock
        Deleted     // Product is deleted/disabled
    }

    /// <summary>
    /// Product code status enumeration.
    /// </summary>
    public enum ProductCodeStatus
    {
        Active,     // Code is active and usable
        Suspended,  // Code is temporarily suspended
        Deleted     // Code is deleted/disabled
    }

    /// <summary>
    /// Product unit status enumeration.
    /// </summary>
    public enum ProductUnitStatus
    {
        Active,     // Unit is active and usable
        Suspended,  // Unit is temporarily suspended
        Deleted     // Unit is deleted/disabled
    }

    /// <summary>
    /// Price list status enumeration.
    /// </summary>
    public enum PriceListStatus
    {
        Active,     // Price list is active and usable
        Suspended,  // Temporarily suspended
        Deleted     // Price list is deleted/disabled
    }

    /// <summary>
    /// Price list entry status enumeration.
    /// </summary>
    public enum PriceListEntryStatus
    {
        Active,     // Active and usable entry
        Deleted     // Deleted/disabled entry
    }

    /// <summary>
    /// Document status enumeration.
    /// </summary>
    public enum DocumentStatus
    {
        Draft,      // Document is in draft state
        Approved,   // Document is approved
        Rejected,   // Document is rejected
        Cancelled   // Document is cancelled
    }

    /// <summary>
    /// Document row type enumeration.
    /// </summary>
    public enum DocumentRowType
    {
        Product,    // Product row
        Service,    // Service row
        Note        // Note row
    }

    /// <summary>
    /// Payment status enumeration.
    /// </summary>
    public enum PaymentStatus
    {
        Pending,    // Payment is pending
        Paid,       // Payment is completed
        Partial,    // Payment is partially completed
        Cancelled   // Payment is cancelled
    }

    /// <summary>
    /// Approval status enumeration.
    /// </summary>
    public enum ApprovalStatus
    {
        Pending,    // Approval is pending
        Approved,   // Approved
        Rejected    // Rejected
    }

    /// <summary>
    /// Cashier status enumeration.
    /// </summary>
    public enum CashierStatus
    {
        Active,      // Cashier is active and usable
        Suspended,   // Temporarily suspended
        Deleted      // Cashier is deleted/disabled
    }

    /// <summary>
    /// Cashier group status enumeration.
    /// </summary>
    public enum CashierGroupStatus
    {
        Active,      // Group is active and assignable
        Suspended,   // Group is temporarily suspended
        Deleted      // Group is deleted/disabled
    }

    /// <summary>
    /// Cashier privilege status enumeration.
    /// </summary>
    public enum CashierPrivilegeStatus
    {
        Active,      // Privilege is active and assignable
        Suspended,   // Privilege is temporarily suspended
        Deleted      // Privilege is deleted/disabled
    }

    /// <summary>
    /// Printer status enumeration.
    /// </summary>
    public enum PrinterStatus
    {
        Active,         // Printer is ready and working
        Offline,        // Printer is offline
        Error,          // Printer has an error
        Suspended       // Printer is temporarily suspended
    }

    /// <summary>
    /// Station status enumeration.
    /// </summary>
    public enum StationStatus
    {
        Active,         // Station is active and operational
        Offline,        // Station is offline
        Maintenance,    // Station is under maintenance
        Suspended       // Station is temporarily suspended
    }

    /// <summary>
    /// VAT rate status enumeration.
    /// </summary>
    public enum VatRateStatus
    {
        Active,     // VAT rate is active and usable
        Suspended,  // VAT rate is temporarily suspended
        Deleted     // VAT rate is deleted/disabled
    }

    /// <summary>
    /// Business party type enumeration.
    /// </summary>
    public enum BusinessPartyType
    {
        Customer,   // Customer
        Supplier,   // Supplier
        Both        // Both customer and supplier
    }

    /// <summary>
    /// Storage location status enumeration.
    /// </summary>
    public enum StorageLocationStatus
    {
        Active,     // Location is active and usable
        Suspended,  // Location is temporarily suspended
        Deleted     // Location is deleted/disabled
    }

    /// <summary>
    /// Storage facility status enumeration.
    /// </summary>
    public enum StorageFacilityStatus
    {
        Active,     // Facility is active and usable
        Suspended,  // Facility is temporarily suspended
        Deleted     // Facility is deleted/disabled
    }

    /// <summary>
    /// VAT rate status for products enumeration.
    /// </summary>
    public enum ProductVatRateStatus
    {
        Active,     // VAT rate is active and usable
        Suspended,  // VAT rate is temporarily suspended
        Deleted     // VAT rate is deleted/disabled
    }

    /// <summary>
    /// Promotion rule type enumeration.
    /// </summary>
    public enum PromotionRuleType
    {
        Percentage, // Percentage discount
        Fixed,      // Fixed amount discount
        BuyXGetY    // Buy X get Y promotion
    }

    /// <summary>
    /// Payment method enumeration.
    /// </summary>
    public enum PaymentMethod
    {
        Cash,       // Cash payment
        Card,       // Card payment
        Transfer,   // Bank transfer
        Check,      // Check payment
        Other       // Other payment methods
    }
}