namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Status enumeration for store operators/cashiers.
    /// </summary>
    public enum CashierStatus
    {
        /// <summary>
        /// Operator is active and can use the system.
        /// </summary>
        Active,
        
        /// <summary>
        /// Operator is temporarily suspended.
        /// </summary>
        Suspended,
        
        /// <summary>
        /// Operator is locked for security or administrative reasons.
        /// </summary>
        Locked,
        
        /// <summary>
        /// Operator is deleted/disabled.
        /// </summary>
        Deleted
    }
}