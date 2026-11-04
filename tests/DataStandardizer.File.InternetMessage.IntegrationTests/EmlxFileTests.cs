using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="EmlxReader"/> and <see cref="EmlxWriter"/>, and of a partial file with its part in a file of its own.
    /// </summary>
    public class EmlxFileTests
    {
        private const string Message = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\none\n";
        private const string Plist = "<plist><dict><key>subject</key><string>Hi</string></dict></plist>";
        private const string File = "57\n" + Message + Plist;
        private const string Subject = "Hi";

        private const string PartialMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\n\n--b--\n";
        private const string PartialFile = "289\n" + PartialMessage + Plist;
        private const string PartContent = "AAEC";
        private const string PartialFileName = "1.partial.emlx";
        private const string PartFileName = "1.2.emlxpart";

        // The three octets that the base64 of PartContent decodes to.
        private static readonly byte[] DecodedPart = { 0, 1, 2 };

        private static readonly byte[] OldContent = Octets.Of("old content, longer than what is written, because it has to be replaced whole, and this text does that, as it is well over the length of the new file, and it goes on for a good while after that too");

        [Fact]
        public void ReadEntry_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(File)))
            using (var subject = new EmlxReader(file.Path))
            {
                // act
                var testResult = subject.ReadEntry();

                // assert
                testResult.Metadata.Subject.Should().Be(Subject);
            }
        }

        [Fact]
        public async Task ReadEntryAsync_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(File)))
            {
                var subject = new EmlxReader(file.Path);
                await AsyncDisposal.UsingAsync(subject, async () =>
                {
                    // act
                    var testResult = await subject.ReadEntryAsync();

                    // assert
                    testResult.Metadata.Subject.Should().Be(Subject);
                });
            }
        }

        [Fact]
        public void Constructor_ReaderFilePath_OpensForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(File)))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new EmlxReader(file.Path))
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
            using (var file = TemporaryFile.Create(Octets.Of(File)))
            {
                var subject = new EmlxReader(file.Path);

                // act
                subject.Dispose();

                // assert
                Action testAction = () => file.OpenExclusively();
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_ReaderNullPath_Throws()
        {
            // act
            Action testAction = () => new EmlxReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WriterNullPath_Throws()
        {
            // act
            Action testAction = () => new EmlxWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteEntry_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var input = TemporaryFile.Create(Octets.Of(File)))
            using (var output = TemporaryFile.Create(OldContent))
            using (var reader = new EmlxReader(input.Path))
            {
                var entry = reader.ReadEntry();

                // act
                using (var subject = new EmlxWriter(output.Path))
                {
                    subject.WriteEntry(entry);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(output.Path)).Should().Be(File);
            }
        }

        [Fact]
        public async Task WriteEntryAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            using (var input = TemporaryFile.Create(Octets.Of(File)))
            using (var output = TemporaryFile.Create(OldContent))
            using (var reader = new EmlxReader(input.Path))
            {
                var entry = reader.ReadEntry();
                var subject = new EmlxWriter(output.Path);

                // act
                await AsyncDisposal.UsingAsync(subject, () => subject.WriteEntryAsync(entry));

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(output.Path)).Should().Be(File);
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
                using (new EmlxWriter(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void ReadEntry_PartialFileWithAResolverOfItsPartFiles_ReadsThePart()
        {
            // arrange
            using (var part = TemporaryFile.Create(Octets.Of(PartContent)))
            using (var file = TemporaryFile.Create(Octets.Of(PartialFile)))
            using (var subject = new EmlxReader(file.Path))
            {
                string partFileName = null;
                EmlxPartResolver resolver = missing =>
                {
                    partFileName = missing.GetPartFileName(PartialFileName);
                    return new FileStream(part.Path, FileMode.Open, FileAccess.Read);
                };

                // act
                var testResult = subject.ReadEntry(resolver);

                // assert
                partFileName.Should().Be(PartFileName);
                testResult.MissingParts.Should().BeEmpty();
                testResult.Message.Attachments.Should().ContainSingle().Which.GetContent().ToArray().Should().Equal(DecodedPart);
            }
        }
    }
}
