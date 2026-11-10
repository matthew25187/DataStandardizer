#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Decides how an asynchronous operation of a reader or writer completes, and reports it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="OperationCanceledException"/> is a cancellation only if the caller's token is canceled, whatever token the exception
    /// carries, or if it carries none.  Any other exception, including an <see cref="OperationCanceledException"/> while the caller's token
    /// isn't canceled, is a fault.
    /// </para>
    /// <para>
    /// An async method can't report such a fault, since its builder cancels the task for any <see cref="OperationCanceledException"/>.  So an
    /// operation's async core catches every exception and returns the task that the operation completes as, its outcome, and the operation
    /// returns that task through <see cref="Unwrap(ValueTask{Task})"/>.
    /// </para>
    /// </remarks>
    internal static class AsyncOutcome
    {
        /// <summary>
        /// Returns a task that completes as an operation that threw <paramref name="exception"/> does.
        /// </summary>
        /// <param name="exception">The exception the operation threw.</param>
        /// <param name="cancellationToken">The token the caller passed to the operation.</param>
        /// <param name="isCanceled">Set to <c>true</c> if the operation is canceled; otherwise left as it is.</param>
        /// <returns>A canceled task if the operation is canceled; otherwise a task faulted with <paramref name="exception"/>.</returns>
        internal static Task Fail(Exception exception, CancellationToken cancellationToken, ref bool isCanceled)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                isCanceled = true;
                return Task.FromCanceled(cancellationToken);
            }

            return Task.FromException(exception);
        }

        /// <summary>
        /// Returns the outcome of an operation with a result that threw <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The exception the operation threw.</param>
        /// <param name="cancellationToken">The token the caller passed to the operation.</param>
        /// <returns>A canceled task if the operation is canceled; otherwise a task faulted with <paramref name="exception"/>.</returns>
        internal static ValueTask<TResult> Fail<TResult>(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
                return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));

            return new ValueTask<TResult>(Task.FromException<TResult>(exception));
        }

        /// <summary>
        /// Returns the task that an operation's outcome completes as.  An outcome that is already known costs nothing more; only one that has to
        /// wait is unwrapped.
        /// </summary>
        internal static Task Unwrap(ValueTask<Task> outcome)
        {
            return outcome.IsCompletedSuccessfully ? outcome.Result : outcome.AsTask().Unwrap();
        }

        /// <summary>
        /// Returns the task that an operation's outcome completes as.  An outcome that is already known costs nothing more; only one that has to
        /// wait is unwrapped.
        /// </summary>
        internal static ValueTask<TResult> Unwrap<TResult>(ValueTask<ValueTask<TResult>> outcome)
        {
            if (outcome.IsCompletedSuccessfully)
                return outcome.Result;

            // The continuation isn't async, so an OperationCanceledException that it rethrows faults its task, as the outcome says.
            return new ValueTask<TResult>(outcome.AsTask().ContinueWith(completed => completed.GetAwaiter().GetResult().AsTask(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap());
        }

        /// <summary>
        /// Waits for a release to finish, then returns <paramref name="outcome"/>, or a task faulted with the release's exception if the release
        /// fails.
        /// </summary>
        /// <remarks>
        /// A release takes no cancellation token, so an <see cref="OperationCanceledException"/> from it faults the task, as any other exception
        /// does.  The operation's own failure is then observed, so that it isn't reported as unobserved.
        /// </remarks>
        internal static async ValueTask<Task> ReleaseThenReportAsync(ValueTask release, Task outcome)
        {
            try
            {
                await release.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _ = outcome.Exception;
                return Task.FromException(exception);
            }

            return outcome;
        }
    }
}
#endif
