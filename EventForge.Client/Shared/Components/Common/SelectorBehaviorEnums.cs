namespace EventForge.Client.Shared.Components.Common;

/// <summary>
/// Defines how entity editing should be handled in unified selectors
/// </summary>
public enum EntityEditMode
{
    None,           // No editing allowed
    QuickDialog,    // Opens quick edit dialog
    FullPage,       // Opens full detail page
    Delegate        // Delegates to parent via event
}

/// <summary>
/// Defines how entity creation should be handled
/// </summary>
public enum EntityCreateMode
{
    None,           // No creation allowed
    QuickDialog,    // Opens quick create dialog
    Prompt,         // Shows inline prompt
    Delegate        // Delegates to parent via event
}

/// <summary>
/// Defines what information should be displayed
/// </summary>
[Flags]
public enum EntityDisplayMode
{
    Basic = 1,          // Name only
    FiscalInfo = 2,     // VAT, Tax Code
    Address = 4,        // Full address
    Contacts = 8,       // Preferred contact
    Groups = 16,        // Business party groups
    All = Basic | FiscalInfo | Address | Contacts | Groups
}
