using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;
using static DataStandardizer.File.vCard.Tests.XcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="XcardReader"/> and <see cref="XcardWriter"/>, and that they give what the synchronous members give.
    /// </summary>
    public class XcardAsyncTests
    {
        private const string MinimalVcard = "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nEND:VCARD\r\n";
        private const string MinimalXcard = "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard></vcards>";
        private const string TwoMinimalXcards =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard><vcard><fn><text>A</text></fn></vcard></vcards>";

        // A block smaller than most elements, so that each read is split across blocks.
        private const int SmallReadLength = 7;

        [Fact]
        public async Task ReadCardsAsync_AsyncOnlyStream_ReadsTheCardsWithNoSynchronousRead()
        {
            // arrange
            var stream = new AsyncOnlyStream(Encoding.UTF8.GetBytes(RfcXcardExample), false, SmallReadLength);
            using (var reader = new XcardReader(stream))
            {
                // act
                var testResult = await AsyncEnumerables.ToListAsync(reader.ReadCardsAsync());

                // assert
                Describe(testResult).Should().BeEquivalentTo(new[] { RfcXcardExampleProperties }, options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task ReadCardAsync_ForeignElement_ReadsItAsTheSynchronousReadDoes()
        {
            // arrange
            const string xml = "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn><a xmlns=\"http://e\">b</a><note><text>c</text></note></vcard></vcards>";
            var expected = Describe(ReadXcard(xml));
            var stream = new AsyncOnlyStream(Encoding.UTF8.GetBytes(xml), false, SmallReadLength);
            using (var reader = new XcardReader(stream))
            {
                // act
                var testResult = Describe(await reader.ReadCardAsync());

                // assert
                testResult.Should().BeEquivalentTo(expected[0], options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task ReadCardAsync_XmlReaderWithoutAsync_ThrowsInvalidOperation()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(MinimalXcard)))
            using (var reader = new XcardReader(xmlReader))
            {
                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task ReadCardAsync_CanceledToken_Throws()
        {
            // arrange
            using (var cancellation = new CancellationTokenSource())
            using (var reader = new XcardReader(new AsyncOnlyStream(Encoding.UTF8.GetBytes(MinimalXcard))))
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
            using (var cancellation = new CancellationTokenSource())
            using (var reader = new XcardReader(new MemoryStream(Encoding.UTF8.GetBytes(MinimalXcard))))
            {
                cancellation.Cancel();
                await ReadPastCancellationAsync(reader, cancellation.Token);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task WriteCardsAsync_AsyncOnlyStream_WritesWhatTheSynchronousWriteDoes()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var writer = new XcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardsAsync(cards));

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(WriteXcard(cards));
        }

        [Fact]
        public async Task WriteCardsAsync_AsynchronousSequence_WritesTheCards()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var cards = AsyncEnumerables.Yielding(ReadVcards(MinimalVcard + MinimalVcard));
            var writer = new XcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardsAsync(cards));

            // assert
            Canonical(Encoding.UTF8.GetString(stream.ToArray())).Should().Be(Canonical(TwoMinimalXcards));
        }

        [Fact]
        public async Task WriteCardAsync_XmlPropertyToAnAsyncOnlyStream_CopiesItAsItIs()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var card = ReadVcards("BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nXML:<a xmlns=\"http://e\">b</a>\r\nEND:VCARD\r\n")[0];
            var writer = new XcardWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteCardAsync(card).AsTask());

            // assert
            Canonical(Encoding.UTF8.GetString(stream.ToArray())).Should().Be(
                Canonical("<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn><a xmlns=\"http://e\">b</a></vcard></vcards>"));
        }

        [Fact]
        public async Task WriteCardAsync_XmlWriterWithoutAsync_ThrowsInvalidOperation()
        {
            // arrange
            var card = ReadVcards(MinimalVcard)[0];
            using (var xmlWriter = XmlWriter.Create(new StringWriter()))
            using (var writer = new XcardWriter(xmlWriter))
            {
                // act
                Func<Task> testAction = async () => await writer.WriteCardAsync(card);

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task WriteCardAsync_CanceledToken_Throws()
        {
            // arrange
            var card = ReadVcards(MinimalVcard)[0];
            using (var cancellation = new CancellationTokenSource())
            using (var writer = new XcardWriter(new MemoryStream()))
            {
                cancellation.Cancel();

                // act
                Func<Task> testAction = async () => await writer.WriteCardAsync(card, cancellation.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }

        /// <summary>
        /// Reads a card with a canceled token, and leaves the <see cref="OperationCanceledException"/> for the test to go on from.
        /// </summary>
        private static async Task ReadPastCancellationAsync(XcardReader reader, CancellationToken cancellationToken)
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
    }
}
