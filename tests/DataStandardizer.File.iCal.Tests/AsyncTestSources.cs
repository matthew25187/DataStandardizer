using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// The messages of the exceptions that the test streams, readers and writers throw when they are used synchronously.
    /// </summary>
    internal static class AsyncPathMessages
    {
        public const string SynchronousRead = "Synchronous read on the async path.";
        public const string SynchronousWrite = "Synchronous write on the async path.";
        public const string SynchronousFlush = "Synchronous flush on the async path.";
    }

    /// <summary>
    /// A stream that can only be read asynchronously, at most <paramref name="maxReadLength"/> bytes at a time.  Its synchronous members throw, so
    /// any synchronous I/O on the async path fails the test.
    /// </summary>
    internal sealed class AsyncOnlyStream : Stream
    {
        private readonly byte[] _data;
        private readonly bool _completeSynchronously;
        private readonly int _maxReadLength;
        private int _position;

        public AsyncOnlyStream(byte[] data, bool completeSynchronously = false, int maxReadLength = int.MaxValue)
        {
            _data = data;
            _completeSynchronously = completeSynchronously;
            _maxReadLength = maxReadLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#else
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
#endif
        {
            if (!_completeSynchronously)
            {
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(Math.Min(buffer.Length, _maxReadLength), _data.Length - _position);
            _data.AsSpan(_position, count).CopyTo(buffer.Span);
            _position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A text reader that can only be read asynchronously, a few characters at a time.  Its synchronous members throw, so any synchronous I/O on
    /// the async path fails the test.
    /// </summary>
    internal sealed class AsyncOnlyTextReader : TextReader
    {
        private readonly string _text;
        private readonly int _maxReadLength;
        private int _position;

        public AsyncOnlyTextReader(string text, int maxReadLength = int.MaxValue)
        {
            _text = text;
            _maxReadLength = maxReadLength;
        }

        public override int Peek()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

        public override int Read()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<char> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }
#endif

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return ReadAsync(buffer.AsMemory(index, count)).AsTask();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
#else
        public async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
#endif
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(Math.Min(buffer.Length, _maxReadLength), _text.Length - _position);
            _text.AsSpan(_position, count).CopyTo(buffer.Span);
            _position += count;
            return count;
        }
    }

    /// <summary>
    /// A text reader whose asynchronous reads wait until the test opens a gate, so a read can be held in progress.
    /// </summary>
    internal sealed class GatedTextReader : TextReader
    {
        private readonly string _text;
        private TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _position;
        private TaskCompletionSource<bool> _readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public GatedTextReader(string text)
        {
            _text = text;
        }

        /// <summary>
        /// Gets a task that completes when a read starts waiting at the gate.
        /// </summary>
        public Task ReadStarted => _readStarted.Task;

        /// <summary>
        /// Gets the number of reads that have got through the gate.
        /// </summary>
        public int ReadsCompleted { get; private set; }

        /// <summary>
        /// Lets waiting and later reads through.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        /// <summary>
        /// Makes later reads wait again.
        /// </summary>
        public void CloseGate()
        {
            _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return ReadAsync(buffer.AsMemory(index, count)).AsTask();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
#else
        public async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
#endif
        {
            _readStarted.TrySetResult(true);
            await TestTasks.WaitAsync(_gate.Task, cancellationToken);
            var count = Math.Min(buffer.Length, _text.Length - _position);
            _text.AsSpan(_position, count).CopyTo(buffer.Span);
            _position += count;
            ReadsCompleted++;
            return count;
        }
    }

    /// <summary>
    /// Produces and consumes asynchronous sequences, which C# 7.3 can do with neither an iterator nor <c>await foreach</c>.
    /// </summary>
    internal static class AsyncEnumerables
    {
        /// <summary>
        /// Reads all of the items of an asynchronous sequence.
        /// </summary>
        public static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
        {
            var items = new List<T>();
            var enumerator = source.GetAsyncEnumerator();
            await AsyncDisposal.UsingAsync(enumerator, async () =>
            {
                while (await enumerator.MoveNextAsync())
                {
                    items.Add(enumerator.Current);
                }
            });

            return items;
        }

        /// <summary>
        /// Gives the items as an asynchronous sequence that yields before each one, as an async iterator that awaits <c>Task.Yield()</c>
        /// before each <c>yield return</c> would.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="onEnumerate">Called with the token given to <c>GetAsyncEnumerator</c>.</param>
        public static IAsyncEnumerable<T> Yielding<T>(IEnumerable<T> items, Action<CancellationToken> onEnumerate = null)
        {
            return new YieldingAsyncEnumerable<T>(items, onEnumerate);
        }

        private sealed class YieldingAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> _items;
            private readonly Action<CancellationToken> _onEnumerate;

            public YieldingAsyncEnumerable(IEnumerable<T> items, Action<CancellationToken> onEnumerate)
            {
                _items = items;
                _onEnumerate = onEnumerate;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                _onEnumerate?.Invoke(cancellationToken);
                return new Enumerator(_items.GetEnumerator());
            }

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private readonly IEnumerator<T> _items;

                public Enumerator(IEnumerator<T> items)
                {
                    _items = items;
                }

                public T Current => _items.Current;

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (!_items.MoveNext())
                    {
                        return false;
                    }

                    await Task.Yield();
                    return true;
                }

                public ValueTask DisposeAsync()
                {
                    _items.Dispose();
                    return default;
                }
            }
        }
    }

    internal static class TestTasks
    {
        /// <summary>
        /// Cancels <paramref name="source"/>, as <c>CancellationTokenSource.CancelAsync</c> does from .NET 8.
        /// </summary>
        public static Task CancelAsync(CancellationTokenSource source)
        {
#if NET8_0_OR_GREATER
            return source.CancelAsync();
#else
            source.Cancel();
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Waits for <paramref name="task"/> to complete, or until <paramref name="cancellationToken"/> is cancelled, as
        /// <c>Task.WaitAsync(CancellationToken)</c> does from .NET 6.
        /// </summary>
        public static async Task WaitAsync(Task task, CancellationToken cancellationToken)
        {
#if NET6_0_OR_GREATER
            await task.WaitAsync(cancellationToken);
#else
            await await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationToken));
#endif
        }
    }
}
