using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="ImfReader"/> and <see cref="ImfWriter"/>.
    /// </summary>
    public class ImfMessageFileTests
    {
        private const string Message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n\r\nBody\r\n";

        private static readonly byte[] OldContent = Octets.Of("old content, longer than what is written, because it has to be replaced whole, and this text does that");

        [Fact]
        public void ReadMessage_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Message)))
            using (var reader = new ImfReader(file.Path))
            {
                // act
                var testResult = reader.ReadMessage();

                // assert
                testResult.Fields.Should().HaveCount(2);
                Octets.ToText(testResult.RawBody.Span).Should().Be("Body\r\n");
            }
        }

        [Fact]
        public async Task ReadMessageAsync_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Message)))
            {
                var reader = new ImfReader(file.Path);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    var testResult = await reader.ReadMessageAsync();

                    // assert
                    testResult.Fields.Should().HaveCount(2);
                });
            }
        }

        [Fact]
        public void Constructor_FilePath_OpensForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Message)))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new ImfReader(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Dispose_NothingRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Message)))
            {
                var reader = new ImfReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action reopen = file.OpenExclusively;
                reopen.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_BadOptions_LeavesTheFileUnopened()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Message)))
            {
                // act
                Action testAction = () => new ImfReader(file.Path, new ImfOptions { MaxEntitySize = 0 });

                // assert
                testAction.Should().Throw<ArgumentException>();
                Action reopen = file.OpenExclusively;
                reopen.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_NullPath_Throws()
        {
            // act
            Action testAction = () => new ImfReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteMessage_FilePath_ReplacesTheFile()
        {
            // arrange
            var message = ImfMessages.Read(Message);
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                using (var writer = new ImfWriter(file.Path))
                {
                    writer.WriteMessage(message);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Message);
            }
        }

        [Fact]
        public async Task WriteMessageAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            var message = ImfMessages.Read(Message);
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                var writer = new ImfWriter(file.Path);
                await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(message));

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Message);
            }
        }

        [Fact]
        public void Constructor_WriterFilePath_OpensForAsynchronousWriting()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new ImfWriter(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Constructor_WriterNullPath_Throws()
        {
            // act
            Action testAction = () => new ImfWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
