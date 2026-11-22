#if NETSTANDARD2_0_OR_GREATER
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// Carries an <see cref="OperationCanceledException"/> from the caller's output across an asynchronous <see cref="System.Xml.XmlWriter"/>
    /// on .NET Framework, which would otherwise lose it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On .NET Framework, System.Xml treats a task as a successful write when it has completed and has no exception, which a canceled task never
    /// has.  Its own async methods await the output, and the async method builder cancels their tasks for an
    /// <see cref="OperationCanceledException"/>, so any cancellation from the output that completes before the call returns, whether the output
    /// throws it, returns a task faulted with it, or returns a task that has already been canceled, is lost one level up, and the XML writer
    /// carries on as though the write succeeded.  .NET (Core) checks for successful completion instead, and the <c>net10.0</c> build writes to its
    /// output directly.
    /// </para>
    /// <para>
    /// <see cref="XmlOutputTextWriter"/> and <see cref="XmlOutputStream"/> wrap such an exception in an <see cref="XmlOutputCanceledException"/>,
    /// which faults System.Xml's tasks rather than canceling them, and <see cref="Unwrap"/> gives the original back once it has reached the writer.
    /// </para>
    /// </remarks>
    internal static class XmlOutputCancellation
    {
        /// <summary>
        /// Gives the exception the output failed with, when <paramref name="exception"/> carried it across the XML writer, or else
        /// <paramref name="exception"/>.
        /// </summary>
        public static Exception Unwrap(Exception exception)
        {
            return exception is XmlOutputCanceledException carrier ? carrier.InnerException : exception;
        }
    }

    /// <summary>
    /// Carries an <see cref="OperationCanceledException"/> from the caller's output across an asynchronous <see cref="System.Xml.XmlWriter"/>,
    /// as <see cref="XmlOutputCancellation"/> describes.  It never reaches the caller.
    /// </summary>
    internal sealed class XmlOutputCanceledException : Exception
    {
        public XmlOutputCanceledException(OperationCanceledException innerException) : base(innerException.Message, innerException)
        {
        }
    }

    /// <summary>
    /// A text writer over the caller's text writer, for an asynchronous <see cref="System.Xml.XmlWriter"/>, that carries each cancellation of an
    /// asynchronous write or flush across the XML writer, as <see cref="XmlOutputCancellation"/> describes.
    /// </summary>
    /// <remarks>
    /// Every member passes through to the caller's writer.  Disposing it disposes the caller's writer, which the XML writer does only when its
    /// <see cref="System.Xml.XmlWriterSettings.CloseOutput"/> setting is <c>true</c>.
    /// </remarks>
    internal sealed class XmlOutputTextWriter : TextWriter
    {
        private readonly TextWriter _writer;

        public XmlOutputTextWriter(TextWriter writer) : base(writer.FormatProvider)
        {
            _writer = writer;
        }

        public override System.Text.Encoding Encoding => _writer.Encoding;

        public override IFormatProvider FormatProvider => _writer.FormatProvider;

#if NETSTANDARD2_1_OR_GREATER
        [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
        public override string NewLine
        {
            get => _writer.NewLine;
            set => _writer.NewLine = value;
        }

        public override void Write(char value) => _writer.Write(value);

        public override void Write(char[] buffer, int index, int count) => _writer.Write(buffer, index, count);

#if NETSTANDARD2_1_OR_GREATER
        public override void Write(string? value) => _writer.Write(value);
#else
        public override void Write(string value) => _writer.Write(value);
#endif

        public override void Flush() => _writer.Flush();

        public override async Task WriteAsync(char value)
        {
            try
            {
                await _writer.WriteAsync(value).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

#if NETSTANDARD2_1_OR_GREATER
        public override async Task WriteAsync(string? value)
#else
        public override async Task WriteAsync(string value)
#endif
        {
            try
            {
                await _writer.WriteAsync(value).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            try
            {
                await _writer.WriteAsync(buffer, index, count).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task WriteLineAsync()
        {
            try
            {
                await _writer.WriteLineAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task WriteLineAsync(char value)
        {
            try
            {
                await _writer.WriteLineAsync(value).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

#if NETSTANDARD2_1_OR_GREATER
        public override async Task WriteLineAsync(string? value)
#else
        public override async Task WriteLineAsync(string value)
#endif
        {
            try
            {
                await _writer.WriteLineAsync(value).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task WriteLineAsync(char[] buffer, int index, int count)
        {
            try
            {
                await _writer.WriteLineAsync(buffer, index, count).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task FlushAsync()
        {
            try
            {
                await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

#if NETSTANDARD2_1_OR_GREATER
        public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                await _writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                await _writer.WriteLineAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override ValueTask DisposeAsync() => _writer.DisposeAsync();
#endif

        public override string ToString() => _writer.ToString();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _writer.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A stream over the caller's stream, for an asynchronous <see cref="System.Xml.XmlWriter"/>, that carries each cancellation of an
    /// asynchronous write or flush across the XML writer, as <see cref="XmlOutputCancellation"/> describes.
    /// </summary>
    /// <remarks>
    /// Every member passes through to the caller's stream.  Disposing it disposes the caller's stream, which the XML writer does only when its
    /// <see cref="System.Xml.XmlWriterSettings.CloseOutput"/> setting is <c>true</c>.
    /// </remarks>
    internal sealed class XmlOutputStream : Stream
    {
        private readonly Stream _stream;

        public XmlOutputStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush() => _stream.Flush();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _stream.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public override void WriteByte(byte value) => _stream.WriteByte(value);

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await _stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

#if NETSTANDARD2_1_OR_GREATER
        public override int Read(Span<byte> buffer) => _stream.Read(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _stream.ReadAsync(buffer, cancellationToken);

        public override void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                throw new XmlOutputCanceledException(exception);
            }
        }

        public override ValueTask DisposeAsync() => _stream.DisposeAsync();
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
#endif
