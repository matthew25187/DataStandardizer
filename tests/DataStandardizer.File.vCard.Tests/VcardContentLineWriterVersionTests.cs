using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of what <see cref="VcardContentLineWriter"/> does that File.iCal's writer doesn't: groups, and caret encoding and folding by the version of the
    /// card each line is in.
    /// </summary>
    public class VcardContentLineWriterVersionTests
    {
        private const string Group = "item1";
        private const string Property = "X-A";
        private const string Parameter = "X-P";

        // The lines of a card up to its properties, in each version.
        private static readonly VcardContentLine Begin = new VcardContentLine("BEGIN", "VCARD");
        private static readonly VcardContentLine End = new VcardContentLine("END", "VCARD");
        private static readonly VcardContentLine Version21 = new VcardContentLine("VERSION", "2.1");
        private static readonly VcardContentLine Version30 = new VcardContentLine("VERSION", "3.0");
        private static readonly VcardContentLine Version40 = new VcardContentLine("VERSION", "4.0");

        // Eighty octets, which a fold at 75 octets would break.
        private const string EightyOctets = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab";

        // ref. RFC 6350 §3.2: the line "X-A:" and EightyOctets folded at 75 octets.
        private const string EightyOctetsFolded = "X-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrs\r\n tuvwxyzab\r\n";

        // A card that uses a group on a line, the group on a card's own lines (RFC 2426 §4), a caret-encoded value, a quoted value, multi-octet text and a fold.
        private const string CardFoldedAt75Octets =
            "item1.BEGIN:VCARD\r\n" +
            "item1.VERSION:4.0\r\n" +
            "item1.TEL;TYPE=\"home,voice\";X-P=a^n^'b^^:tel:+1\r\n" +
            "item2.NOTE:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijkl\r\n" +
            " \x20AC\r\n" +
            "item1.END:VCARD\r\n";

        // What reading CardFoldedAt75Octets gives, as LineDescription describes it.
        private static readonly string[] CardFoldedAt75OctetsLines =
        {
            "1/1 item1.BEGIN:VCARD",
            "2/2 item1.VERSION:4.0",
            "3/3 item1.TEL;TYPE=home,voice;X-P=a\n\"b^:tel:+1",
            "4/4 item2.NOTE:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijkl\x20AC",
            "5/6 item1.END:VCARD"
        };

        // A vCard 3.0 card whose caret sequences are ordinary text, which is written back as it is, and a 2.1 card with a line that a fold would break.
        private const string CardsWithoutCaretEncoding =
            "BEGIN:VCARD\r\n" +
            "VERSION:3.0\r\n" +
            "X-A;X-P=a^nb^'c^^d:x\r\n" +
            "END:VCARD\r\n" +
            "BEGIN:VCARD\r\n" +
            "VERSION:2.1\r\n" +
            "X-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab\r\n" +
            "END:VCARD\r\n";

        // The vCard 2.1 line in CardsWithoutCaretEncoding is not folded, so reading it gives one physical line.
        private static readonly string[] CardsWithoutCaretEncodingLines =
        {
            "1/1 BEGIN:VCARD",
            "2/2 VERSION:3.0",
            "3/3 X-A;X-P=a^nb^'c^^d:x",
            "4/4 END:VCARD",
            "5/5 BEGIN:VCARD",
            "6/6 VERSION:2.1",
            "7/7 X-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab",
            "8/8 END:VCARD"
        };

        // ref. RFC 6350 §8: what reading the example gives once written: the same lines, with the RFC's two folds gone.
        private static readonly string[] ExampleLinesOnceWritten =
        {
            "1/1 BEGIN:VCARD",
            "2/2 VERSION:4.0",
            "3/3 FN:Simon Perreault",
            "4/4 N:Perreault;Simon;;;ing. jr,M.Sc.",
            "5/5 BDAY:--0203",
            "6/6 ANNIVERSARY:20090808T1430-0500",
            "7/7 GENDER:M",
            "8/8 LANG;PREF=1:fr",
            "9/9 LANG;PREF=2:en",
            "10/10 ORG;TYPE=work:Viagenie",
            "11/11 ADR;TYPE=work:;Suite D2-630;2875 Laurier;Quebec;QC;G1V 2M2;Canada",
            "12/12 TEL;VALUE=uri;TYPE=work,voice;PREF=1:tel:+1-418-656-9254;ext=102",
            "13/13 TEL;VALUE=uri;TYPE=work,cell,voice,video,text:tel:+1-418-262-6501",
            "14/14 EMAIL;TYPE=work:simon.perreault@viagenie.ca",
            "15/15 GEO;TYPE=work:geo:46.772673,-71.282945",
            "16/16 KEY;TYPE=work;VALUE=uri:http://www.viagenie.ca/simon.perreault/simon.asc",
            "17/17 TZ:-0500",
            "18/18 URL;TYPE=home:http://nomis80.org",
            "19/19 END:VCARD"
        };

        #region Groups

        [Fact]
        public void WriteLine_Group_IsWrittenAsThePrefix()
        {
            // arrange
            var line = new VcardContentLine(Group, "TEL", new[] { new VcardContentLineParameter("TYPE", "home") }, "123");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("item1.TEL;TYPE=home:123\r\n");
        }

        [Fact]
        public void WriteLine_NullGroup_HasNoPrefix()
        {
            // arrange
            var line = new VcardContentLine(null, "TEL", new VcardContentLineParameter[0], "123");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("TEL:123\r\n");
        }

        [Theory]
        [InlineData("")]
        [InlineData("a_b")]
        [InlineData("a.b")]
        [InlineData("a b")]
        public void WriteLine_InvalidGroup_ThrowsAndWritesNothing(string group)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var line = new VcardContentLine(group, "TEL", new VcardContentLineParameter[0], "123");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("group");
                exception.Data[VcardFileException.DataItemName.LineNumber].Should().Be(1L);
                text.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_InvalidValueOnGroupedLineInCard_ReportsWhereItIs()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                writer.WriteLines(new[] { Begin, Version40, End, Begin, Version40 });
                var line = new VcardContentLine(Group, "NOTE", new VcardContentLineParameter[0], "\n");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var data = testAction.Should().Throw<VcardFileException>().Which.Data;
                data[VcardFileException.DataItemName.Group].Should().Be(Group);
                data[VcardFileException.DataItemName.CardIndex].Should().Be(1);
                data[VcardFileException.DataItemName.LineNumber].Should().Be(6L);
                data[VcardFileException.DataItemName.PropertyName].Should().Be("NOTE");
            }
        }

        #endregion

        #region Quoting

        [Theory]
        [InlineData("a:b", "X-A;X-P=\"a:b\":x\r\n")]
        [InlineData("a;b", "X-A;X-P=\"a;b\":x\r\n")]
        [InlineData("a,b", "X-A;X-P=\"a,b\":x\r\n")]
        [InlineData("a b", "X-A;X-P=a b:x\r\n")]
        public void WriteLine_ParameterValueWithDelimiter_IsQuotedInEveryVersion(string value, string expected)
        {
            // arrange
            var line = LineWithParameter(value);

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(expected);
        }

        #endregion

        #region Caret encoding

        [Theory]
        [InlineData("a^b", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=a^^b:x\r\n")]
        [InlineData("say \"hi\"", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=say ^'hi^':x\r\n")]
        [InlineData("one\ntwo", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=one^ntwo:x\r\n")]
        [InlineData("one\r\ntwo", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=one^ntwo:x\r\n")]
        [InlineData("one\rtwo", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=one^ntwo:x\r\n")]
        [InlineData("\"Doe, J\"\n^", "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=\"^'Doe, J^'^n^^\":x\r\n")]
        public void WriteLine_ParameterValueInVersion40Card_IsCaretEncoded(string value, string expected)
        {
            // arrange
            var lines = new[] { Begin, Version40, LineWithParameter(value) };

            // act
            var testResult = WriteAll(lines);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_VersionLine_IsTheFirstLineCaretEncoded()
        {
            // arrange
            var version = new VcardContentLine("VERSION", new[] { new VcardContentLineParameter(Parameter, "a^b") }, "4.0");

            // act
            var testResult = WriteAll(Begin, version);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION;X-P=a^^b:4.0\r\n");
        }

        [Fact]
        public void WriteLine_EndLine_IsTheLastLineCaretEncoded()
        {
            // arrange
            var end = new VcardContentLine("END", new[] { new VcardContentLineParameter(Parameter, "a^b") }, "VCARD");

            // act
            var testResult = WriteAll(Begin, Version40, end, LineWithParameter("a^b"));

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:4.0\r\nEND;X-P=a^^b:VCARD\r\nX-A;X-P=a^b:x\r\n");
        }

        // Each way a card can say it is vCard 4.0: names and the card's value in any case (RFC 5234 §2.3), and a group on the lines (RFC 2426 §4).
        public static TheoryData<VcardContentLine, VcardContentLine> Version40Spellings()
        {
            return new TheoryData<VcardContentLine, VcardContentLine>
            {
                { new VcardContentLine("begin", "vcard"), new VcardContentLine("version", "4.0") },
                { new VcardContentLine("Begin", "VCard"), new VcardContentLine("Version", "4.0") },
                {
                    new VcardContentLine(Group, "BEGIN", new VcardContentLineParameter[0], "VCARD"),
                    new VcardContentLine(Group, "VERSION", new VcardContentLineParameter[0], "4.0")
                }
            };
        }

        [Theory]
        [MemberData(nameof(Version40Spellings), DisableDiscoveryEnumeration = true)]
        public void WriteLine_AnySpellingOfVersion40_CaretEncodes(VcardContentLine begin, VcardContentLine version)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLines(new[] { begin, version });
                var start = text.ToString().Length;

                // act
                writer.WriteLine(LineWithParameter("a^b"));

                // assert
                text.ToString().Substring(start).Should().Be("X-A;X-P=a^^b:x\r\n");
            }
        }

        // Each line that isn't in a vCard 4.0 card, after the lines that put it there.
        public static TheoryData<string, VcardContentLine[]> LinesOutsideVersion40()
        {
            return new TheoryData<string, VcardContentLine[]>
            {
                { "vCard 3.0", new[] { Begin, Version30 } },
                { "vCard 2.1", new[] { Begin, Version21 } },
                { "Outside a card", new VcardContentLine[0] },
                { "Before VERSION", new[] { Begin } },
                { "After END", new[] { Begin, Version40, End } },
                { "In a new card", new[] { Begin, Version40, End, Begin } },
                { "After BEGIN with no END", new[] { Begin, Version40, Begin } },
                { "VERSION outside a card", new[] { Version40 } },
                { "Another version value", new[] { Begin, new VcardContentLine("VERSION", "4.00") } }
            };
        }

        [Theory]
        [MemberData(nameof(LinesOutsideVersion40), DisableDiscoveryEnumeration = true)]
        public void WriteLine_CaretOutsideVersion40Card_IsAnOrdinaryCharacter(string scenario, VcardContentLine[] before)
        {
            // arrange
            _ = scenario;
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLines(before);
                var start = text.ToString().Length;

                // act
                writer.WriteLine(LineWithParameter("a^nb^^c"));

                // assert
                text.ToString().Substring(start).Should().Be("X-A;X-P=a^nb^^c:x\r\n");
            }
        }

        [Theory]
        [MemberData(nameof(LinesOutsideVersion40), DisableDiscoveryEnumeration = true)]
        public void WriteLine_DoubleQuoteOutsideVersion40Card_ThrowsAndWritesNothing(string scenario, VcardContentLine[] before)
        {
            // arrange
            _ = scenario;
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLines(before);
                var written = text.ToString();

                // act
                Action testAction = () => writer.WriteLine(LineWithParameter("a\"b"));

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("U+0022").And.Contain("parameter X-P").And.Contain("caret encoding");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Property);
                text.ToString().Should().Be(written);
            }
        }

        [Theory]
        [InlineData("a\nb")]
        [InlineData("a\rb")]
        [InlineData("a\r\nb")]
        public void WriteLine_LineBreakInVersion30Card_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLines(new[] { Begin, Version30 });

                // act
                Action testAction = () => writer.WriteLine(LineWithParameter(value));

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Message.Should().Contain("caret encoding");
                text.ToString().Should().Be("BEGIN:VCARD\r\nVERSION:3.0\r\n");
            }
        }

        [Theory]
        [InlineData("a\"b", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=ab:x\r\n")]
        [InlineData("say \"hi\"", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=say hi:x\r\n")]
        [InlineData("a\nb", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=a b:x\r\n")]
        [InlineData("a\r\nb", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=a b:x\r\n")]
        [InlineData("a\rb", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=a b:x\r\n")]
        [InlineData("a^b", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=a^b:x\r\n")]
        [InlineData("a\"b,c", "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=\"ab,c\":x\r\n")]
        public void WriteLine_AllowLossyVersionConversionInVersion30Card_WritesWithoutCarets(string value, string expected)
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowLossyVersionConversion = true };
            var lines = new[] { Begin, Version30, LineWithParameter(value) };

            // act
            var testResult = WriteAll(options, lines);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_AllowLossyVersionConversionInVersion40Card_StillCaretEncodes()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowLossyVersionConversion = true };
            var lines = new[] { Begin, Version40, LineWithParameter("a\"b\nc^") };

            // act
            var testResult = WriteAll(options, lines);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=a^'b^nc^^:x\r\n");
        }

        [Fact]
        public void WriteLine_AcceptCaretEncodingInAllVersionsInVersion30Card_StillThrows()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter(), new VcardOptions { MaxLineLength = null, AcceptCaretEncodingInAllVersions = true }))
            {
                writer.WriteLines(new[] { Begin, Version30 });

                // act
                Action testAction = () => writer.WriteLine(LineWithParameter("a\"b"));

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void WriteLine_VersionLineThatCannotBeWritten_DoesNotSetTheVersion()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLine(Begin);
                Record.Exception(() => writer.WriteLine(new VcardContentLine("VERSION", new[] { new VcardContentLineParameter("X_P", "a") }, "4.0")));

                // act
                writer.WriteLine(LineWithParameter("a^b"));

                // assert
                text.ToString().Should().Be("BEGIN:VCARD\r\nX-A;X-P=a^b:x\r\n");
            }
        }

        [Fact]
        public void WriteLine_VersionLineThatFailsToWrite_DoesNotSetTheVersion()
        {
            // arrange: the text writer fails on its second write, which is the VERSION line.
            using (var text = new FailingWriteTextWriter(2))
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLine(Begin);
                Record.Exception(() => writer.WriteLine(Version40));

                // act
                writer.WriteLine(LineWithParameter("a^b"));

                // assert
                text.ToString().Should().Be("BEGIN:VCARD\r\nX-A;X-P=a^b:x\r\n");
            }
        }

        #endregion

        #region Folding by version

        [Fact]
        public void WriteLine_LongLineInVersion21Card_IsNotFolded()
        {
            // arrange
            var lines = new[] { Begin, Version21, new VcardContentLine(Property, EightyOctets) };

            // act
            var testResult = WriteAll(lines);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nX-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab\r\n");
        }

        [Fact]
        public void WriteLine_LongEndLineOfVersion21Card_IsNotFolded()
        {
            // arrange
            var end = new VcardContentLine("END", new[] { new VcardContentLineParameter(Parameter, EightyOctets) }, "VCARD");

            // act
            var testResult = WriteAll(Begin, Version21, end);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nEND;X-P=abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab:VCARD\r\n");
        }

        // Each line that a writer folds, after the lines that put it there.
        public static TheoryData<string, VcardContentLine[]> LinesOutsideVersion21()
        {
            return new TheoryData<string, VcardContentLine[]>
            {
                { "vCard 4.0", new[] { Begin, Version40 } },
                { "vCard 3.0", new[] { Begin, Version30 } },
                { "Outside a card", new VcardContentLine[0] },
                { "Before VERSION", new[] { Begin } },
                { "After END of a 2.1 card", new[] { Begin, Version21, End } },
                { "In a new card after a 2.1 card", new[] { Begin, Version21, End, Begin } }
            };
        }

        [Theory]
        [MemberData(nameof(LinesOutsideVersion21), DisableDiscoveryEnumeration = true)]
        public void WriteLine_LongLineOutsideVersion21Card_IsFolded(string scenario, VcardContentLine[] before)
        {
            // arrange
            _ = scenario;
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLines(before);
                var start = text.ToString().Length;

                // act
                writer.WriteLine(new VcardContentLine(Property, EightyOctets));

                // assert
                text.ToString().Substring(start).Should().Be(EightyOctetsFolded);
            }
        }

        #endregion

        #region Round trips

        // The text of each card, and what reading it gives once written.
        public static TheoryData<string, string[]> LinesOnceWritten()
        {
            return new TheoryData<string, string[]>
            {
                { Rfc6350Examples.Example, ExampleLinesOnceWritten },
                { CardFoldedAt75Octets, CardFoldedAt75OctetsLines },
                { CardsWithoutCaretEncoding, CardsWithoutCaretEncodingLines }
            };
        }

        // The text of each card, and the text that is written.
        public static TheoryData<string, string> TextOnceWritten()
        {
            return new TheoryData<string, string>
            {
                { Rfc6350Examples.Example, Rfc6350Examples.ExampleWritten },
                { CardFoldedAt75Octets, CardFoldedAt75Octets },
                { CardsWithoutCaretEncoding, CardsWithoutCaretEncoding }
            };
        }

        [Theory]
        [MemberData(nameof(LinesOnceWritten))]
        public void WriteLines_LinesRead_ReadBackAsTheSameLines(string source, string[] expected)
        {
            // arrange
            var lines = ReadAll(CreateReader(source));

            // act
            var testResult = LineDescription.OfAll(ReadAll(CreateReader(WriteAll(lines.ToArray()))));

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(TextOnceWritten))]
        public void WriteLines_LinesRead_WritesTheExpectedText(string source, string expected)
        {
            // arrange
            var lines = ReadAll(CreateReader(source));

            // act
            var testResult = WriteAll(lines.ToArray());

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLines_CardFoldedAt75OctetsAsBytes_IsWrittenBackByteForByte()
        {
            // arrange
            var data = new UTF8Encoding(false).GetBytes(CardFoldedAt75Octets);
            var lines = ReadAll(CreateStreamReader(data));
            using (var stream = new MemoryStream())
            {
                // act
                using (var writer = new VcardContentLineWriter(stream))
                {
                    writer.WriteLines(lines);
                }

                // assert
                stream.ToArray().Should().Equal(data);
            }
        }

        #endregion

        private static VcardContentLine LineWithParameter(string value)
        {
            return new VcardContentLine(Property, new[] { new VcardContentLineParameter(Parameter, value) }, "x");
        }

        private static string WriteAll(params VcardContentLine[] lines)
        {
            return WriteAll(new VcardOptions { MaxLineLength = null }, lines);
        }

        private static string WriteAll(VcardOptions options, params VcardContentLine[] lines)
        {
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text, options))
                {
                    writer.WriteLines(lines);
                }

                return text.ToString();
            }
        }
    }
}
