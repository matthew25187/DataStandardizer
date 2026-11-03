using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="ImfEntityReader"/>.
    /// </summary>
    public class ImfEntityReaderFileTests
    {
        private const string Message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\n";
        private static readonly byte[] MessageBytes = Octets.Of(Message);

        private static readonly string[] ExpectedEvents =
        {
            "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /",
        };

        [Fact]
        public void ReadEvent_FilePath_ReadsTheEvents()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            using (var reader = new ImfEntityReader(file.Path))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(ExpectedEvents);
            }
        }

        [Fact]
        public async Task ReadEventAsync_FilePath_ReadsTheEvents()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            {
                var reader = new ImfEntityReader(file.Path);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    var testResult = await EntityEvents.ReadOutlineAsync(reader);

                    // assert
                    testResult.Should().Equal(ExpectedEvents);
                });
            }
        }

        [Fact]
        public void ReadEvent_FilePathWithOptions_AppliesThem()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            using (var reader = new ImfEntityReader(file.Path, new ImfOptions { MaxHeaderSectionLength = 1 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void Constructor_FilePath_OpensTheFileForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                var reader = new ImfEntityReader(file.Path);

                // assert
                recorder.Opened.IsAsync.Should().BeTrue();
                recorder.Opened.CanRead.Should().BeTrue();
                recorder.Opened.CanWrite.Should().BeFalse();
                reader.Dispose();
            }
        }

        [Fact]
        public void Dispose_FilePathNotRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            {
                var reader = new ImfEntityReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Dispose_FilePathRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            {
                var reader = new ImfEntityReader(file.Path);
                reader.ReadEvent();

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public async Task DisposeAsync_FilePathNotRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            {
                var reader = new ImfEntityReader(file.Path);

                // act
                await reader.DisposeAsync();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_InvalidOptionsWithFilePath_DoesNotOpenTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(MessageBytes))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                Action testAction = () => new ImfEntityReader(file.Path, new ImfOptions { MaxNestingDepth = 0 });

                // assert
                testAction.Should().Throw<ArgumentException>().WithParameterName("options");
                recorder.Opened.Should().BeNull();
            }
        }

        [Fact]
        public void Constructor_NullFilePath_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfEntityReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageFilePath");
        }

        [Fact]
        public void Constructor_MissingFile_ThrowsFileNotFoundException()
        {
            // arrange
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // act
            Action testAction = () => new ImfEntityReader(path);

            // assert
            testAction.Should().Throw<FileNotFoundException>();
        }
    }
}
