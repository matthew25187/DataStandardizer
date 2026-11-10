using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Checks that every file-path constructor opens its file with <c>FileOptions.Asynchronous</c>, so the asynchronous members do no
    /// synchronous I/O, and with the buffer size it means to.
    /// </summary>
    public class IcalFileStreamsTests
    {
        // The buffer sizes, in bytes, that the iCalendar readers and writers, and the jCal and xCal ones, give their file streams.
        private const int IcalStreamBufferSize = 16384;
        private const int JcalAndXcalStreamBufferSize = 4096;

        public static TheoryData<string, Func<string, IDisposable>, int> FileReaders => new TheoryData<string, Func<string, IDisposable>, int>()
        {
            { nameof(IcalContentLineReader), path => new IcalContentLineReader(path), IcalStreamBufferSize },
            { nameof(IcalReader), path => new IcalReader(path), IcalStreamBufferSize },
            { nameof(XcalReader), path => new XcalReader(path), JcalAndXcalStreamBufferSize },
            { nameof(JcalReader), path => new JcalReader(path), JcalAndXcalStreamBufferSize },
        };

        public static TheoryData<string, Func<string, IDisposable>, int> FileWriters => new TheoryData<string, Func<string, IDisposable>, int>()
        {
            { nameof(IcalContentLineWriter), path => new IcalContentLineWriter(path), IcalStreamBufferSize },
            { nameof(IcalWriter), path => new IcalWriter(path), IcalStreamBufferSize },
            { nameof(XcalWriter), path => new XcalWriter(path), JcalAndXcalStreamBufferSize },
            { nameof(JcalWriter), path => new JcalWriter(path), JcalAndXcalStreamBufferSize },
        };

        [Theory]
        [MemberData(nameof(FileReaders), DisableDiscoveryEnumeration = true)]
        public void Constructor_FilePathReader_OpensFileAsynchronouslyForReading(string type, Func<string, IDisposable> create, int bufferSize)
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                // act
                var testResult = OpenedStreams(() => create(file.Path));

                // assert
                testResult.Should().ContainSingle(type).Which.Should()
                    .BeEquivalentTo(new OpenedStream(isAsync: true, canRead: true, canWrite: false, bufferSize: bufferSize));
            }
        }

        [Theory]
        [MemberData(nameof(FileWriters), DisableDiscoveryEnumeration = true)]
        public void Constructor_FilePathWriter_OpensFileAsynchronouslyForWriting(string type, Func<string, IDisposable> create, int bufferSize)
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                // act
                var testResult = OpenedStreams(() => create(file.Path));

                // assert
                testResult.Should().ContainSingle(type).Which.Should()
                    .BeEquivalentTo(new OpenedStream(isAsync: true, canRead: false, canWrite: true, bufferSize: bufferSize));
            }
        }

        /// <summary>
        /// Creates and disposes a reader or writer, and returns how each file stream it opened was opened.  Each file stream is disposed too, so
        /// that the file can be deleted even if the reader or writer left it open.
        /// </summary>
        private static List<OpenedStream> OpenedStreams(Func<IDisposable> create)
        {
            var opened = new List<OpenedStream>();
            var fileStreams = new List<FileStream>();
            IcalFileStreams.Opened = (fileStream, bufferSize) =>
            {
                fileStreams.Add(fileStream);
                opened.Add(new OpenedStream(fileStream.IsAsync, fileStream.CanRead, fileStream.CanWrite, bufferSize));
            };
            try
            {
                create().Dispose();
            }
            finally
            {
                IcalFileStreams.Opened = null;
                fileStreams.ForEach(fileStream => fileStream.Dispose());
            }

            return opened;
        }

        private sealed class OpenedStream
        {
            public OpenedStream(bool isAsync, bool canRead, bool canWrite, int bufferSize)
            {
                IsAsync = isAsync;
                CanRead = canRead;
                CanWrite = canWrite;
                BufferSize = bufferSize;
            }

            public bool IsAsync { get; }

            public bool CanRead { get; }

            public bool CanWrite { get; }

            public int BufferSize { get; }
        }
    }
}
