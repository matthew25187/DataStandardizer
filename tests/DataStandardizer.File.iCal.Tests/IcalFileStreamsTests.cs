using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Checks that every file-path constructor opens its file with <c>FileOptions.Asynchronous</c>, so the asynchronous members do no
    /// synchronous I/O.
    /// </summary>
    public class IcalFileStreamsTests
    {
        public static TheoryData<string, Func<string, IDisposable>> FileReaders => new TheoryData<string, Func<string, IDisposable>>()
        {
            { nameof(IcalContentLineReader), path => new IcalContentLineReader(path) },
            { nameof(IcalReader), path => new IcalReader(path) },
            { nameof(XcalReader), path => new XcalReader(path) },
            { nameof(JcalReader), path => new JcalReader(path) }
        };

        public static TheoryData<string, Func<string, IDisposable>> FileWriters => new TheoryData<string, Func<string, IDisposable>>()
        {
            { nameof(IcalContentLineWriter), path => new IcalContentLineWriter(path) },
            { nameof(IcalWriter), path => new IcalWriter(path) },
            { nameof(XcalWriter), path => new XcalWriter(path) },
            { nameof(JcalWriter), path => new JcalWriter(path) }
        };

        [Theory]
        [MemberData(nameof(FileReaders))]
        public void Constructor_FilePathReader_OpensFileAsynchronously(string type, Func<string, IDisposable> create)
        {
            // arrange
            var path = Path.GetTempFileName();
            try
            {
                // act
                var testResult = OpenedStreams(() => create(path));

                // assert
                testResult.Should().ContainSingle(type).Which.Should().BeEquivalentTo(new OpenedStream(isAsync: true, canRead: true, canWrite: false));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(FileWriters))]
        public void Constructor_FilePathWriter_OpensFileAsynchronously(string type, Func<string, IDisposable> create)
        {
            // arrange
            var path = Path.GetTempFileName();
            try
            {
                // act
                var testResult = OpenedStreams(() => create(path));

                // assert
                testResult.Should().ContainSingle(type).Which.Should().BeEquivalentTo(new OpenedStream(isAsync: true, canRead: false, canWrite: true));
            }
            finally
            {
                System.IO.File.Delete(path);
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
            IcalFileStreams.Opened = fileStream =>
            {
                fileStreams.Add(fileStream);
                opened.Add(new OpenedStream(fileStream.IsAsync, fileStream.CanRead, fileStream.CanWrite));
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
            public OpenedStream(bool isAsync, bool canRead, bool canWrite)
            {
                IsAsync = isAsync;
                CanRead = canRead;
                CanWrite = canWrite;
            }

            public bool IsAsync { get; }

            public bool CanRead { get; }

            public bool CanWrite { get; }
        }
    }
}
