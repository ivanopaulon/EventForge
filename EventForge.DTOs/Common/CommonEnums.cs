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
    /// Contact purpose enumeration for extended contact functionality.
    /// </summary>
    public enum ContactPurpose
    {
        Primary,        // Primary contact
        Emergency,      // Emergency contact
        Billing,        // Billing contact
        Coach,          // Team coach contact
        Medical,        // Medical contact
        Legal,          // Legal representative
        Other           // Other purpose
    }

    /// <summary>
    /// Product classification type enumeration.
    /// Reduced to Category, Family, MerchandiseGroup to match product taxonomy.
    /// </summary>
    public enum ProductClassificationType
    {
        Category,
        Family,
        MerchandiseGroup
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
        Attivo,     // Active and usable entry (keeping original Italian value)
        Active,     // Active and usable entry (English alternative)
        Deleted     // Deleted/disabled entry
    }

    /// <summary>
    /// Document status enumeration.
    /// </summary>
    public enum DocumentStatus
    {
        Draft,      // Document is in draft state
        Open,       // Document is open and being worked on
        Closed,     // Document is closed (finalized)
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
    /// Discount type enumeration.
    /// </summary>
    public enum DiscountType
    {
        /// <summary>
        /// Discount as percentage.
        /// </summary>
        Percentage,

        /// <summary>
        /// Discount as absolute value.
        /// </summary>
        Value
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
    /// Cash register (POS) status enumeration.
    /// </summary>
    public enum CashRegisterStatus
    {
        Active,         // POS is active and usable
        Suspended,      // Temporarily suspended
        Maintenance,    // Under maintenance
        Disabled        // Disabled/not usable
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
        Cliente,    // Customer (keeping original Italian value)
        Customer,   // Customer (English alternative)
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
        Discount,           // Percentage or fixed discount
        BuyXGetY,           // Buy X get Y free or discounted
        FixedPrice,         // Fixed price for a set of products
        Bundle,             // Bundle of products at special price
        CartAmountDiscount, // Discount if cart total exceeds a threshold
        CategoryDiscount,   // Discount on a product category
        CustomerSpecific,   // Discount for specific customers/groups
        Coupon,             // Requires coupon code
        TimeLimited,        // Valid only in certain time slots
        Exclusive           // Not combinable with other promotions
    }

    /// <summary>
    /// Payment method enumeration.
    /// </summary>
    public enum PaymentMethod
    {
        Cash,           // Cash payment
        Card,           // Card payment
        BankTransfer,   // Bank transfer
        Check,          // Check payment
        Other           // Other payment methods
    }

    /// <summary>
    /// Template access level enumeration.
    /// </summary>
    public enum TemplateAccessLevel
    {
        Public,
        Private,
        Team,
        Department,
        Organization
    }

    /// <summary>
    /// Recurrence pattern enumeration.
    /// </summary>
    public enum RecurrencePattern
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
        Custom
    }

    /// <summary>
    /// Recurrence status enumeration.
    /// </summary>
    public enum RecurrenceStatus
    {
        Active,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Workflow state enumeration.
    /// </summary>
    public enum WorkflowState
    {
        Draft,
        InProgress,
        PendingApproval,
        Approved,
        Rejected,
        OnHold,
        Cancelled,
        Completed
    }

    /// <summary>
    /// Workflow priority enumeration.
    /// </summary>
    public enum WorkflowPriority
    {
        Low,
        Normal,
        High,
        Critical,
        Urgent
    }

    /// <summary>
    /// Workflow step type enumeration.
    /// </summary>
    public enum WorkflowStepType
    {
        Approval,
        Review,
        Notification,
        AutoProcess,
        ConditionalBranch,
        DataValidation,
        ExternalIntegration,
        DigitalSignature
    }

    /// <summary>
    /// Workflow execution status enumeration.
    /// </summary>
    public enum WorkflowExecutionStatus
    {
        Started,
        InProgress,
        OnHold,
        Completed,
        Failed,
        Cancelled,
        Escalated
    }

    /// <summary>
    /// Workflow step status enumeration.
    /// </summary>
    public enum WorkflowStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Approved,
        Rejected,
        Skipped,
        Failed,
        OnHold,
        Escalated
    }

    /// <summary>
    /// Reminder type enumeration.
    /// </summary>
    public enum ReminderType
    {
        Deadline,
        Renewal,
        Review,
        Approval,
        Payment,
        Expiration,
        Followup,
        Custom
    }

    /// <summary>
    /// Reminder priority enumeration.
    /// </summary>
    public enum ReminderPriority
    {
        Low,
        Normal,
        High,
        Critical,
        Urgent
    }

    /// <summary>
    /// Reminder status enumeration.
    /// </summary>
    public enum ReminderStatus
    {
        Active,
        Pending,
        Sent,
        Snoozed,
        Completed,
        Cancelled,
        Expired
    }

    /// <summary>
    /// Schedule type enumeration.
    /// </summary>
    public enum ScheduleType
    {
        Renewal,
        Review,
        Audit,
        Backup,
        Cleanup,
        Notification,
        Report,
        Integration,
        Custom
    }

    /// <summary>
    /// Schedule frequency enumeration.
    /// </summary>
    public enum ScheduleFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly,
        Custom
    }

    /// <summary>
    /// Schedule priority enumeration.
    /// </summary>
    public enum SchedulePriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Schedule status enumeration.
    /// </summary>
    public enum ScheduleStatus
    {
        Active,
        Inactive,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Team member eligibility status enumeration.
    /// </summary>
    public enum EligibilityStatus
    {
        Eligible,       // Member is eligible to participate
        NotEligible,    // Member is not eligible (missing documents, etc.)
        Suspended,      // Member is temporarily suspended
        UnderReview     // Eligibility is under review
    }

    /// <summary>
    /// Document reference type enumeration for team management.
    /// </summary>
    public enum DocumentReferenceType
    {
        MedicalCertificate,     // Medical certificate
        MembershipCard,         // Federation membership card
        InsurancePolicy,        // Insurance policy document
        ProfilePhoto,           // Member profile photo
        TeamLogo,               // Team logo image
        IdentityDocument,       // Identity document (ID, passport)
        ParentalConsent,        // Parental consent for minors
        PrivacyConsent,         // Privacy/photo consent
        Other                   // Other document type
    }

    /// <summary>
    /// Document reference sub-type enumeration for more specific categorization.
    /// </summary>
    public enum DocumentReferenceSubType
    {
        None,                   // No specific sub-type
        ProfilePhoto,           // Profile photo
        TeamLogo,               // Team logo
        Thumbnail,              // Thumbnail image
        OriginalDocument,       // Original document
        CertifiedCopy,          // Certified copy
        Scan,                   // Scanned document
        DigitalSignature        // Digitally signed document
    }
}