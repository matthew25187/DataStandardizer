using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.InternetMessage.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="MboxReader"/> and <see cref="MboxWriter"/>, and of a large mailbox in a file.
    /// </summary>
    public class MboxFileTests
    {
        private const string Message = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\none\n";
        private const string Mailbox = "From a@b.test Thu Jan  1 00:00:00 2026\n" + Message + "\n";

        // ref. #426: a mailbox of this many messages has to be read with steady memory use.
        private const int ManyMessages = 10000;

        // The block of the reader, which is all that it holds of a file between messages.
        private const long BlockSize = 16 * 1024;

        private static readonly byte[] OldContent = Octets.Of("old content, longer than what is written, because it has to be replaced whole, and this text does that, as it is well over the length of the new file");

        [Fact]
        public void ReadMessage_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Mailbox)))
            using (var subject = new MboxReader(file.Path))
            {
                // act
                var testResult = Mailboxes.ReadAll(subject);

                // assert
                testResult.Should().Equal(Message);
            }
        }

        [Fact]
        public async Task ReadMessageAsync_FilePath_ReadsTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Mailbox)))
            {
                var subject = new MboxReader(file.Path);
                await AsyncDisposal.UsingAsync(subject, async () =>
                {
                    // act
                    var testResult = await Mailboxes.ReadAllAsync(subject);

                    // assert
                    testResult.Should().Equal(Message);
                });
            }
        }

        [Fact]
        public void Constructor_ReaderFilePath_OpensForAsynchronousReading()
        {
            // arrange
            using (var file = TemporaryFile.Create(Octets.Of(Mailbox)))
            using (var recorder = new OpenedFileStreamRecorder())
            {
                // act
                using (new MboxReader(file.Path))
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
            using (var file = TemporaryFile.Create(Octets.Of(Mailbox)))
            {
                var subject = new MboxReader(file.Path);

                // act
                subject.Dispose();

                // assert
                Action reopen = file.OpenExclusively;
                reopen.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_ReaderNullPath_Throws()
        {
            // act
            Action testAction = () => new MboxReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WriterNullPath_Throws()
        {
            // act
            Action testAction = () => new MboxWriter((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteMessage_FilePath_ReplacesTheFile()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(Mailbox);
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                using (var subject = new MboxWriter(file.Path))
                {
                    subject.WriteMessage(entry.Envelope, entry.Message);
                }

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Mailbox);
            }
        }

        [Fact]
        public async Task WriteMessageAsync_FilePath_ReplacesTheFile()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(Mailbox);
            using (var file = TemporaryFile.Create(OldContent))
            {
                // act
                var subject = new MboxWriter(file.Path);
                await AsyncDisposal.UsingAsync(subject, () => subject.WriteMessageAsync(entry.Envelope, entry.Message));

                // assert
                Octets.ToText(System.IO.File.ReadAllBytes(file.Path)).Should().Be(Mailbox);
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
                using (new MboxWriter(file.Path))
                {
                    // assert
                    recorder.Opened.IsAsync.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void ReadMessage_LargeMailboxFile_HoldsOnlyWhatABlockDoes()
        {
            // arrange
            using (var file = TemporaryFile.Create(Mailboxes.Repeat(Mailbox, ManyMessages)))
            using (var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
            using (var subject = new MboxReader(stream))
            {
                // act
                var testResult = Mailboxes.ReadAndMeasure(subject, () => stream.Position);

                // assert
                testResult.Count.Should().Be(ManyMessages);
                testResult.MostHeld.Should().BeLessOrEqualTo(BlockSize);
            }
        }
    }
}
