using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardWriterTests
    {
        [Fact]
        public void WriteCard_Rfc6350ExampleRead_WritesItBack()
        {
            // arrange
            var card = VcardCardTestData.Read(Rfc6350Examples.Example);

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(Rfc6350Examples.ExampleWritten);
        }

        [Fact]
        public void WriteCard_CardBuiltInCode_WritesVersionFirstAndPropertiesInOrder()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Notes.Add("a, b");

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "NOTE:a\\, b", "END:VCARD"));
        }

        [Fact]
        public void WriteCard_UnknownPropertiesAndParameters_RoundTripUnchanged()
        {
            // arrange
            var text = VcardCardTestData.Card("item.X-A;X-B=\"c,d\";X-E=f:g\\h", "JSPROP;JSPTR=x:1", "NOTE;X-C=1:a");
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(text);
        }

        public static TheoryData<string, Action<VcardCard>, VcardDiagnosticKind> RefusedCardData()
        {
            return new TheoryData<string, Action<VcardCard>, VcardDiagnosticKind>
            {
                {
                    "no FN",
                    card => card.RemoveProperties(VcardPropertyNames.FormattedName),
                    VcardDiagnosticKind.MissingRequiredProperty
                },
                {
                    "two N",
                    card =>
                    {
                        card.Properties.Add(new VcardContentLine("N", "a;;;;"));
                        card.Properties.Add(new VcardContentLine("N", "b;;;;"));
                    },
                    VcardDiagnosticKind.CardinalityViolation
                },
                {
                    "a value that isn't valid",
                    card => card.Properties.Add(new VcardContentLine("URL", "not a uri")),
                    VcardDiagnosticKind.InvalidValue
                },
                {
                    "a parameter that isn't allowed",
                    card => card.Properties.Add(new VcardContentLine("GENDER", new[] { new VcardContentLineParameter("PREF", "1") }, "M")),
                    VcardDiagnosticKind.InvalidParameter
                }
            };
        }

        [Theory]
        [MemberData(nameof(RefusedCardData))]
        public void WriteCard_NonconformingCard_ThrowsAndWritesNothing(string scenario, Action<VcardCard> change, VcardDiagnosticKind expected)
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            change(card);
            var output = new StringWriter();
            using (var writer = new VcardWriter(output))
            {
                // act
                Action testAction = () => writer.WriteCard(card);

                // assert
                testAction.Should().Throw<VcardFileException>(scenario)
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
                output.ToString().Should().BeEmpty();
            }
        }

        public static TheoryData<string, Action<VcardCard>, VcardOptions, string> AcceptedCardData()
        {
            return new TheoryData<string, Action<VcardCard>, VcardOptions, string>
            {
                {
                    "no FN",
                    card => card.RemoveProperties(VcardPropertyNames.FormattedName),
                    new VcardOptions { AllowMissingRequiredProperties = true },
                    VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "END:VCARD")
                },
                {
                    "two N",
                    card =>
                    {
                        card.Properties.Add(new VcardContentLine("N", "a;;;;"));
                        card.Properties.Add(new VcardContentLine("N", "b;;;;"));
                    },
                    new VcardOptions { AllowCardinalityViolations = true },
                    VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "N:a;;;;", "N:b;;;;", "END:VCARD")
                },
                {
                    "a value that isn't valid",
                    card => card.Properties.Add(new VcardContentLine("URL", "not a uri")),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "URL:not a uri", "END:VCARD")
                },
                {
                    "a parameter that isn't allowed",
                    card => card.Properties.Add(new VcardContentLine("GENDER", new[] { new VcardContentLineParameter("PREF", "1") }, "M")),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "GENDER;PREF=1:M", "END:VCARD")
                }
            };
        }

        [Theory]
        [MemberData(nameof(AcceptedCardData))]
        public void WriteCard_NonconformingCardAndOptionSet_WritesIt(string scenario, Action<VcardCard> change, VcardOptions options, string expected)
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            change(card);
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options))
            {
                // act
                writer.WriteCard(card);

                // assert
                output.ToString().Should().Be(expected, scenario);
            }
        }

        [Fact]
        public void WriteCard_LegacySyntaxAndAcceptLegacySyntax_StillThrows()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE:a"), new VcardOptions { AcceptLegacySyntax = true });
            using (var writer = new VcardWriter(new StringWriter(), new VcardOptions { AcceptLegacySyntax = true }))
            {
                // act
                Action testAction = () => writer.WriteCard(card);

                // assert
                testAction.Should().Throw<VcardFileException>()
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.LegacySyntax);
            }
        }

        public static TheoryData<string> DelimiterNameData()
        {
            return new TheoryData<string> { "BEGIN", "END", "VERSION" };
        }

        [Theory]
        [MemberData(nameof(DelimiterNameData))]
        public void WriteCard_DelimiterInProperties_ThrowsWhateverTheOptions(string name)
        {
            // arrange
            var options = new VcardOptions { AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true };
            var card = VcardCardTestData.NewCard();
            card.Properties.Add(new VcardContentLine(name, "VCARD"));
            using (var writer = new VcardWriter(new StringWriter(), options))
            {
                // act
                Action testAction = () => writer.WriteCard(card);

                // assert
                testAction.Should().Throw<VcardFileException>()
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        [Fact]
        public void WriteCards_Several_ReportsProgressAfterEach()
        {
            // arrange
            var cards = new[] { VcardCardTestData.NewCard(), VcardCardTestData.NewCard() };
            var progress = new CollectingProgress();
            using (var writer = new VcardWriter(new StringWriter()))
            {
                // act
                writer.WriteCards(cards, progress);

                // assert
                progress.Reports.Should().Equal(new VcardWriteProgress(4, 1), new VcardWriteProgress(8, 2));
            }
        }

        [Fact]
        public void WriteCards_NullCard_ThrowsAfterWritingTheOnesBefore()
        {
            // arrange
            var cards = new[] { VcardCardTestData.NewCard(), null };
            var output = new StringWriter();
            using (var writer = new VcardWriter(output))
            {
                // act
                Action testAction = () => writer.WriteCards(cards);

                // assert
                testAction.Should().Throw<ArgumentException>();
                writer.CardsWritten.Should().Be(1);
            }
        }

        [Fact]
        public void LinesWritten_AfterACard_CountsItsLines()
        {
            // arrange
            const long MinimalCardLineCount = 4;
            using (var writer = new VcardWriter(new StringWriter()))
            {
                writer.WriteCard(VcardCardTestData.NewCard());

                // act
                var testResult = writer.LinesWritten;

                // assert
                testResult.Should().Be(MinimalCardLineCount);
            }
        }

        [Fact]
        public void WriteCard_ToStream_WritesUtf8WithoutPreamble()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var output = new MemoryStream();
            using (var writer = new VcardWriter(output))
            {
                // act
                writer.WriteCard(card);
            }

            // assert
            Encoding.UTF8.GetString(output.ToArray()).Should().Be(VcardCardTestData.Card());
        }

        [Fact]
        public void WriteCard_BareLineFeedOption_UsesIt()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var options = new VcardOptions { LineBreak = "\n" };

            // act
            var testResult = VcardCardTestData.Write(card, options);

            // assert
            testResult.Should().Be("BEGIN:VCARD\nVERSION:4.0\nFN:Jane Doe\nEND:VCARD\n");
        }

        public static TheoryData<Func<VcardWriter, object>> DisposedMemberData()
        {
            return new TheoryData<Func<VcardWriter, object>>
            {
                writer => writer.CardsWritten,
                writer => writer.LinesWritten,
                writer =>
                {
                    writer.Flush();
                    return null;
                },
                writer =>
                {
                    writer.WriteCard(new VcardCard());
                    return null;
                },
                writer =>
                {
                    writer.WriteCards(new VcardCard[0]);
                    return null;
                }
            };
        }

        [Theory]
        [MemberData(nameof(DisposedMemberData))]
        public void Member_WriterDisposed_ThrowsObjectDisposedException(Func<VcardWriter, object> member)
        {
            // arrange
            var writer = new VcardWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ToString_Progress_GivesBothCounts()
        {
            // arrange
            var progress = new VcardWriteProgress(4, 1);

            // act
            var testResult = progress.ToString();

            // assert
            testResult.Should().Be("LinesWritten = 4, CardsWritten = 1");
        }

        [Fact]
        public void WriteCard_NullCard_ThrowsArgumentNullException()
        {
            // arrange
            using (var writer = new VcardWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCard(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public async Task WriteCardAsync_AsyncOnlyStream_WritesTheCardWithNoSynchronousWrite()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var output = new AsyncOnlyWriteStream();
            var writer = new VcardWriter(output);

            // act
            await writer.WriteCardAsync(card);
            await writer.DisposeAsync();

            // assert
            Encoding.UTF8.GetString(output.ToArray()).Should().Be(VcardCardTestData.Card());
        }

        [Fact]
        public async Task WriteCardsAsync_Enumerable_ReportsProgressAfterEach()
        {
            // arrange
            var cards = new List<VcardCard> { VcardCardTestData.NewCard(), VcardCardTestData.NewCard() };
            var progress = new CollectingProgress();
            using (var writer = new VcardWriter(new StringWriter()))
            {
                // act
                await writer.WriteCardsAsync(cards, progress);

                // assert
                progress.Reports.Should().Equal(new VcardWriteProgress(4, 1), new VcardWriteProgress(8, 2));
            }
        }

        [Fact]
        public async Task WriteCardsAsync_AsyncEnumerable_WritesEach()
        {
            // arrange
            var cards = AsyncEnumerables.Yielding(new[] { VcardCardTestData.NewCard(), VcardCardTestData.NewCard() });
            var output = new StringWriter();
            using (var writer = new VcardWriter(output))
            {
                // act
                await writer.WriteCardsAsync(cards);

                // assert
                output.ToString().Should().Be(VcardCardTestData.TwoMinimalCards);
            }
        }

        [Fact]
        public async Task WriteCardsAsync_NoCardsAndCanceledToken_ThrowsOperationCanceledException()
        {
            // arrange
            using (var writer = new VcardWriter(new StringWriter()))
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                // act
                Func<Task> testAction = () => writer.WriteCardsAsync(new List<VcardCard>(), null, source.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }

        [Fact]
        public async Task WriteCardAsync_NonconformingCard_FaultsTheTask()
        {
            // arrange
            using (var writer = new VcardWriter(new StringWriter()))
            {
                // act
                Func<Task> testAction = async () => await writer.WriteCardAsync(new VcardCard());

                // assert
                await testAction.Should().ThrowAsync<VcardFileException>();
            }
        }

        [Fact]
        public async Task WriteCardAsync_CanceledToken_ThrowsAndCannotContinue()
        {
            // arrange
            using (var writer = new VcardWriter(new StringWriter()))
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                await VcardCardTestData.WritePastCancellationAsync(writer, VcardCardTestData.NewCard(), source.Token);

                // act
                Action testAction = () => writer.WriteCard(VcardCardTestData.NewCard());

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteCardAsync_NullCard_ThrowsFromTheCall()
        {
            // arrange
            using (var writer = new VcardWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCardAsync(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        private sealed class CollectingProgress : IProgress<VcardWriteProgress>
        {
            public List<VcardWriteProgress> Reports { get; } = new List<VcardWriteProgress>();

            public void Report(VcardWriteProgress value)
            {
                Reports.Add(value);
            }
        }
    }
}
