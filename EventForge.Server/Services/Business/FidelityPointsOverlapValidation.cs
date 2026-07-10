namespace EventForge.Server.Services.Business;

internal static class FidelityPointsOverlapValidation
{
    internal static void EnsureNoOverlap<T>(
        IEnumerable<T> items,
        DateTime start,
        DateTime end,
        Func<T, DateTime> getStart,
        Func<T, DateTime?> getEnd,
        Func<T, string> describeConflict)
    {
        var overlapping = items.FirstOrDefault(item =>
            getStart(item) <= end &&
            (getEnd(item) ?? DateTime.MaxValue) >= start);

        if (overlapping is not null)
        {
            throw new InvalidOperationException(describeConflict(overlapping));
        }
    }
}
