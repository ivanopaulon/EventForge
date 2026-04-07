namespace Prym.Server.Services.FiscalPrinting.CustomProtocol;

/// <summary>
/// Constants for the Custom fiscal printer communication protocol.
/// Defines control characters, command codes, department codes, and error codes
/// used to build and parse messages exchanged with Custom fiscal printers.
/// </summary>
public static class CustomProtocolCommands
{
    // -------------------------------------------------------------------------
    //  Control characters
    // -------------------------------------------------------------------------

    /// <summary>Start of Text – marks the beginning of a command frame.</summary>
    public const byte STX = 0x02;

    /// <summary>End of Text – marks the end of the command data before the checksum byte.</summary>
    public const byte ETX = 0x03;

    /// <summary>Acknowledgement – sent by the printer to confirm a command was accepted.</summary>
    public const byte ACK = 0x06;

    /// <summary>Negative Acknowledgement – sent by the printer when a command is rejected.</summary>
    public const byte NAK = 0x15;

    /// <summary>Enquiry – used to request the current printer status (CMD_READ_STATUS).</summary>
    public const byte ENQ = 0x05;

    /// <summary>Field Separator – separates fields within a command frame.</summary>
    public const byte FS = 0x1C;

    /// <summary>Escape – used for special encoding sequences.</summary>
    public const byte ESC = 0x1B;

    // -------------------------------------------------------------------------
    //  Receipt commands (base)
    // -------------------------------------------------------------------------

    /// <summary>Opens a new fiscal receipt. Must be sent before any item commands.</summary>
    public const string CMD_OPEN_RECEIPT = "01";

    /// <summary>Prints a single item (vendita base) on the receipt. Fields: description, quantity, unit price, VAT code, department.</summary>
    public const string CMD_PRINT_ITEM = "02";

    /// <summary>
    /// Prints an item with a discount (sconto su riga).
    /// Fields: description, quantity, unit price, VAT code, department, discount value, discount type (P or A).
    /// Use <see cref="DISCOUNT_TYPE_PERCENTAGE"/> or <see cref="DISCOUNT_TYPE_AMOUNT"/> for the discount type field.
    /// </summary>
    public const string CMD_PRINT_ITEM_WITH_DISCOUNT = "02S";

    /// <summary>
    /// Prints an item with a surcharge (maggiorazione su riga).
    /// Fields: description, quantity, unit price, VAT code, department, surcharge value, surcharge type (P or A).
    /// Use <see cref="DISCOUNT_TYPE_PERCENTAGE"/> or <see cref="DISCOUNT_TYPE_AMOUNT"/> for the surcharge type field.
    /// </summary>
    public const string CMD_PRINT_ITEM_WITH_SURCHARGE = "02M";

    /// <summary>
    /// Prints a free item (omaggio / gift).
    /// The item is printed at zero price with the ITEM_FLAG_FREE flag.
    /// Fields: description, quantity, unit price (original), VAT code, department.
    /// </summary>
    public const string CMD_PRINT_ITEM_FREE = "02G";

    /// <summary>
    /// Prints the receipt subtotal.
    /// Can be sent before payment commands to display the running total.
    /// </summary>
    public const string CMD_SUBTOTAL = "03";

    /// <summary>
    /// Applies a global discount to the entire receipt (sconto globale scontrino).
    /// Must be sent after the last item and before CMD_PAYMENT.
    /// Fields: discount value, discount type (P or A), description.
    /// Use <see cref="DISCOUNT_TYPE_PERCENTAGE"/> or <see cref="DISCOUNT_TYPE_AMOUNT"/> for the type field.
    /// </summary>
    public const string CMD_GLOBAL_DISCOUNT = "03S";

    /// <summary>
    /// Applies a global surcharge to the entire receipt (maggiorazione globale – e.g., cover charge).
    /// Must be sent after the last item and before CMD_PAYMENT.
    /// Fields: surcharge value, surcharge type (P or A), description, VAT code.
    /// Use <see cref="DISCOUNT_TYPE_PERCENTAGE"/> or <see cref="DISCOUNT_TYPE_AMOUNT"/> for the type field.
    /// </summary>
    public const string CMD_GLOBAL_SURCHARGE = "03M";

    /// <summary>Registers a payment method and amount. Repeat for multiple payment methods.</summary>
    public const string CMD_PAYMENT = "04";

    /// <summary>Closes the fiscal receipt and triggers the physical print.</summary>
    public const string CMD_CLOSE_RECEIPT = "05";

    // -------------------------------------------------------------------------
    //  Cancel / refund commands
    // -------------------------------------------------------------------------

    /// <summary>Cancels the currently open receipt (annullo scontrino). Only valid when a receipt is open.</summary>
    public const string CMD_CANCEL_RECEIPT = "30";

    /// <summary>Prints a full refund receipt (reso totale) referencing the original receipt.</summary>
    public const string CMD_REFUND_RECEIPT = "31";

    /// <summary>Prints a partial refund receipt (reso parziale) for one or more items.</summary>
    public const string CMD_REFUND_ITEM = "32";

    // -------------------------------------------------------------------------
    //  Non-fiscal / descriptive commands
    // -------------------------------------------------------------------------

