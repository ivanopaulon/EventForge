using System;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// Information about a document lock status.
    /// </summary>
    public class DocumentLockInfo
    {
        /// <summary>
        /// Document ID.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Indicates if the document is currently locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// User who currently holds the lock (email/username).
        /// </summary>
        public string? LockedBy { get; set; }

        /// <summary>
        /// Timestamp when the lock was acquired.
        /// </summary>
        public DateTime? LockedAt { get; set; }

        /// <summary>
        /// SignalR connection ID holding the lock.
        /// </summary>
        public string? ConnectionId { get; set; }
    }
}
