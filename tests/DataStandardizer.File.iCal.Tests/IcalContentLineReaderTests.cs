using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalContentLineReaderTests
    {
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
                testResult.Name.Should().Be("SUMMARY");
                testResult.Parameters.Should().BeEmpty();
                testResult.Value.Should().Be("Bastille Day Party");
                testResult.LineNumber.Should().Be(1);
                testResult.PhysicalLineNumber.Should().Be(1);
                reader.ReadLine().Should().BeNull();
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
                testResult.Value.Should().Be("40.446816;-80.00679");
            }
        }

        [Fact]
        public void ReadLine_NonAsciiValue_ReturnsValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:Café ☕ 😀")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("Café ☕ 😀");
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
            using (var reader = CreateReader("﻿" + Crlf("BEGIN:VCALENDAR")))
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
                testResult.GetParameter("TZID").Should().BeSameAs(testResult.Parameters[0]);
                testResult.GetParameter("tzid").Should().BeSameAs(testResult.Parameters[0]);
                testResult.GetParameter("VALUE").Should().BeNull();
            }
        }

        [Fact]
        public void CurrentLine_NamesInAnyCase_CompareCaseInsensitively()
        {
            // arrange
            using (var reader = CreateReader(Crlf("dtStart;Value=DATE;tzId=America/New_York:19980312")))
            {
                // act
                reader.ReadContentLine();
                var testResult = reader.CurrentLine;

                // assert
                testResult.NameEquals("DTSTART".AsSpan()).Should().BeTrue();
                testResult.NameEquals("dtstart".AsSpan()).Should().BeTrue();
                testResult.NameEquals("DTEND".AsSpan()).Should().BeFalse();
                testResult.NameEquals("DTSTAR".AsSpan()).Should().BeFalse();
                testResult.IndexOfParameter("TZID".AsSpan()).Should().Be(1);
                testResult.IndexOfParameter("value".AsSpan()).Should().Be(0);
                testResult.IndexOfParameter("X-NONE".AsSpan()).Should().Be(-1);
            }
        }

        [Fact]
        public void CurrentLine_NameEqualsNonLetters_ComparesExactly()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A1:v")))
            {
                // act
                reader.ReadContentLine();
                var testResult = reader.CurrentLine;

                // assert
                testResult.NameEquals("x-a1".AsSpan()).Should().BeTrue();
                testResult.NameEquals("X\rA1".AsSpan()).Should().BeFalse(); // '-' | 0x20 == '\r' | 0x20, but neither is a letter
                testResult.NameEquals("X-Q1".AsSpan()).Should().BeFalse();
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
            // ref. RFC 5545§3.2.4
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
        public void CurrentLine_MixedQuotedAndUnquotedValues_ReportsQuoting()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,\"b,c\",,d:v")))
            {
                // act
                reader.ReadContentLine();
                var testResult = reader.CurrentLine;

                // assert
                testResult.ParameterCount.Should().Be(1);
                testResult.GetParameterValueCount(0).Should().Be(4);
                testResult.GetParameterValue(0, 0).ToString().Should().Be("a");
                testResult.GetParameterValue(0, 1).ToString().Should().Be("b,c");
                testResult.GetParameterValue(0, 2).ToString().Should().BeEmpty();
                testResult.GetParameterValue(0, 3).ToString().Should().Be("d");
                testResult.IsParameterValueQuoted(0, 0).Should().BeFalse();
                testResult.IsParameterValueQuoted(0, 1).Should().BeTrue();
                testResult.IsParameterValueQuoted(0, 2).Should().BeFalse();
                testResult.IsParameterValueQuoted(0, 3).Should().BeFalse();
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
            using (var reader = CreateReader(Crlf("ATTENDEE;CN=José Müller;X-Q=\"東京\":mailto:jose@example.com")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("José Müller");
                testResult.Parameters[1].Values.Should().Equal("東京");
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

        [Fact]
        public void CurrentLine_CaretEncodedParameterValue_KeepsRawValue()
        {
            // arrange
            // ref. RFC 6868§4
            using (var reader = CreateReader(Crlf("ATTENDEE;CN=George Herman ^'Babe^' Ruth:mailto:babe@example.com")))
            {
                // act
                reader.ReadContentLine();
                var testResult = reader.CurrentLine;

                // assert
                testResult.GetRawParameterValue(0, 0).ToString().Should().Be("George Herman ^'Babe^' Ruth");
                testResult.GetParameterValue(0, 0).ToString().Should().Be("George Herman \"Babe\" Ruth");
                testResult.GetRawParameterValueMemory(0, 0).ToString().Should().Be("George Herman ^'Babe^' Ruth");
                testResult.GetParameterValueMemory(0, 0).ToString().Should().Be("George Herman \"Babe\" Ruth");
                testResult.RawLine.ToString().Should().Be("ATTENDEE;CN=George Herman ^'Babe^' Ruth:mailto:babe@example.com");
            }
        }

        [Fact]
        public void CurrentLine_DecodedValuesTakenEarlier_StayValidAfterLaterDecodes()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a^nb;X-Q=c^'d;X-R=e^^f:g\\nh\\,i")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var first = view.GetParameterValueMemory(0, 0);
                var second = view.GetParameterValueMemory(1, 0);
                var text = view.GetTextValueMemory();
                var third = view.GetParameterValueMemory(2, 0);
                var firstAgain = view.GetParameterValueMemory(0, 0);

                // assert
                first.ToString().Should().Be("a\nb");
                second.ToString().Should().Be("c\"d");
                text.ToString().Should().Be("g\nh,i");
                third.ToString().Should().Be("e^f");
                firstAgain.Equals(first).Should().BeTrue("a decoded value is decoded once and kept");
            }
        }

        [Fact]
        public void CurrentLine_ParameterIndexOutOfRange_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-TEST;X-P=a,b:v")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action parameterAction = () => view.GetParameterName(1).ToString();
                Action negativeAction = () => view.GetParameterValueCount(-1);
                Action valueAction = () => view.GetParameterValue(0, 2).ToString();

                // assert
                parameterAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("parameterIndex");
                negativeAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("parameterIndex");
                valueAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("valueIndex");
            }
        }

        #endregion

        #region TEXT values

        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("a\\\\b", "a\\b")]
        [InlineData("a\\;b", "a;b")]
        [InlineData("a\\,b", "a,b")]
        [InlineData("a\\nb", "a\nb")]
        [InlineData("a\\Nb", "a\nb")]
        [InlineData("\\\\n", "\\n")]
        public void GetTextValue_EscapedValue_IsUnescaped(string rawValue, string expectedValue)
        {
            // arrange
            using (var reader = CreateReader(Crlf("DESCRIPTION:" + rawValue)))
            {
                // act
                reader.ReadContentLine();
                var viewResult = reader.CurrentLine.GetTextValue().ToString();
                var lineResult = reader.CurrentLine.ToContentLine().GetTextValue();

                // assert
                viewResult.Should().Be(expectedValue);
                lineResult.Should().Be(expectedValue);
                reader.CurrentLine.RawValue.ToString().Should().Be(rawValue);
            }
        }

        [Theory]
        [InlineData("a\\:b")]
        [InlineData("a\\")]
        [InlineData("a\\x")]
        public void GetTextValue_InvalidEscape_Throws(string rawValue)
        {
            // arrange
            // The value is folded so the bad escape is on the third physical line.
            using (var reader = CreateReader(Crlf("X-A:a", "DESCRIPTION:", " " + rawValue)))
            {
                reader.ReadLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;
                var line = view.ToContentLine();

                // act
                Action viewAction = () => view.GetTextValue().ToString();
                Action lineAction = () => line.GetTextValue();

                // assert
                foreach (var testAction in new[] { viewAction, lineAction })
                {
                    var exception = testAction.Should().Throw<IcalFileException>().Which;
                    exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                    exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                    exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DESCRIPTION");
                }
            }
        }

        [Fact]
        public void GetTextValue_InvalidEscape_ReaderCanContinue()
        {
            // arrange
            using (var reader = CreateReader(Crlf("DESCRIPTION:a\\qb", "SUMMARY:next")))
            {
                reader.ReadContentLine();

                // act
                Action testAction = () => reader.CurrentLine.GetTextValue().ToString();

                // assert
                testAction.Should().Throw<IcalFileException>();
                reader.ReadLine().Value.Should().Be("next");
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
            // ref. RFC 5545§3.1
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
                testResult.Parameters[0].Name.Should().Be("TZID");
                testResult.Parameters[0].Values.Should().Equal("America/New_York");
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
            // ref. RFC 6868§4
            using (var reader = CreateReader(Crlf("GEO;X-ADDRESS=\"Pittsburgh Pirates^n115 Federal St^nPittsburgh, PA 15", " 212\":40.446816;-80.00679"), bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212");
                testResult.Value.Should().Be("40.446816;-80.00679");
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
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLine_LongFoldedLine_IsUnfolded(int bufferSize)
        {
            // arrange
            var expectedValue = string.Concat(Enumerable.Range(0, 2000).Select(index => (char)('a' + index % 26)));
            var physicalLines = new List<string> { "DESCRIPTION:" + expectedValue.Substring(0, 50) };
            for (var index = 50; index < expectedValue.Length; index += 74)
            {
                physicalLines.Add(" " + expectedValue.Substring(index, Math.Min(74, expectedValue.Length - index)));
            }

            physicalLines.Add("SUMMARY:after");
            using (var reader = CreateReader(Crlf(physicalLines.ToArray()), bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[0].Value.Should().Be(expectedValue);
                testResult[1].Value.Should().Be("after");
                testResult[1].PhysicalLineNumber.Should().Be(physicalLines.Count);
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
                reader.ReadLine().Should().BeNull();
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
            // "€" is E2 82 AC and "😀" is F0 9F 98 80.  Fold after the first octet of one and inside the other twice.
            var data = Concat(
                Encoding.UTF8.GetBytes("SUMMARY:caf"), new byte[] { 0xE2 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0x82, 0xAC },
                Encoding.UTF8.GetBytes(" "), new byte[] { 0xF0, 0x9F }, Encoding.ASCII.GetBytes("\r\n\t"), new byte[] { 0x98 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0x80 },
                Encoding.UTF8.GetBytes("!\r\nX-NEXT:é\r\n"));
            using (var reader = CreateStreamReader(data, streamBufferSize: streamBufferSize, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be("caf€ 😀!");
                testResult[1].Value.Should().Be("é");
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
                testResult.Value.Should().Be("€");
                reader.PhysicalLinesRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadLine_FoldInsideUtf8SequenceFromFile_RestoresCharacter()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllBytes(testFilePath, Concat(Encoding.UTF8.GetBytes("SUMMARY:"), new byte[] { 0xE2, 0x82 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0xAC }));
                using (var reader = new IcalContentLineReader(testFilePath))
                {
                    // act
                    var testResult = reader.ReadLine();

                    // assert
                    testResult.Value.Should().Be("€");
                }
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void ReadLine_Utf16StreamWithByteOrderMark_IsDecoded()
        {
            // arrange
            var data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Crlf("SUMMARY:caf", " é ☕"))).ToArray();
            using (var reader = CreateStreamReader(data))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("café ☕");
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
            using (var stream = new MemoryStream(IcalTestData.Latin1.GetBytes(Crlf("SUMMARY:caf", " é"))))
            using (var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 }))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("café");
            }
        }

        #endregion

        #region Line breaks

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

        [Fact]
        public void ReadLine_AfterLineBreakError_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = CreateReader("SUMMARY:a\nX-B:d\r\n"))
            {
                Action firstAction = () => reader.ReadLine();
                firstAction.Should().Throw<IcalFileException>();

                // act
                Action readLineAction = () => reader.ReadLine();
                Action readContentLineAction = () => reader.ReadContentLine();
                Action currentLineAction = () => _ = reader.CurrentLine;

                // assert
                readLineAction.Should().Throw<InvalidOperationException>();
                readContentLineAction.Should().Throw<InvalidOperationException>();
                currentLineAction.Should().Throw<InvalidOperationException>();
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
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("SUMMARY");
                reader.ReadLine().Name.Should().Be("X-C");
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
                reader.ReadLine().Should().BeNull();
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
                testResult.Select(line => line.Value).Should().Equal("VCALENDAR", "VCALENDAR");
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
            // arrange
            // Each physical line is within the limit; the logical line is not.
            using (var reader = CreateReader(Crlf("X-A:1", "SUMMARY:12", " 345"), new IcalOptions { MaxLineLength = 12 }, bufferSize))
            {
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                Action nextAction = () => reader.ReadLine();
                nextAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_NoMaxLineLength_ReadsVeryLongLine()
        {
            // arrange
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
        public void Constructor_MaxLineLengthLessThanOne_Throws(int maxLineLength)
        {
            // arrange
            var options = new IcalOptions { MaxLineLength = maxLineLength };

            // act
            Action textReaderAction = () => new IcalContentLineReader(new StringReader(string.Empty), options);
            Action streamAction = () => new IcalContentLineReader(new MemoryStream(), options);

            // assert
            textReaderAction.Should().Throw<ArgumentException>().WithParameterName("options");
            streamAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        #endregion

        #region Syntax errors

        [Theory]
        [InlineData("", "empty", 1)]
        [InlineData(":value", "property name", 1)]
        [InlineData("SUMMARY", "':'", 1)]
        [InlineData("SUMMARY value", "';' or ':'", 1)]
        [InlineData("SUM_MARY:value", "';' or ':'", 1)]
        [InlineData("SUMMARY;:value", "parameter name", 1)]
        [InlineData("SUMMARY;X-P:value", "'='", 1)]
        [InlineData("SUMMARY;X-P", "'='", 1)]
        [InlineData("SUMMARY;X-P=a", "':'", 1)]
        [InlineData("SUMMARY;X-P=\"a:value", "closing", 1)]
        [InlineData("SUMMARY;X-P=\"a\"b:value", "',', ';' or ':'", 1)]
        [InlineData("SUMMARY;X-P=\"a\"", "',', ';' or ':'", 1)]
        [InlineData("SUMMARY;X-P=a\"b\":value", "U+0022", 1)]
        [InlineData("SUMMARY;X-P=\"a\u0001\":value", "U+0001", 1)]
        [InlineData("SUMMARY;X-P=a\u007F:value", "U+007F", 1)]
        [InlineData("SUMMARY:a\u0000b", "U+0000", 1)]
        [InlineData("SUMMARY:a\u001Fb", "U+001F", 1)]
        [InlineData("SUMMARY:a\u007Fb", "U+007F", 1)]
        public void ReadLine_InvalidSyntax_Throws(string line, string expectedMessagePart, long expectedPhysicalLineNumber)
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
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(expectedPhysicalLineNumber);

                // The bad line has been consumed.
                reader.LinesRead.Should().Be(1);
                reader.ReadLine().Name.Should().Be("X-NEXT");
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
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
            }
        }

        [Fact]
        public void ReadContentLine_SyntaxError_LeavesNoCurrentLine()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", "BAD")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => reader.ReadContentLine();

                // assert
                testAction.Should().Throw<IcalFileException>();
                Action currentLineAction = () => _ = reader.CurrentLine;
                currentLineAction.Should().Throw<InvalidOperationException>();
                Action viewAction = () => view.Name.ToString();
                viewAction.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region View and consistency

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
        public void CurrentLine_AfterEndOfInput_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = reader.ReadContentLine();

                // assert
                testResult.Should().BeFalse();
                Action currentLineAction = () => _ = reader.CurrentLine;
                currentLineAction.Should().Throw<InvalidOperationException>();
                Action viewAction = () => view.Name.ToString();
                viewAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_AfterReadLine_IsLineJustRead()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a")))
            {
                // act
                var line = reader.ReadLine();
                var testResult = reader.CurrentLine;

                // assert
                testResult.Name.ToString().Should().Be(line.Name);
                testResult.RawValue.ToString().Should().Be(line.Value);
            }
        }

        [Fact]
        public void CurrentLine_ViewOfEarlierLine_ThrowsOnEveryMember()
        {
            // arrange
            using (var reader = CreateReader(Crlf("X-A;X-P=a:b", "X-B:c")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;
                reader.ReadContentLine();

                // act
                var testActions = GetViewMemberActions(view);

                // assert
                foreach (var testAction in testActions)
                {
                    testAction.Should().Throw<InvalidOperationException>();
                }

                reader.CurrentLine.Name.ToString().Should().Be("X-B");
            }
        }

        [Fact]
        public void CurrentLine_ViewAfterDispose_ThrowsObjectDisposedOnEveryMember()
        {
            // arrange
            var reader = CreateReader(Crlf("X-A;X-P=a:b"));
            reader.ReadContentLine();
            var view = reader.CurrentLine;
            reader.Dispose();

            // act
            var testActions = GetViewMemberActions(view);

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void DefaultView_Members_Throw()
        {
            // arrange
            var view = default(IcalContentLineView);

            // act
            var testActions = GetViewMemberActions(view);

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_MemoryAccessors_MatchSpanAccessors()
        {
            // arrange
            using (var reader = CreateReader(Crlf("ATTENDEE;CN=a^'b;ROLE=\"X\",Y:mailto:a\\,b")))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act & assert
                view.NameMemory.ToString().Should().Be(view.Name.ToString());
                view.RawLineMemory.ToString().Should().Be(view.RawLine.ToString());
                view.RawValueMemory.ToString().Should().Be(view.RawValue.ToString());
                view.GetTextValueMemory().ToString().Should().Be(view.GetTextValue().ToString());
                for (var parameterIndex = 0; parameterIndex < view.ParameterCount; parameterIndex++)
                {
                    view.GetParameterNameMemory(parameterIndex).ToString().Should().Be(view.GetParameterName(parameterIndex).ToString());
                    for (var valueIndex = 0; valueIndex < view.GetParameterValueCount(parameterIndex); valueIndex++)
                    {
                        view.GetParameterValueMemory(parameterIndex, valueIndex).ToString().Should().Be(view.GetParameterValue(parameterIndex, valueIndex).ToString());
                        view.GetRawParameterValueMemory(parameterIndex, valueIndex).ToString().Should().Be(view.GetRawParameterValue(parameterIndex, valueIndex).ToString());
                    }
                }
            }
        }

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

        [Theory]
        [MemberData(nameof(ConsistencyData))]
        public void ReadContentLineAndReadLine_EveryFixture_Agree(string fixtureName, int bufferSize)
        {
            // arrange
            var text = RfcFixtures.All[fixtureName];
            using (var viewReader = CreateReader(text, bufferSize: bufferSize))
            using (var lineReader = CreateReader(text, bufferSize: 16 * 1024))
            {
                // act
                var viewLines = new List<LineSnapshot>();
                while (viewReader.ReadContentLine())
                {
                    viewLines.Add(LineSnapshot.FromView(viewReader.CurrentLine));
                }

                var lines = ReadAll(lineReader).Select(LineSnapshot.FromLine).ToList();

                // assert
                lines.Should().NotBeEmpty();
                viewLines.Should().BeEquivalentTo(lines, options => options.WithStrictOrdering());
                viewReader.LinesRead.Should().Be(lineReader.LinesRead);
                viewReader.PhysicalLinesRead.Should().Be(lineReader.PhysicalLinesRead);
            }
        }

        private static Func<object>[] GetViewMemberActions(IcalContentLineView view)
        {
            return new Func<object>[]
            {
                () => view.LineNumber,
                () => view.PhysicalLineNumber,
                () => view.RawLine.ToString(),
                () => view.RawLineMemory,
                () => view.Name.ToString(),
                () => view.NameMemory,
                () => view.NameEquals("X-A".AsSpan()),
                () => view.ParameterCount,
                () => view.RawValue.ToString(),
                () => view.RawValueMemory,
                () => view.GetParameterName(0).ToString(),
                () => view.GetParameterNameMemory(0),
                () => view.IndexOfParameter("X-P".AsSpan()),
                () => view.GetParameterValueCount(0),
                () => view.GetRawParameterValue(0, 0).ToString(),
                () => view.GetRawParameterValueMemory(0, 0),
                () => view.GetParameterValue(0, 0).ToString(),
                () => view.GetParameterValueMemory(0, 0),
                () => view.IsParameterValueQuoted(0, 0),
                () => view.GetTextValue().ToString(),
                () => view.GetTextValueMemory(),
                () => view.ToContentLine()
            };
        }

        #endregion

        #region Counters

        [Fact]
        public void Counters_AfterEachRead_CountLogicalAndPhysicalLines()
        {
            // arrange
            using (var reader = CreateReader(Crlf("A:1", "B:2", " 3", "  4", "C:5")))
            {
                // act & assert
                reader.ReadLine();
                reader.LinesRead.Should().Be(1);
                reader.PhysicalLinesRead.Should().Be(1);
                reader.ReadLine();
                reader.LinesRead.Should().Be(2);
                reader.PhysicalLinesRead.Should().Be(4);
                reader.ReadLine().PhysicalLineNumber.Should().Be(5);
                reader.LinesRead.Should().Be(3);
                reader.PhysicalLinesRead.Should().Be(5);
                reader.ReadLine().Should().BeNull();
                reader.LinesRead.Should().Be(3);
                reader.PhysicalLinesRead.Should().Be(5);
            }
        }

        #endregion

        #region Disposal

        [Fact]
        public void Members_AfterDispose_ThrowObjectDisposed()
        {
            // arrange
            var reader = CreateReader(Crlf("SUMMARY:a"));
            reader.ReadContentLine();
            reader.Dispose();

            // act
            var testActions = new Func<object>[]
            {
                () => reader.ReadLine(),
                () => reader.ReadContentLine(),
                () => reader.ReadLines(),
                () => reader.CurrentLine,
                () => reader.LinesRead,
                () => reader.PhysicalLinesRead
            };

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineReader));
            }
        }

        [Fact]
        public void ReadLines_DisposedDuringEnumeration_ThrowsObjectDisposed()
        {
            // arrange
            var reader = CreateReader(Crlf("A:1", "B:2"));
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
            using (var textReader = new StringReader(Crlf("A:1", "B:2")))
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
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Crlf("A:1"))))
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
            using (var stream = new MemoryStream(IcalTestData.Latin1.GetBytes(Crlf("A:1"))))
            {
                var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 });
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                stream.CanRead.Should().BeTrue();
            }
        }

        [Fact]
        public void Dispose_FilePath_ClosesFile()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(testFilePath, Crlf("A:1"));
                var reader = new IcalContentLineReader(testFilePath);
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                Action openAction = () => new FileStream(testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose();
                openAction.Should().NotThrow();
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        #endregion

        #region Constructors

        [Fact]
        public void Constructor_NullArguments_Throw()
        {
            // arrange
            var options = new IcalOptions();

            // act & assert
            ((Action)(() => new IcalContentLineReader((Stream)null))).Should().Throw<ArgumentNullException>().WithParameterName("icalStream");
            ((Action)(() => new IcalContentLineReader((Stream)null, options))).Should().Throw<ArgumentNullException>().WithParameterName("icalStream");
            ((Action)(() => new IcalContentLineReader(new MemoryStream(), null))).Should().Throw<ArgumentNullException>().WithParameterName("options");
            ((Action)(() => new IcalContentLineReader((string)null))).Should().Throw<ArgumentNullException>().WithParameterName("icalFilePath");
            ((Action)(() => new IcalContentLineReader((string)null, options))).Should().Throw<ArgumentNullException>().WithParameterName("icalFilePath");
            ((Action)(() => new IcalContentLineReader("unused.ics", null))).Should().Throw<ArgumentNullException>().WithParameterName("options");
            ((Action)(() => new IcalContentLineReader((TextReader)null))).Should().Throw<ArgumentNullException>().WithParameterName("reader");
            ((Action)(() => new IcalContentLineReader((TextReader)null, options))).Should().Throw<ArgumentNullException>().WithParameterName("reader");
            ((Action)(() => new IcalContentLineReader(new StringReader(string.Empty), null))).Should().Throw<ArgumentNullException>().WithParameterName("options");
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
        public void Constructor_Stream_UsesStreamBufferSize()
        {
            // arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Crlf("A:1"))))
            {
                // act
                using (var reader = new IcalContentLineReader(stream))
                {
                    // assert
                    var streamReader = ReaderInspector.GetUnderlyingReader(reader).Should().BeOfType<StreamReader>().Subject;
                    ReaderInspector.GetBufferSize(streamReader).Should().Be(IcalContentLineReader.StreamBufferSize);
                    streamReader.BaseStream.Should().BeOfType<Utf8FoldRealigningStream>();
                }
            }
        }

        [Fact]
        public void Constructor_FilePath_UsesStreamBufferSize()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                // act
                using (var reader = new IcalContentLineReader(testFilePath))
                {
                    // assert
                    var streamReader = ReaderInspector.GetUnderlyingReader(reader).Should().BeOfType<StreamReader>().Subject;
                    ReaderInspector.GetBufferSize(streamReader).Should().Be(IcalContentLineReader.StreamBufferSize);
                    var foldRealigningStream = streamReader.BaseStream.Should().BeOfType<Utf8FoldRealigningStream>().Subject;
                    var fileStream = ReaderInspector.GetInnerStream(foldRealigningStream).Should().BeOfType<FileStream>().Subject;
                    ReaderInspector.GetBufferSize(fileStream).Should().Be(IcalContentLineReader.StreamBufferSize);
                }
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Constructor_StreamWithOtherEncoding_DoesNotRealignFolds()
        {
            // arrange
            using (var stream = new MemoryStream(IcalTestData.Latin1.GetBytes(Crlf("A:1"))))
            {
                // act
                using (var reader = new IcalContentLineReader(stream, new IcalOptions { Encoding = IcalTestData.Latin1 }))
                {
                    // assert
                    var streamReader = ReaderInspector.GetUnderlyingReader(reader).Should().BeOfType<StreamReader>().Subject;
                    streamReader.BaseStream.Should().BeSameAs(stream);
                    streamReader.CurrentEncoding.Should().Be(IcalTestData.Latin1);
                }
            }
        }

        #endregion

        private static byte[] Concat(params byte[][] parts)
        {
            return parts.SelectMany(part => part).ToArray();
        }
    }
}
