using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="ImfHeaderReader"/>.
    /// </summary>
    public class ImfHeaderReaderFileTests
    {
        private const string Section = "Subject: Hi\r\n there\r\n\r\nbody";
        private static readonly byte[] SectionBytes = Octets.Of(Section);

        private static readonly FieldSnapshot[] ExpectedFields =
        {
            new FieldSnapshot("Subject: Hi\r\n there\r\n", "Subject", " Hi there", " Hi there", 0, 1),
        };

        [Fact]
        public void ReadField_FilePath_ReadsTheFields()
        {
            // arrange
            using (var file = TemporaryFile.Create(SectionBytes))
            using (var reader = new ImfHeaderReader(file.Path))
            {
                // act
                var testResult = HeaderSections.ReadAll(reader);

                // assert
                testResult.Should().BeEquivalentTo(ExpectedFields, options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task ReadFieldAsync_FilePath_ReadsTheFields()
        {
            // arrange
            using (var file = TemporaryFile.Create(SectionBytes))
            {
                var reader = new ImfHeaderReader(file.Path);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    var testResult = await HeaderSections.ReadAllAsync(reader);

                    // assert
                    testResult.Should().BeEquivalentTo(ExpectedFields, options => options.WithStrictOrdering());
                });
            }
        }

        [Fact]
        public void ReadField_FilePathWithOptions_AppliesThem()
        {
            // arrange
            using (var file = TemporaryFile.Create(SectionBytes))
            using (var reader = new ImfHeaderReader(file.Path, new ImfOptions { MaxHeaderFieldCount = 1, MaxHeaderSectionLength = 1 }))
            {
                // act
                Action testAction = () => reader.ReadField();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void Constructor_FilePath_OpensTheFileForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(SectionBytes))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                var reader = new ImfHeaderReader(file.Path);

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
            using (var file = TemporaryFile.Create(SectionBytes))
            {
                var reader = new ImfHeaderReader(file.Path);

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
            using (var file = TemporaryFile.Create(SectionBytes))
            {
                var reader = new ImfHeaderReader(file.Path);
                reader.ReadField();

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
            using (var file = TemporaryFile.Create(SectionBytes))
            {
                var reader = new ImfHeaderReader(file.Path);

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
            using (var file = TemporaryFile.Create(SectionBytes))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                Action testAction = () => new ImfHeaderReader(file.Path, new ImfOptions { MaxHeaderFieldCount = 0 });

                // assert
                testAction.Should().Throw<ArgumentException>().WithParameterName("options");
                recorder.Opened.Should().BeNull();
            }
        }

        [Fact]
        public void Constructor_NullFilePath_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfHeaderReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageFilePath");
        }

        [Fact]
        public void Constructor_MissingFile_ThrowsFileNotFoundException()
        {
            // arrange
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // act
            Action testAction = () => new ImfHeaderReader(path);

            // assert
            testAction.Should().Throw<FileNotFoundException>();
        }
    }
}
