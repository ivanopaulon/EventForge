namespace EventForge.DTOs.Common
{

    /// <summary>
    /// Represents a paginated result with metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items for the current page.
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages.
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Total number of pages.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Whether there is a next page.
        /// </summary>
        public bool HasNextPage => Page < TotalPages;
    }
}
