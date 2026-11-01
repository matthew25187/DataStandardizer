using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.Communication.IntegrationTests
{
    /// <summary>
    /// Runs calls on dedicated threads that are released at the same moment, so that they contend for whatever the calls share.
    /// </summary>
    /// <remarks>
    /// The one place in the integration tests that loops, branches and catches, so that each concurrency test is a single call to
    /// <see cref="Run{TInput, TResult}"/>. <see cref="ConcurrentRunnerTests"/> covers it.
    /// </remarks>
    internal static class ConcurrentRunner
    {
        /// <summary>
        /// Calls <paramref name="call"/> <paramref name="callsPerInput"/> times with each of <paramref name="inputs"/>, each call on its own
        /// thread, and waits up to <paramref name="timeout"/> for them all to return.
        /// </summary>
        /// <returns>
        /// Whether every call returned in time, the result of each call that returned, in the order of the inputs, and the exceptions the
        /// calls threw.
        /// </returns>
        public static ConcurrentRunResult<TResult> Run<TInput, TResult>(IReadOnlyList<TInput> inputs, int callsPerInput, Func<TInput, TResult> call,
            TimeSpan timeout)
        {
            var callCount = inputs.Count * callsPerInput;
            var results = new TResult[callCount];
            var exceptions = new ConcurrentQueue<Exception>();
            var barrier = new Barrier(callCount);
            var tasks = new Task[callCount];
            for (var index = 0; index < callCount; index++)
            {
                var slot = index;
                var input = inputs[index / callsPerInput];
                tasks[index] = Task.Factory.StartNew(
                    () =>
                    {
                        barrier.SignalAndWait();
                        try
                        {
                            results[slot] = call(input);
                        }
                        catch (Exception exception)
                        {
                            exceptions.Enqueue(exception);
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            // A call that never returns, such as one spinning on a corrupted cache, fails the run rather than hanging it.
            var completed = Task.WaitAll(tasks, timeout);

            return new ConcurrentRunResult<TResult>(completed, results, exceptions.ToArray());
        }
    }

    /// <summary>
    /// The outcome of <see cref="ConcurrentRunner.Run{TInput, TResult}"/>.
    /// </summary>
    internal sealed class ConcurrentRunResult<TResult>
    {
        public ConcurrentRunResult(bool completed, IReadOnlyList<TResult> results, IReadOnlyList<Exception> exceptions)
        {
            Completed = completed;
            Results = results;
            Exceptions = exceptions;
        }

        /// <summary>
        /// Whether every call returned before the timeout.
        /// </summary>
        public bool Completed { get; }

        /// <summary>
        /// The result of each call, in the order of the inputs; a call that threw or didn't return leaves the default value.
        /// </summary>
        public IReadOnlyList<TResult> Results { get; }

        /// <summary>
        /// The exceptions that the calls threw.
        /// </summary>
        public IReadOnlyList<Exception> Exceptions { get; }
    }
}
