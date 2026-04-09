namespace EventForge.DTOs.External.WhatsApp;

/// <summary>
/// DTO that represents an order received from WhatsApp, enriched by the server
/// before being stored or forwarded to internal order-management workflows.
/// </summary>
public class WhatsAppOrderReceivedDto
{
    /// <summary>Unique WhatsApp message ID (wamid.*).</summary>
    public string WhatsAppMessageId { get; set; } = string.Empty;

    /// <summary>Sender phone number (without "+" prefix).</summary>
    public string SenderPhone { get; set; } = string.Empty;

    /// <summary>Display name from the sender's WhatsApp profile.</summary>
    public string? SenderName { get; set; }

    /// <summary>WhatsApp Catalog ID that was used.</summary>
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>Optional note written by the customer.</summary>
    public string? CustomerNote { get; set; }

    /// <summary>UTC timestamp when the order message was sent by the customer.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Line items of the order.</summary>
    public List<WhatsAppOrderLineDto> Lines { get; set; } = [];

    /// <summary>
    /// ID of the internal <c>RetailCartSession</c> or <c>DocumentHeader</c> created for this
    /// order, set after the order has been persisted.
    /// </summary>
    public Guid? InternalOrderId { get; set; }

    /// <summary>Processing status of the order within EventForge.</summary>
    public WhatsAppOrderStatus ProcessingStatus { get; set; } = WhatsAppOrderStatus.Received;

    /// <summary>Human-readable message describing the processing result.</summary>
    public string? ProcessingMessage { get; set; }
}

/// <summary>A single line item of a WhatsApp order.</summary>
public class WhatsAppOrderLineDto
{
    /// <summary>Retailer-defined product ID (maps to a product code in EventForge).</summary>
    public string ProductRetailerId { get; set; } = string.Empty;

    /// <summary>Quantity ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Unit price declared by WhatsApp catalog at order time.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Total line amount (Quantity × UnitPrice).</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Processing status of a WhatsApp-sourced order.</summary>
public enum WhatsAppOrderStatus
{
    /// <summary>Message received but not yet processed.</summary>
    Received,
    /// <summary>Order was successfully created in the internal system.</summary>
    Created,
    /// <summary>Order could not be processed due to an error.</summary>
    Failed,
    /// <summary>Order was acknowledged but awaits manual review.</summary>
    PendingReview
}
