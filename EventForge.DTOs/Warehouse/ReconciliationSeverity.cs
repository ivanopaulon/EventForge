namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Represents the severity level of stock reconciliation discrepancies
    /// </summary>
    public enum ReconciliationSeverity
    {
        /// <summary>
        /// ✅ No discrepancy - stock is correct
        /// </summary>
        Correct = 0,

        /// <summary>
        /// ⚠️ Minor discrepancy - difference less than 10%
        /// </summary>
        Minor = 1,

        /// <summary>
        /// ❌ Major discrepancy - difference greater than 10%
        /// </summary>
        Major = 2,

        /// <summary>
        /// 🔴 Missing stock - current quantity is 0 but should have stock
        /// </summary>
        Missing = 3
    }
}
