namespace EventForge.DTOs.RetailCart
{
    /// <summary>
    /// DTO for creating a new retail cart session.
    /// </summary>
    public class CreateCartSessionDto
    {
        /// <summary>
        /// Customer ID (optional).
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Sales channel (optional).
        /// </summary>
        public string? SalesChannel { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";
    }
}