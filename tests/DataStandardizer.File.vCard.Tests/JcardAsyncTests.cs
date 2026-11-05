using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="JcardReader"/> and <see cref="JcardWriter"/>, which do no synchronous I/O, and of the rule that only one operation may be in
    /// progress at a time.
    /// </summary>
    public class JcardAsyncTests
    {
        private const string MinimalVcard = "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nEND:VCARD\r\n";
        private const string MinimalJcard = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]]";
        private const string TwoMinimalJcards = "[" + MinimalJcard + "," + MinimalJcard + "]";

        // A block smaller than most JSON tokens, so that each read is split across blocks.
        private const int SmallReadLength = 7;

        [Fact]
        public async Task ReadCardsAsync_AsyncOnlyStream_ReadsTheCardsWithNoSynchronousRead()
        {
            // arrange
            var stream = new AsyncOnlyStream(Utf8(RfcExampleJcard), false, SmallReadLength);
            using (var reader = new JcardReader(stream))
            {
                // act
                var testResult = await AsyncEnumerables.ToListAsync(reader.ReadCardsAsync());

                // assert
                Describe(testResult).Should().BeEquivalentTo(new[] { RfcExampleProperties }, options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task ReadCardAsync_CanceledToken_Throws()
        {
            // arrange
            var stream = new AsyncOnlyStream(Utf8(MinimalJcard));
            using (var cancellation = new CancellationTokenSource())
            using (var reader = new JcardReader(stream))
            {
                cancellation.Cancel();

                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync(cancellation.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }

        [Fact]
        public async Task ReadCard_AfterACanceledRead_ThrowsInvalidOperation()
        {
            // arrange
            var stream = new AsyncOnlyStream(Utf8(MinimalJcard));
            using (var cancellation = new CancellationTokenSource())
            using (var reader = new JcardReader(stream))
            {
                cancellation.Cancel();
                await ReadPastCancellationAsync(reader, cancellation.Token);

                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task ReadCard_WhileAnAsynchronousReadIsInProgress_Throws()
        {
            // arrange
            var source = new GatedReadStream(Utf8(MinimalJcard));
            using (var reader = new JcardReader(source))
            {
                var pending = reader.ReadCardAsync();

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                await AsyncDisposal.FinallyAsync(
                    () => testAction.Should().Throw<InvalidOperationException>(),
                    async () =>
                    {
                        source.OpenGate();
                        await pending;
                    });
            }
        }

        [Fact]
        public async Task ReadCardAsync_DisposedWhileInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var source = new GatedReadStream(Utf8(MinimalJcard));
            var reader = new JcardReader(source);
            var pending = reader.ReadCardAsync();
            await source.ReadStarted;
            reader.Dispose();
            source.OpenGate();

            // act
            Func<Task> testAction = async () => await pending;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task WriteCardsAsync_AsyncOnlyStream_WritesTheCardsWithNoSynchronousWrite()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var writer = new JcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardsAsync(cards));

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(TwoMinimalJcards);
        }

        [Fact]
        public async Task WriteCardsAsync_AsynchronousSequence_WritesTheCards()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var cards = AsyncEnumerables.Yielding(ReadVcards(MinimalVcard + MinimalVcard));
            var writer = new JcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardsAsync(cards));

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(TwoMinimalJcards);
        }

        [Fact]
        public async Task DisposeAsync_OneCardHeld_WritesItWithNoSynchronousWrite()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var card = ReadVcards(MinimalVcard)[0];
            var writer = new JcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardAsync(card).AsTask());

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(MinimalJcard);
        }

        [Fact]
        public async Task WriteCardAsync_CanceledToken_Throws()
        {
            // arrange
            var card = ReadVcards(MinimalVcard)[0];
            var writer = new JcardWriter(new AsyncOnlyWriteStream());
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                // act
                Func<Task> testAction = async () => await writer.WriteCardAsync(card, cancellation.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }

        /// <summary>
        /// Reads a card with a canceled token, and leaves the cancellation for the test to go on from.
        /// </summary>
        private static async Task ReadPastCancellationAsync(JcardReader reader, CancellationToken cancellationToken)
        {
            try
            {
                await reader.ReadCardAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Assert.Fail("The read was expected to be canceled.");
        }

        /// <summary>
        /// A stream whose asynchronous reads wait until the test opens a gate, so a read can be held in progress.  Its synchronous reads throw.
        /// </summary>
        private sealed class GatedReadStream : Stream
        {
            private readonly byte[] _data;
            private readonly TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _position;
            private readonly TaskCompletionSource<bool> _readStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public GatedReadStream(byte[] data)
            {
                _data = data;
            }

            /// <summary>
            /// Gets a task that completes when a read starts waiting at the gate.
            /// </summary>
            public Task ReadStarted => _readStarted.Task;

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            /// <summary>
            /// Lets waiting and later reads through.
            /// </summary>
            public void OpenGate()
            {
                _gate.TrySetResult(true);
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _readStarted.TrySetResult(true);
                await TestTasks.WaitAsync(_gate.Task, cancellationToken);
                var length = Math.Min(count, _data.Length - _position);
                Array.Copy(_data, _position, buffer, offset, length);
                _position += length;
                return length;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
