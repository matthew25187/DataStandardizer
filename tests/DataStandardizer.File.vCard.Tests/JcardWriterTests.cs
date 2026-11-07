using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="JcardWriter"/>.
    /// </summary>
    public class JcardWriterTests
    {
        private const string MinimalVcard = "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nEND:VCARD\r\n";
        private const string MinimalJcard = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]]";
        private const string WriteFailedMessage = "*write to the stream or a flush of it failed*";

        public static TheoryData<string, string> Properties() => new TheoryData<string, string>
        {
            // ref. RFC 7095 §3.3.1.2: the group is the group parameter, in lower case.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nCONTACT.FN:Mr. John Q. Public\\, Esq.\r\nEND:VCARD\r\n", "[\"fn\",{\"group\":\"contact\"},\"text\",\"Mr. John Q. Public, Esq.\"]" },

            // ref. RFC 7095 §3.3: each value of a multi-valued property is an element.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nCATEGORIES:computers,cameras\r\nEND:VCARD\r\n", "[\"categories\",{},\"text\",\"computers\",\"cameras\"]" },

            // ref. RFC 7095 §3.3.1.3: a structured value, with an array for a component of several values, and a single string for a value of one component.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nADR:;;My Street,Left Side,Second Shack;Hometown;PA;18252;U.S.A.\r\nEND:VCARD\r\n",
                "[\"adr\",{},\"text\",[\"\",\"\",[\"My Street\",\"Left Side\",\"Second Shack\"],\"Hometown\",\"PA\",\"18252\",\"U.S.A.\"]]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nGENDER:F;grrrl\r\nEND:VCARD\r\n", "[\"gender\",{},\"text\",[\"F\",\"grrrl\"]]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nGENDER:M\r\nEND:VCARD\r\n", "[\"gender\",{},\"text\",\"M\"]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nORG:ABC\\, Inc.;North American Division;Marketing\r\nEND:VCARD\r\n",
                "[\"org\",{},\"text\",[\"ABC, Inc.\",\"North American Division\",\"Marketing\"]]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nCLIENTPIDMAP:1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b\r\nEND:VCARD\r\n",
                "[\"clientpidmap\",{},\"text\",[\"1\",\"urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b\"]]" },

            // ref. RFC 6350 §6.2.7 and §6.7.7: the identity of GENDER and the URI of CLIENTPIDMAP are the rest of the value, which may hold a semicolon.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nGENDER:M;alpha;beta\r\nEND:VCARD\r\n", "[\"gender\",{},\"text\",[\"M\",\"alpha;beta\"]]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nCLIENTPIDMAP:1;http://example.com/a;b\r\nEND:VCARD\r\n", "[\"clientpidmap\",{},\"text\",[\"1\",\"http://example.com/a;b\"]]" },

            // ref. RFC 9554 §2.2 and §2.3: N has seven components with the new ones, and ADR eighteen; without them, five and seven, as in the text form.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nN:Doe;Jane;;;;Smith;III\r\nEND:VCARD\r\n", "[\"n\",{},\"text\",[\"Doe\",\"Jane\",\"\",\"\",\"\",\"Smith\",\"III\"]]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nN:Doe;Jane;;;\r\nEND:VCARD\r\n", "[\"n\",{},\"text\",[\"Doe\",\"Jane\",\"\",\"\",\"\"]]" },

            // ref. RFC 7095 §3.4.2: the values of a list parameter are an array, whether they were one quoted value, a list or a repeated parameter; one value is a string.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nTEL;TYPE=\"work,voice\":123\r\nEND:VCARD\r\n", "[\"tel\",{\"type\":[\"work\",\"voice\"]},\"text\",\"123\"]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nTEL;TYPE=work;TYPE=voice:123\r\nEND:VCARD\r\n", "[\"tel\",{\"type\":[\"work\",\"voice\"]},\"text\",\"123\"]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nTEL;TYPE=work:123\r\nEND:VCARD\r\n", "[\"tel\",{\"type\":\"work\"},\"text\",\"123\"]" },

            // ref. RFC 7095 §3.4: a parameter that isn't a list keeps a comma, and the case of its value.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nADR;LABEL=\"a,B\":;;;;;;\r\nEND:VCARD\r\n", "[\"adr\",{\"label\":\"a,B\"},\"text\",[\"\",\"\",\"\",\"\",\"\",\"\",\"\"]]" },

            // ref. RFC 7095 §3.4.1: the VALUE parameter is the type identifier.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nTZ;VALUE=utc-offset:-0500\r\nEND:VCARD\r\n", "[\"tz\",{},\"utc-offset\",\"-05:00\"]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-TIME-LOCAL;VALUE=time:123000\r\nEND:VCARD\r\n", "[\"x-time-local\",{},\"time\",\"12:30:00\"]" },

            // ref. RFC 7095 §5.1: a property whose type isn't known is a string of its text, with the type unknown; a VALUE parameter that names another type is the type identifier.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-COFFEE-DATA:Stenophylla;Guinea\\,Africa\r\nEND:VCARD\r\n", "[\"x-coffee-data\",{},\"unknown\",\"Stenophylla;Guinea\\\\,Africa\"]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-A;VALUE=X-MYTYPE:a\\b\r\nEND:VCARD\r\n", "[\"x-a\",{},\"x-mytype\",\"a\\\\b\"]" },

            // ref. RFC 7095 §3.5.8 to §3.5.10: JSON literals and numbers.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-NON-SMOKING;VALUE=boolean:TRUE\r\nEND:VCARD\r\n", "[\"x-non-smoking\",{},\"boolean\",true]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-KARMA-POINTS;VALUE=integer:+42\r\nEND:VCARD\r\n", "[\"x-karma-points\",{},\"integer\",42]" },
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nX-GRADE;VALUE=float:1.30\r\nEND:VCARD\r\n", "[\"x-grade\",{},\"float\",1.30]" },

            // ref. RFC 7095 §3.5.2: a URI has no escaping of vCard.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nGEO:geo:46.772673\\,-71.282945\r\nEND:VCARD\r\n", "[\"geo\",{},\"uri\",\"geo:46.772673,-71.282945\"]" },

            // ref. RFC 7095 §3.1: TEXT has its escaping removed.
            { "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nNOTE:a\\,b;c\\\\d\\ne\r\nEND:VCARD\r\n", "[\"note\",{},\"text\",\"a,b;c\\\\d\\ne\"]" },
        };

        [Theory]
        [MemberData(nameof(Properties))]
        public void WriteCard_Property_WritesThePropertyArrayOfRfc7095(string vcard, string expected)
        {
            // arrange
            var cards = ReadVcards(vcard);

            // act
            var testResult = WriteJcard(cards);

            // assert
            testResult.Should().EndWith("," + expected + "]]");
        }

        [Fact]
        public void WriteCard_OneCard_WritesItsJcardObject()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard);

            // act
            var testResult = WriteJcard(cards);

            // assert
            testResult.Should().Be(MinimalJcard);
        }

        [Fact]
        public void WriteCards_TwoCards_WritesAnArrayOfJcardObjects()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard + MinimalVcard);

            // act
            var testResult = WriteJcard(cards);

            // assert
            testResult.Should().Be("[" + MinimalJcard + "," + MinimalJcard + "]");
        }

        [Fact]
        public void Dispose_NoCards_WritesNothing()
        {
            // act
            var testResult = WriteJcard(new List<VcardCard>());

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void WriteCard_CardOfVcard30_WritesVersion40()
        {
            // arrange
            var cards = ReadVcards("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:A\r\nN:A;;;;\r\nEND:VCARD\r\n");

            // act
            var testResult = WriteJcard(cards);

            // assert
            testResult.Should().Be("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"n\",{},\"text\",[\"A\",\"\",\"\",\"\",\"\"]]]]");
        }

        [Fact]
        public void WriteCard_CardBreakingRfc6350WithDefaultOptions_Throws()
        {
            // arrange
            var writer = new JcardWriter(new MemoryStream());

            // act
            Action testAction = () => writer.WriteCard(new VcardCard());

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void WriteCard_AfterACardBreakingRfc6350_WritesTheNextCardAsTheOnlyOne()
        {
            // arrange
            var output = new MemoryStream();
            var card = ReadVcards(MinimalVcard)[0];
            using (var writer = new JcardWriter(output))
            {
                WritePastFailure(writer, new VcardCard());

                // act
                writer.WriteCard(card);
            }

            // assert
            Encoding.UTF8.GetString(output.ToArray()).Should().Be(MinimalJcard);
        }

        [Fact]
        public void WriteCard_CardBreakingRfc6350WithTheOptionThatAcceptsIt_IsWritten()
        {
            // arrange
            var options = new VcardOptions { AllowMissingRequiredProperties = true };

            // act
            var testResult = WriteJcard(new VcardCard(), options);

            // assert
            testResult.Should().Be("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"]]]");
        }

        [Fact]
        public void WriteCard_InvalidDateAllowed_WritesItAsAStringOfItsType()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var cards = ReadVcards("BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nBDAY:circa 1800\r\nEND:VCARD\r\n", options);

            // act
            var testResult = WriteJcard(cards, options);

            // assert
            testResult.Should().EndWith(",[\"bday\",{},\"date-and-or-time\",\"circa 1800\"]]]");
        }

        [Fact]
        public void WriteCard_TextWithAnEscapeThatIsNotValidAllowed_WritesItAsUnknown()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("FN", "A"));
            card.Properties.Add(new VcardContentLine("NOTE", "a\\qb"));

            // act
            var testResult = WriteJcard(card, options);

            // assert
            testResult.Should().EndWith(",[\"note\",{},\"unknown\",\"a\\\\qb\"]]]");
        }

        public static TheoryData<VcardContentLine> LinesJcardCannotHold() => new TheoryData<VcardContentLine>
        {
            // ref. RFC 7095 §3.4: a parameter is a key of an object, which can't be repeated, unless it is a list.
            new VcardContentLine("NOTE", new[] { new VcardContentLineParameter("LANGUAGE", "en"), new VcardContentLineParameter("LANGUAGE", "fr") }, "a"),

            // ref. RFC 7095 §3.4.1: the type identifier is one value, so it can't hold two VALUE parameters.
            new VcardContentLine("X-A", new[] { new VcardContentLineParameter("VALUE", "text"), new VcardContentLineParameter("VALUE", "uri") }, "a"),

            // ref. RFC 7095 §7.1: GROUP "MUST NOT be used in plain vCard".
            new VcardContentLine("NOTE", new[] { new VcardContentLineParameter("GROUP", "a") }, "a"),

            // A lone surrogate can't be read back from JSON.
            new VcardContentLine("NOTE", new[] { new VcardContentLineParameter("X-A", "\xD800") }, "a"),

            // ref. RFC 6350 §3.3: a name is letters, digits and hyphens.
            new VcardContentLine("X_A", "a"),
        };

        [Theory]
        [MemberData(nameof(LinesJcardCannotHold))]
        public void WriteCard_LineJcardCannotHold_ThrowsWithNoDiagnosticKind(VcardContentLine line)
        {
            // arrange: the options let the card past the conformance check, whose exceptions have a diagnostic kind.
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("FN", "A"));
            card.Properties.Add(line);
            var writer = new JcardWriter(new MemoryStream(), options);

            // act
            Action testAction = () => writer.WriteCard(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data.Contains(VcardFileException.DataItemName.DiagnosticKind).Should().BeFalse();
        }

        [Fact]
        public void WriteCards_Progress_CountsTheLinesOfTheVcfForm()
        {
            // arrange: each card is BEGIN, VERSION, FN and END in .vcf form.
            const long expectedLines = 8;
            const long expectedCards = 2;
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var reports = new List<VcardWriteProgress>();
            var progress = new SynchronousProgress<VcardWriteProgress>(reports.Add);
            using (var writer = new JcardWriter(new MemoryStream()))
            {
                // act
                writer.WriteCards(cards, progress);

                // assert
                reports[reports.Count - 1].Should().Be(new VcardWriteProgress(expectedLines, expectedCards));
                writer.PropertiesWritten.Should().Be(expectedCards);
            }
        }

        [Fact]
        public void WriteCards_NullCard_Throws()
        {
            // arrange
            var writer = new JcardWriter(new MemoryStream());

            // act
            Action testAction = () => writer.WriteCards(new VcardCard[] { null });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("cards");
        }

        [Fact]
        public void Constructor_EncodingThatIsNotUtf8_Throws()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.Unicode };

            // act
            Action testAction = () => new JcardWriter(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_StreamThatCannotBeWritten_Throws()
        {
            // arrange
            var stream = new MemoryStream(new byte[0], false);

            // act
            Action testAction = () => new JcardWriter(stream);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("jcardStream");
        }

        [Fact]
        public void WriteCard_Disposed_Throws()
        {
            // arrange
            var writer = new JcardWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.WriteCard(new VcardCard());

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WriteCard_AfterAWriteToTheStreamFailed_ThrowsInvalidOperation()
        {
            // arrange: the first card is held, and the second writes it to the stream, which fails.
            var cards = ReadVcards(MinimalVcard + MinimalVcard + MinimalVcard);
            var writer = new JcardWriter(new FailingWriteStream());
            writer.WriteCard(cards[0]);
            WritePastIOFailure(writer, cards[1]);

            // act
            Action testAction = () => writer.WriteCard(cards[2]);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Dispose_AfterAWriteToTheStreamFailed_WritesNothingMore()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var stream = new FailingWriteStream();
            var writer = new JcardWriter(stream);
            writer.WriteCard(cards[0]);
            WritePastIOFailure(writer, cards[1]);

            // act
            writer.Dispose();

            // assert
            stream.WriteCount.Should().Be(1);
        }

        [Fact]
        public void WriteCard_AfterAFlushOfTheStreamFailed_ThrowsInvalidOperation()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var writer = new JcardWriter(new FailingFlushStream());
            writer.WriteCard(cards[0]);
            FlushPastIOFailure(writer);

            // act
            Action testAction = () => writer.WriteCard(cards[1]);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(WriteFailedMessage);
        }

        [Fact]
        public void Dispose_AfterAFlushOfTheStreamFailed_DoesNotWriteTheHeldCard()
        {
            // arrange
            var stream = new FailingFlushStream();
            var writer = new JcardWriter(stream);
            writer.WriteCard(ReadVcards(MinimalVcard)[0]);
            FlushPastIOFailure(writer);

            // act
            writer.Dispose();

            // assert
            stream.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void Flush_OneCard_HoldsItUntilDisposed()
        {
            // arrange
            var output = new MemoryStream();
            var writer = new JcardWriter(output);
            writer.WriteCards(ReadVcards(MinimalVcard));

            // act
            writer.Flush();

            // assert
            output.ToArray().Should().BeEmpty();
        }

        /// <summary>
        /// Writes a card, and leaves the <see cref="IOException"/> of the stream for the test to go on from.
        /// </summary>
        private static void WritePastIOFailure(JcardWriter writer, VcardCard card)
        {
            try
            {
                writer.WriteCard(card);
            }
            catch (IOException)
            {
                return;
            }

            Assert.Fail("The write was expected to fail.");
        }

        /// <summary>
        /// Flushes, and leaves the <see cref="IOException"/> of the stream for the test to go on from.
        /// </summary>
        private static void FlushPastIOFailure(JcardWriter writer)
        {
            try
            {
                writer.Flush();
            }
            catch (IOException)
            {
                return;
            }

            Assert.Fail("The flush was expected to fail.");
        }

        /// <summary>
        /// A stream whose every flush fails; its writes succeed.
        /// </summary>
        private sealed class FailingFlushStream : MemoryStream
        {
            public override void Flush()
            {
                throw new IOException("The test stream fails every flush.");
            }
        }

        /// <summary>
        /// A stream whose every write fails, and counts the writes it was asked for.
        /// </summary>
        private sealed class FailingWriteStream : MemoryStream
        {
            /// <summary>
            /// Gets the number of writes the stream was asked for.
            /// </summary>
            public int WriteCount { get; private set; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteCount++;
                throw new IOException("The test stream fails every write.");
            }
        }

        /// <summary>
        /// An <see cref="IProgress{T}"/> that reports on the calling thread, so that a test sees every report before the call returns.
        /// </summary>
        private sealed class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> _report;

            public SynchronousProgress(Action<T> report)
            {
                _report = report;
            }

            public void Report(T value)
            {
                _report(value);
            }
        }
    }
}
