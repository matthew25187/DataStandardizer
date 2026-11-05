using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of how <see cref="VcardContentLineWriter"/> writes the lexical forms of vCard 2.1.
    /// </summary>
    public class VcardLegacySyntaxWriterTests
    {
        private const string EncodingParameter = "ENCODING";
        private const string QuotedPrintable = "QUOTED-PRINTABLE";
        private const string Base64 = "BASE64";

        private const string CardStart = "BEGIN:VCARD\r\nVERSION:2.1\r\n";

        private static readonly VcardContentLine Begin = new VcardContentLine("BEGIN", "VCARD");
        private static readonly VcardContentLine Version21 = new VcardContentLine("VERSION", "2.1");
        private static readonly VcardContentLine Version30 = new VcardContentLine("VERSION", "3.0");

        private const string Name = "A";

        // 28 characters.
        private const string HeaderLine = "A;ENCODING=QUOTED-PRINTABLE:";

        // 47 characters, which with HeaderLine make 75, the longest line (vCard 2.1 §2.1.3).
        private const string FortySevenCharacters = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstu";

        // ref. RFC 4648 §4: "Hello".
        private const string HelloBase64 = "SGVsbG8=";

        // Base64 of 96 zero octets: 128 characters, or two lines of 64 (vCard 2.1 §2.2.3).
        private const string SixtyFourAs = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // SixtyFourAs is 48 zero octets; then "Hello".
        private static readonly byte[] FortyEightZerosThenHello =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x48, 0x65, 0x6C, 0x6C, 0x6F
        };

        // 80 characters, more than a line can hold.
        private const string LongName = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // 46 + 74 + 28 characters: what fits on the line after HeaderLine and two soft line breaks, and the rest.
        private const string FortySixAs = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string SeventyFourAs = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string TwentyEightAs = "aaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        // 200 characters.
        private const string TwoHundredBs =
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" +
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        private static VcardContentLine QuotedPrintableLine(string value)
        {
            return new VcardContentLine(Name, new[] { new VcardContentLineParameter(EncodingParameter, QuotedPrintable) }, value);
        }

        private static VcardContentLine Base64Line(string value)
        {
            return new VcardContentLine("KEY", new[] { new VcardContentLineParameter(EncodingParameter, Base64) }, value);
        }

        private static string Write(params VcardContentLine[] lines)
        {
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text))
                {
                    writer.WriteLines(lines);
                }

                return text.ToString();
            }
        }

        // Each value, and the whole of what is written for it.  A line holds 74 characters and a '=', or 75 if it is the last.
        public static TheoryData<string, string> SoftBreakData()
        {
            return new TheoryData<string, string>
            {
                { "short", CardStart + HeaderLine + "short\r\n" },
                { string.Empty, CardStart + HeaderLine + "\r\n" },
                { FortySevenCharacters, CardStart + HeaderLine + FortySevenCharacters + "\r\n" },
                { FortySevenCharacters + "v", CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrst=\r\nuv\r\n" },

                // =0D would reach the 75th character, which leaves no room for a '=' after it, so it moves to the next line whole.
                { "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr=0Dz", CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr=\r\n=0Dz\r\n" },

                // =0D reaches the 74th character, so it stays.
                { "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopq=0Dzz", CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopq=0D=\r\nzz\r\n" },

                // =0D ends the value at the 75th character, so it stays, with no soft line break.
                { "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr=0D", CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr=0D\r\n" },

                // A space at the end of a line would be taken as padding, but the '=' protects it.  Here it starts the next line, where it is data.
                { "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrst uvw", CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrst=\r\n uvw\r\n" }
            };
        }

        [Theory]
        [MemberData(nameof(SoftBreakData))]
        public void WriteLines_QuotedPrintableValue_BreaksItWithSoftLineBreaks(string value, string expected)
        {
            // arrange
            var line = QuotedPrintableLine(value);

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLines_QuotedPrintableValueOfManyLines_BreaksEveryLine()
        {
            // arrange
            var line = QuotedPrintableLine(FortySixAs + SeventyFourAs + TwentyEightAs);

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + HeaderLine + FortySixAs + "=\r\n" + SeventyFourAs + "=\r\n" + TwentyEightAs + "\r\n");
        }

        [Fact]
        public void WriteLines_NameLongerThanLineLimit_BreaksBeforeTheValue()
        {
            // arrange
            var line = new VcardContentLine(LongName, new[] { new VcardContentLineParameter(EncodingParameter, QuotedPrintable) }, "b");

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + LongName + ";ENCODING=QUOTED-PRINTABLE:=\r\nb\r\n");
        }

        [Theory]
        [InlineData("a=")]
        [InlineData("a=G0")]
        [InlineData("a=3d")]
        [InlineData("caf\x00E9")]
        public void WriteLine_ValueThatIsNotQuotedPrintable_Throws(string value)
        {
            // arrange
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text))
                {
                    writer.WriteLines(new[] { Begin, Version21 });
                    var line = QuotedPrintableLine(value);

                    // act
                    Action testAction = () => writer.WriteLine(line);

                    // assert
                    testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid QUOTED-PRINTABLE*");
                }
            }
        }

        [Fact]
        public void WriteLine_ValueThatIsNotQuotedPrintableInVersion21Card_ReportsTheLine()
        {
            // arrange
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text))
                {
                    writer.WriteLines(new[] { Begin, Version21 });
                    var line = QuotedPrintableLine("a=");

                    // act
                    Action testAction = () => writer.WriteLine(line);

                    // assert
                    var testException = testAction.Should().Throw<VcardFileException>().Which;
                    testException.Data[VcardFileException.DataItemName.LineNumber].Should().Be(3L);
                    testException.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Name);
                    testException.Data[VcardFileException.DataItemName.CardIndex].Should().Be(0);
                }
            }
        }

        [Theory]
        [InlineData("short")]
        [InlineData(FortySevenCharacters + "v")]
        [InlineData("=0D=0A=C3=A9 tail")]
        public void WriteLines_QuotedPrintableValue_ReadsBackAsTheSameValue(string value)
        {
            // arrange
            var written = Write(Begin, Version21, QuotedPrintableLine(value));
            using (var reader = CreateReader(written))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be(value);
            }
        }

        [Fact]
        public void WriteLines_LongQuotedPrintableValue_DecodesToTheOriginalText()
        {
            // arrange
            var written = Write(Begin, Version21, new VcardContentLine(Name, new[]
            {
                new VcardContentLineParameter(EncodingParameter, QuotedPrintable),
                new VcardContentLineParameter("CHARSET", "UTF-8")
            }, "=C3=A9" + TwoHundredBs));
            using (var reader = CreateReader(written))
            {
                // act
                var testResult = ReadLast(reader).GetDecodedText();

                // assert
                testResult.Should().Be("\x00E9" + TwoHundredBs);
            }
        }

        [Fact]
        public void WriteLines_QuotedPrintableOutsideVersion21Card_FoldsIt()
        {
            // arrange
            var line = QuotedPrintableLine(FortySevenCharacters + "v");

            // act
            var testResult = Write(Begin, Version30, line);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:3.0\r\n" + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstu\r\n v\r\n");
        }

        [Theory]
        [InlineData("NOT QP=", QuotedPrintable, "BEGIN:VCARD\r\nVERSION:3.0\r\nA;ENCODING=QUOTED-PRINTABLE:NOT QP=\r\n")]
        [InlineData("NOT BASE64!", Base64, "BEGIN:VCARD\r\nVERSION:3.0\r\nA;ENCODING=BASE64:NOT BASE64!\r\n")]
        public void WriteLine_InvalidValueOutsideVersion21Card_IsWrittenAsAnyOtherLine(string value, string encoding, string expected)
        {
            // arrange
            var line = new VcardContentLine(Name, new[] { new VcardContentLineParameter(EncodingParameter, encoding) }, value);

            // act
            var testResult = Write(Begin, Version30, line);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLines_Base64Value_WritesItOnIndentedLinesWithAnEmptyLine()
        {
            // arrange
            var line = Base64Line(HelloBase64);

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + "KEY;ENCODING=BASE64:\r\n    SGVsbG8=\r\n\r\n");
        }

        [Fact]
        public void WriteLines_EmptyBase64Value_WritesItsEmptyLine()
        {
            // arrange
            var line = Base64Line(string.Empty);

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + "KEY;ENCODING=BASE64:\r\n\r\n");
        }

        [Fact]
        public void WriteLines_LongBase64Value_BreaksItAt64Characters()
        {
            // arrange
            var line = Base64Line(SixtyFourAs + SixtyFourAs);

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + "KEY;ENCODING=BASE64:\r\n    " + SixtyFourAs + "\r\n    " + SixtyFourAs + "\r\n\r\n");
        }

        [Fact]
        public void WriteLines_Base64ValueWithWhitespaceFromAReader_LaysItOutAgain()
        {
            // arrange
            var line = Base64Line("    SGVs    bG8=");

            // act
            var testResult = Write(Begin, Version21, line);

            // assert
            testResult.Should().Be(CardStart + "KEY;ENCODING=BASE64:\r\n    SGVsbG8=\r\n\r\n");
        }

        [Theory]
        [InlineData("SGVsbG8")]
        [InlineData("SGVs:bG8=")]
        public void WriteLine_ValueThatIsNotBase64_Throws(string value)
        {
            // arrange
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text))
                {
                    writer.WriteLines(new[] { Begin, Version21 });
                    var line = Base64Line(value);

                    // act
                    Action testAction = () => writer.WriteLine(line);

                    // assert
                    testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid BASE64*");
                }
            }
        }

        [Fact]
        public void WriteLines_Base64Value_ReadsBackAsTheSameOctets()
        {
            // arrange
            var written = Write(Begin, Version21, Base64Line(SixtyFourAs + HelloBase64), new VcardContentLine("NOTE", "next"));
            using (var reader = CreateReader(written))
            {
                var lines = ReadAll(reader);

                // act
                var testResult = lines[2].GetDecodedValue();

                // assert
                testResult.Should().Equal(FortyEightZerosThenHello);
                lines[3].Value.Should().Be("next");
            }
        }

        [Fact]
        public void WriteLines_LineReadWithValuelessParameters_WritesThemWithTheirNames()
        {
            // arrange
            using (var reader = CreateReader(Crlf("BEGIN:VCARD", "VERSION:2.1", "TEL;HOME;VOICE:1")))
            {
                var lines = ReadAll(reader);

                // act
                var testResult = Write(lines.ToArray());

                // assert
                testResult.Should().Be(CardStart + "TEL;TYPE=HOME;TYPE=VOICE:1\r\n");
            }
        }

        [Fact]
        public void WriteLines_NestedCard_StaysInTheOuterCardsVersion()
        {
            // arrange
            using (var reader = CreateReader(Crlf("BEGIN:VCARD", "VERSION:2.1", "AGENT:", "BEGIN:VCARD", "VERSION:2.1", "END:VCARD")))
            {
                var lines = ReadAll(reader);
                lines.Add(new VcardContentLine("X-A", LongName));

                // act
                var testResult = Write(lines.ToArray());

                // assert
                testResult.Should().EndWith("END:VCARD\r\nX-A:" + LongName + "\r\n");
            }
        }

        [Fact]
        public async Task WriteLineAsync_QuotedPrintableAndBase64_WritesWhatTheSyncWriteDoes()
        {
            // arrange
            using (var text = new StringWriter())
            {
                using (var writer = new VcardContentLineWriter(text))
                {
                    var line = QuotedPrintableLine(FortySevenCharacters + "v");
                    var base64 = Base64Line(HelloBase64);
                    await writer.WriteLineAsync(Begin);
                    await writer.WriteLineAsync(Version21);

                    // act
                    await writer.WriteLinesAsync(new[] { line, base64 });

                    // assert
                    text.ToString().Should().Be(CardStart + HeaderLine + "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrst=\r\nuv\r\nKEY;ENCODING=BASE64:\r\n    SGVsbG8=\r\n\r\n");
                }
            }
        }
    }
}
