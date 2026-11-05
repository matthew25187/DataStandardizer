using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardReaderAsyncTests
    {
        private static readonly string TwoCards = VcardCardTestData.Stream(VcardCardTestData.Card("NOTE:first"), VcardCardTestData.Card("NOTE:second"));

        [Fact]
        public async Task ReadCardAsync_Version30Card_GivesItInCanonicalFormAsAReadDoes()
        {
            // arrange
            var text = Vcard30TestData.Card("TEL;TYPE=pref:+1");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
                testResult.Telephones.Single().Pref.Should().Be(1);
            }
        }

        [Fact]
        public async Task ReadCardAsync_Version21CardWithAnAgent_GivesItInCanonicalFormAsAReadDoes()
        {
            // arrange
            var text = Vcard21TestData.Card("TEL;PREF;CELL:+1", "AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", "END:VCARD");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
                testResult.Telephones.Single().Pref.Should().Be(1);
                testResult.Agents.Single().Card.Name.Value.FamilyNames.Should().Equal("Friday");
            }
        }

        [Fact]
        public async Task WriteCardAsync_WriteVersion21_WritesWhatAWriteDoes()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card("TEL;PREF;CELL:+1", "AGENT:BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", "END:VCARD"));
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, new VcardOptions { WriteVersion = VcardVersion.V2_1 }))
            {
                // act
                await writer.WriteCardAsync(card);

                // assert
                output.ToString().Should().Be(Vcard21TestData.WrittenCardStart + "TEL;CELL;PREF:+1\r\nAGENT:BEGIN:VCARD\r\nVERSION:2.1\r\nN:Friday;Fred;;;\r\nEND:VCARD\r\n" +
                                              Vcard21TestData.WrittenCardEnd);
            }
        }

        [Fact]
        public async Task WriteCardAsync_WriteVersion30_WritesWhatAWriteDoes()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("TEL;TYPE=pref:+1"));
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, new VcardOptions { WriteVersion = VcardVersion.V3_0 }))
            {
                // act
                await writer.WriteCardAsync(card);

                // assert
                output.ToString().Should().Be(Vcard30TestData.Card("TEL;TYPE=pref:+1"));
            }
        }

        [Fact]
        public async Task ReadCardAsync_AsyncOnlyStream_ReadsTheCardWithNoSynchronousRead()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(Rfc6350Examples.ExampleWithEscapedComma);
            using (var reader = new VcardReader(new AsyncOnlyStream(data)))
            {
                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.FormattedNames[0].Value.Should().Be("Simon Perreault");
            }
        }

        [Fact]
        public async Task ReadCardsAsync_SeveralCards_GivesEachInOrder()
        {
            // arrange
            using (var reader = new VcardReader(new AsyncOnlyTextReader(TwoCards)))
            {
                // act
                var testResult = await AsyncEnumerables.ToListAsync(reader.ReadCardsAsync());

                // assert
                testResult.Select(card => card.Notes[0].Value).Should().Equal("first", "second");
            }
        }

        [Fact]
        public async Task ReadCardsAsync_SameData_GivesWhatReadCardsGives()
        {
            // arrange
            using (var syncReader = new VcardReader(new StringReader(Rfc6350Examples.ExampleWithEscapedComma)))
            using (var asyncReader = new VcardReader(new StringReader(Rfc6350Examples.ExampleWithEscapedComma)))
            {
                var expected = LineDescription.OfAll(syncReader.ReadCards().Single().Properties);

                // act
                var cards = await AsyncEnumerables.ToListAsync(asyncReader.ReadCardsAsync());

                // assert
                LineDescription.OfAll(cards.Single().Properties).Should().Equal(expected);
            }
        }

        [Fact]
        public async Task ReadCardAsync_AfterLastCard_ReturnsNull()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.Card())))
            {
                await reader.ReadCardAsync();

                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public async Task ReadCardAsync_NonconformingCard_FaultsTheTask()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.BareCard("NOTE:a"))))
            {
                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync();

                // assert
                await testAction.Should().ThrowAsync<VcardFileException>();
            }
        }

        [Fact]
        public async Task ReadCardAsync_AfterNonconformingCard_GivesTheNextCard()
        {
            // arrange
            var text = VcardCardTestData.NonconformingCardThenCard;
            using (var reader = new VcardReader(new StringReader(text)))
            {
                await VcardCardTestData.ReadPastFailureAsync(reader);

                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(VcardCardTestData.FormattedNameValue);
            }
        }

        [Fact]
        public void ReadCardAsync_MalformedLine_ReportsItThroughTheTaskNotTheCall()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.BareCard("no colon"))))
            {
                // act
                var testResult = reader.ReadCardAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
            }
        }

        [Fact]
        public async Task ReadCardAsync_CanceledToken_ThrowsAndCannotContinue()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.Card())))
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                await VcardCardTestData.ReadPastFailureAsync(reader, source.Token);

                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        [Fact]
        public async Task ReadCardAsync_CanceledToken_ThrowsOperationCanceledException()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.Card())))
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                // act
                Func<Task> testAction = async () => await reader.ReadCardAsync(source.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }

        [Fact]
        public async Task ReadCard_WhileAsyncReadInProgress_ThrowsInvalidOperationException()
        {
            // arrange
            var source = new GatedTextReader(VcardCardTestData.Card());
            using (var reader = new VcardReader(source))
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
        public void ReadCardAsync_ReaderDisposed_ThrowsFromTheCall()
        {
            // arrange
            var reader = new VcardReader(new StringReader(VcardCardTestData.Card()));
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadCardAsync();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadCardsAsync_ReaderDisposed_ThrowsFromTheCall()
        {
            // arrange
            var reader = new VcardReader(new StringReader(VcardCardTestData.Card()));
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadCardsAsync();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = new VcardReader(new StringReader(VcardCardTestData.Card()));
            await reader.DisposeAsync();

            // act
            Func<Task> testAction = async () => await reader.DisposeAsync();

            // assert
            await testAction.Should().NotThrowAsync();
        }
    }
}
