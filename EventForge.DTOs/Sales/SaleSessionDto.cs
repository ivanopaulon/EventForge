namespace EventForge.DTOs.Sales
{

    /// <summary>
    /// DTO for a complete sale session with all details.
    /// </summary>
    public class SaleSessionDto
    {
        /// <summary>
        /// Session unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Operator (cashier) identifier.
        /// </summary>
        public Guid OperatorId { get; set; }

        /// <summary>
        /// Operator name.
        /// </summary>
        public string? OperatorName { get; set; }

        /// <summary>
        /// POS terminal identifier.
        /// </summary>
        public Guid PosId { get; set; }

        /// <summary>
        /// POS name.
        /// </summary>
        public string? PosName { get; set; }

        /// <summary>
        /// Customer identifier.
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Customer name.
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Sale type.
        /// </summary>
        public string? SaleType { get; set; }

        /// <summary>
        /// Session status.
        /// </summary>
        public SaleSessionStatusDto Status { get; set; }

        /// <summary>
        /// Total amount before discounts.
        /// </summary>
        public decimal OriginalTotal { get; set; }

        /// <summary>
        /// Total discount amount.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Final total after discounts.
        /// </summary>
        public decimal FinalTotal { get; set; }

        /// <summary>
        /// Tax amount.
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Session items.
        /// </summary>
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();

        /// <summary>
        /// Session payments.
        /// </summary>
        public List<SalePaymentDto> Payments { get; set; } = new List<SalePaymentDto>();

        /// <summary>
        /// Session notes.
        /// </summary>
        public List<SessionNoteDto> Notes { get; set; } = new List<SessionNoteDto>();

        /// <summary>
        /// Table identifier (for bar/restaurant).
        /// </summary>
        public Guid? TableId { get; set; }

        /// <summary>
        /// Table number.
        /// </summary>
        public string? TableNumber { get; set; }

        /// <summary>
        /// Document identifier if created.
        /// </summary>
        public Guid? DocumentId { get; set; }

        /// <summary>
        /// Session created timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Session updated timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Session closed timestamp.
        /// </summary>
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Applied coupon codes.
        /// </summary>
        public string? CouponCodes { get; set; }

        /// <summary>
        /// Parent session ID if this is a split/merged child session.
        /// </summary>
        public Guid? ParentSessionId { get; set; }

        /// <summary>
        /// Split type if this session was created from a split.
        /// </summary>
        public string? SplitType { get; set; }

        /// <summary>
        /// Split percentage if split by percentage.
        /// </summary>
        public decimal? SplitPercentage { get; set; }

        /// <summary>
        /// Merge reason if this session is result of merge.
        /// </summary>
        public string? MergeReason { get; set; }

        /// <summary>
        /// Number of child sessions (0 if no children).
        /// </summary>
        public int ChildSessionCount { get; set; }

        /// <summary>
        /// Amount remaining to be paid.
        /// </summary>
        public decimal RemainingAmount => FinalTotal - Payments.Where(p => p.Status == PaymentStatusDto.Completed).Sum(p => p.Amount);

        /// <summary>
        /// Indicates if the session is fully paid.
        /// </summary>
        public bool IsFullyPaid => RemainingAmount <= 0;
    }
}
