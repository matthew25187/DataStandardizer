namespace DataStandardizer.Chronology.IntegrationTests;

/// <summary>
/// The outcome of running calls on several threads at once.
/// </summary>
/// <typeparam name="TResult">The type of the result of each thread.</typeparam>
/// <param name="Joined">Whether every thread finished within the timeout, measured from when the threads started.</param>
/// <param name="Results">The result of each thread, or the default value for a thread that threw or did not finish.</param>
/// <param name="Exceptions">The exception that each thread threw, or <see langword="null"/> for a thread that did not throw.</param>
internal sealed record ConcurrentRun<TResult>(bool Joined, IReadOnlyList<TResult?> Results, IReadOnlyList<Exception?> Exceptions);

/// <summary>
/// Runs calls on several threads, which a barrier releases together so that the calls overlap.
/// </summary>
/// <remarks>
/// What the calls exercise depends on how the thread scheduler interleaves them, so only integration tests use this.
/// </remarks>
internal static class ConcurrentRunner
{
    /// <summary>
    /// Runs the same call on each of <paramref name="threadCount"/> threads.
    /// </summary>
    internal static ConcurrentRun<TResult> RunOnEveryThread<TResult>(int threadCount, Func<TResult> call, TimeSpan timeout)
    {
        var threadWork = new Func<TResult>[threadCount];
        Array.Fill(threadWork, call);

        return Run(threadWork, timeout);
    }

    /// <summary>
    /// Runs every one of <paramref name="calls"/> on each thread, in the order that <paramref name="orders"/> gives for that thread,
    /// one order to a thread.
    /// </summary>
    /// <returns>
    /// The results of each thread, in the order of <paramref name="calls"/>, whatever order the thread made them in.
    /// </returns>
    internal static ConcurrentRun<IReadOnlyList<TResult>> RunInOrders<TResult>(IReadOnlyList<Func<TResult>> calls, IReadOnlyList<IReadOnlyList<int>> orders, TimeSpan timeout)
    {
        var threadWork = new Func<IReadOnlyList<TResult>>[orders.Count];
        for (var thread = 0; thread < orders.Count; thread++)
        {
            var order = orders[thread];
            threadWork[thread] = () =>
            {
                var results = new TResult[calls.Count];
                foreach (var position in order)
                    results[position] = calls[position]();

                return results;
            };
        }

        return Run(threadWork, timeout);
    }

    private static ConcurrentRun<TResult> Run<TResult>(IReadOnlyList<Func<TResult>> threadWork, TimeSpan timeout)
    {
        var threadCount = threadWork.Count;
        var results = new TResult?[threadCount];
        var exceptions = new Exception?[threadCount];
        var barrier = new Barrier(threadCount);
        var threads = new Thread[threadCount];
        for (var index = 0; index < threadCount; index++)
        {
            var thread = index;
            threads[thread] = new Thread(() =>
            {
                try
                {
                    if (!barrier.SignalAndWait(timeout))
                        throw new TimeoutException("The threads were not released together.");

                    results[thread] = threadWork[thread]();
                }
                catch (Exception exception)
                {
                    exceptions[thread] = exception;
                }
            })
            {
                // A thread that does not finish must not keep the test host running.
                IsBackground = true,
            };
        }

        foreach (var thread in threads)
            thread.Start();

        // One deadline for every thread, so that the run as a whole must finish within the timeout.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var joined = true;
        foreach (var thread in threads)
        {
            var remaining = timeout - stopwatch.Elapsed;
            joined &= thread.Join(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
        }

        // A thread that has not finished may still be using the barrier.
        if (joined)
            barrier.Dispose();

        return new ConcurrentRun<TResult>(joined, results, exceptions);
    }
}
