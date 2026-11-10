namespace DataStandardizer.File.CSV.IntegrationTests;

/// <summary>
/// Runs a call on several threads at once, for the tests of behaviour that depends on how calls on different threads
/// interleave.
/// </summary>
internal static class ParallelRunner
{
    /// <summary>
    /// Makes <paramref name="callCount"/> calls to <paramref name="call"/> in parallel, passing each its index, and
    /// returns the result of each call at its index.
    /// </summary>
    /// <exception cref="AggregateException">One or more of the calls threw an exception.</exception>
    public static T[] Run<T>(int callCount, Func<int, T> call)
    {
        var results = new T[callCount];
        Parallel.For(0, callCount, index => results[index] = call(index));

        return results;
    }
}
