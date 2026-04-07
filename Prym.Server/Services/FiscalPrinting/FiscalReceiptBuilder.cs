using Prym.DTOs.FiscalPrinting;
using Prym.Server.Services.FiscalPrinting.CustomProtocol;

namespace Prym.Server.Services.FiscalPrinting;

/// <summary>
/// Builds the ordered sequence of binary command frames for a fiscal receipt,
/// refund, daily closure, or other operation.
/// Uses <see cref="CustomCommandBuilder"/> internally and mirrors the 11 POS
/// scenarios defined in the Custom protocol specification.
/// </summary>
/// <remarks>
/// Instantiate once per receipt operation. All <c>Build*</c> methods are pure
/// (no side effects) and return independent byte arrays ready to be sent via
/// <see cref="Communication.ICustomPrinterCommunication.SendCommandAsync"/>.
/// </remarks>
public sealed class FiscalReceiptBuilder
{
    // -------------------------------------------------------------------------
    //  Loyalty line templates
    // -------------------------------------------------------------------------

    private const int MaxDescriptiveLineLength = 40;

    // -------------------------------------------------------------------------
    //  Open / close receipt
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the <c>CMD_OPEN_RECEIPT</c> ("01") command frame.
    /// Must be the first command sent at the start of every receipt.
    /// </summary>
    public byte[] BuildOpenReceiptCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_OPEN_RECEIPT)
            .Build();

    /// <summary>
    /// Builds the <c>CMD_CLOSE_RECEIPT</c> ("05") command frame.
    /// Must be the last command sent after all payments are registered.
    /// </summary>
    public byte[] BuildCloseReceiptCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_CLOSE_RECEIPT)
            .Build();

    // -------------------------------------------------------------------------
    //  Items (all 11 POS scenarios)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the appropriate item command frame for the given <paramref name="item"/>,
    /// selecting among CMD_PRINT_ITEM ("02"), CMD_PRINT_ITEM_WITH_DISCOUNT ("02S"),
    /// CMD_PRINT_ITEM_WITH_SURCHARGE ("02M"), and CMD_PRINT_ITEM_FREE ("02G")
    /// based on <see cref="FiscalReceiptItem.ItemFlag"/> and <see cref="FiscalReceiptItem.Discount"/>.
    /// </summary>
    /// <param name="item">The receipt line item to encode.</param>
    /// <returns>One binary command frame.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
    public byte[] BuildPrintItemCommand(FiscalReceiptItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Case 5 – Omaggio (free item): CMD_PRINT_ITEM_FREE "02G"
        if (item.ItemFlag == CustomProtocolCommands.ITEM_FLAG_FREE)
        {
            return new CustomCommandBuilder()
                .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_FREE)
                .AddField(item.Description)
                .AddField(item.Quantity)
                .AddField(item.UnitPrice)
                .AddField(item.VatCode)
                .AddField(item.Department)
                .Build();
        }

        // Cases 2 & 3 – Sconto riga (line discount): CMD_PRINT_ITEM_WITH_DISCOUNT "02S"
        if (item.Discount > 0 && string.IsNullOrEmpty(item.DiscountType) is false
            && (item.ItemFlag == CustomProtocolCommands.ITEM_FLAG_NORMAL
                || item.ItemFlag == CustomProtocolCommands.ITEM_FLAG_RETURN))
        {
            string cmd = item.DiscountType == CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT
                             ? CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT
                             : CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT;

            // If the discount is negative it implies a surcharge: CMD_PRINT_ITEM_WITH_SURCHARGE "02M"
            // (case 4 – maggiorazione riga): caller sets Discount to the surcharge amount
            // and DiscountType to a surcharge marker. We distinguish by convention: positive
            // Discount with explicit flag "M" maps to surcharge command.
            // Default: treat as discount.

            return new CustomCommandBuilder()
                .StartCommand(cmd)
                .AddField(item.Description)
                .AddField(item.Quantity)
                .AddField(item.UnitPrice)
                .AddField(item.VatCode)
                .AddField(item.Department)
                .AddField(item.Discount.Value)
                .AddField(item.DiscountType)
                .Build();
        }

        // Case 4 – Maggiorazione riga: when DiscountType ends with "M" (surcharge marker)
        if (item.Discount > 0 && item.DiscountType is not null
            && item.DiscountType.EndsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            return new CustomCommandBuilder()
                .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_SURCHARGE)
                .AddField(item.Description)
                .AddField(item.Quantity)
                .AddField(item.UnitPrice)
                .AddField(item.VatCode)
                .AddField(item.Department)
                .AddField(item.Discount.Value)
                .AddField(item.DiscountType.TrimEnd('M', 'm'))
                .Build();
        }

        // Cases 1 & 6 – Vendita normale / Reso (normal or return): CMD_PRINT_ITEM "02"
        return new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
            .AddField(item.Description)
            .AddField(item.Quantity)
            .AddField(item.UnitPrice)
            .AddField(item.VatCode)
            .AddField(item.Department)
            .Build();
    }

    // -------------------------------------------------------------------------
    //  Global discount / surcharge
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the <c>CMD_GLOBAL_DISCOUNT</c> ("03S") command frame (case 7 and 8).
    /// Must be sent after all items and before payments.
    /// </summary>
    /// <param name="discount">Global discount descriptor.</param>
    /// <returns>One binary command frame.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="discount"/> is null.</exception>
    public byte[] BuildGlobalDiscountCommand(FiscalDiscount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        return new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)
            .AddField(discount.Value)
            .AddField(discount.Type)
            .AddField(discount.Description)
            .Build();
    }

    /// <summary>
    /// Builds the <c>CMD_GLOBAL_SURCHARGE</c> ("03M") command frame (case 9 – coperto / service fee).
    /// Must be sent after all items and before payments.
    /// </summary>
    /// <param name="surcharge">Global surcharge descriptor.</param>
    /// <returns>One binary command frame.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="surcharge"/> is null.</exception>
    public byte[] BuildGlobalSurchargeCommand(FiscalSurcharge surcharge)
    {
        ArgumentNullException.ThrowIfNull(surcharge);

        return new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_SURCHARGE)
            .AddField(surcharge.Value)
            .AddField(surcharge.Type)
            .AddField(surcharge.Description)
            .AddField(surcharge.VatCode)
            .Build();
    }

    // -------------------------------------------------------------------------
    //  Payments (cases 10 & 11)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds one <c>CMD_PAYMENT</c> ("04") command frame per payment method.
    /// For multiple payment methods (case 10) send the returned frames in order.
    /// When the tendered amount exceeds the total (case 11 – resto) the printer
    /// automatically computes and prints the change.
    /// </summary>
    /// <param name="payments">One or more payment entries.</param>
    /// <returns>One frame per payment entry, in the same order as <paramref name="payments"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payments"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="payments"/> is empty.</exception>
    public IReadOnlyList<byte[]> BuildPaymentCommands(IEnumerable<FiscalPayment> payments)
    {
        ArgumentNullException.ThrowIfNull(payments);

        var frames = new List<byte[]>();
        foreach (var payment in payments)
        {
            var frame = new CustomCommandBuilder()
                .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
                .AddField(payment.Amount)
                .AddField(payment.MethodCode)
                .AddField(payment.Description)
                .Build();

            frames.Add(frame);
        }

        if (frames.Count == 0)
            throw new ArgumentException("At least one payment entry is required.", nameof(payments));

        return frames;
    }

    // -------------------------------------------------------------------------
    //  Non-fiscal / descriptive lines
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds <c>CMD_PRINT_DESCRIPTIVE</c> ("20") frames for each loyalty information line
    /// (card number, points earned/redeemed, current balance, next reward threshold).
    /// Returns an empty list when <paramref name="loyalty"/> is null or the card is not present.
    /// </summary>
    /// <param name="loyalty">Loyalty program data attached to the receipt.</param>
    /// <returns>Zero or more descriptive line frames.</returns>
    public IReadOnlyList<byte[]> BuildLoyaltyLines(LoyaltyReceiptData? loyalty)
    {
        if (loyalty is null || !loyalty.HasCard) return [];

        var lines = new List<string>
        {
            "*** FIDELITY ***",
            $"Carta: {loyalty.CardType} {loyalty.CardNumber}",
            $"Punti acquistati: {loyalty.PointsEarned}",
        };

        if (loyalty.PointsRedeemed > 0)
            lines.Add($"Punti utilizzati: {loyalty.PointsRedeemed}");

        lines.Add($"Saldo punti: {loyalty.CurrentBalance}");

        if (loyalty.NextRewardPoints > 0)
            lines.Add($"Prossimo premio: {loyalty.NextRewardPoints} punti");

        if (loyalty.DiscountApplied > 0)
            lines.Add($"Sconto fidelity: €{loyalty.DiscountApplied:F2}");

        return BuildDescriptiveLines(lines);
    }

    /// <summary>
    /// Builds <c>CMD_PRINT_DESCRIPTIVE</c> ("20") frames for a list of raw text lines
    /// (custom receipt header or footer). Lines longer than 40 characters are truncated.
    /// </summary>
    /// <param name="lines">Text lines to print as non-fiscal descriptive lines.</param>
    /// <returns>One frame per line.</returns>
    public IReadOnlyList<byte[]> BuildCustomLines(IEnumerable<string>? lines)
    {
        if (lines is null) return [];
        return BuildDescriptiveLines(lines);
    }

    // -------------------------------------------------------------------------
    //  Full-receipt sequence helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the complete ordered sequence of command frames for a full fiscal receipt.
    /// Sequence: header lines → open → items → global discount/surcharge → payments → loyalty lines → footer lines → close.
    /// </summary>
    /// <param name="receipt">Complete receipt data.</param>
    /// <returns>Ordered list of binary frames ready for sequential transmission.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="receipt"/> is null.</exception>
    public IReadOnlyList<byte[]> BuildFullReceiptSequence(FiscalReceiptData receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var sequence = new List<byte[]>();

        // Custom header lines (non-fiscal)
        sequence.AddRange(BuildCustomLines(receipt.HeaderLines));

        // Open receipt
        sequence.Add(BuildOpenReceiptCommand());

        // Items
        foreach (var item in receipt.Items)
            sequence.Add(BuildPrintItemCommand(item));

        // Global discount (optional)
        if (receipt.GlobalDiscount is not null)
            sequence.Add(BuildGlobalDiscountCommand(receipt.GlobalDiscount));

        // Global surcharge (optional)
        if (receipt.GlobalSurcharge is not null)
            sequence.Add(BuildGlobalSurchargeCommand(receipt.GlobalSurcharge));

        // Payments
        sequence.AddRange(BuildPaymentCommands(receipt.Payments));

        // Loyalty lines (non-fiscal, after payment)
        sequence.AddRange(BuildLoyaltyLines(receipt.LoyaltyData));

        // Custom footer lines (non-fiscal)
        sequence.AddRange(BuildCustomLines(receipt.FooterLines));

        // Close receipt
        sequence.Add(BuildCloseReceiptCommand());

        return sequence;
    }

    /// <summary>
    /// Builds the command sequence for a full refund receipt (reso totale).
    /// Opens a new receipt, sends each item with negative quantity, then closes.
    /// </summary>
    /// <param name="refund">Refund data including original receipt reference and items.</param>
    /// <returns>Ordered list of binary frames.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="refund"/> is null.</exception>
    public IReadOnlyList<byte[]> BuildRefundReceiptSequence(FiscalRefundData refund)
    {
        ArgumentNullException.ThrowIfNull(refund);

        var sequence = new List<byte[]>();
        sequence.Add(BuildOpenReceiptCommand());

        foreach (var item in refund.Items)
        {
            // Ensure quantity is negative for refund items
            var refundItem = new FiscalReceiptItem
            {
                Description = item.Description,
                Quantity = item.Quantity > 0 ? -item.Quantity : item.Quantity,
                UnitPrice = item.UnitPrice,
                VatCode = item.VatCode,
                Department = item.Department,
                ItemFlag = CustomProtocolCommands.ITEM_FLAG_RETURN
            };
            sequence.Add(BuildPrintItemCommand(refundItem));
        }

        sequence.AddRange(BuildPaymentCommands(refund.Payments));
        sequence.Add(BuildCloseReceiptCommand());
        return sequence;
    }

    /// <summary>
    /// Builds the <c>CMD_CANCEL_RECEIPT</c> ("30") command frame.
    /// Cancels the currently open receipt. Only valid when a receipt is open.
    /// </summary>
    public byte[] BuildCancelReceiptCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_CANCEL_RECEIPT)
            .Build();

    /// <summary>
    /// Builds the <c>CMD_DAILY_CLOSURE</c> ("50") command frame.
    /// Executes the daily Z-report (chiusura fiscale). This operation is irreversible.
    /// </summary>
    public byte[] BuildDailyClosureCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_DAILY_CLOSURE)
            .Build();

    /// <summary>
    /// Builds the <c>CMD_OPEN_DRAWER</c> ("40") command frame.
    /// </summary>
    public byte[] BuildOpenDrawerCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_OPEN_DRAWER)
            .Build();

    /// <summary>
    /// Builds the <c>CMD_READ_STATUS</c> ("10") command frame used by the monitoring service.
    /// </summary>
    public byte[] BuildReadStatusCommand()
        => new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_READ_STATUS)
            .Build();

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<byte[]> BuildDescriptiveLines(IEnumerable<string> lines)
    {
        var frames = new List<byte[]>();
        foreach (var line in lines)
        {
            var text = line.Length > MaxDescriptiveLineLength
                ? line[..MaxDescriptiveLineLength]
                : line;

            frames.Add(new CustomCommandBuilder()
                .StartCommand(CustomProtocolCommands.CMD_PRINT_DESCRIPTIVE)
                .AddField(text)
                .Build());
        }
        return frames;
    }
}
