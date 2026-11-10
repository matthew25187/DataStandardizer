namespace DataStandardizer.File.CSV.IntegrationTests;

/// <summary>
/// Measures the memory that an operation allocates on the calling thread.
/// </summary>
internal static class AllocationMeter
{
    /// <summary>
    /// Runs the action and gives the bytes it allocated.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="exception">The exception that the action threw, or <c>null</c> if it didn't throw.</param>
    public static long BytesAllocatedBy(Action action, out Exception? exception)
    {
        exception = null;
        var before = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            action();
        }
        catch (Exception caught)
        {
            exception = caught;
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
