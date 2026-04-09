using System.Text.Json.Serialization;

namespace EventForge.DTOs.External.WhatsApp;

// ─────────────────────────────────────────────────────────────────────────────
//  Root payload sent by the Meta platform to the webhook endpoint.
//  Reference: https://developers.facebook.com/docs/whatsapp/cloud-api/webhooks/payload-examples
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Root object of every Meta WhatsApp Cloud API webhook notification.
/// </summary>
public class WhatsAppWebhookPayload
{
    /// <summary>Always "whatsapp_business_account".</summary>
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    /// <summary>One entry per WhatsApp Business Account involved in the event.</summary>
    [JsonPropertyName("entry")]
    public List<WhatsAppEntry> Entry { get; set; } = [];
}

/// <summary>Entry for a single WhatsApp Business Account.</summary>
public class WhatsAppEntry
{
    /// <summary>WhatsApp Business Account ID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Changes reported in this entry.</summary>
    [JsonPropertyName("changes")]
    public List<WhatsAppChange> Changes { get; set; } = [];
}

/// <summary>A single change event inside an entry.</summary>
public class WhatsAppChange
{
    /// <summary>The value payload for this change.</summary>
    [JsonPropertyName("value")]
    public WhatsAppChangeValue Value { get; set; } = new();

    /// <summary>Type of field that changed (usually "messages").</summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
}

/// <summary>Detailed value payload inside a change.</summary>
public class WhatsAppChangeValue
{
    /// <summary>Always "whatsapp".</summary>
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = string.Empty;

    /// <summary>Phone number metadata for the receiving business number.</summary>
    [JsonPropertyName("metadata")]
    public WhatsAppMetadata Metadata { get; set; } = new();

    /// <summary>Sender contact profiles (present when messages are included).</summary>
    [JsonPropertyName("contacts")]
    public List<WhatsAppContact>? Contacts { get; set; }

    /// <summary>Incoming messages.</summary>
    [JsonPropertyName("messages")]
    public List<WhatsAppMessage>? Messages { get; set; }

    /// <summary>Delivery / read status updates.</summary>
    [JsonPropertyName("statuses")]
    public List<WhatsAppStatus>? Statuses { get; set; }
}

/// <summary>Metadata about the business phone number that received the event.</summary>
public class WhatsAppMetadata
{
    /// <summary>Display phone number of the business (e.g. "+39 02 1234567").</summary>
    [JsonPropertyName("display_phone_number")]
    public string DisplayPhoneNumber { get; set; } = string.Empty;

    /// <summary>ID of the business phone number (used in API calls).</summary>
    [JsonPropertyName("phone_number_id")]
    public string PhoneNumberId { get; set; } = string.Empty;
}

/// <summary>Contact profile of the message sender.</summary>
public class WhatsAppContact
{
    [JsonPropertyName("profile")]
    public WhatsAppProfile Profile { get; set; } = new();

    /// <summary>WhatsApp user ID (phone number without "+" prefix).</summary>
    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty;
}

/// <summary>Display name as stored in the user's profile.</summary>
public class WhatsAppProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A single incoming message from a customer.
/// Supports text, order, and interactive message types.
/// </summary>
public class WhatsAppMessage
{
    /// <summary>Sender phone number (without "+" prefix).</summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>Unique WhatsApp message ID (wamid.*).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Unix epoch timestamp (seconds) when the message was sent by the user.</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Message type: "text", "order", "interactive", "image", etc.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Populated when <see cref="Type"/> is "text".</summary>
    [JsonPropertyName("text")]
    public WhatsAppTextBody? Text { get; set; }

    /// <summary>Populated when <see cref="Type"/> is "order" (customer placed a catalog order).</summary>
    [JsonPropertyName("order")]
    public WhatsAppOrderMessage? Order { get; set; }
}

/// <summary>Plain-text message body.</summary>
public class WhatsAppTextBody
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

/// <summary>Order message sent when a customer completes a catalog order on WhatsApp.</summary>
public class WhatsAppOrderMessage
{
    /// <summary>WhatsApp Catalog ID.</summary>
    [JsonPropertyName("catalog_id")]
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>Optional free-text note added by the customer.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Line items in the order.</summary>
    [JsonPropertyName("product_items")]
    public List<WhatsAppProductItem> ProductItems { get; set; } = [];
}

/// <summary>A single product line item within a WhatsApp order.</summary>
public class WhatsAppProductItem
{
    /// <summary>The retailer-defined product ID (matches product code in the catalog).</summary>
    [JsonPropertyName("product_retailer_id")]
    public string ProductRetailerId { get; set; } = string.Empty;

    /// <summary>Quantity ordered.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>Unit price at the time of order.</summary>
    [JsonPropertyName("item_price")]
    public decimal ItemPrice { get; set; }

    /// <summary>ISO 4217 currency code (e.g. "EUR").</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}

/// <summary>Delivery or read status update for a previously sent message.</summary>
public class WhatsAppStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Status value: "sent", "delivered", "read", "failed".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;
}