    /// <summary>Prints a non-fiscal descriptive line (used for loyalty/fidelity information, custom messages).</summary>
    public const string CMD_PRINT_DESCRIPTIVE = "20";

    /// <summary>Prints a visual separator line on the receipt.</summary>
    public const string CMD_PRINT_SEPARATOR = "21";

    // -------------------------------------------------------------------------
    //  Management commands
    // -------------------------------------------------------------------------

    /// <summary>Executes the daily fiscal closure (chiusura giornaliera / Z-report). Irreversible operation.</summary>
    public const string CMD_DAILY_CLOSURE = "50";

    /// <summary>Reads the data from the last daily fiscal closure (numero scontrini, totale, data).</summary>
    public const string CMD_READ_DAILY_CLOSURE = "51";

    /// <summary>
    /// Reads the current printer status.
    /// The response contains a 3-byte bitmap with error flags, warning flags, and operational state flags.
    /// </summary>
    public const string CMD_READ_STATUS = "10";

    /// <summary>Reads the current date and time from the fiscal printer's internal clock.</summary>
    public const string CMD_READ_DATETIME = "11";

    /// <summary>Opens the cash drawer connected to the fiscal printer.</summary>
    public const string CMD_OPEN_DRAWER = "40";

    // -------------------------------------------------------------------------
    //  Department codes
    // -------------------------------------------------------------------------

    /// <summary>Default department (reparto generico).</summary>
    public const int DEPT_DEFAULT = 1;

    /// <summary>Food department (reparto alimentari).</summary>
    public const int DEPT_FOOD = 2;

    /// <summary>Beverage department (reparto bevande).</summary>
    public const int DEPT_BEVERAGE = 3;

    /// <summary>Non-food department (reparto non-alimentari).</summary>
    public const int DEPT_NON_FOOD = 4;

    // -------------------------------------------------------------------------
    //  Discount / surcharge type flags
    // -------------------------------------------------------------------------

    /// <summary>
    /// Discount or surcharge expressed as a percentage of the item or receipt total.
    /// Used as the "type" field in <see cref="CMD_PRINT_ITEM_WITH_DISCOUNT"/>,
    /// <see cref="CMD_PRINT_ITEM_WITH_SURCHARGE"/>, <see cref="CMD_GLOBAL_DISCOUNT"/>,
    /// and <see cref="CMD_GLOBAL_SURCHARGE"/>.
    /// Example: value "1000" with type "P" means -10.00%.
    /// </summary>
    public const string DISCOUNT_TYPE_PERCENTAGE = "P";

    /// <summary>
    /// Discount or surcharge expressed as a fixed monetary amount.
    /// Used as the "type" field in <see cref="CMD_PRINT_ITEM_WITH_DISCOUNT"/>,
    /// <see cref="CMD_PRINT_ITEM_WITH_SURCHARGE"/>, <see cref="CMD_GLOBAL_DISCOUNT"/>,
    /// and <see cref="CMD_GLOBAL_SURCHARGE"/>.
    /// Example: value "500" with type "A" means -€5.00.
    /// </summary>
    public const string DISCOUNT_TYPE_AMOUNT = "A";

    // -------------------------------------------------------------------------
    //  Item flags
    // -------------------------------------------------------------------------

    /// <summary>
    /// Normal item flag. Used for regular sales with <see cref="CMD_PRINT_ITEM"/>
    /// and discount/surcharge variants.
    /// </summary>
    public const string ITEM_FLAG_NORMAL = "0";

    /// <summary>
    /// Free/gift item flag (omaggio). Used with <see cref="CMD_PRINT_ITEM_FREE"/>
    /// to indicate the item is given at no charge.
    /// The original price is printed but the amount is not added to the total.
    /// </summary>
    public const string ITEM_FLAG_FREE = "1";

    /// <summary>
    /// Return/refund item flag (reso). Used for return transactions where the
    /// quantity is negative and the amount is credited back.
    /// </summary>
    public const string ITEM_FLAG_RETURN = "2";

    // -------------------------------------------------------------------------
    //  Error codes
    // -------------------------------------------------------------------------

    /// <summary>Error E001 – Paper out. The printer has run out of paper and cannot print.</summary>
    public const string ERR_OUT_OF_PAPER = "E001";

    /// <summary>Error E002 – Fiscal memory full. The printer's fiscal memory is at 100% capacity. Requires authorised technical intervention.</summary>
    public const string ERR_FISCAL_MEMORY_FULL = "E002";

    /// <summary>Error E003 – Communication error. Failed to send or receive data from the printer.</summary>
    public const string ERR_COMMUNICATION = "E003";

    /// <summary>Error E004 – Invalid command. The printer did not recognise the command code.</summary>
    public const string ERR_INVALID_COMMAND = "E004";

    /// <summary>Error E005 – Receipt already open. Cannot open a new receipt while one is already in progress.</summary>
    public const string ERR_RECEIPT_ALREADY_OPEN = "E005";

    /// <summary>Error E006 – No receipt open. Cannot perform item/payment/close operations without an open receipt.</summary>
    public const string ERR_NO_RECEIPT_OPEN = "E006";
}
