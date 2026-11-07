using System;
using System.IO;
using System.Text;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the synchronous members of <see cref="VcardContentLineReader"/>.
    /// </summary>
    public class VcardContentLineReaderTests
    {
        private const string Telephone = "TEL";
        private const string Group = "item1";
        private const string LineA = "A:1";
        private const string LineB = "B:2";
        private const string SyntaxErrorLine = "BAD";
        private const string NameOfLineA = "A";
        private const string NameOfLineB = "B";
        private const string ValueOfLineA = "1";

        // A grouped line with a parameter and a value, for the tests of a view's members.
        private const string GroupedLineWithParameter = "item1.TEL;P=1:2\r\n";

        // U+FEFF, the byte order mark as a character, before LineA.
        private const string ByteOrderMarkCharacterLine = "\xFEFFA:1\r\n";

        // ref. RFC 6350 §3.3: "BEGIN:VCARD", "VERSION:4.0" and "END:VCARD".
        private const string BeginCard = "BEGIN:VCARD";
        private const string Version40 = "VERSION:4.0";
        private const string EndCard = "END:VCARD";

        // ref. RFC 2426 §4: the VERSION of a vCard 3.0 card.  The vCard 2.1 specification §2.1.1 has the same form.
        private const string Version30 = "VERSION:3.0";
        private const string Version21 = "VERSION:2.1";

        // ref. RFC 6868 §3: ^n is a newline, ^' a double quote and ^^ a caret.
        private const string CaretLine = "X-A;P=a^nb^'c^^d:v";
        private const string CaretValueDecoded = "a\nb\"c^d";
        private const string CaretValueRaw = "a^nb^'c^^d";

        // "A:" and the euro sign in UTF-8, E2 82 AC, with a CRLF and a fold's space between its second and third octets; then a line break.
        private static readonly byte[] FoldInsideEuroSign = { 0x41, 0x3A, 0xE2, 0x82, 0x0D, 0x0A, 0x20, 0xAC, 0x0D, 0x0A };
        private const string EuroSign = "\x20AC";

        // "A:1" and a line break, in UTF-8 after its byte order mark, EF BB BF.
        private static readonly byte[] Utf8ByteOrderMarkLine = { 0xEF, 0xBB, 0xBF, 0x41, 0x3A, 0x31, 0x0D, 0x0A };

        // "A:1" and a line break, in UTF-16 LE after its byte order mark, FF FE.
        private static readonly byte[] Utf16ByteOrderMarkLine = { 0xFF, 0xFE, 0x41, 0x00, 0x3A, 0x00, 0x31, 0x00, 0x0D, 0x00, 0x0A, 0x00 };

        // "A:" and the byte FF, which never appears in UTF-8 (RFC 3629 §1), and a line break.
        private static readonly byte[] InvalidUtf8Line = { 0x41, 0x3A, 0xFF, 0x0D, 0x0A };
        private const string LatinSmallYWithDiaeresis = "\xFF";
        private const string ReplacementCharacter = "\xFFFD";

        public static TheoryData<int, int> StreamBufferSizeData()
        {
            var data = new TheoryData<int, int>();
            foreach (var streamBufferSize in new[] { 1, 2, 3, 5, 7, DefaultBufferSize })
            {
                foreach (var bufferSize in new[] { 1, 7, DefaultBufferSize })
                {
                    data.Add(streamBufferSize, bufferSize);
                }
            }

            return data;
        }

        // Each is a whole card that the reader must not decode carets in, with the last line read being the caret line.
        public static TheoryData<string> CaretNotDecodedData()
        {
            return new TheoryData<string>
            {
                { Crlf(BeginCard, Version30, CaretLine) },
                { Crlf(BeginCard, Version21, CaretLine) },
                { Crlf(BeginCard, "VERSION:5.0", CaretLine) },
                { Crlf(BeginCard, CaretLine) },
                { Crlf(CaretLine) },
                { Crlf(Version40, CaretLine) },
                { Crlf(BeginCard, Version40, EndCard, CaretLine) },
                { Crlf(BeginCard, Version40, EndCard, BeginCard, CaretLine) },
                { Crlf(BeginCard, Version40, EndCard, BeginCard, Version30, CaretLine) },
                { Crlf(BeginCard, Version40, Version30, CaretLine) }
            };
        }

        // Each is a card that the reader must decode carets in, with the last line read being the caret line.
        public static TheoryData<string> CaretDecodedData()
        {
            return new TheoryData<string>
            {
                { Crlf(BeginCard, Version40, CaretLine) },
                { Crlf("begin:vcard", "version:4.0", CaretLine) },
                { Crlf("a.BEGIN:VCARD", "a.VERSION:4.0", CaretLine) },
                { Crlf("BEGIN:VCARD", "VERSION;VALUE=text:4.0", CaretLine) },
                { Crlf(BeginCard, Version30, EndCard, BeginCard, Version40, CaretLine) },
                { Crlf(BeginCard, Version30, Version40, CaretLine) }
            };
        }

        public static TheoryData<string> MalformedLineData()
        {
            return new TheoryData<string>
            {
                { string.Empty },
                { "NO-COLON" },
                { ":value" },
                { "A B:value" },
                { "A;:value" },
                { "A;P:value" },
                { "A;P=\"x:value" },
                { "A;P=\"x\"y:value" },
                { "A;P=x\"y:value" },
                { "A;P=\x01:value" },
                { "A:\x01" },
                { "A:\x7F" },
                { "A:bare\rreturn" },
                { ".A:value" },
                { "a.b.A:value" },
                { "a.:value" }
            };
        }

        public static TheoryData<Func<VcardContentLineReader, object>> MembersAfterDispose()
        {
            return new TheoryData<Func<VcardContentLineReader, object>>
            {
                reader => reader.ReadLine(),
                reader => reader.ReadContentLine(),
                reader => reader.ReadLines(),
                reader => reader.CurrentLine,
                reader => reader.LinesRead,
                reader => reader.PhysicalLinesRead,
                reader => reader.ReadLineAsync(),
                reader => reader.ReadContentLineAsync(),
                reader => reader.ReadLinesAsync()
            };
        }

        public static TheoryData<Func<VcardContentLineView, object>> ViewMembers()
        {
            return new TheoryData<Func<VcardContentLineView, object>>
            {
                view => view.LineNumber,
                view => view.PhysicalLineNumber,
                view => view.RawLine.Length,
                view => view.RawLineMemory,
                view => view.Group.Length,
                view => view.GroupMemory,
                view => view.Name.Length,
                view => view.NameMemory,
                view => view.ParameterCount,
                view => view.RawValue.Length,
                view => view.RawValueMemory,
                view => view.GroupEquals(Group.AsSpan()),
                view => view.NameEquals(Telephone.AsSpan()),
                view => view.GetParameterName(0).Length,
                view => view.GetParameterNameMemory(0),
                view => view.IndexOfParameter("P".AsSpan()),
                view => view.GetParameterValueCount(0),
                view => view.GetRawParameterValue(0, 0).Length,
                view => view.GetRawParameterValueMemory(0, 0),
                view => view.GetParameterValue(0, 0).Length,
                view => view.GetParameterValueMemory(0, 0),
                view => view.IsParameterValueQuoted(0, 0),
                view => view.GetTextValue().Length,
                view => view.GetTextValueMemory(),
                view => view.ToContentLine()
            };
        }

        #region Folding

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLine_FoldedWithSpaceAndTabAcrossRefills_Unfolds(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("A:12\r\n 34\r\n\t56\r\n", bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("123456");
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLine_FoldedLine_CountsLogicalAndPhysicalLines(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("A:12\r\n 34\r\n\t56\r\nB:2\r\n", bufferSize: bufferSize))
            {
                // act
                var testResult = LineDescription.OfAll(ReadAll(reader));
                var linesRead = reader.LinesRead;
                var physicalLinesRead = reader.PhysicalLinesRead;

                // assert
                testResult.Should().Equal("1/1 A:123456", "2/4 B:2");
                linesRead.Should().Be(2);
                physicalLinesRead.Should().Be(4);
            }
        }

        [Theory]
        [InlineData(Rfc6350Examples.NoteFoldedOnce)]
        [InlineData(Rfc6350Examples.NoteFoldedTwice)]
        public void ReadLine_Rfc6350FoldExamples_GiveTheUnfoldedNote(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(Rfc6350Examples.NoteValue);
            }
        }

        [Theory]
        [MemberData(nameof(StreamBufferSizeData))]
        public void ReadLine_FoldInsideUtf8SequenceFromStream_RestoresCharacter(int streamBufferSize, int bufferSize)
        {
            // arrange
            using (var reader = CreateStreamReader(FoldInsideEuroSign, streamBufferSize: streamBufferSize, bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();
                var physicalLinesRead = reader.PhysicalLinesRead;

                // assert
                testResult.Value.Should().Be(EuroSign);
                physicalLinesRead.Should().Be(2);
            }
        }

        #endregion

        #region Groups

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLine_GroupedLine_GivesGroupAndName(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader("item1.TEL:1\r\n", bufferSize: bufferSize))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Group.Should().Be(Group);
                testResult.Name.Should().Be(Telephone);
            }
        }

        [Fact]
        public void ReadLine_UngroupedLine_HasNullGroup()
        {
            // arrange
            using (var reader = CreateReader("TEL:1\r\n"))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Group.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("item1.X-CUSTOM:1\r\n", "item1", "X-CUSTOM")]
        [InlineData("a-1.UNKNOWN:1\r\n", "a-1", "UNKNOWN")]
        public void ReadLine_GroupOnUnknownOrExtensionProperty_GivesGroupAndName(string text, string expectedGroup, string expectedName)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Group.Should().Be(expectedGroup);
                testResult.Name.Should().Be(expectedName);
            }
        }

        [Fact]
        public void GroupEquals_DifferentCase_IsTrue()
        {
            // arrange
            using (var reader = CreateReader("ITEM1.tel:1\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GroupEquals(Group.AsSpan());

                // assert
                testResult.Should().BeTrue();
            }
        }

        [Fact]
        public void NameEquals_DifferentCase_IsTrue()
        {
            // arrange
            using (var reader = CreateReader("ITEM1.tel:1\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.NameEquals(Telephone.AsSpan());

                // assert
                testResult.Should().BeTrue();
            }
        }

        [Fact]
        public void GroupEquals_OtherGroup_IsFalse()
        {
            // arrange
            using (var reader = CreateReader("item2.TEL:1\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GroupEquals(Group.AsSpan());

                // assert
                testResult.Should().BeFalse();
            }
        }

        [Fact]
        public void Group_ViewOfUngroupedLine_IsEmpty()
        {
            // arrange
            using (var reader = CreateReader("TEL:1\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.Group.Length;

                // assert
                testResult.Should().Be(0);
            }
        }

        #endregion

        #region Parameters

        [Fact]
        public void ReadLine_QuotedParameterValueWithDelimiters_KeepsThem()
        {
            // arrange
            using (var reader = CreateReader("A;P=\"x:y;z,w\":v\r\n"))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("x:y;z,w");
                testResult.Value.Should().Be("v");
            }
        }

        [Fact]
        public void ReadLine_CommaSeparatedValues_GivesEachValue()
        {
            // arrange
            using (var reader = CreateReader("A;TYPE=work,\"voice,x\",home:v\r\n"))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("work", "voice,x", "home");
            }
        }

        [Fact]
        public void ReadLine_EmptyParameterValue_IsAccepted()
        {
            // arrange
            using (var reader = CreateReader("A;P=:v\r\n"))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal(string.Empty);
            }
        }

        [Fact]
        public void GetParameter_DifferentCase_FindsParameter()
        {
            // arrange
            using (var reader = CreateReader("A;type=work:v\r\n"))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.GetParameter("TYPE");

                // assert
                testResult.Values.Should().Equal("work");
            }
        }

        [Fact]
        public void GetParameter_NoSuchParameter_IsNull()
        {
            // arrange
            using (var reader = CreateReader("A;type=work:v\r\n"))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.GetParameter("PREF");

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void IsParameterValueQuoted_QuotedAndUnquoted_TellsThemApart()
        {
            // arrange
            using (var reader = CreateReader("A;P=\"x\",y:v\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = new[] { view.IsParameterValueQuoted(0, 0), view.IsParameterValueQuoted(0, 1) };

                // assert
                testResult.Should().Equal(true, false);
            }
        }

        [Fact]
        public void IndexOfParameter_DifferentCase_FindsParameter()
        {
            // arrange
            using (var reader = CreateReader("A;P=1;q=2:v\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.IndexOfParameter("Q".AsSpan());

                // assert
                testResult.Should().Be(1);
            }
        }

        [Fact]
        public void IndexOfParameter_NoSuchParameter_IsMinusOne()
        {
            // arrange
            using (var reader = CreateReader("A;P=1:v\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.IndexOfParameter("Q".AsSpan());

                // assert
                testResult.Should().Be(-1);
            }
        }

        [Fact]
        public void GetParameterName_IndexOutOfRange_Throws()
        {
            // arrange
            using (var reader = CreateReader("A:v\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetParameterNameMemory(0);

                // assert
                testAction.Should().Throw<ArgumentOutOfRangeException>();
            }
        }

        #endregion

        #region Caret encoding

        [Theory]
        [MemberData(nameof(CaretDecodedData))]
        public void ReadLine_LineOfVersion40Card_DecodesCarets(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = ReadLast(reader);

                // assert
                testResult.Parameters[0].Values.Should().Equal(CaretValueDecoded);
            }
        }

        [Theory]
        [MemberData(nameof(CaretNotDecodedData))]
        public void ReadLine_LineNotInVersion40Card_LeavesCarets(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = ReadLast(reader);

                // assert
                testResult.Parameters[0].Values.Should().Equal(CaretValueRaw);
            }
        }

        [Theory]
        [MemberData(nameof(CaretNotDecodedData))]
        public void ReadLine_AcceptCaretEncodingInAllVersions_DecodesCarets(string text)
        {
            // arrange
            var options = new VcardOptions { AcceptCaretEncodingInAllVersions = true };
            using (var reader = CreateReader(text, options))
            {
                // act
                var testResult = ReadLast(reader);

                // assert
                testResult.Parameters[0].Values.Should().Equal(CaretValueDecoded);
            }
        }

        [Fact]
        public void GetParameterValue_ViewOfLineInVersion40Card_DecodesCarets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, CaretLine)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GetParameterValue(0, 0).ToString();

                // assert
                testResult.Should().Be(CaretValueDecoded);
            }
        }

        [Fact]
        public void GetParameterValue_ViewOfLineInVersion30Card_LeavesCarets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, CaretLine)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GetParameterValue(0, 0).ToString();

                // assert
                testResult.Should().Be(CaretValueRaw);
            }
        }

        [Fact]
        public void ToContentLine_ViewOfLineInVersion30Card_LeavesCarets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, CaretLine)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.ToContentLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal(CaretValueRaw);
            }
        }

        [Fact]
        public void GetRawParameterValue_ViewOfLineInVersion40Card_KeepsCarets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, CaretLine)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GetRawParameterValue(0, 0).ToString();

                // assert
                testResult.Should().Be(CaretValueRaw);
            }
        }

        [Fact]
        public void ReadLine_VersionLineOwnParameter_IsDecodedInVersion40()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, "VERSION;X-A=^^:4.0")))
            {
                reader.ReadLine();

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("^");
            }
        }

        [Fact]
        public void ReadLine_EndLineOwnParameter_IsDecodedInVersion40()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "END;X-A=^^:VCARD")))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[2].Parameters[0].Values.Should().Equal("^");
            }
        }

        [Fact]
        public void ReadLine_BeginWithOtherValue_DoesNotStartACard()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "BEGIN:OTHER", CaretLine)))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[3].Parameters[0].Values.Should().Equal(CaretValueDecoded);
            }
        }

        [Fact]
        public void ReadLine_EndWithOtherValue_DoesNotEndTheCard()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "END:OTHER", CaretLine)))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[3].Parameters[0].Values.Should().Equal(CaretValueDecoded);
            }
        }

        #endregion

        #region TEXT unescaping

        [Theory]
        [InlineData("A:a\\\\b\r\n", "a\\b")]
        [InlineData("A:a\\,b\r\n", "a,b")]
        [InlineData("A:a\\;b\r\n", "a;b")]
        [InlineData("A:a\\nb\r\n", "a\nb")]
        [InlineData("A:a\\Nb\r\n", "a\nb")]
        [InlineData("A:plain\r\n", "plain")]
        [InlineData(Rfc6350Examples.EscapedFormattedName, Rfc6350Examples.FormattedName)]
        public void GetTextValue_ValidEscapes_Unescapes(string text, string expected)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                var line = reader.ReadLine();

                // act
                var testResult = line.GetTextValue();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("A:a\\\\b\r\n", "a\\b")]
        [InlineData("A:a\\,b\r\n", "a,b")]
        [InlineData("A:a\\;b\r\n", "a;b")]
        [InlineData("A:a\\nb\r\n", "a\nb")]
        [InlineData("A:a\\Nb\r\n", "a\nb")]
        public void GetTextValue_ViewWithValidEscapes_Unescapes(string text, string expected)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.GetTextValue().ToString();

                // assert
                testResult.Should().Be(expected);
            }
        }

        // ref. RFC 6350 §3.4 and §4: "\:" and the like are not among the escapes.  Older producers write them, but accepting them is for #304's
        // AcceptLegacySyntax to decide; by default they are errors.
        [Theory]
        [InlineData("A:a\\:b\r\n")]
        [InlineData("A:a\\tb\r\n")]
        [InlineData("A:a\\\r\n")]
        public void GetTextValue_OtherBackslash_Throws(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                var line = reader.ReadLine();

                // act
                Action testAction = () => line.GetTextValue();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Theory]
        [InlineData("A:a\\:b\r\n")]
        [InlineData("A:a\\tb\r\n")]
        [InlineData("A:a\\\r\n")]
        public void GetTextValue_ViewWithOtherBackslash_Throws(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetTextValueMemory();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void GetTextValue_InvalidEscapeOnFoldedLineInCard_ReportsWhereItIs()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, EndCard, BeginCard, "item1.NOTE:ab", " c\\:d")))
            {
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                var line = reader.ReadLine();

                // act
                Action testAction = () => line.GetTextValue();

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Data[VcardFileException.DataItemName.LineNumber].Should().Be(4L);
                exception.Data[VcardFileException.DataItemName.PhysicalLineNumber].Should().Be(5L);
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be("NOTE");
                exception.Data[VcardFileException.DataItemName.Group].Should().Be(Group);
                exception.Data[VcardFileException.DataItemName.CardIndex].Should().Be(1);
            }
        }

        [Fact]
        public void GetTextValue_ViewWithInvalidEscape_ReportsGroupAndCard()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, "item1.NOTE:a\\:b")))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetTextValueMemory();

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Data[VcardFileException.DataItemName.Group].Should().Be(Group);
                exception.Data[VcardFileException.DataItemName.CardIndex].Should().Be(0);
            }
        }

        #endregion

        #region Line breaks and limits

        [Fact]
        public void ReadLine_BareLineFeed_Throws()
        {
            // arrange
            using (var reader = CreateReader("A:1\nB:2\r\n"))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_AfterBareLineFeed_CannotContinue()
        {
            // arrange
            using (var reader = CreateReader("A:1\nB:2\r\n"))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_AcceptBareLineFeed_ReadsLines()
        {
            // arrange
            var options = new VcardOptions { AcceptBareLineFeed = true };
            using (var reader = CreateReader("A:1\n 2\nB:2\n", options))
            {
                // act
                var testResult = LineDescription.OfAll(ReadAll(reader));

                // assert
                testResult.Should().Equal("1/1 A:12", "2/3 B:2");
            }
        }

        [Fact]
        public void ReadLine_NoFinalLineBreak_Throws()
        {
            // arrange
            using (var reader = CreateReader("A:1"))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_AfterMissingFinalLineBreak_ReachesTheEnd()
        {
            // arrange
            using (var reader = CreateReader("A:1"))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadLine_AcceptMissingFinalLineBreak_ReadsLine()
        {
            // arrange
            var options = new VcardOptions { AcceptMissingFinalLineBreak = true };
            using (var reader = CreateReader("A:1", options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(ValueOfLineA);
            }
        }

        [Theory]
        [InlineData("A:12345\r\n")]
        [InlineData("A:123\r\n 45\r\n")]
        public void ReadLine_LineLongerThanMaxLineLength_Throws(string text)
        {
            // arrange
            const int maxLineLength = 6;
            var options = new VcardOptions { MaxLineLength = maxLineLength };
            using (var reader = CreateReader(text, options))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_LineAsLongAsMaxLineLength_IsRead()
        {
            // arrange
            const int maxLineLength = 7;
            var options = new VcardOptions { MaxLineLength = maxLineLength };
            using (var reader = CreateReader("A:12345\r\n", options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("12345");
            }
        }

        [Fact]
        public void ReadLine_AfterLineTooLong_CannotContinue()
        {
            // arrange
            const int maxLineLength = 2;
            var options = new VcardOptions { MaxLineLength = maxLineLength };
            using (var reader = CreateReader("A:12345\r\nB:2\r\n", options))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void MaxLineLength_Default_IsNull()
        {
            // arrange
            var options = new VcardOptions();

            // act
            var testResult = options.MaxLineLength;

            // assert
            testResult.Should().BeNull();
        }

        #endregion

        #region Syntax errors

        [Theory]
        [MemberData(nameof(MalformedLineData))]
        public void ReadLine_MalformedLine_Throws(string line)
        {
            // arrange
            using (var reader = CreateReader(Crlf(line)))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLine_AfterSyntaxError_ReadsTheNextLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf(SyntaxErrorLine, LineB), bufferSize: bufferSize))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be(NameOfLineB);
                testResult.LineNumber.Should().Be(2);
            }
        }

        [Fact]
        public void ReadLine_SyntaxErrorAfterGroupInCard_ReportsWhereItIs()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, "item1.TEL;BAD:1")))
            {
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Data[VcardFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[VcardFileException.DataItemName.PhysicalLineNumber].Should().Be(2L);
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Telephone);
                exception.Data[VcardFileException.DataItemName.Group].Should().Be(Group);
                exception.Data[VcardFileException.DataItemName.CardIndex].Should().Be(0);
            }
        }

        [Fact]
        public void ReadLine_SyntaxErrorOutsideCard_LeavesOutGroupAndCardIndex()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, EndCard, "TEL;BAD:1")))
            {
                reader.ReadLine();
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Data.Contains(VcardFileException.DataItemName.Group).Should().BeFalse();
                exception.Data.Contains(VcardFileException.DataItemName.CardIndex).Should().BeFalse();
            }
        }

        [Fact]
        public void ReadLine_SyntaxErrorOnThirdCardLine_CountsCardsFromZero()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, EndCard, BeginCard, EndCard, BeginCard, SyntaxErrorLine)))
            {
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.CardIndex].Should().Be(2);
            }
        }

        #endregion

        #region Encoding

        [Fact]
        public void ReadLine_Utf8FromStream_Decodes()
        {
            // arrange
            using (var reader = CreateStreamReader(Encoding.UTF8.GetBytes("A:\x20AC\r\n")))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(EuroSign);
            }
        }

        [Fact]
        public void ReadLine_InvalidUtf8FromStream_Throws()
        {
            // arrange
            using (var reader = CreateStreamReader(InvalidUtf8Line))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>().WithInnerException<DecoderFallbackException>();
            }
        }

        [Fact]
        public void ReadLine_AfterInvalidUtf8_CannotContinue()
        {
            // arrange
            using (var reader = CreateStreamReader(InvalidUtf8Line))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_Utf16DataWithByteOrderMark_Throws()
        {
            // arrange
            using (var reader = CreateStreamReader(Utf16ByteOrderMarkLine))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_Utf8ByteOrderMarkFromStream_Throws()
        {
            // arrange
            using (var reader = CreateStreamReader(Utf8ByteOrderMarkLine))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_Utf8ByteOrderMarkWithUtf8EncodingWithPreamble_Throws()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.UTF8 };
            using (var reader = CreateStreamReader(Utf8ByteOrderMarkLine, options))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_Utf16ByteOrderMarkWithUtf16Encoding_Throws()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.Unicode };
            using (var reader = CreateStreamReader(Utf16ByteOrderMarkLine, options))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_AcceptByteOrderMarkWithUtf8EncodingWithPreamble_SkipsIt()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.UTF8, AcceptByteOrderMark = true };
            using (var reader = CreateStreamReader(Utf8ByteOrderMarkLine, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be(NameOfLineA);
            }
        }

        [Fact]
        public void ReadLine_AcceptByteOrderMarkWithUtf16Encoding_SkipsIt()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.Unicode, AcceptByteOrderMark = true };
            using (var reader = CreateStreamReader(Utf16ByteOrderMarkLine, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be(NameOfLineA);
            }
        }

        [Fact]
        public void ReadLine_ByteOrderMarkCharacterFromTextReader_Throws()
        {
            // arrange
            using (var reader = CreateReader(ByteOrderMarkCharacterLine))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_AcceptByteOrderMarkFromStream_SkipsIt()
        {
            // arrange
            var options = new VcardOptions { AcceptByteOrderMark = true };
            using (var reader = CreateStreamReader(Utf8ByteOrderMarkLine, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be(NameOfLineA);
            }
        }

        [Fact]
        public void ReadLine_AcceptByteOrderMarkFromTextReader_SkipsIt()
        {
            // arrange
            var options = new VcardOptions { AcceptByteOrderMark = true };
            using (var reader = CreateReader(ByteOrderMarkCharacterLine, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Name.Should().Be(NameOfLineA);
            }
        }

        [Fact]
        public void ReadLine_ChosenEncoding_DecodesWithIt()
        {
            // arrange
            var options = new VcardOptions { Encoding = Latin1 };
            using (var reader = CreateStreamReader(InvalidUtf8Line, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(LatinSmallYWithDiaeresis);
            }
        }

        [Fact]
        public void ReadLine_ChosenReplacingUtf8Encoding_ReplacesInvalidData()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.UTF8 };
            using (var reader = CreateStreamReader(InvalidUtf8Line, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(ReplacementCharacter);
            }
        }

        [Fact]
        public void Constructor_TextReaderWithEncoding_Throws()
        {
            // arrange
            var options = new VcardOptions { Encoding = Encoding.UTF8 };
            var textReader = new StringReader(LineA);

            // act
            Action testAction = () => new VcardContentLineReader(textReader, options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ReadLine_DecoderFailureFromCallersTextReader_IsNotConverted()
        {
            // arrange
            var failure = new DecoderFallbackException();
            using (var reader = new VcardContentLineReader(new ThrowingTextReader(failure)))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<DecoderFallbackException>();
            }
        }

        [Fact]
        public void ReadLine_SourceThrows_PassesOnTheException()
        {
            // arrange
            var failure = new IOException();
            using (var reader = new VcardContentLineReader(new ThrowingTextReader(failure)))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<IOException>();
            }
        }

        [Fact]
        public void ReadLine_AfterSourceThrew_CannotContinue()
        {
            // arrange
            var failure = new IOException();
            using (var reader = new VcardContentLineReader(new ThrowingTextReader(failure)))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_AfterDecoderFailureFromCallersTextReader_CannotContinue()
        {
            // arrange
            var failure = new DecoderFallbackException();
            using (var reader = new VcardContentLineReader(new ThrowingTextReader(failure)))
            {
                Record.Exception(() => reader.ReadLine());

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region Constructors

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // arrange
            Stream stream = null;

            // act
            Action testAction = () => new VcardContentLineReader(stream);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("vcardStream");
        }

        [Fact]
        public void Constructor_NullTextReader_Throws()
        {
            // arrange
            TextReader textReader = null;

            // act
            Action testAction = () => new VcardContentLineReader(textReader);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("reader");
        }

        [Fact]
        public void Constructor_NullPath_Throws()
        {
            // arrange
            string path = null;

            // act
            Action testAction = () => new VcardContentLineReader(path);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("vcardFilePath");
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // arrange
            VcardOptions options = null;
            var stream = new MemoryStream();

            // act
            Action testAction = () => new VcardContentLineReader(stream, options);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_UnreadableStream_Throws()
        {
            // arrange
            var stream = new MemoryStream();
            stream.Dispose();

            // act
            Action testAction = () => new VcardContentLineReader(stream);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_MaxLineLengthBelowOne_Throws()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = 0 };
            var textReader = new StringReader(LineA);

            // act
            Action testAction = () => new VcardContentLineReader(textReader, options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Dispose_CallersStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream();
            var reader = new VcardContentLineReader(stream);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CallersTextReader_LeavesItOpen()
        {
            // arrange
            var textReader = new StringReader(Crlf(LineA));
            var reader = new VcardContentLineReader(textReader);

            // act
            reader.Dispose();

            // assert
            textReader.ReadLine().Should().Be(LineA);
        }

        #endregion

        #region Reading

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLines_Rfc6350Example_GivesItsLines(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Rfc6350Examples.Example, bufferSize: bufferSize))
            {
                // act
                var testResult = LineDescription.OfAll(ReadAll(reader));
                var physicalLinesRead = reader.PhysicalLinesRead;

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
                physicalLinesRead.Should().Be(Rfc6350Examples.ExamplePhysicalLineCount);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadContentLine_Rfc6350Example_ViewAgreesWithReadLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Rfc6350Examples.Example, bufferSize: bufferSize))
            {
                // act
                var testResult = DescribeViews(reader);

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
            }
        }

        [Theory]
        [MemberData(nameof(StreamBufferSizeData))]
        public void ReadLines_Rfc6350ExampleFromStream_GivesItsLines(int streamBufferSize, int bufferSize)
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(Rfc6350Examples.Example);
            using (var reader = CreateStreamReader(data, streamBufferSize: streamBufferSize, bufferSize: bufferSize))
            {
                // act
                var testResult = LineDescription.OfAll(ReadAll(reader));

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
            }
        }

        [Fact]
        public void ReadLine_Rfc6350AddressExample_SplitsQuotedParameters()
        {
            // arrange
            using (var reader = CreateReader(Rfc6350Examples.AddressWithQuotedParameters))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Parameters[0].Values.Should().Equal("geo:12.3457,78.910");
                testResult.Parameters[1].Values.Should().Equal("Mr. John Q. Public, Esq.\\nMail Drop: TNE QB\\n123 Main Street\\nAny Town, CA  91921-1234\\nU.S.A.");
                testResult.Value.Should().Be(";;123 Main Street;Any Town;CA;91921-1234;U.S.A.");
            }
        }

        [Fact]
        public void ReadLine_EmptyData_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReader(string.Empty))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadContentLine_EmptyData_ReturnsFalse()
        {
            // arrange
            using (var reader = CreateReader(string.Empty))
            {
                // act
                var testResult = reader.ReadContentLine();

                // assert
                testResult.Should().BeFalse();
            }
        }

        [Fact]
        public void ReadContentLine_LineRead_LeavesLinesReadCounted()
        {
            // arrange
            using (var reader = CreateReader(Crlf(LineA, LineB)))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.LinesRead;

                // assert
                testResult.Should().Be(1);
            }
        }

        [Fact]
        public void RawLine_View_IsTheWholeLineUnfolded()
        {
            // arrange
            using (var reader = CreateReader("item1.TEL;P=1:2\r\n 3\r\n"))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = view.RawLine.ToString();

                // assert
                testResult.Should().Be("item1.TEL;P=1:23");
            }
        }

        [Fact]
        public void ToContentLine_ViewOfGroupedLine_CopiesTheLine()
        {
            // arrange
            using (var reader = CreateReader(GroupedLineWithParameter))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                var testResult = LineDescription.Of(view.ToContentLine());

                // assert
                testResult.Should().Be("1/1 item1.TEL;P=1:2");
            }
        }

        #endregion

        #region Current line

        [Fact]
        public void CurrentLine_BeforeFirstRead_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf(LineA)))
            {
                // act
                Func<object> testAction = () => reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_AtEndOfData_Throws()
        {
            // arrange
            using (var reader = CreateReader(string.Empty))
            {
                reader.ReadContentLine();

                // act
                Func<object> testAction = () => reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CurrentLine_AfterSyntaxError_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf(LineA, SyntaxErrorLine)))
            {
                reader.ReadContentLine();
                Record.Exception(() => reader.ReadContentLine());

                // act
                Func<object> testAction = () => reader.CurrentLine;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_AfterReaderMovedOn_Throws(Func<VcardContentLineView, object> member)
        {
            // arrange
            using (var reader = CreateReader(Crlf("item1.TEL;P=1:2", LineB)))
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

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_AfterDispose_Throws(Func<VcardContentLineView, object> member)
        {
            // arrange
            var reader = CreateReader(GroupedLineWithParameter);
            reader.ReadContentLine();
            var view = reader.CurrentLine;
            reader.Dispose();

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Theory]
        [MemberData(nameof(ViewMembers))]
        public void ViewMember_DefaultView_Throws(Func<VcardContentLineView, object> member)
        {
            // arrange
            var view = default(VcardContentLineView);

            // act
            Action testAction = () => member(view);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(MembersAfterDispose))]
        public void Member_AfterDispose_Throws(Func<VcardContentLineReader, object> member)
        {
            // arrange
            var reader = CreateReader(Crlf(LineA));
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        // A read that needs no refill has nothing for a test to hold it at, so this goes straight to the step where such a read checks.
        [Fact]
        public void CompleteLine_ReaderDisposed_Throws()
        {
            // arrange
            var reader = CreateReader(Crlf(LineA));
            reader.Dispose();

            // act
            Action testAction = () => reader.CompleteLine(ContentLineScanner.ScanStatus.LineRead);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = CreateReader(Crlf(LineA));
            reader.Dispose();

            // act
            Action testAction = reader.Dispose;

            // assert
            testAction.Should().NotThrow();
        }

        #endregion

        private sealed class ThrowingTextReader : TextReader
        {
            private readonly Exception _exception;

            internal ThrowingTextReader(Exception exception)
            {
                _exception = exception;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                throw _exception;
            }
        }
    }
}
