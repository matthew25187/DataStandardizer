using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="ImfHeaderWriter"/> and <see cref="ImfEntityWriter"/>.
    /// </summary>
    public class ImfWriterFileTests
    {
        private const string Field = "A: 1\r\n";
        private const string RawMessage = "A: 1\r\n\r\nx";

        private static readonly byte[] OldContent = Octets.Of("old content, longer than what is written");

        [Fact]
        public void WriteField_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                using (var writer = new ImfHeaderWriter(file.Path))
                {
                    writer.WriteField("A", "1", ImfHeaderFieldKind.Unstructured);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Field);
            }
        }

        [Fact]
        public async Task WriteFieldAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                var writer = new ImfHeaderWriter(file.Path);
                await AsyncDisposal.UsingAsync(writer, () => writer.WriteFieldAsync("A", "1", ImfHeaderFieldKind.Unstructured).AsTask());

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Field);
            }
        }

        [Fact]
        public void WriteMessage_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                using (var writer = new ImfEntityWriter(file.Path))
                {
                    writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(RawMessage)));
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(RawMessage);
            }
        }

        [Fact]
        public async Task WriteMessageAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                var writer = new ImfEntityWriter(file.Path);
                await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(RawMessage))));

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(RawMessage);
            }
        }

        [Fact]
        public void Constructor_HeaderWriterFilePath_OpensForAsynchronousWriting()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new ImfHeaderWriter(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Constructor_EntityWriterFilePath_OpensForAsynchronousWriting()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new ImfEntityWriter(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Dispose_HeaderWriterFilePath_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                var writer = new ImfHeaderWriter(file.Path);
                writer.Dispose();

                // act
                Action testAction = file.OpenExclusively;

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public async Task DisposeAsync_EntityWriterFilePath_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                var writer = new ImfEntityWriter(file.Path);
                await writer.DisposeAsync();

                // act
                Action testAction = file.OpenExclusively;

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_HeaderWriterNullPath_Throws()
        {
            // act
            Action testAction = () => new ImfHeaderWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageFilePath");
        }

        [Fact]
        public void Constructor_EntityWriterNullPath_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageFilePath");
        }

        [Fact]
        public void Constructor_EntityWriterFilePathAndBadOptions_ThrowsBeforeOpeningTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                Action testAction = () => new ImfEntityWriter(file.Path, new ImfOptions { MaxPartCount = 0 });

                // assert
                testAction.Should().Throw<ArgumentException>();
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Octets.ToText(OldContent));
            }
        }

        [Fact]
        public void WriteMessage_FileStreamContent_ReadsItAgainForTheOutput()
        {
            // arrange
            using (var content = TemporaryFile.Create(OldContent))
            using (var output = TemporaryFile.Create(new byte[0]))
            using (var contentStream = new FileStream(content.Path, FileMode.Open, FileAccess.Read))
            {
                var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, contentStream);

                // act
                using (var writer = new ImfEntityWriter(output.Path))
                {
                    writer.WriteMessage(source);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(output.Path)).Should().Be(
                    "MIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\nold content, longer than what is written");
            }
        }
    }
}
