namespace EventForge.DTOs.Sales
{
    /// <summary>
    /// Defines the mode for barcode scanning in POS.
    /// </summary>
    public enum ScanMode
    {
        /// <summary>
        /// Automatically add scanned product to cart.
        /// </summary>
        AddToCart = 0,

        /// <summary>
        /// Only display product price without adding to cart (price check).
        /// </summary>
        PriceCheckOnly = 1
    }
}
