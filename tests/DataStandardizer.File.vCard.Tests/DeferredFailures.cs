using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Holds back what yields during an operation, the failures that <c>FailureTiming.AfterYielding</c> reports and the steps of a
    /// sequence from <c>AsyncEnumerables.Yielding</c>, until the operation has returned its task, so that the code under test is always
    /// given a task that has not completed.  Whether a task that yields has completed by the time it is returned depends on the thread pool, and
    /// code takes a different path for a task that has already completed, so the same test would otherwise take either path from run to run.
    /// </summary>
    /// <remarks>
    /// Once the operation has returned, <see cref="Run"/> opens the gates on its own thread, so the code waiting on them continues there, before
    /// <see cref="Run"/> returns: the code under test awaits with <c>ConfigureAwait(false)</c>, so its continuations run as each gate opens.
    /// What that code holds back as it continues, such as a failure of its target after a sequence has resumed it, is opened in turn, until
    /// nothing is left waiting.  That gives the same order on every run.
    /// </remarks>
    internal sealed class DeferredFailures
    {
        private static readonly AsyncLocal<DeferredFailures> Current = new AsyncLocal<DeferredFailures>();

        private readonly object _lock = new object();
        private readonly List<TaskCompletionSource<bool>> _gates = new List<TaskCompletionSource<bool>>();
        private bool _isReleased;

        /// <summary>
        /// Runs an operation, and then completes what it held back, in the order it was held back, until nothing is left waiting.
        /// </summary>
        /// <param name="operation">The operation, which returns its task as soon as it has to wait.</param>
        /// <returns>The task of the operation.</returns>
        public static Task Run(Func<Task> operation)
        {
            var deferred = new DeferredFailures();
            var previous = Current.Value;
            Current.Value = deferred;
            try
            {
                return operation();
            }
            finally
            {
                Current.Value = previous;
                deferred.Release();
            }
        }

        /// <summary>
        /// Gives a task that fails as a yielding task does, but only once the operation that is running has returned.
        /// </summary>
        /// <returns>The task, or <c>null</c> when no operation is running, or it has finished releasing what it held back.</returns>
        public static Task<TResult> TryDefer<TResult>(Func<Exception> createException)
        {
            var gate = TryHold();
            return gate == null ? null : FailAfterGateAsync<TResult>(gate, createException);
        }

        /// <summary>
        /// Gives a task that completes once the operation that is running has returned.
        /// </summary>
        /// <returns>The task, or <c>null</c> when no operation is running, or it has finished releasing what it held back.</returns>
        public static Task TryHold()
        {
            var deferred = Current.Value;
            return deferred == null ? null : deferred.Hold();
        }

        private Task Hold()
        {
            var gate = new TaskCompletionSource<bool>();
            lock (_lock)
            {
                if (_isReleased) return null;

                _gates.Add(gate);
            }

            return gate.Task;
        }

        // An async method, so that its task ends as the async method builder ends it: canceled, with the exception, for an
        // OperationCanceledException, and faulted for any other exception.  It continues on the thread that opens the gate.
        private static async Task<TResult> FailAfterGateAsync<TResult>(Task gate, Func<Exception> createException)
        {
            await gate.ConfigureAwait(false);
            throw createException();
        }

        // Opens the gates, and then any that the code continuing from them adds, until none is left.  A task runs a continuation as it completes
        // only where there is no synchronization context other than the default one, and the test framework sets its own, so there is none while
        // the gates open.
        private void Release()
        {
            var context = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            using (new SynchronizationContextRestorer(context))
            {
                while (true)
                {
                    TaskCompletionSource<bool>[] gates;
                    lock (_lock)
                    {
                        if (_gates.Count == 0)
                        {
                            _isReleased = true;
                            return;
                        }

                        gates = _gates.ToArray();
                        _gates.Clear();
                    }

                    foreach (var gate in gates)
                    {
                        gate.SetResult(true);
                    }
                }
            }
        }

        private sealed class SynchronizationContextRestorer : IDisposable
        {
            private readonly SynchronizationContext _context;

            public SynchronizationContextRestorer(SynchronizationContext context)
            {
                _context = context;
            }

            public void Dispose()
            {
                SynchronizationContext.SetSynchronizationContext(_context);
            }
        }
    }
}
