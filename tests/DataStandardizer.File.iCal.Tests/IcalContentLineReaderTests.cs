using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalContentLineReaderTests
    {
        private const string Summary = "SUMMARY";
        private const string TimeZoneIdParameter = "TZID";
        private const string NewlineInText = "a\nb";
        private const string Rfc6868Geo = "40.446816;-80.00679";
        private const string LineA = "A:1";

        // A line with an RFC 6868 caret-encoded parameter value, a quoted one, and an escaped comma in its value.
        private const string AccessorsLine = "ATTENDEE;CN=a^'b;ROLE=\"X\",Y:mailto:a\\,b";

        // The number of bytes that a reader over a stream asks the stream for at a time: a read until the stream ends asks for this many, then
        // asks again for as many to find that it has ended.
        private const int StreamBufferSize = 16384;

        // LineA and its line break in ASCII, which is also UTF-8 and ISO-8859-1.
        private static readonly byte[] LineAInAscii = { 0x41, 0x3A, 0x31, 0x0D, 0x0A };

        // ref. RFC 6868 §4: a parameter value with caret-encoded quotation marks, and the value it stands for.
        private const string Rfc6868EncodedName = "George Herman ^'Babe^' Ruth";
        private const string Rfc6868DecodedName = "George Herman \"Babe\" Ruth";
        private const string Rfc6868Line = "ATTENDEE;CN=George Herman ^'Babe^' Ruth:mailto:babe@example.com";

        // Text with a letter, a symbol inside the Basic Multilingual Plane and an emoji outside it: "Caf\xE9", a hot beverage and a grinning face.
        private const string NonAsciiText = "Caf\xE9 \x2615 \xD83D\xDE00";

        // A content line read after U+FEFF ZERO WIDTH NO-BREAK SPACE, which is the byte order mark as a character.
        private const string ByteOrderMarkThenBegin = "\xFEFFBEGIN:VCALENDAR\r\n";

        // ref. RFC 3629 §3: "\x20AC" (the euro sign) is E2 82 AC and "\xD83D\xDE00" (a grinning face) is F0 9F 98 80 in UTF-8.  The SUMMARY is folded
        // after the first octet of one and inside the other twice, and the line after it holds e-acute.
        internal static readonly byte[] FoldsInsideUtf8Sequences = Concat(
            Encoding.UTF8.GetBytes("SUMMARY:caf"), new byte[] { 0xE2 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0x82, 0xAC },
            Encoding.UTF8.GetBytes(" "), new byte[] { 0xF0, 0x9F }, Encoding.ASCII.GetBytes("\r\n\t"), new byte[] { 0x98 }, Encoding.ASCII.GetBytes("\r\n "),
            new byte[] { 0x80 }, Encoding.UTF8.GetBytes("!\r\nX-NEXT:\xE9\r\n"));

        internal const string FoldsInsideUtf8SequencesValue = "caf\x20AC \xD83D\xDE00!";

        public static TheoryData<Func<IcalContentLineView, string>, string> Rfc6868ViewValues =>
            new TheoryData<Func<IcalContentLineView, string>, string>
            {
                { view => view.GetRawParameterValue(0, 0).ToString(), Rfc6868EncodedName },
                { view => view.GetParameterValue(0, 0).ToString(), Rfc6868DecodedName },
                { view => view.GetRawParameterValueMemory(0, 0).ToString(), Rfc6868EncodedName },
                { view => view.GetParameterValueMemory(0, 0).ToString(), Rfc6868DecodedName },
                { view => view.RawLine.ToString(), Rfc6868Line },
            };

        public static TheoryData<Action<IcalContentLineView>, string> OutOfRangeIndexCalls => new TheoryData<Action<IcalContentLineView>, string>
        {
            { view => view.GetParameterName(1).ToString(), "parameterIndex" },
            { view => view.GetParameterValueCount(-1), "parameterIndex" },
            { view => view.GetParameterValue(0, 2).ToString(), "valueIndex" },
        };

        #region Content line structure

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_NameAndValue_ReturnsLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:Bastille Day Party"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().NotBeNull();
                testResult.Name.Should().Be(Summary);
                testResult.Parameters.Should().BeEmpty();
                testResult.Value.Should().Be("Bastille Day Party");
                testResult.LineNumber.Should().Be(1);
                testResult.PhysicalLineNumber.Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_AfterTheOnlyLine_ReturnsNull(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:Bastille Day Party"), bufferSize: bufferSize))
            {
                reader.ReadLine();

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadLine_EmptyValue_ReturnsEmptyValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("DESCRIPTION:")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadLine_ValueContainsColonsAndSemicolons_KeepsThemInValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("GEO;X-P=a:40.446816;-80.00679")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters.Should().ContainSingle();
                testResult.Parameters[0].Values.Should().Equal("a");
                testResult.Value.Should().Be(Rfc6868Geo);
            }
        }

        [Fact]
        public void ReadLine_NonAsciiValue_ReturnsValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:" + NonAsciiText)))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(NonAsciiText);
            }
        }

        [Fact]
        public void ReadLine_TabInValue_IsAccepted()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a\tb")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("a\tb");
            }
        }

        [Fact]
        public void ReadLine_XName_ReturnsLine()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("X-ABC-MMSUBJ");
                testResult.Parameters.Select(parameter => parameter.Name).Should().Equal("VALUE", "FMTTYPE");
                testResult.Value.Should().Be("http://www.example.org/mysubj.au");
            }
        }

        [Fact]
        public void ReadLine_LeadingByteOrderMarkCharacter_IsSkipped()
        {
            // arrange
            using (var reader = CreateReader(ByteOrderMarkThenBegin))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("BEGIN");
            }
        }

        [Fact]
        public void ReadLine_EmptyInput_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReader(string.Empty))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
                reader.LinesRead.Should().Be(0);
                reader.PhysicalLinesRead.Should().Be(0);
            }
        }

        [Fact]
        public void ReadLine_NamesInAnyCase_AreKeptAsWritten()
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;tzId=America/New_York:19980312T083000")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("dtStart");
                testResult.Parameters[0].Name.Should().Be("tzId");
            }
        }

        [Theory]
        [InlineData(TimeZoneIdParameter)]
        [InlineData("tzid")]
        public void GetParameter_NameInAnyCase_GetsTheParameter(string name)
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;tzId=America/New_York:19980312T083000")))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.GetParameter(name);

                // assert
                testResult.Should().BeSameAs(line.Parameters[0]);
            }
        }

        [Fact]
        public void GetParameter_NameNotOnTheLine_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;tzId=America/New_York:19980312T083000")))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.GetParameter("VALUE");

                // assert
                testResult.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("DTSTART", true)]
        [InlineData("dtstart", true)]
        [InlineData("DTEND", false)]
        [InlineData("DTSTAR", false)]
        public void NameEquals_NamesInAnyCase_CompareCaseInsensitively(string name, bool expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;Value=DATE;tzId=America/New_York:19980312")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.NameEquals(name.AsSpan());

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(TimeZoneIdParameter, 1)]
        [InlineData("value", 0)]
        [InlineData("X-NONE", -1)]
        public void IndexOfParameter_NamesInAnyCase_CompareCaseInsensitively(string name, int expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;Value=DATE;tzId=America/New_York:19980312")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.IndexOfParameter(name.AsSpan());

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("x-a1", true)]
        [InlineData("X\rA1", false)] // '-' | 0x20 == '\r' | 0x20, but neither is a letter
        [InlineData("X-Q1", false)]
        public void NameEquals_NonLetters_ComparesExactly(string name, bool expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A1:v")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.NameEquals(name.AsSpan());

                // assert
                testResult.Should().Be(expected);
            }
        }

        #endregion

        #region Parameters

        [Fact]
        public void ReadLine_QuotedParameterValueWithDelimiters_KeepsDelimitersInValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=\"a:b;c,d\";X-Q=e:value")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters.Should().HaveCount(2);
                testResult.Parameters[0].Name.Should().Be("X-P");
                testResult.Parameters[0].Values.Should().Equal("a:b;c,d");
                testResult.Parameters[1].Values.Should().Equal("e");
                testResult.Value.Should().Be("value");
            }
        }

        [Fact]
        public void ReadLine_MultipleParameterValues_ReturnsEachValue()
        {
            // arrange
            // ref. RFC 5545 §3.2.4
            using (var reader = CreateReader(Crlf("ATTENDEE;DELEGATED-TO=\"mailto:jdoe@example.com\",\"mailto:jqpublic@example.com\":mailto:jsmith@example.com")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters.Should().ContainSingle();
                testResult.Parameters[0].Name.Should().Be("DELEGATED-TO");
                testResult.Parameters[0].Values.Should().Equal("mailto:jdoe@example.com", "mailto:jqpublic@example.com");
                testResult.Value.Should().Be("mailto:jsmith@example.com");
            }
        }

        [Fact]
        public void ParameterCount_MixedQuotedAndUnquotedValues_IsOne()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,\"b,c\",,d:v")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.ParameterCount;

                // assert
                testResult.Should().Be(1);
            }
        }

        [Fact]
        public void GetParameterValueCount_MixedQuotedAndUnquotedValues_CountsEachValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,\"b,c\",,d:v")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetParameterValueCount(0);

                // assert
                testResult.Should().Be(4);
            }
        }

        [Theory]
        [InlineData(0, "a")]
        [InlineData(1, "b,c")]
        [InlineData(2, "")]
        [InlineData(3, "d")]
        public void GetParameterValue_MixedQuotedAndUnquotedValues_GivesEachValue(int valueIndex, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,\"b,c\",,d:v")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetParameterValue(0, valueIndex).ToString();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        public void IsParameterValueQuoted_MixedQuotedAndUnquotedValues_ReportsQuoting(int valueIndex, bool expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,\"b,c\",,d:v")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.IsParameterValueQuoted(0, valueIndex);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void ReadLine_EmptyParameterValues_AreKept()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=;X-Q=\"\":v")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal(string.Empty);
                testResult.Parameters[1].Values.Should().Equal(string.Empty);
            }
        }

        [Fact]
        public void ReadLine_NonAsciiParameterValues_AreAccepted()
        {
            // arrange
            using (var reader = CreateReader(Crlf("ATTENDEE;CN=Jos\xE9 M\xFCller;X-Q=\"\x6771\x4EAC\":mailto:jose@example.com")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("Jos\xE9 M\xFCller");
                testResult.Parameters[1].Values.Should().Equal("\x6771\x4EAC");
            }
        }

        [Theory]
        [InlineData("^n", "\n")]
        [InlineData("^'", "\"")]
        [InlineData("^^", "^")]
        [InlineData("a^nb^'c^^d", "a\nb\"c^d")]
        [InlineData("^x", "^x")]
        [InlineData("^N", "^N")]
        [InlineData("a^", "a^")]
        [InlineData("^^n", "^n")]
        [InlineData("^^^n", "^\n")]
        [InlineData("no carets", "no carets")]
        public void ReadLine_CaretEncodedParameterValue_IsDecoded(string rawValue, string expectedValue)
        {
            // arrange
            using (var reader = CreateReader(Crlf($"X-TEST;X-P=\"{rawValue}\";X-Q={rawValue}:v")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal(expectedValue);
                testResult.Parameters[1].Values.Should().Equal(expectedValue);
            }
        }

        [Theory]
        [MemberData(nameof(Rfc6868ViewValues), DisableDiscoveryEnumeration = true)]
        public void CurrentLine_CaretEncodedParameterValue_GivesRawAndDecodedValues(Func<IcalContentLineView, string> accessor, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(Rfc6868Line)))
            {
                reader.ReadContentLine();

                // act
                var testResult = accessor(reader.CurrentLine);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void CurrentLine_DecodedValuesTakenEarlier_StayValidAfterLaterDecodes()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a^nb;X-Q=c^'d;X-R=e^^f:g\\nh\\,i")))
            {
                reader.ReadContentLine();

                // act
                var testResult = DecodeInTurn(reader.CurrentLine);

                // assert
                testResult.First.ToString().Should().Be(NewlineInText);
                testResult.Second.ToString().Should().Be("c\"d");
                testResult.Text.ToString().Should().Be("g\nh,i");
                testResult.Third.ToString().Should().Be("e^f");
                testResult.FirstAgain.Equals(testResult.First).Should().BeTrue("a decoded value is decoded once and kept");
            }
        }

        [Theory]
        [MemberData(nameof(OutOfRangeIndexCalls), DisableDiscoveryEnumeration = true)]
        public void CurrentLine_ParameterIndexOutOfRange_Throws(Action<IcalContentLineView> call, string parameterName)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,b:v")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => call(view);

                // assert
                testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(parameterName);
            }
        }

        #endregion

        #region TEXT values

        public static TheoryData<string, string> EscapedTextValues => new TheoryData<string, string>
        {
            { "DESCRIPTION:plain", "plain" },
            { "DESCRIPTION:a\\\\b", "a\\b" },
            { "DESCRIPTION:a\\;b", "a;b" },
            { "DESCRIPTION:a\\,b", "a,b" },
            { "DESCRIPTION:a\\nb", NewlineInText },
            { "DESCRIPTION:a\\Nb", NewlineInText },
            { "DESCRIPTION:\\\\n", "\\n" },
        };

        // The value is folded so the bad escape is on the third physical line.
        public static TheoryData<string> InvalidEscapeContinuations => new TheoryData<string>
        {
            " a\\:b",
            " a\\",
            " a\\x",
        };

        // The longest unfolded line in LongFoldedLines, of letters a to z in turn, and each buffer size it is read with.
        public static TheoryData<int, string, string, int> LongFoldedLines()
        {
            var value = new StringBuilder();
            for (var index = 0; index < 2000; index++)
            {
                value.Append((char)('a' + index % 26));
            }

            var expected = value.ToString();
            var physicalLines = new List<string> { "DESCRIPTION:" + expected.Substring(0, 50) };
            for (var index = 50; index < expected.Length; index += 74)
            {
                physicalLines.Add(" " + expected.Substring(index, Math.Min(74, expected.Length - index)));
            }

            physicalLines.Add("SUMMARY:after");
            var data = new TheoryData<int, string, string, int>();
            foreach (var bufferSize in BufferSizes)
            {
                data.Add(bufferSize, Crlf(physicalLines.ToArray()), expected, physicalLines.Count);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(EscapedTextValues))]
        public void GetTextValue_EscapedViewValue_IsUnescaped(string line, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(line)))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetTextValue().ToString();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(EscapedTextValues))]
        public void GetTextValue_EscapedLineValue_IsUnescaped(string line, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(line)))
            {
                var contentLine = reader.ReadLine();

                // act
                var testResult = contentLine.GetTextValue();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("DESCRIPTION:a\\nb", "a\\nb")]
        [InlineData("DESCRIPTION:\\\\n", "\\\\n")]
        public void RawValue_EscapedValue_IsKeptAsWritten(string line, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(line)))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.RawValue.ToString();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEscapeContinuations))]
        public void GetTextValue_InvalidEscapeInView_ThrowsWithLocation(string continuation)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A:a", "DESCRIPTION:", continuation)))
            {
                reader.ReadLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetTextValue().ToString();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DESCRIPTION");
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEscapeContinuations))]
        public void GetTextValue_InvalidEscapeInLine_ThrowsWithLocation(string continuation)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A:a", "DESCRIPTION:", continuation)))
            {
                reader.ReadLine();
                var line = reader.ReadLine();

                // act
                Action testAction = () => line.GetTextValue();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DESCRIPTION");
            }
        }

        [Fact]
        public void GetTextValue_InvalidEscape_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("DESCRIPTION:a\\qb", "SUMMARY:next")))
            {
                reader.ReadContentLine();

                // act
                Action testAction = () => reader.CurrentLine.GetTextValue().ToString();

                // assert
                testAction.Should().Throw<IcalFileException>();
            }
        }

        [Fact]
        public void ReadLine_AfterInvalidEscape_ReadsTheNextLine()
        {
            // arrange
            using (var reader = CreateReader(Crlf("DESCRIPTION:a\\qb", "SUMMARY:next")))
            {
                reader.ReadContentLine();
                Record.Exception(() => reader.CurrentLine.GetTextValue().ToString());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("next");
            }
        }

        [Fact]
        public void GetTextValue_LineNotReadFromSource_ReportsLineZero()
        {
            // arrange
            var line = new IcalContentLine("DESCRIPTION", "bad\\q");

            // act
            Action testAction = () => line.GetTextValue();

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(0L);
            exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(0L);
        }

        #endregion

        #region Folding

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_FoldWithSpace_IsUnfolded(int bufferSize)
        {
            // arrange
            // ref. RFC 5545 §3.1
            using (var reader = CreateReader(Crlf("DESCRIPTION:This is a lo", " ng description", "  that exists on a long line.", "SUMMARY:x"), bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be("This is a long description that exists on a long line.");
                testResult[0].PhysicalLineNumber.Should().Be(1);
                testResult[1].PhysicalLineNumber.Should().Be(4);
                testResult[1].LineNumber.Should().Be(2);
                reader.LinesRead.Should().Be(2);
                reader.PhysicalLinesRead.Should().Be(4);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_FoldWithTab_IsUnfolded(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:ab", "\tcd", "\t\tef"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("abcd\tef");
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_FoldInEveryPart_IsUnfolded(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("DTS", " TART;TZ", " ID=Ameri", " ca/New_York;X-P=\"a:", " b\",c", " :1998", " 0312T083000"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("DTSTART");
                testResult.Parameters[0].Name.Should().Be(TimeZoneIdParameter);
                testResult.Parameters[0].Values.Should().Equal(Rfc5545Examples.NewYorkTimeZoneId);
                testResult.Parameters[1].Values.Should().Equal("a:b", "c");
                testResult.Value.Should().Be("19980312T083000");
                reader.PhysicalLinesRead.Should().Be(7);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_FoldInsideQuotedParameterValue_IsUnfolded(int bufferSize)
        {
            // arrange
            // ref. RFC 6868 §4
            using (var reader = CreateReader(Crlf("GEO;X-ADDRESS=\"Pittsburgh Pirates^n115 Federal St^nPittsburgh, PA 15", " 212\":40.446816;-80.00679"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212");
                testResult.Value.Should().Be(Rfc6868Geo);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_ConsecutiveFolds_AreUnfolded(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", " ", " ", " b"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("ab");
                reader.PhysicalLinesRead.Should().Be(4);
            }
        }

        [Theory]
        [MemberData(nameof(LongFoldedLines))]
        public void ReadLine_LongFoldedLine_IsUnfolded(int bufferSize, string text, string expected, int physicalLineCount)
        {
            // arrange
            using (var reader = CreateReader(text, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[0].Value.Should().Be(expected);
                testResult[1].Value.Should().Be("after");
                testResult[1].PhysicalLineNumber.Should().Be(physicalLineCount);
            }
        }

        [Fact]
        public void ReadLine_FoldAtEndOfInput_IsUnfolded()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\r\n b\r\n "))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("ab");
            }
        }

        [Fact]
        public void ReadLine_AfterFoldAtEndOfInput_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\r\n b\r\n "))
            {
                reader.ReadLine();

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
                reader.PhysicalLinesRead.Should().Be(3);
            }
        }

        [Fact]
        public void ReadLine_FirstLineStartsWithWhiteSpace_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf(" SUMMARY:a")))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<IcalFileException>();
            }
        }

        #endregion

        #region Folds inside UTF-8 sequences

        public static TheoryData<int, int> StreamBufferSizeData()
        {
            var data = new TheoryData<int, int>();
            foreach (var streamBufferSize in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 13, 16 * 1024 })
            {
                data.Add(streamBufferSize, 16 * 1024);
                data.Add(streamBufferSize, 3);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(StreamBufferSizeData))]
        public void ReadLine_FoldInsideUtf8Sequence_RestoresCharacter(int streamBufferSize, int bufferSize)
        {
            // arrange
            using (var reader = CreateStreamReader(FoldsInsideUtf8Sequences, streamBufferSize: streamBufferSize, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be(FoldsInsideUtf8SequencesValue);
                testResult[1].Value.Should().Be("\xE9");
                testResult[1].PhysicalLineNumber.Should().Be(5);
                reader.PhysicalLinesRead.Should().Be(5);
            }
        }

        [Fact]
        public void ReadLine_FoldWithBareLineFeedInsideUtf8Sequence_RestoresCharacterWhenAccepted()
        {
            // arrange
            var data = Concat(Encoding.UTF8.GetBytes("SUMMARY:"), new byte[] { 0xE2, 0x82 }, Encoding.ASCII.GetBytes("\n "), new byte[] { 0xAC }, Encoding.ASCII.GetBytes("\n"));
            using (var reader = CreateStreamReader(data, new IcalOptions { AcceptBareLineFeed = true }))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("\x20AC");
                reader.PhysicalLinesRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadLine_Utf16StreamWithByteOrderMark_IsDecoded()
        {
            // arrange
            var data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Crlf("SUMMARY:caf", " \xE9 \x2615"))).ToArray();
            using (var reader = CreateStreamReader(data))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("caf\xE9 \x2615");
            }
        }

        [Fact]
        public void ReadLine_Utf8StreamWithByteOrderMark_SkipsByteOrderMark()
        {
            // arrange
            var data = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Crlf("BEGIN:VCALENDAR"))).ToArray();
            using (var reader = CreateStreamReader(data))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("BEGIN");
            }
        }

        [Fact]
        public void ReadLine_EncodingOption_DecodesWithEncoding()
        {
            // arrange
            using (var stream = new MemoryStream(IcalTestData.Latin1.GetBytes(Crlf("SUMMARY:caf", " \xE9"))))
            using (var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 }))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("caf\xE9");
            }
        }

        #endregion

        #region Line breaks

        public static TheoryData<Func<IcalContentLineReader, object>> MembersAfterLineBreakError => new TheoryData<Func<IcalContentLineReader, object>>
        {
            reader => reader.ReadLine(),
            reader => reader.ReadContentLine(),
            reader => reader.CurrentLine,
        };

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_BareLineFeedByDefault_Throws(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\r\n b\r\nX-A:c\nX-B:d\r\n", bufferSize: bufferSize))
            {
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
            }
        }

        [Fact]
        public void ReadLine_BareLineFeedInFoldByDefault_Throws()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\n b\r\n"))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<IcalFileException>()
                    .Which.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(1L);
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterLineBreakError), DisableDiscoveryEnumeration = true)]
        public void Member_AfterLineBreakError_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\nX-B:d\r\n"))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => member(reader);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                reader.LinesRead.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_BareLineFeedAccepted_IsLineBreakAndFold(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\n b\r\n c\nX-A:d\n\te\nX-B:f\r\nX-C:g\n", new IcalOptions { AcceptBareLineFeed = true }, bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Select(line => line.Value).Should().Equal("abc", "de", "f", "g");
                testResult.Select(line => line.PhysicalLineNumber).Should().Equal(1, 4, 6, 7);
                reader.PhysicalLinesRead.Should().Be(7);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_BareCarriageReturn_IsNotLineBreak(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a\rX-B:b", "X-C:c"), bufferSize: bufferSize))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("U+000D");
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Summary);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_AfterBareCarriageReturn_ReadsTheNextLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a\rX-B:b", "X-C:c"), bufferSize: bufferSize))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("X-C");
                reader.PhysicalLinesRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadLine_BareCarriageReturnAtEndOfInput_IsNotLineBreak()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\r", bufferSize: 1))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<IcalFileException>().Which.Message.Should().Contain("U+000D");
            }
        }

        [Fact]
        public void ReadLine_AfterBareCarriageReturnAtEndOfInput_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\r", bufferSize: 1))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_LastLineWithoutLineBreak_IsRead(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("BEGIN:VCALENDAR\r\nEND:VCALENDAR", bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Select(line => line.Value).Should().Equal(IcalLines.CalendarPath, IcalLines.CalendarPath);
                reader.PhysicalLinesRead.Should().Be(2);
            }
        }

        #endregion

        #region Maximum line length

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_LineAtMaxLineLength_IsRead(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:12", " 34"), new IcalOptions { MaxLineLength = 12 }, bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("1234");
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_UnfoldedLineOverMaxLineLength_Throws(int bufferSize)
        {
            // arrange: each physical line is within the limit; the logical line is not.
            using (var reader = CreateReader(Crlf("X-A:1", "SUMMARY:12", " 345"), new IcalOptions { MaxLineLength = 12 }, bufferSize))
            {
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_AfterUnfoldedLineOverMaxLineLength_ThrowsInvalidOperation(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A:1", "SUMMARY:12", " 345"), new IcalOptions { MaxLineLength = 12 }, bufferSize))
            {
                reader.ReadLine();
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_NoMaxLineLength_ReadsVeryLongLine()
        {
            // arrange: a value of a million characters, read through a small buffer.
            var value = new string('x', 1_000_000);
            using (var reader = CreateReader(Crlf("DESCRIPTION:" + value), bufferSize: 64))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(value);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_TextReaderMaxLineLengthLessThanOne_Throws(int maxLineLength)
        {
            // arrange
            var options = new IcalOptions { MaxLineLength = maxLineLength };

            // act
            Action testAction = () => new IcalContentLineReader(new StringReader(string.Empty), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_StreamMaxLineLengthLessThanOne_Throws(int maxLineLength)
        {
            // arrange
            var options = new IcalOptions { MaxLineLength = maxLineLength };

            // act
            Action testAction = () => new IcalContentLineReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        #endregion

        #region Syntax errors

        public static TheoryData<string, string> InvalidSyntax => new TheoryData<string, string>
        {
            { "", "empty" },
            { ":value", "property name" },
            { Summary, "':'" },
            { "SUMMARY value", "';' or ':'" },
            { "SUM_MARY:value", "';' or ':'" },
            { "SUMMARY;:value", "parameter name" },
            { "SUMMARY;X-P:value", "'='" },
            { "SUMMARY;X-P", "'='" },
            { "SUMMARY;X-P=a", "':'" },
            { "SUMMARY;X-P=\"a:value", "closing" },
            { "SUMMARY;X-P=\"a\"b:value", "',', ';' or ':'" },
            { "SUMMARY;X-P=\"a\"", "',', ';' or ':'" },
            { "SUMMARY;X-P=a\"b\":value", "U+0022" },
            { "SUMMARY;X-P=\"a\x0001\":value", "U+0001" },
            { "SUMMARY;X-P=a\x007F:value", "U+007F" },
            { "SUMMARY:a\x0000b", "U+0000" },
            { "SUMMARY:a\x001Fb", "U+001F" },
            { "SUMMARY:a\x007Fb", "U+007F" },
        };

        [Theory]
        [MemberData(nameof(InvalidSyntax))]
        public void ReadLine_InvalidSyntax_Throws(string line, string expectedMessagePart)
        {
            // arrange
            using (var reader = CreateReader(Crlf(line, "X-NEXT:ok")))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain(expectedMessagePart);
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(1L);
                reader.LinesRead.Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidSyntax))]
        public void ReadLine_AfterInvalidSyntax_ReadsTheNextLine(string line, string expectedMessagePart)
        {
            // arrange
            _ = expectedMessagePart;
            using (var reader = CreateReader(Crlf(line, "X-NEXT:ok")))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be("X-NEXT");
                reader.LinesRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadLine_ErrorOnContinuationLine_ReportsPhysicalLineOfError()
        {
            // arrange
            using (var reader = CreateReader(Crlf("BEGIN:VEVENT", "ATTENDEE;CN=a", " b;ROLE=", " REQ-PARTICIPANT;X-P", " :mailto:a@example.com")))
            {
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(5L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("ATTENDEE");
            }
        }

        [Fact]
        public void ReadLine_ErrorBeforeName_HasNoPropertyName()
        {
            // arrange
            using (var reader = CreateReader(Crlf("-:x", "=x")))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
            }
        }

        [Fact]
        public void ReadContentLine_SyntaxError_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", "BAD")))
            {
                reader.ReadContentLine();

                // act
                Action testAction = () => reader.ReadContentLine();

                // assert
                testAction.Should().Throw<IcalFileException>();
            }
        }

        [Fact]
        public void CurrentLine_AfterSyntaxError_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", "BAD")))
            {
                reader.ReadContentLine();
                Record.Exception(() => reader.ReadContentLine());

                // act
                Action testAction = () => _ = reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void Name_ViewTakenBeforeSyntaxError_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", "BAD")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;
                Record.Exception(() => reader.ReadContentLine());

                // act
                Action testAction = () => view.Name.ToString();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region View and consistency

        // Each member of a view, called on a view of the line "X-A;X-P=a:b".
        public static TheoryData<Func<IcalContentLineView, object>> ViewMembers => new TheoryData<Func<IcalContentLineView, object>>
        {
            view => view.LineNumber,
            view => view.PhysicalLineNumber,
            view => view.RawLine.ToString(),
            view => view.RawLineMemory,
            view => view.Name.ToString(),
            view => view.NameMemory,
            view => view.NameEquals("X-A".AsSpan()),
            view => view.ParameterCount,
            view => view.RawValue.ToString(),
            view => view.RawValueMemory,
            view => view.GetParameterName(0).ToString(),
            view => view.GetParameterNameMemory(0),
            view => view.IndexOfParameter("X-P".AsSpan()),
            view => view.GetParameterValueCount(0),
            view => view.GetRawParameterValue(0, 0).ToString(),
            view => view.GetRawParameterValueMemory(0, 0),
            view => view.GetParameterValue(0, 0).ToString(),
            view => view.GetParameterValueMemory(0, 0),
            view => view.IsParameterValueQuoted(0, 0),
            view => view.GetTextValue().ToString(),
            view => view.GetTextValueMemory(),
            view => view.ToContentLine(),
        };

        // Each accessor of a view that gives memory, with the accessor that gives the same text as a span, for the line in MemoryAccessorsLine.
        // Each memory accessor and each span accessor of a view of AccessorsLine, with what it gives: the parameter values decoded by RFC 6868
        // and the raw ones as written, both without their quotes, and the text value unescaped by RFC 5545 §3.3.11.
        public static TheoryData<Func<IcalContentLineView, string>, string> ViewAccessors =>
            new TheoryData<Func<IcalContentLineView, string>, string>
            {
                { view => view.NameMemory.ToString(), "ATTENDEE" },
                { view => view.Name.ToString(), "ATTENDEE" },
                { view => view.RawLineMemory.ToString(), AccessorsLine },
                { view => view.RawLine.ToString(), AccessorsLine },
                { view => view.RawValueMemory.ToString(), "mailto:a\\,b" },
                { view => view.RawValue.ToString(), "mailto:a\\,b" },
                { view => view.GetTextValueMemory().ToString(), "mailto:a,b" },
                { view => view.GetTextValue().ToString(), "mailto:a,b" },
                { view => view.GetParameterNameMemory(0).ToString(), "CN" },
                { view => view.GetParameterName(0).ToString(), "CN" },
                { view => view.GetParameterNameMemory(1).ToString(), "ROLE" },
                { view => view.GetParameterName(1).ToString(), "ROLE" },
                { view => view.GetParameterValueMemory(0, 0).ToString(), "a\"b" },
                { view => view.GetParameterValue(0, 0).ToString(), "a\"b" },
                { view => view.GetRawParameterValueMemory(0, 0).ToString(), "a^'b" },
                { view => view.GetRawParameterValue(0, 0).ToString(), "a^'b" },
                { view => view.GetParameterValueMemory(1, 0).ToString(), "X" },
                { view => view.GetParameterValue(1, 0).ToString(), "X" },
                { view => view.GetRawParameterValueMemory(1, 0).ToString(), "X" },
                { view => view.GetRawParameterValue(1, 0).ToString(), "X" },
                { view => view.GetParameterValueMemory(1, 1).ToString(), "Y" },
                { view => view.GetParameterValue(1, 1).ToString(), "Y" },
                { view => view.GetRawParameterValueMemory(1, 1).ToString(), "Y" },
                { view => view.GetRawParameterValue(1, 1).ToString(), "Y" },
            };

        public static TheoryData<string, int> ConsistencyData()
        {
            var data = new TheoryData<string, int>();
            foreach (var fixture in RfcFixtures.All)
            {
                foreach (var bufferSize in new[] { 1, 5, 16 * 1024 })
                {
                    data.Add(fixture.Key, bufferSize);
                }
            }

            return data;
        }

        [Fact]
        public void CurrentLine_BeforeFirstRead_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                // act
                Action testAction = () => _ = reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadContentLine_AfterEndOfInput_ReturnsFalse()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.ReadContentLine();

                // assert
                testResult.Should().BeFalse();
            }
        }

        [Fact]
        public void CurrentLine_AfterEndOfInput_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                Action testAction = () => _ = reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void Name_ViewTakenBeforeEndOfInput_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;
                reader.ReadContentLine();

                // act
                Action testAction = () => view.Name.ToString();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_AfterReadLine_IsLineJustRead()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                reader.ReadLine();

                // act
                var testResult = reader.CurrentLine;

                // assert
                testResult.Name.ToString().Should().Be(Summary);
                testResult.RawValue.ToString().Should().Be("a");
            }
        }

        [Theory]
        [MemberData(nameof(ViewMembers), DisableDiscoveryEnumeration = true)]
        public void Member_ViewOfEarlierLine_ThrowsInvalidOperation(Func<IcalContentLineView, object> member)
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A;X-P=a:b", "X-B:c")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;
                reader.ReadContentLine();

                // act
                Action testAction = () => member(view);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_AfterNextRead_IsTheNextLine()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A;X-P=a:b", "X-B:c")))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.Name.ToString();

                // assert
                testResult.Should().Be("X-B");
            }
        }

        [Theory]
        [MemberData(nameof(ViewMembers), DisableDiscoveryEnumeration = true)]
        public void Member_ViewAfterDispose_ThrowsObjectDisposed(Func<IcalContentLineView, object> member)
        {
            // arrange
            var reader = CreateReader(Crlf("X-A;X-P=a:b"));
            reader.ReadContentLine();
            var view = reader.CurrentLine;
            reader.Dispose();

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Theory]
        [MemberData(nameof(ViewMembers), DisableDiscoveryEnumeration = true)]
        public void Member_DefaultView_ThrowsInvalidOperation(Func<IcalContentLineView, object> member)
        {
            // arrange
            var view = default(IcalContentLineView);

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(ViewAccessors), DisableDiscoveryEnumeration = true)]
        public void Accessor_View_GivesThePartOfTheLine(Func<IcalContentLineView, string> accessor, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(AccessorsLine)))
            {
                reader.ReadContentLine();

                // act
                var testResult = accessor(reader.CurrentLine);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(ConsistencyData))]
        public void ReadContentLine_EveryFixture_ViewsItsLines(string fixtureName, int bufferSize)
        {
            // arrange
            var expected = FixtureExpectations.NumberedLines[fixtureName];
            using (var reader = CreateReader(RfcFixtures.All[fixtureName], bufferSize: bufferSize))
            {
                // act
                var testResult = LineSnapshot.DescribeNumbered(ReadAllViews(reader));

                // assert
                testResult.Should().Equal(expected);
            }
        }

        #endregion

        #region Counters

        // The lines read, and the logical and physical lines counted, after each read of "A:1", "B:2" folded over three lines, "C:5", and the end.
        public static TheoryData<int, string, long, long> Counters => new TheoryData<int, string, long, long>
        {
            { 1, "A", 1, 1 },
            { 2, "B", 2, 4 },
            { 3, "C", 3, 5 },
            { 4, null, 3, 5 },
        };

        [Theory]
        [MemberData(nameof(Counters))]
        public void Counters_AfterEachRead_CountLogicalAndPhysicalLines(int reads, string expectedName, long expectedLines, long expectedPhysicalLines)
        {
            // arrange
            using (var reader = CreateReader(Crlf(LineA, "B:2", " 3", "  4", "C:5")))
            {
                // act
                var testResult = ReadNameOfLastLine(reader, reads);

                // assert
                testResult.Should().Be(expectedName);
                reader.LinesRead.Should().Be(expectedLines);
                reader.PhysicalLinesRead.Should().Be(expectedPhysicalLines);
            }
        }

        #endregion

        #region Disposal

        public static TheoryData<Func<IcalContentLineReader, object>> MembersAfterDispose => new TheoryData<Func<IcalContentLineReader, object>>
        {
            reader => reader.ReadLine(),
            reader => reader.ReadContentLine(),
            reader => reader.ReadLines(),
            reader => reader.CurrentLine,
            reader => reader.LinesRead,
            reader => reader.PhysicalLinesRead,
        };

        [Theory]
        [MemberData(nameof(MembersAfterDispose), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var reader = CreateReader(Crlf("SUMMARY:a"));
            reader.ReadContentLine();
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineReader));
        }

        [Fact]
        public void ReadLines_DisposedDuringEnumeration_ThrowsObjectDisposed()
        {
            // arrange
            var reader = CreateReader(Crlf(LineA, "B:2"));
            using (var enumerator = reader.ReadLines().GetEnumerator())
            {
                enumerator.MoveNext();
                reader.Dispose();

                // act
                Action testAction = () => enumerator.MoveNext();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = CreateReader(Crlf("SUMMARY:a"));
            reader.ReadLine();
            reader.Dispose();

            // act
            Action testAction = () => reader.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerSuppliedTextReader_IsLeftOpen()
        {
            // arrange
            using (var textReader = new StringReader(Crlf(LineA, "B:2")))
            {
                var reader = new IcalContentLineReader(textReader);
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                Action readAction = () => textReader.Peek();
                readAction.Should().NotThrow();
            }
        }

        [Fact]
        public void Dispose_CallerSuppliedStream_IsLeftOpen()
        {
            // arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Crlf(LineA))))
            {
                var reader = new IcalContentLineReader(stream);
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                stream.CanRead.Should().BeTrue();
            }
        }

        [Fact]
        public void Dispose_CallerSuppliedStreamWithOtherEncoding_IsLeftOpen()
        {
            // arrange
            using (var stream = new MemoryStream(IcalTestData.Latin1.GetBytes(Crlf(LineA))))
            {
                var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 });
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                stream.CanRead.Should().BeTrue();
            }
        }

        #endregion

        #region Constructors

        public static TheoryData<Action, string> NullConstructorArguments()
        {
            var options = new IcalOptions();
            return new TheoryData<Action, string>
            {
                { () => new IcalContentLineReader((Stream)null), "icalStream" },
                { () => new IcalContentLineReader((Stream)null, options), "icalStream" },
                { () => new IcalContentLineReader(new MemoryStream(), null), "options" },
                { () => new IcalContentLineReader((string)null), "icalFilePath" },
                { () => new IcalContentLineReader((string)null, options), "icalFilePath" },
                { () => new IcalContentLineReader("unused.ics", null), "options" },
                { () => new IcalContentLineReader((TextReader)null), "reader" },
                { () => new IcalContentLineReader((TextReader)null, options), "reader" },
                { () => new IcalContentLineReader(new StringReader(string.Empty), null), "options" },
            };
        }

        [Theory]
        [MemberData(nameof(NullConstructorArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_NullArgument_ThrowsArgumentNull(Action call, string parameterName)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(parameterName);
        }

        [Fact]
        public void Constructor_UnreadableStream_Throws()
        {
            // arrange
            var stream = new MemoryStream();
            stream.Dispose();

            // act
            Action testAction = () => new IcalContentLineReader(stream);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("icalStream");
        }

        [Fact]
        public void Constructor_TextReaderWithEncodingOption_Throws()
        {
            // arrange
            var options = new IcalOptions { Encoding = Encoding.UTF8 };

            // act
            Action testAction = () => new IcalContentLineReader(new StringReader(string.Empty), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_Stream_ReadsThroughAFoldRealigningStreamOverTheStream()
        {
            // arrange
            using (var stream = new MemoryStream(LineAInAscii))
            using (var reader = new IcalContentLineReader(stream))
            {
                // act
                var testResult = reader.UnderlyingReader;

                // assert
                var streamReader = testResult.Should().BeOfType<StreamReader>().Subject;
                var foldRealigningStream = streamReader.BaseStream.Should().BeOfType<Utf8FoldRealigningStream>().Subject;
                foldRealigningStream.InnerStream.Should().BeSameAs(stream);
            }
        }

        [Fact]
        public void ReadLine_Stream_ReadsTheStreamInBlocksOfTheStreamBufferSize()
        {
            // arrange
            var stream = new ReadRecordingStream(LineAInAscii);
            using (var reader = new IcalContentLineReader(stream))
            {
                // act
                reader.ReadLine();

                // assert
                stream.RequestedCounts.Should().Equal(StreamBufferSize, StreamBufferSize);
            }
        }

        [Fact]
        public void Constructor_StreamWithOtherEncoding_ReadsTheStreamWithoutRealigningFolds()
        {
            // arrange
            using (var stream = new MemoryStream(LineAInAscii))
            using (var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 }))
            {
                // act
                var testResult = reader.UnderlyingReader;

                // assert
                var streamReader = testResult.Should().BeOfType<StreamReader>().Subject;
                streamReader.BaseStream.Should().BeSameAs(stream);
                streamReader.CurrentEncoding.Should().Be(IcalTestData.Latin1);
            }
        }

        [Fact]
        public void ReadLine_StreamWithOtherEncoding_ReadsTheStreamInBlocksOfTheStreamBufferSize()
        {
            // arrange
            var stream = new ReadRecordingStream(LineAInAscii);
            using (var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 }))
            {
                // act
                reader.ReadLine();

                // assert
                stream.RequestedCounts.Should().Equal(StreamBufferSize, StreamBufferSize);
            }
        }

        #endregion

        /// <summary>
        /// Reads every line through the view that <see cref="IcalContentLineReader.ReadContentLine"/> exposes.
        /// </summary>
        internal static List<LineSnapshot> ReadAllViews(IcalContentLineReader reader)
        {
            var views = new List<LineSnapshot>();
            while (reader.ReadContentLine())
            {
                views.Add(LineSnapshot.FromView(reader.CurrentLine));
            }

            return views;
        }

        /// <summary>
        /// Reads a number of lines, and gives the name of the last one read, or null if the last read found the end of the input.
        /// </summary>
        private static string ReadNameOfLastLine(IcalContentLineReader reader, int count)
        {
            IcalContentLine line = null;
            for (var index = 0; index < count; index++)
            {
                line = reader.ReadLine();
            }

            return line?.Name;
        }

        /// <summary>
        /// Takes the decoded values of a view's parameters and TEXT value, in turn, and the first again.
        /// </summary>
        private static (ReadOnlyMemory<char> First, ReadOnlyMemory<char> Second, ReadOnlyMemory<char> Text, ReadOnlyMemory<char> Third,
            ReadOnlyMemory<char> FirstAgain) DecodeInTurn(IcalContentLineView view)
        {
            var first = view.GetParameterValueMemory(0, 0);
            var second = view.GetParameterValueMemory(1, 0);
            var text = view.GetTextValueMemory();
            var third = view.GetParameterValueMemory(2, 0);
            var firstAgain = view.GetParameterValueMemory(0, 0);
            return (first, second, text, third, firstAgain);
        }

        private static byte[] Concat(params byte[][] parts)
        {
            return parts.SelectMany(part => part).ToArray();
        }
    }
}
