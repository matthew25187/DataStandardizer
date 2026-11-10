using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;
using static DataStandardizer.File.vCard.Tests.XcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="XcardWriter"/>.
    /// </summary>
    public class XcardWriterTests
    {
        private const string MinimalVcard = "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\nEND:VCARD\r\n";

        // The whole document XcardWriter writes for MinimalVcard: UTF-8 with no byte order mark, indented by two spaces, with CRLF line breaks.
        private const string MinimalXcard =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\r\n" +
            "  <vcard>\r\n" +
            "    <fn>\r\n" +
            "      <text>A</text>\r\n" +
            "    </fn>\r\n" +
            "  </vcard>\r\n" +
            "</vcards>";

        private const string TwoMinimalXcards =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard><vcard><fn><text>A</text></fn></vcard></vcards>";

        private static readonly VcardOptions InvalidValuesAllowed = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };

        public static TheoryData<string, string> Properties() => Wrap(new[]
        {
            // ref. RFC 6351 §5: the properties of a group are in a group element, whose name attribute holds the group as it is; a group that comes back is another element.
            ("CONTACT.NOTE:a\r\nCONTACT.EMAIL:b@example.com\r\n", "<group name=\"CONTACT\"><note><text>a</text></note><email><text>b@example.com</text></email></group>"),
            ("g.NOTE:a\r\nNOTE:b\r\ng.NOTE:c\r\n", "<group name=\"g\"><note><text>a</text></note></group><note><text>b</text></note><group name=\"g\"><note><text>c</text></note></group>"),

            // ref. RFC 6351 Appendix A: each value of a multi-valued property is an element, as is each component of a structured value.
            ("CATEGORIES:computers,cameras\r\n", "<categories><text>computers</text><text>cameras</text></categories>"),
            (
                "ADR:;;My Street,Left Side;Hometown;PA;18252;U.S.A.\r\n",
                "<adr><pobox/><ext/><street>My Street</street><street>Left Side</street><locality>Hometown</locality><region>PA</region><code>18252</code><country>U.S.A.</country></adr>"
            ),
            ("ORG:ABC\\, Inc.;Marketing\r\n", "<org><text>ABC, Inc.</text><text>Marketing</text></org>"),
            ("GENDER:M;alpha;beta\r\n", "<gender><sex>M</sex><identity>alpha;beta</identity></gender>"),
            ("GENDER:;it's complicated\r\n", "<gender><sex/><identity>it's complicated</identity></gender>"),

            // ref. RFC 6351 Appendix A §6.2.7: the sex is in upper case, which RFC 6350 lets be in either.
            ("GENDER:m\r\n", "<gender><sex>M</sex></gender>"),
            ("CLIENTPIDMAP:1;http://example.com/a;b\r\n", "<clientpidmap><sourceid>1</sourceid><uri>http://example.com/a;b</uri></clientpidmap>"),

            // ref. RFC 9554 §2: the components it adds, in the elements agreed for this package.
            ("N:Doe;Jane;;;;Smith;III\r\n", "<n><surname>Doe</surname><given>Jane</given><additional/><prefix/><suffix/><surname2>Smith</surname2><generation>III</generation></n>"),

            // ref. RFC 6351 Appendix A §4.3.4: a DATE-AND-OR-TIME is in a date, date-time or time element, and a time has no "T".
            ("BDAY:T1230\r\n", "<bday><time>1230</time></bday>"),
            ("BDAY:--0412T1430Z\r\n", "<bday><date-time>--0412T1430Z</date-time></bday>"),
            ("BDAY;VALUE=text:circa 1800\r\n", "<bday><text>circa 1800</text></bday>"),

            // ref. RFC 6351 Appendix A §4.3.1: value-date has no year alone, so it is written as it is in an unknown element, with no VALUE parameter (§6).
            ("BDAY:1985\r\n", "<bday><unknown>1985</unknown></bday>"),

            // ref. RFC 6351 Appendix A: xsd:boolean in lower case, and xsd:integer and xsd:float as they are.
            ("X-A;VALUE=boolean:TRUE\r\n", "<x-a><boolean>true</boolean></x-a>"),
            ("X-A;VALUE=integer:+42\r\n", "<x-a><integer>+42</integer></x-a>"),
            ("X-A;VALUE=float:1.30\r\n", "<x-a><float>1.30</float></x-a>"),
            ("TZ;VALUE=utc-offset:-0500\r\n", "<tz><utc-offset>-0500</utc-offset></tz>"),
            ("REV:20130214T123000Z\r\n", "<rev><timestamp>20130214T123000Z</timestamp></rev>"),
            ("LANG:fr-CA\r\n", "<lang><language-tag>fr-CA</language-tag></lang>"),

            // ref. RFC 6351 §6: escaping is undone, and a URI has no escaping of its commas.
            ("NOTE:a\\,b;c\\\\d\\ne\r\n", "<note><text>a,b;c\\d\ne</text></note>"),
            ("GEO:geo:46.772673\\,-71.282945\r\n", "<geo><uri>geo:46.772673,-71.282945</uri></geo>"),

            // ref. RFC 6351 §6: a property whose type isn't known, and one whose VALUE names a type that has no element, are in an unknown element.
            ("X-COFFEE-DATA:Stenophylla;Guinea\\,Africa\r\n", "<x-coffee-data><unknown>Stenophylla;Guinea\\,Africa</unknown></x-coffee-data>"),
            ("X-A;VALUE=X-MYTYPE:a\r\n", "<x-a><unknown>a</unknown></x-a>"),
            ("X-A;VALUE=text:a\\,b,c\r\n", "<x-a><text>a,b</text><text>c</text></x-a>"),

            // ref. RFC 6351 §5.1 and §6: parameters, with each value in the element of its type, unknown for one that isn't recognised, and a list parameter merged.
            ("TEL;TYPE=work;TYPE=voice:123\r\n", "<tel><parameters><type><text>work</text><text>voice</text></type></parameters><text>123</text></tel>"),
            ("NOTE;PREF=1;DERIVED=TRUE;X-P=a:b\r\n", "<note><parameters><pref><integer>1</integer></pref><derived><boolean>true</boolean></derived><x-p><unknown>a</unknown></x-p></parameters><text>b</text></note>"),
            ("NOTE;LANGUAGE=fr;CREATED=20220705T093412Z:b\r\n", "<note><parameters><language><language-tag>fr</language-tag></language><created><timestamp>20220705T093412Z</timestamp></created></parameters><text>b</text></note>"),
            ("ADR;TZ=America/Montreal;GEO=\"geo:1,2\":;;;;;;\r\n", "<adr><parameters><tz><text>America/Montreal</text></tz><geo><uri>geo:1,2</uri></geo></parameters><pobox/><ext/><street/><locality/><region/><code/><country/></adr>"),
            ("ADR;TZ=\"urn:a\":;;;;;;\r\n", "<adr><parameters><tz><uri>urn:a</uri></tz></parameters><pobox/><ext/><street/><locality/><region/><code/><country/></adr>"),

            // ref. RFC 6351 §6: "The content of "XML" properties is copied as is to XML"; one with a parameter is written as any other property, so as not to lose it.
            ("XML:<a xmlns=\"http://www.w3.org/1999/xhtml\" href=\"http://www.example.com\">My web page!</a>\r\n", "<a xmlns=\"http://www.w3.org/1999/xhtml\" href=\"http://www.example.com\">My web page!</a>"),
            ("XML;ALTID=1:<b xmlns=\"http://e\"/>\r\n", "<xml><parameters><altid><text>1</text></altid></parameters><text>&lt;b xmlns=\"http://e\"/&gt;</text></xml>"),
        });

        [Theory]
        [MemberData(nameof(Properties))]
        public void WriteCard_Property_WritesTheElementsOfRfc6351(string vcard, string expected)
        {
            // arrange
            var cards = ReadVcards(vcard);

            // act
            var testResult = Canonical(WriteXcard(cards));

            // assert
            testResult.Should().Be(Canonical(expected));
        }

        public static TheoryData<string, string> InvalidValues() => Wrap(new[]
        {
            // A value of a type with no escaping is written as it is in its own element, which reads back the same.
            ("X-A;VALUE=date:abc\r\n", "<x-a><date>abc</date></x-a>"),
            ("X-A;VALUE=integer:abc\r\n", "<x-a><integer>abc</integer></x-a>"),

            // A TEXT, URI or structured value whose escaping would change is written as it is in an unknown element.
            ("NOTE:a\\qb\r\n", "<note><unknown>a\\qb</unknown></note>"),
            ("URL:not a uri\r\n", "<url><unknown>not a uri</unknown></url>"),
            ("N:a;b;c;d;e;f\r\n", "<n><unknown>a;b;c;d;e;f</unknown></n>"),

            // A parameter value that isn't valid for its type is written in an unknown element.
            ("NOTE;PREF=abc:a\r\n", "<note><parameters><pref><unknown>abc</unknown></pref></parameters><text>a</text></note>"),
        });

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void WriteCard_InvalidValueAllowed_WritesWhatReadsBackTheSame(string vcard, string expected)
        {
            // arrange
            var cards = ReadVcards(vcard, InvalidValuesAllowed);

            // act
            var testResult = Canonical(WriteXcard(cards, InvalidValuesAllowed));

            // assert
            testResult.Should().Be(Canonical(expected));
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void ReadCard_InvalidValueWrittenAsXcard_GivesTheSameProperty(string vcard, string _)
        {
            // arrange
            var expected = Describe(ReadVcards(vcard, InvalidValuesAllowed));
            var xcard = WriteXcard(ReadVcards(vcard, InvalidValuesAllowed), InvalidValuesAllowed);

            // act
            var testResult = Describe(ReadXcard(xcard, InvalidValuesAllowed));

            // assert
            testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void WriteCard_OneCard_WritesTheIndentedDocument()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard);

            // act
            var testResult = WriteXcard(cards);

            // assert
            testResult.Should().Be(MinimalXcard);
        }

        [Fact]
        public void WriteCard_BareLineFeedOption_BreaksTheLinesWithLineFeeds()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard);
            var options = new VcardOptions { MaxLineLength = null, LineBreak = "\n" };

            // act
            var testResult = WriteXcard(cards, options);

            // assert
            testResult.Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n  <vcard>\n    <fn>\n      <text>A</text>\n    </fn>\n  </vcard>\n</vcards>");
        }

        [Fact]
        public void WriteCards_TwoCards_WritesBothInOneVcardsElement()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard + MinimalVcard);

            // act
            var testResult = Canonical(WriteXcard(cards));

            // assert
            testResult.Should().Be(Canonical(TwoMinimalXcards));
        }

        [Fact]
        public void Dispose_NoCards_WritesNothing()
        {
            // act
            var testResult = WriteXcard(new List<VcardCard>());

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void WriteCard_CardOfVcard30_WritesItAsVcard40()
        {
            // arrange
            var cards = ReadVcards("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:A\r\nN:A;;;;\r\nEND:VCARD\r\n");

            // act
            var testResult = Canonical(WriteXcard(cards));

            // assert
            testResult.Should().Be(Canonical(CardStart + "<n><surname>A</surname><given/><additional/><prefix/><suffix/></n>" + CardEnd));
        }

        [Fact]
        public void WriteCard_CardBreakingRfc6350_Throws()
        {
            // arrange: a card with no FN, which RFC 6350 §6.2.1 requires.
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("NOTE", "a"));
            var writer = new XcardWriter(new MemoryStream());

            // act
            Action testAction = () => writer.WriteCard(card);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void Dispose_AfterACardThatBrokeRfc6350_HasWrittenNothing()
        {
            // arrange: a card with no FN, which RFC 6350 §6.2.1 requires.
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("NOTE", "a"));
            var output = new MemoryStream();
            var writer = new XcardWriter(output);
            WritePastFailure(writer, card);

            // act
            writer.Dispose();

            // assert
            output.Length.Should().Be(0);
        }

        public static TheoryData<VcardContentLine> LinesXcardCannotHold() => new TheoryData<VcardContentLine>
        {
            // ref. RFC 6351 §5: the group element is the group construct, so no property can be GROUP.
            new VcardContentLine("GROUP", "a"),

            // The value element is one type, so it can't hold two VALUE parameters.
            new VcardContentLine("X-A", new[] { new VcardContentLineParameter("VALUE", "text"), new VcardContentLineParameter("VALUE", "uri") }, "a"),

            // ref. XML 1.0 §2.2: U+FFFE is not a character of XML.
            new VcardContentLine("NOTE", "a\xFFFE"),
            new VcardContentLine("NOTE", new[] { new VcardContentLineParameter("X-A", "\xFFFE") }, "a"),

            // ref. RFC 6350 §3.3: a name is letters, digits and hyphens.
            new VcardContentLine("X_A", "a"),

            // ref. RFC 6351 Appendix A §4.3.1: value-date has no year alone, so the type VALUE names would be lost.
            new VcardContentLine("X-A", new[] { new VcardContentLineParameter("VALUE", "date") }, "1985"),
        };

        [Theory]
        [MemberData(nameof(LinesXcardCannotHold))]
        public void WriteCard_LineXcardCannotHold_ThrowsWithNoDiagnosticKind(VcardContentLine line)
        {
            // arrange: the options let the card past the conformance check, whose exceptions have a diagnostic kind.
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("FN", "A"));
            card.Properties.Add(line);
            var writer = new XcardWriter(new MemoryStream(), InvalidValuesAllowed);

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
            using (var writer = new XcardWriter(new MemoryStream()))
            {
                // act
                writer.WriteCards(cards, progress);

                // assert
                (reports[reports.Count - 1], writer.CardsWritten, writer.PropertiesWritten).Should().Be((new VcardWriteProgress(expectedLines, expectedCards), expectedCards, expectedCards));
            }
        }

        [Fact]
        public void WriteCard_ToAnXmlWriterOfALargerDocument_WritesTheVcardsElementWhereItIs()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard);
            var output = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                xmlWriter.WriteStartElement("outer");
                using (var writer = new XcardWriter(xmlWriter))
                {
                    // act
                    writer.WriteCards(cards);
                }

                xmlWriter.WriteEndElement();
            }

            // assert
            output.ToString().Should().Be("<outer><vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn></vcard></vcards></outer>");
        }

        [Fact]
        public void WriteCards_NullCard_Throws()
        {
            // arrange
            var writer = new XcardWriter(new MemoryStream());

            // act
            Action testAction = () => writer.WriteCards(new VcardCard[] { null });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("cards");
        }

        public static TheoryData<Action> ConstructorsWithBadArguments() => new TheoryData<Action>
        {
            () => new XcardWriter((Stream)null),
            () => new XcardWriter(new MemoryStream(), null),
            () => new XcardWriter(new MemoryStream(new byte[0], false)),
            () => new XcardWriter(new MemoryStream(), new VcardOptions { MaxLineLength = null, LineBreak = "\r" }),
            () => new XcardWriter((TextWriter)null),
            () => new XcardWriter(new StringWriter(), new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 }),
            () => new XcardWriter((XmlWriter)null),
            () => new XcardWriter(XmlWriter.Create(new StringWriter()), new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 }),
        };

        [Theory]
        [MemberData(nameof(ConstructorsWithBadArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_BadArgument_ThrowsArgumentException(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WriteCard_Disposed_Throws()
        {
            // arrange
            var writer = new XcardWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.WriteCard(new VcardCard());

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WriteCard_AfterAFlushToTheStreamFailed_ThrowsInvalidOperation()
        {
            // arrange
            var cards = ReadVcards(MinimalVcard + MinimalVcard);
            var writer = new XcardWriter(new FailingWriteStream());
            writer.WriteCard(cards[0]);
            FlushPastIOFailure(writer);

            // act
            Action testAction = () => writer.WriteCard(cards[1]);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Makes the rows of a theory from content lines and the elements they are written as: a card of <c>FN:A</c> and the lines, and the document of a card of
        /// <see cref="XcardTestData.CardStart"/> and the elements.
        /// </summary>
        private static TheoryData<string, string> Wrap((string Lines, string Elements)[] rows)
        {
            var data = new TheoryData<string, string>();
            foreach (var (lines, elements) in rows)
            {
                data.Add("BEGIN:VCARD\r\nVERSION:4.0\r\nFN:A\r\n" + lines + "END:VCARD\r\n", CardStart + elements + CardEnd);
            }

            return data;
        }

        /// <summary>
        /// Writes a card, and leaves a <see cref="VcardFileException"/> for the test to go on from.
        /// </summary>
        private static void WritePastFailure(XcardWriter writer, VcardCard card)
        {
            try
            {
                writer.WriteCard(card);
            }
            catch (VcardFileException)
            {
                return;
            }

            Assert.Fail("The write was expected to fail.");
        }

        /// <summary>
        /// Flushes, and leaves an <see cref="IOException"/> from the stream for the test to go on from.
        /// </summary>
        private static void FlushPastIOFailure(XcardWriter writer)
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
        /// A stream whose writes fail.
        /// </summary>
        private sealed class FailingWriteStream : MemoryStream
        {
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new IOException("The write failed.");
            }
        }

        /// <summary>
        /// Reports progress on the calling thread, where <see cref="Progress{T}"/> would post it.
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
