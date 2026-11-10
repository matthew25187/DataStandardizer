using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MhtmlWriter"/>.
    /// </summary>
    public class MhtmlWriterTests
    {
        private const string Unrelated = "From: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: text/html\r\n\r\n<p>page</p>\r\n";

        [Fact]
        public void WriteDocument_NoDateOrFrom_RecordsDiagnostics()
        {
            // arrange
            var document = Read(MhtmlShapes.RelativePartsNoBase);
            using (var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions))
            {
                // act
                subject.WriteDocument(document);

                // assert
                subject.Diagnostics.Should().HaveCount(2).And.OnlyContain(diagnostic => diagnostic.Kind == ImfDiagnosticKind.MissingRequiredField);
            }
        }

        [Fact]
        public void WriteMessage_NoDateOrFromWithImfWriter_Throws()
        {
            // arrange
            var document = Read(MhtmlShapes.RelativePartsNoBase);
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(document.Message);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteDocument_InvalidStructure_ThrowsAndWritesNothing()
        {
            // arrange
            var document = Read(MhtmlShapes.Word, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true });
            var output = new MemoryStream();
            using (var subject = new MhtmlWriter(output, EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.WriteDocument(document);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteDocument_InvalidStructureAllowed_WritesTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true };
            var document = Read(MhtmlShapes.Word, options);
            var output = new MemoryStream();
            using (var subject = new MhtmlWriter(output, options))
            {
                // act
                subject.WriteDocument(document);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(MhtmlShapes.Word);
            }
        }

        [Fact]
        public void WriteDocument_MessageChangedSinceTheDocumentWasMade_ChecksItAsItNowIs()
        {
            // arrange
            var document = Read(MhtmlShapes.Rfc2557Section94);
            ((ImfMultipart)document.Message.Body).ContentType = DataStandardizer.Communication.Mime.MimeMediaType.MultipartRelated;
            var output = new MemoryStream();
            using (var subject = new MhtmlWriter(output, EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.WriteDocument(document);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
            }
        }

        [Fact]
        public void WriteDocument_NotMultipart_WritesTheSameBytes()
        {
            // arrange
            var document = Read(Unrelated);
            var output = new MemoryStream();
            using (var subject = new MhtmlWriter(output, EntityEvents.DocumentOptions))
            {
                // act
                subject.WriteDocument(document);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(Unrelated);
                subject.BytePosition.Should().Be(Unrelated.Length);
            }
        }

        [Fact]
        public async Task WriteDocumentAsync_Document_WritesTheSameBytes()
        {
            // arrange
            var document = Read(MhtmlShapes.Rfc2557Section93);
            var output = new MemoryStream();
            var subject = new MhtmlWriter(output, EntityEvents.DocumentOptions);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteDocumentAsync(document);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(MhtmlShapes.Rfc2557Section93);
            });
        }

        [Fact]
        public async Task WriteDocumentAsync_InvalidStructure_FailsTheTask()
        {
            // arrange
            var document = Read(MhtmlShapes.Word, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true });
            var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.WriteDocumentAsync(document);

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public void WriteDocument_Null_Throws()
        {
            // arrange
            using (var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions))
            {
                // act
                Action testAction = () => subject.WriteDocument(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteDocument_Disposed_Throws()
        {
            // arrange
            var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions);
            subject.Dispose();

            // act
            Action testAction = () => subject.WriteDocument(null);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Flush_Disposed_Throws()
        {
            // arrange
            var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions);
            subject.Dispose();

            // act
            Action testAction = () => subject.Flush();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task FlushAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MhtmlWriter(new MemoryStream(), EntityEvents.DocumentOptions);
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.FlushAsync();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new MhtmlWriter((Stream)null, EntityEvents.DocumentOptions);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        private static MhtmlDocument Read(string document)
        {
            return Read(document, new ImfOptions { AllowMissingRequiredFields = true });
        }

        private static MhtmlDocument Read(string document, ImfOptions options)
        {
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(document)), options))
            {
                return reader.ReadDocument();
            }
        }
    }
}
