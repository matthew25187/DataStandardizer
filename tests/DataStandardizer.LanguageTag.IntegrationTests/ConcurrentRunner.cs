using System;
using System.Collections.Generic;
using System.Threading;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// What one thread of <see cref="ConcurrentRunner.Run{T}"/> got: the value its work returned, or the exception it threw.
    /// </summary>
    internal readonly struct ThreadOutcome<T>
    {
        internal ThreadOutcome(T value, Exception exception)
        {
            Value = value;
            Exception = exception;
        }

        internal T Value { get; }

        internal Exception Exception { get; }
    }

    /// <summary>
    /// Runs the same work on several threads at once, for a test of thread safety.
    /// </summary>
    /// <remarks>
    /// Its own tests are in <see cref="ConcurrentRunnerTests"/>. A concurrency test makes one call to <see cref="Run{T}"/>, so the test itself
    /// has no loop or <c>try</c>.
    /// </remarks>
    internal static class ConcurrentRunner
    {
        /// <summary>
        /// Start <paramref name="threadCount"/> threads, release them at once with a barrier, and have each run <paramref name="work"/>.
        /// </summary>
        /// <returns>The outcome of each thread, in the order the threads were started.</returns>
        internal static IReadOnlyList<ThreadOutcome<T>> Run<T>(int threadCount, Func<T> work)
        {
            var outcomes = new ThreadOutcome<T>[threadCount];
            using (var startBarrier = new Barrier(threadCount))
            {
                var threads = new Thread[threadCount];
                for (var threadIndex = 0; threadIndex < threadCount; threadIndex++)
                {
                    var index = threadIndex;
                    threads[index] = new Thread(() =>
                    {
                        try
                        {
                            startBarrier.SignalAndWait();
                            outcomes[index] = new ThreadOutcome<T>(work(), null);
                        }
                        catch (Exception exception)
                        {
                            outcomes[index] = new ThreadOutcome<T>(default(T), exception);
                        }
                    });
                }

                foreach (var thread in threads) thread.Start();
                foreach (var thread in threads) thread.Join();
            }

            return outcomes;
        }
    }
}
