using System;
using System.IO;
#if NETSTANDARD2_0
using System.Threading;
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Keeps a defect of .NET Framework's <see cref="System.Xml.XmlReader"/> away from the stream or text reader that an XML reader is created
    /// over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On .NET Framework, <c>XmlReader.ReadAsync</c> takes a task that has already been canceled for one that has succeeded, and calls itself
    /// again for as long as its source's read completes that way, until the stack overflows.  A source's read completes that way when it throws
    /// an <see cref="OperationCanceledException"/>, or returns a task that has already been canceled or faulted with one, since the XML reader's
    /// own async methods cancel their tasks for it.  A <see cref="StackOverflowException"/> can't be caught, so the process ends.
    /// </para>
    /// <para>
    /// So on the <c>netstandard2.0</c> build, the only one that runs on .NET Framework, the source is wrapped, and such a failure reaches the XML
    /// reader as a <see cref="SourceCanceledException"/> in a faulted task, which it reports as it does any other failure of its source.  The
    /// read then completes with the <see cref="OperationCanceledException"/> it holds, through <see cref="Unwrap"/>, as it does on every other
    /// target.  A read that hasn't completed when it is returned is shielded the same way once it completes, since it could complete before the
    /// XML reader looks at its task.  Other builds use the source as it is.
    /// </para>
    /// </remarks>
    internal static class XmlSourceGuard
    {
        /// <summary>
        /// Returns the stream to create an XML reader over in place of <paramref name="stream"/>.  Disposing it disposes
        /// <paramref name="stream"/>.
        /// </summary>
        internal static Stream Guard(Stream stream)
        {
#if NETSTANDARD2_0
            return new GuardedStream(stream);
#else
            return stream;
#endif
        }

        /// <summary>
        /// Returns the text reader to create an XML reader over in place of <paramref name="reader"/>.  Disposing it disposes
        /// <paramref name="reader"/>.
        /// </summary>
        internal static TextReader Guard(TextReader reader)
        {
#if NETSTANDARD2_0
            return new GuardedTextReader(reader);
#else
            return reader;
#endif
        }

        /// <summary>
        /// Returns the exception that a read failed with, as the caller sees it: the <see cref="OperationCanceledException"/> that a
        /// <see cref="SourceCanceledException"/> carries, or any other exception as it is.
        /// </summary>
        internal static Exception Unwrap(Exception exception)
        {
#if NETSTANDARD2_0
            if (exception is SourceCanceledException carrier) return carrier.InnerException;
#endif
            return exception;
        }

#if NETSTANDARD2_0
        /// <summary>
        /// Returns <paramref name="read"/> itself if it has succeeded; otherwise a task that completes as it does, except that it faults with a
        /// <see cref="SourceCanceledException"/> where <paramref name="read"/> fails with an <see cref="OperationCanceledException"/>, so it is
        /// never canceled.
        /// </summary>
        private static Task<int> Shield(Task<int> read)
        {
            return read.Status == TaskStatus.RanToCompletion ? read : ShieldAsync(read);
        }

        private static async Task<int> ShieldAsync(Task<int> read)
        {
            try
            {
                // Observe the failure as awaiting the read would: a canceled read gives a TaskCanceledException.
                return await read.ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                // An async method faults its task for any exception that isn't an OperationCanceledException, even when it completes
                // synchronously.
                throw new SourceCanceledException(exception);
            }
        }

        /// <summary>
        /// Returns a task faulted with a <see cref="SourceCanceledException"/> for a read that threw <paramref name="exception"/>.
        /// </summary>
        private static Task<int> Shield(OperationCanceledException exception)
        {
            return Task.FromException<int>(new SourceCanceledException(exception));
        }

        /// <summary>
        /// Carries an <see cref="OperationCanceledException"/> from a source's read through the XML reader.  It never reaches the caller.
        /// </summary>
        internal sealed class SourceCanceledException : Exception
        {
            internal SourceCanceledException(OperationCanceledException innerException)
                : base("The source of the XML reader failed a read with an OperationCanceledException.", innerException)
            {
            }
        }

        /// <summary>
        /// A read-only stream that passes each call to another, shielding its asynchronous reads.  Disposing it disposes the other stream.
        /// </summary>
        private sealed class GuardedStream : Stream
        {
            private readonly Stream _stream;

            internal GuardedStream(Stream stream)
            {
                _stream = stream;
            }

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => false;

            public override long Length => _stream.Length;

            public override long Position
            {
                get => _stream.Position;
                set => _stream.Position = value;
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                Task<int> read;
                try
                {
                    read = _stream.ReadAsync(buffer, offset, count, cancellationToken);
                }
                catch (OperationCanceledException exception)
                {
                    return Shield(exception);
                }

                return Shield(read);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        _stream.Dispose();
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        /// <summary>
        /// A text reader that passes each call to another, shielding its asynchronous reads.  Disposing it disposes the other text reader.
        /// </summary>
        private sealed class GuardedTextReader : TextReader
        {
            private readonly TextReader _reader;

            internal GuardedTextReader(TextReader reader)
            {
                _reader = reader;
            }

            public override int Peek()
            {
                return _reader.Peek();
            }

            public override int Read()
            {
                return _reader.Read();
            }

            public override int Read(char[] buffer, int index, int count)
            {
                return _reader.Read(buffer, index, count);
            }

            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                Task<int> read;
                try
                {
                    read = _reader.ReadAsync(buffer, index, count);
                }
                catch (OperationCanceledException exception)
                {
                    return Shield(exception);
                }

                return Shield(read);
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        _reader.Dispose();
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
#endif
    }
}
