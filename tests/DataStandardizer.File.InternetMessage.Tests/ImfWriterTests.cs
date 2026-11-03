using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfWriter"/>.
    /// </summary>
    public class ImfWriterTests
    {
        private const string Header = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n";
        private const string Body = "Body\r\n";
        private const string Message = Header + "\r\n" + Body;

        // The bytes of Header and Message.
        private const int HeaderLength = 55;
        private const int MessageLength = 63;

        private const int FirstFieldLength = 39;
        private const int TwoMessagesLength = 126;
        private const string TwoMessages = Message + Message;

        [Theory]
        [MemberData(nameof(Rfc5322Messages.All), MemberType = typeof(Rfc5322Messages))]
        public void WriteMessage_AppendixAMessage_WritesItBackByteForByte(string message)
        {
            // arrange
            var subject = ImfMessages.Read(message);
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                // act
                writer.WriteMessage(subject);

                // assert
                Octets.ToText(stream.ToArray()).Should().Be(message);
            }
        }

        [Theory]
        [MemberData(nameof(Rfc5322Messages.All), MemberType = typeof(Rfc5322Messages))]
        public async Task WriteMessageAsync_AppendixAMessage_WritesItBackByteForByte(string message)
        {
            // arrange
            var subject = ImfMessages.Read(message);
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(subject));

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(message);
        }

        [Fact]
        public void WriteMessage_NoEmptyLineWasRead_WritesNone()
        {
            // arrange
            var subject = ImfMessages.Read(Header);

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(Header);
        }

        [Fact]
        public void WriteMessage_BodySetAfterNoEmptyLineWasRead_AddsTheEmptyLine()
        {
            // arrange
            var subject = ImfMessages.Read(Header);
            subject.RawBody = Octets.Of(Body);

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(Message);
        }

        [Fact]
        public void WriteMessage_BareLineFeedsRead_KeepsThem()
        {
            // arrange
            const string message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\n\nBody\n";
            var subject = ImfMessages.Read(message, new ImfOptions { AcceptBareLineFeed = true });

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void WriteMessage_NewFieldForAContainer_UsesALineFeed()
        {
            // arrange
            var subject = ImfMessages.Read("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\n", new ImfOptions { AcceptBareLineFeed = true });
            subject.Subject = "Hi";
            subject.RawBody = Octets.Of("Body\n");
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream, new ImfOptions(), true))
            {
                // act
                writer.WriteMessage(subject);

                // assert
                Octets.ToText(stream.ToArray()).Should().Be("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\nSubject: Hi\n\nBody\n");
            }
        }

        [Fact]
        public void WriteMessage_NonAsciiBodyInDefaultWriter_IsRefusedAndWritesNothing()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            subject.Fields.Add(new ImfHeaderField("X-A", "\x00E9", ImfHeaderFieldKind.Unstructured));
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_BadFieldName_IsRefusedAndWritesNothing()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            subject.Fields.Add(new ImfHeaderField("Bad Name", "1", ImfHeaderFieldKind.Unstructured));
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_AfterARefusal_CanWriteAnother()
        {
            // arrange
            var refused = new ImfMessage();
            var subject = ImfMessages.Read(Message);
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                Record.Exception(() => writer.WriteMessage(refused));

                // act
                writer.WriteMessage(subject);

                // assert
                Octets.ToText(stream.ToArray()).Should().Be(Message);
            }
        }

        [Fact]
        public void WriteMessage_TwoMessages_WritesThemOneAfterAnother()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                // act
                writer.WriteMessage(subject);
                writer.WriteMessage(subject);

                // assert
                Octets.ToText(stream.ToArray()).Should().Be(TwoMessages);
                writer.EntitiesWritten.Should().Be(2);
                writer.FieldsWritten.Should().Be(4);
                writer.BytePosition.Should().Be(TwoMessagesLength);
            }
        }

        [Fact]
        public void WriteMessage_Progress_ReportsEachFieldAndTheBody()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var progress = new RecordingProgress();
            using (var writer = new ImfWriter(new MemoryStream()))
            {
                // act
                writer.WriteMessage(subject, progress);

                // assert
                progress.Reports.Should().Equal(
                    new ImfWriteProgress(1, 0, FirstFieldLength),
                    new ImfWriteProgress(2, 0, HeaderLength),
                    new ImfWriteProgress(2, 0, MessageLength),
                    new ImfWriteProgress(2, 1, MessageLength));
            }
        }

        [Fact]
        public async Task WriteMessageAsync_Progress_ReportsEachFieldAndTheBody()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var progress = new RecordingProgress();
            var writer = new ImfWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(subject, progress));

            // assert
            progress.Reports.Should().Equal(
                new ImfWriteProgress(1, 0, FirstFieldLength),
                new ImfWriteProgress(2, 0, HeaderLength),
                new ImfWriteProgress(2, 0, MessageLength),
                new ImfWriteProgress(2, 1, MessageLength));
        }

        [Fact]
        public void WriteMessage_ProgressThrows_EndsTheCallAndLeavesTheWriterBroken()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            using (var writer = new ImfWriter(new MemoryStream()))
            {
                var firstWrite = Record.Exception(() => writer.WriteMessage(subject, new ThrowingProgress()));

                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                firstWrite.Should().NotBeNull();
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_StreamFails_LeavesTheWriterBroken()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            using (var writer = new ImfWriter(new FailingWriteStream()))
            {
                var firstWrite = Record.Exception(() => writer.WriteMessage(subject));

                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                firstWrite.Should().BeOfType<IOException>();
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task WriteMessageAsync_StreamFails_FaultsTheTask()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var writer = new ImfWriter(new FailingWriteStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var testResult = writer.WriteMessageAsync(subject);

                // assert
                Func<Task> awaited = () => testResult;
                await awaited.Should().ThrowAsync<IOException>();
            });
        }

        [Fact]
        public async Task WriteMessageAsync_AlreadyCanceled_CancelsTheTaskAndWritesNothing()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var stream = new MemoryStream();
            var writer = new ImfWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();

                    // act
                    Func<Task> testAction = () => writer.WriteMessageAsync(subject, null, cancellation.Token);

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                    stream.Length.Should().Be(0);
                }
            });
        }

        [Fact]
        public async Task WriteMessageAsync_WriteInProgress_ThrowsFromTheCall()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var stream = new GatedWriteStream();
            var writer = new ImfWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var write = writer.WriteMessageAsync(subject);

                // act
                Action testAction = () => writer.WriteMessageAsync(subject);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await write;
            });
        }

        [Fact]
        public void WriteMessage_NullMessage_Throws()
        {
            // arrange
            using (var writer = new ImfWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_Disposed_Throws()
        {
            // arrange
            var subject = ImfMessages.Read(Message);
            var writer = new ImfWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.WriteMessage(subject);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new ImfWriter(stream);

            // act
            writer.Dispose();

            // assert
            stream.CanWrite.Should().BeTrue();
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNothingMore()
        {
            // arrange
            var writer = new ImfWriter(new MemoryStream());
            await writer.DisposeAsync();

            // act
            Func<Task> testAction = async () => await writer.DisposeAsync();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public void Flush_Disposed_Throws()
        {
            // arrange
            var writer = new ImfWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.Flush();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Flush_FailingStream_LeavesTheWriterBroken()
        {
            // arrange
            using (var writer = new ImfWriter(new FailingWriteStream()))
            {
                var firstFlush = Record.Exception(() => writer.Flush());

                // act
                Action testAction = () => writer.Flush();

                // assert
                firstFlush.Should().BeOfType<IOException>();
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task FlushAsync_Stream_Completes()
        {
            // arrange
            var writer = new ImfWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = () => writer.FlushAsync();

                // assert
                await testAction.Should().NotThrowAsync();
            });
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new ImfWriter((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_StreamThatCannotWrite_Throws()
        {
            // act
            Action testAction = () => new ImfWriter(new ReadOnlyMemoryStream());

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfWriter(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NoCharsetResolver_Throws()
        {
            // act
            Action testAction = () => new ImfWriter(new MemoryStream(), new ImfOptions { CharsetResolver = null });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var writer = new ImfWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => { var ignored = writer.Diagnostics; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }
    }
}
