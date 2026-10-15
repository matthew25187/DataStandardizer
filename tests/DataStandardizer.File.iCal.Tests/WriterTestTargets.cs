using System.Text;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// A stream that can only be written asynchronously.  Its synchronous members throw, so any synchronous I/O on the async path fails the test.
/// </summary>
internal sealed class AsyncOnlyWriteStream : Stream
{
    private readonly MemoryStream _data = new();

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the number of asynchronous flushes.
    /// </summary>
    public int FlushCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public byte[] ToArray() => _data.ToArray();

    public override void Flush()
    {
        throw new InvalidOperationException("Synchronous flush on the async path.");
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        FlushCount++;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
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
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        _data.Write(buffer.Span);
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// A text writer that can only be written asynchronously.  Its synchronous members throw, so any synchronous I/O on the async path fails the
/// test.
/// </summary>
internal sealed class AsyncOnlyTextWriter : TextWriter
{
    private readonly StringBuilder _text = new();

    public override Encoding Encoding => new UTF8Encoding(false);

    public int FlushCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public override string ToString() => _text.ToString();

    public override void Flush()
    {
        throw new InvalidOperationException("Synchronous flush on the async path.");
    }

    public override async Task FlushAsync()
    {
        await Task.Yield();
        FlushCount++;
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return FlushAsync();
    }

    public override void Write(char value)
    {
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override void Write(char[] buffer, int index, int count)
    {
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        return WriteAsync(buffer.AsMemory(index, count));
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        _text.Append(buffer.Span);
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// A text writer whose asynchronous writes wait until the test opens a gate, so a write can be held in progress.
/// </summary>
internal sealed class GatedTextWriter : TextWriter
{
    private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly StringBuilder _text = new();
    private TaskCompletionSource _writeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override Encoding Encoding => new UTF8Encoding(false);

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets a task that completes when a write starts waiting at the gate.
    /// </summary>
    public Task WriteStarted => _writeStarted.Task;

    /// <summary>
    /// Lets waiting and later writes through.
    /// </summary>
    public void OpenGate()
    {
        _gate.TrySetResult();
    }

    /// <summary>
    /// Makes later writes wait again.
    /// </summary>
    public void CloseGate()
    {
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public override string ToString() => _text.ToString();

    public override void Write(char value)
    {
        throw new InvalidOperationException("Synchronous write on the async path.");
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        return WriteAsync(buffer.AsMemory(index, count));
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        // Copy now: the writer's buffer belongs to it again once the write completes, not before.
        var text = buffer.ToString();
        _writeStarted.TrySetResult();
        await _gate.Task.WaitAsync(cancellationToken);
        _text.Append(text);
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// A string writer that counts flushes and records whether it has been disposed.
/// </summary>
internal sealed class TrackingStringWriter : StringWriter
{
    public int FlushCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public override void Flush()
    {
        FlushCount++;
        base.Flush();
    }

    public override Task FlushAsync()
    {
        FlushCount++;
        return Task.CompletedTask;
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        FlushCount++;
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// Records progress reports as they are made, rather than posting them to a synchronization context as <see cref="Progress{T}"/> does.
/// </summary>
internal sealed class RecordingProgress(Action<IcalWriteProgress>? onReport = null) : IProgress<IcalWriteProgress>
{
    public List<IcalWriteProgress> Reports { get; } = [];

    public void Report(IcalWriteProgress value)
    {
        Reports.Add(value);
        onReport?.Invoke(value);
    }
}
