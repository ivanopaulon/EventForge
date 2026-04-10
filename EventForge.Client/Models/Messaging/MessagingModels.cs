namespace EventForge.Client.Models.Messaging;

/// <summary>
/// Identifies the origin channel of a conversation.
/// </summary>
public enum MessagingChannel { Internal, WhatsApp }

/// <summary>
/// Presence / availability state of a user.
/// </summary>
public enum PresenceStatus { Online, Away, Busy, Offline }

/// <summary>
/// A single rendered message bubble inside a conversation.
/// </summary>
public sealed class MessageEntry
{
    public required string Text        { get; init; }
    public bool            IsMe        { get; init; }
    public required string SenderName  { get; init; }
    public required string Time        { get; init; }
}

/// <summary>
/// View-model for one entry in the sidebar conversation list.
/// </summary>
public sealed class ConversationItem
{
    public Guid              Id           { get; init; }
    public required string   Name         { get; init; }
    public required string   Initials     { get; init; }
    public required string   AvatarColor  { get; init; }
    public MessagingChannel  Channel      { get; init; }
    public PresenceStatus    Presence     { get; set;  }
    public required string   LastMessage  { get; set;  }
    public required string   LastTime     { get; set;  }
    public int               UnreadCount  { get; set;  }
    public List<MessageEntry> Messages    { get; init; } = [];
}

/// <summary>
/// View-model for a selectable contact in StartConversationDialog.
/// </summary>
public sealed class ContactItem
{
    public Guid             Id          { get; init; }
    public required string  Name        { get; init; }
    public required string  Initials    { get; init; }
    public required string  AvatarColor { get; init; }
    public MessagingChannel Channel     { get; init; }
    public PresenceStatus   Presence    { get; init; }
    /// <summary>Role/department — for internal users only.</summary>
    public string?          Role        { get; init; }
    /// <summary>Phone number — for WhatsApp contacts only.</summary>
    public string?          Phone       { get; init; }
}
