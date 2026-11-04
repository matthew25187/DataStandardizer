using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="MhtmlReader"/> and <see cref="MhtmlWriter"/>.
    /// </summary>
    public class MhtmlFileTests
    {
        // A page with no Date or From field, which only the MHTML types accept.
        private const string Document = "MIME-Version: 1.0\r\nContent-Type: text/html\r\nContent-Location: http://example.org/page.html\r\n\r\n<p>page</p>\r\n";
        private const string PageUrl = "http://example.org/page.html";

        private static readonly byte[] OldContent = Octets.Of("old content, longer than what is written, because it has to be replaced whole, and this text does that");

        [Fact]
        public void ReadDocument_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Document)))
            using (var subject = new MhtmlReader(file.Path))
            {
                // act
                var testResult = subject.ReadDocument();

                // assert
                testResult.Resources[0].Uri.OriginalString.Should().Be(PageUrl);
            }
        }

        [Fact]
        public async Task ReadDocumentAsync_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Document)))
            {
                var subject = new MhtmlReader(file.Path);
                await AsyncDisposal.UsingAsync(subject, async () =>
                {
                    // act
                    var testResult = await subject.ReadDocumentAsync();

                    // assert
                    testResult.Resources[0].Uri.OriginalString.Should().Be(PageUrl);
                });
            }
        }

        [Fact]
        public void Constructor_FilePath_OpensForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Document)))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new MhtmlReader(file.Path))
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
            using (var file = TemporaryFile.Create(Octets.Of(Document)))
            {
                var subject = new MhtmlReader(file.Path);

                // act
                subject.Dispose();

                // assert
                Action reopen = file.OpenExclusively;
                reopen.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_NullPath_Throws()
        {
            // act
            Action testAction = () => new MhtmlReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteDocument_FilePath_ReplacesTheFile()
        {
            // arrange
            MhtmlDocument document;
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(Document))))
            {
                document = reader.ReadDocument();
            }

            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                using (var subject = new MhtmlWriter(file.Path))
                {
                    subject.WriteDocument(document);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Document);
            }
        }

        [Fact]
        public async Task WriteDocumentAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            MhtmlDocument document;
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(Document))))
            {
                document = reader.ReadDocument();
            }

            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                var subject = new MhtmlWriter(file.Path);
                await AsyncDisposal.UsingAsync(subject, () => subject.WriteDocumentAsync(document));

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Document);
            }
        }

        [Fact]
        public void Constructor_WriterNullPath_Throws()
        {
            // act
            Action testAction = () => new MhtmlWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
