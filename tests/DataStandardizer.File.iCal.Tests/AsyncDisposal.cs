using System;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Disposes a reader or writer asynchronously once a test is done with it, as <c>await using</c> would.  The tests also build for .NET
    /// Framework, whose C# 7.3 has no <c>await using</c>.
    /// </summary>
    /// <remarks>
    /// Test infrastructure: it holds the <c>try</c>/<c>finally</c> so that the tests that use it don't have to.
    /// </remarks>
    internal static class AsyncDisposal
    {
        /// <summary>
        /// Runs <paramref name="body"/>, then disposes <paramref name="resource"/> asynchronously, whether or not the body threw.
        /// </summary>
        public static async Task UsingAsync(IAsyncDisposable resource, Func<Task> body)
        {
            try
            {
                await body();
            }
            finally
            {
                await resource.DisposeAsync();
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/>, then disposes <paramref name="resource"/> asynchronously, whether or not the body threw.
        /// </summary>
        public static async Task UsingAsync(IAsyncDisposable resource, Action body)
        {
            try
            {
                body();
            }
            finally
            {
                await resource.DisposeAsync();
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/>, then <paramref name="cleanUp"/>, whether or not the body threw.  For a test that has more to undo than
        /// disposing one reader or writer, such as letting an operation it held in progress finish.
        /// </summary>
        public static async Task FinallyAsync(Func<Task> body, Func<Task> cleanUp)
        {
            try
            {
                await body();
            }
            finally
            {
                await cleanUp();
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/>, then <paramref name="cleanUp"/>, whether or not the body threw.  For a test that has more to undo than
        /// disposing one reader or writer, such as letting an operation it held in progress finish.
        /// </summary>
        public static async Task FinallyAsync(Action body, Func<Task> cleanUp)
        {
            try
            {
                body();
            }
            finally
            {
                await cleanUp();
            }
        }
    }
}
