namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// A stream that can only be read asynchronously.  Its synchronous members throw, so any synchronous I/O on the async path fails the test.
/// </summary>
internal sealed class AsyncOnlyStream(byte[] data, bool completeSynchronously = false) : Stream
{
    private int _position;

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
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override int Read(Span<byte> buffer)
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!completeSynchronously)
        {
            await Task.Yield();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var count = Math.Min(buffer.Length, data.Length - _position);
        data.AsSpan(_position, count).CopyTo(buffer.Span);
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
internal sealed class AsyncOnlyTextReader(string text, int maxReadLength = int.MaxValue) : TextReader
{
    private int _position;

    public override int Peek()
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override int Read()
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override int Read(char[] buffer, int index, int count)
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override int Read(Span<char> buffer)
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        return ReadAsync(buffer.AsMemory(index, count)).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        var count = Math.Min(Math.Min(buffer.Length, maxReadLength), text.Length - _position);
        text.AsSpan(_position, count).CopyTo(buffer.Span);
        _position += count;
        return count;
    }
}

/// <summary>
/// A text reader whose asynchronous reads wait until the test opens a gate, so a read can be held in progress.
/// </summary>
internal sealed class GatedTextReader(string text) : TextReader
{
    private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _position;
    private TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
        _gate.TrySetResult();
    }

    /// <summary>
    /// Makes later reads wait again.
    /// </summary>
    public void CloseGate()
    {
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public override int Read(char[] buffer, int index, int count)
    {
        throw new InvalidOperationException("Synchronous read on the async path.");
    }

    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
    {
        _readStarted.TrySetResult();
        await _gate.Task.WaitAsync(cancellationToken);
        var count = Math.Min(buffer.Length, text.Length - _position);
        text.AsSpan(_position, count).CopyTo(buffer.Span);
        _position += count;
        ReadsCompleted++;
        return count;
    }
}
