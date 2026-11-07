#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Records that an asynchronous operation was canceled, so that the reader or writer refuses later operations.
    /// </summary>
    internal interface ICancellationRecorder
    {
        /// <summary>
        /// Records that an operation was canceled.  May be called more than once.
        /// </summary>
        void RecordCancellation();
    }

    /// <summary>
    /// Decides whether an asynchronous operation of a reader or writer was canceled or faulted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="OperationCanceledException"/> is a cancellation only if the caller's token is canceled, whatever token the exception
    /// carries, or if it carries none.  Any other exception, including an <see cref="OperationCanceledException"/> while the caller's token
    /// isn't canceled, is a fault.
    /// </para>
    /// <para>
    /// An async method cancels its task for any <see cref="OperationCanceledException"/>.  So each operation's async core catches one only while
    /// the caller's token is canceled, and lets any other through, and the operation returns the core's task through <c>Decide</c>, which
    /// faults a canceled task instead while the caller's token isn't canceled.  A task that completed successfully or faulted is returned as it
    /// is, so an operation that completes synchronously costs nothing more.
    /// </para>
    /// </remarks>
    internal static class AsyncOutcome
    {
        internal static Task Decide(Task operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            if (operation.IsCompleted)
                return DecideCompleted(operation, cancellationToken, recorder);

            return operation.ContinueWith(new Decision(cancellationToken, recorder).Decide, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap();
        }

        internal static Task<TResult> Decide<TResult>(Task<TResult> operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            if (operation.IsCompleted)
                return DecideCompleted(operation, cancellationToken, recorder);

            return operation.ContinueWith(new Decision(cancellationToken, recorder).Decide<TResult>, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        }

        internal static ValueTask Decide(ValueTask operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            return operation.IsCompletedSuccessfully ? operation : new ValueTask(Decide(operation.AsTask(), cancellationToken, recorder));
        }

        internal static ValueTask<TResult> Decide<TResult>(ValueTask<TResult> operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            return operation.IsCompletedSuccessfully ? operation : new ValueTask<TResult>(Decide(operation.AsTask(), cancellationToken, recorder));
        }

        private static Task DecideCompleted(Task operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            if (!operation.IsCanceled)
                return operation;

            if (cancellationToken.IsCancellationRequested)
            {
                // The core may have seen the token before it was canceled, and not recorded the cancellation.
                recorder.RecordCancellation();
                return operation;
            }

            return Task.FromException(GetCancellationException(operation));
        }

        private static Task<TResult> DecideCompleted<TResult>(Task<TResult> operation, CancellationToken cancellationToken, ICancellationRecorder recorder)
        {
            if (!operation.IsCanceled)
                return operation;

            if (cancellationToken.IsCancellationRequested)
            {
                recorder.RecordCancellation();
                return operation;
            }

            return Task.FromException<TResult>(GetCancellationException(operation));
        }

        /// <summary>
        /// Gets the exception that a canceled task gives when it is awaited: the <see cref="OperationCanceledException"/> its async method threw.
        /// </summary>
        private static OperationCanceledException GetCancellationException(Task canceled)
        {
            try
            {
                canceled.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException exception)
            {
                return exception;
            }

            // A canceled task always throws when it is awaited.
            throw new InvalidOperationException("A canceled task completed without an exception.");
        }

        /// <summary>
        /// Decides an operation once it has completed.
        /// </summary>
        private sealed class Decision
        {
            private readonly CancellationToken _cancellationToken;
            private readonly ICancellationRecorder _recorder;

            internal Decision(CancellationToken cancellationToken, ICancellationRecorder recorder)
            {
                _cancellationToken = cancellationToken;
                _recorder = recorder;
            }

            internal Task Decide(Task completed)
            {
                return DecideCompleted(completed, _cancellationToken, _recorder);
            }

            internal Task<TResult> Decide<TResult>(Task<TResult> completed)
            {
                return DecideCompleted(completed, _cancellationToken, _recorder);
            }
        }
    }
}
#endif
