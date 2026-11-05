using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of how <see cref="VcardContentLineReader"/> reads the lexical forms of vCard 2.1, in a vCard 2.1 card and under
    /// <see cref="VcardOptions.AcceptLegacySyntax"/>, and that nothing else reads them.
    /// </summary>
    public class VcardLegacySyntaxReaderTests
    {
        private const string BeginCard = "BEGIN:VCARD";
        private const string EndCard = "END:VCARD";
        private const string Version21 = "VERSION:2.1";
        private const string Version30 = "VERSION:3.0";
        private const string Version40 = "VERSION:4.0";

        // ref. vCard 2.1 §2.4.1.1: telephone types with no name.
        private const string BareTypesLine = "TEL;WORK;VOICE:+1-800-555-1234";

        // ref. vCard 2.1 §2.1.3: quoted-printable with soft line breaks, whose continuation lines start with two spaces that are data.
        private const string SoftBreakFirst = "LABEL;ENCODING=QUOTED-PRINTABLE:123 Winding Way=0D=0A=";
        private const string SoftBreakSecond = "  Any Town, CA 12345=0D=0A=";
        private const string SoftBreakThird = "  USA";
        private const string SoftBreakValue = "123 Winding Way=0D=0A  Any Town, CA 12345=0D=0A  USA";
        private const string SoftBreakText = "123 Winding Way\r\n  Any Town, CA 12345\r\n  USA";

        // ref. vCard 2.1 §2.6.3: an encoding with no name.  "SGVsbG8=" is "Hello" (RFC 4648 §4), split across two lines with the specification's indent.
        private const string Base64Header = "SOUND;WAVE;BASE64:";
        private const string Base64FirstLine = "    SGVs";
        private const string Base64SecondLine = "    bG8=";
        private const string Base64Value = "    SGVs    bG8=";
        private const string NextProperty = "NOTE:next";
        private static readonly byte[] HelloOctets = { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        private const string LineEndingInEquals = "NOTE;ENCODING=QUOTED-PRINTABLE:a=";
        private const string SoftBreakDescription = "3/3 LABEL;ENCODING=QUOTED-PRINTABLE:" + SoftBreakValue;
        private const string Base64Description = "3/3 SOUND;TYPE=WAVE;ENCODING=BASE64:" + Base64Value;

        private static readonly VcardOptions AcceptLegacy = new VcardOptions { AcceptLegacySyntax = true };
        private static readonly VcardOptions Rfc6350Folds = new VcardOptions { AcceptRfc6350FoldsInVersion21 = true };

        // A line of 20 characters, then 21 more soft line breaks, one more than a limit of 20 allows.
        private static readonly string ManySoftLineBreaks = Crlf(BeginCard, Version21, "A;QUOTED-PRINTABLE:=",
            "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=", "=");

        private static readonly string BareTypesCard = Crlf(BeginCard, Version21, BareTypesLine, EndCard);
        private static readonly string SoftBreakCard = Crlf(BeginCard, Version21, SoftBreakFirst, SoftBreakSecond, SoftBreakThird, NextProperty);
        private static readonly string Base64Card = Crlf(BeginCard, Version21, Base64Header, Base64FirstLine, Base64SecondLine, string.Empty, NextProperty);

        // The same lines, in each place that isn't known to be a vCard 2.1 card.
        private static TheoryData<string> NotVersion21(params string[] lines)
        {
            var before = new[]
            {
                new[] { BeginCard, Version30 },
                new[] { BeginCard, Version40 },
                new[] { BeginCard },
                new string[0],
                new[] { BeginCard, Version21, EndCard }
            };

            var data = new TheoryData<string>();
            foreach (var card in before)
            {
                data.Add(Crlf(card) + Crlf(lines));
            }

            return data;
        }

        public static TheoryData<string> BareTypesOutsideVersion21Data() => NotVersion21(BareTypesLine);

        public static TheoryData<string> LineEndingInEqualsOutsideVersion21Data() => NotVersion21(LineEndingInEquals, NextProperty);

        public static TheoryData<string> SoftBreakOutsideVersion21Data() => NotVersion21(LineEndingInEquals, "b");

        // Reads a line that is expected to fail, so that a test can see what the reader does next.
        private static void ReadFailingLine(VcardContentLineReader reader)
        {
            try
            {
                reader.ReadLine();
            }
            catch (VcardFileException)
            {
                // That is what the test is expecting.
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLines_ValuelessParameters_GivesThemAsTypeValues(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(BareTypesCard, bufferSize: bufferSize))
            {
                // act
                var testResult = DescribeViews(reader);

                // assert
                testResult.Should().Equal(
                    "1/1 BEGIN:VCARD",
                    "2/2 VERSION:2.1",
                    "3/3 TEL;TYPE=WORK;TYPE=VOICE:+1-800-555-1234",
                    "4/4 END:VCARD");
            }
        }

        [Fact]
        public void ReadLast_ValuelessParametersWithMixedTypeParameter_KeepsTheirOrder()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "TEL;HOME;TYPE=FAX;CELL:1")))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().Be("3/3 TEL;TYPE=HOME;TYPE=FAX;TYPE=CELL:1");
            }
        }

        [Fact]
        public void LegacySyntax_LineWithValuelessParameter_SaysSo()
        {
            // arrange
            using (var reader = CreateReader(BareTypesCard))
            {
                var line = ReadAll(reader)[2];

                // act
                var testResult = line.LegacySyntax;

                // assert
                testResult.Should().Be(VcardLegacySyntax.ValuelessParameter);
            }
        }

        [Fact]
        public void LegacySyntax_ViewOfLineWithEveryFeature_SaysSo()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE;HOME;QUOTED-PRINTABLE;CHARSET=UTF-8:a")))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.LegacySyntax;

                // assert
                testResult.Should().Be(VcardLegacySyntax.ValuelessParameter | VcardLegacySyntax.QuotedPrintable | VcardLegacySyntax.Charset);
            }
        }

        [Fact]
        public void LegacySyntax_UnknownParametersInVersion40Card_SaysSoWithoutChangingTheValue()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "NOTE;ENCODING=8BIT;CHARSET=UTF-8:a")))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.LegacySyntax;

                // assert
                testResult.Should().Be(VcardLegacySyntax.EightBit | VcardLegacySyntax.Charset);
            }
        }

        [Theory]
        [InlineData("NOTE;ENCODING=7BIT:a", VcardLegacySyntax.SevenBit)]
        [InlineData("NOTE;ENCODING=8BIT:a", VcardLegacySyntax.EightBit)]
        [InlineData("NOTE;ENCODING=QUOTED-PRINTABLE:a", VcardLegacySyntax.QuotedPrintable)]
        [InlineData("NOTE;encoding=base64:a", VcardLegacySyntax.Base64)]
        [InlineData("NOTE;ENCODING=b:a", VcardLegacySyntax.None)]
        [InlineData("NOTE;X-P=BASE64:a", VcardLegacySyntax.None)]
        public void LegacySyntax_LineOutsideACard_ComesFromItsParameters(string text, VcardLegacySyntax expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(text)))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.LegacySyntax;

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(BareTypesOutsideVersion21Data))]
        public void ReadLast_ValuelessParameterOutsideVersion21Card_Throws(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("Expected '=' after the parameter name.");
            }
        }

        [Theory]
        [MemberData(nameof(BareTypesOutsideVersion21Data))]
        public void ReadLast_ValuelessParameterOutsideVersion21CardWithAcceptLegacySyntax_GivesThemAsTypeValues(string text)
        {
            // arrange
            using (var reader = CreateReader(text, AcceptLegacy))
            {
                // act
                var testResult = ReadLast(reader);

                // assert
                testResult.GetParameter("TYPE").Values.Should().Equal("WORK");
            }
        }

        [Theory]
        [InlineData("PHOTO;BMP;URL:a")]
        [InlineData("PHOTO;URL:a")]
        [InlineData("PHOTO;CID:a")]
        [InlineData("PHOTO;UTF-8:a")]
        [InlineData("PHOTO;X-HOME:a")]
        public void ReadLast_ValuelessParameterVersion21DoesNotAllow_Throws(string line)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, line)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("Expected '=' after the parameter name.");
            }
        }

        [Theory]
        [InlineData("PHOTO;URL:a", "VALUE", "URL")]
        [InlineData("PHOTO;content-id:a", "VALUE", "content-id")]
        [InlineData("PHOTO;INLINE:a", "VALUE", "INLINE")]
        [InlineData("PHOTO;UTF-8:a", "CHARSET", "UTF-8")]
        [InlineData("PHOTO;latin1:a", "CHARSET", "latin1")]
        [InlineData("PHOTO;ISO-8859-1:a", "CHARSET", "ISO-8859-1")]
        [InlineData("PHOTO;us-ascii:a", "CHARSET", "us-ascii")]
        [InlineData("PHOTO;QUOTED-PRINTABLE:a", "ENCODING", "QUOTED-PRINTABLE")]
        [InlineData("PHOTO;8BIT:a", "ENCODING", "8BIT")]
        [InlineData("PHOTO;X-OTHER:a", "TYPE", "X-OTHER")]
        public void ReadLast_ValuelessParameterWithAcceptLegacySyntax_BelongsToItsParameter(string line, string expectedName, string expectedValue)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, line), AcceptLegacy))
            {
                // act
                var testResult = ReadLast(reader);

                // assert
                testResult.GetParameter(expectedName).Values.Should().Equal(expectedValue);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLines_SoftLineBreaks_JoinsThemKeepingPhysicalLineNumbers(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(SoftBreakCard, bufferSize: bufferSize))
            {
                // act
                var testResult = DescribeViews(reader);

                // assert
                testResult.Should().Equal(
                    "1/1 BEGIN:VCARD",
                    "2/2 VERSION:2.1",
                    SoftBreakDescription,
                    "4/6 NOTE:next");
            }
        }

        [Fact]
        public void PhysicalLinesRead_SoftLineBreaks_CountsEveryLine()
        {
            // arrange
            using (var reader = CreateReader(SoftBreakCard))
            {
                ReadAll(reader);

                // act
                var testResult = reader.PhysicalLinesRead;

                // assert
                testResult.Should().Be(6);
            }
        }

        [Fact]
        public void GetDecodedText_SoftLineBreaks_GivesTheText()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, SoftBreakFirst, SoftBreakSecond, SoftBreakThird)))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.GetDecodedText();

                // assert
                testResult.Should().Be(SoftBreakText);
            }
        }

        [Theory]
        [InlineData("NOTE;QUOTED-PRINTABLE:a=", "3/3 NOTE;ENCODING=QUOTED-PRINTABLE:ab")]
        [InlineData("NOTE;encoding=quoted-printable:a=", "3/3 NOTE;encoding=quoted-printable:ab")]
        [InlineData("NOTE;X-P=\"x:y\";ENCODING=QUOTED-PRINTABLE:a=", "3/3 NOTE;X-P=x:y;ENCODING=QUOTED-PRINTABLE:ab")]
        [InlineData("NOTE;ENCODING=\"QUOTED-PRINTABLE\":a=", "3/3 NOTE;ENCODING=QUOTED-PRINTABLE:ab")]
        [InlineData("NOTE;ENCODING=QUOTED-PRINTABLE,8BIT:a=", "3/3 NOTE;ENCODING=QUOTED-PRINTABLE|8BIT:ab")]
        public void ReadLast_FormsOfSoftLineBreak_JoinsThem(string firstLine, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, firstLine, "b")))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void ReadLast_SoftLineBreakWithWhitespaceAfterIt_JoinsAndRemovesTheWhitespace()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE;ENCODING=QUOTED-PRINTABLE:a= \t", "b")))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("ab");
            }
        }

        [Fact]
        public void ReadLast_SoftLineBreakThenEndOfData_CompletesTheLine()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, LineEndingInEquals)))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("a");
            }
        }

        [Fact]
        public void PhysicalLinesRead_SoftLineBreakThenEndOfData_CountsNoLineAfterIt()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, LineEndingInEquals)))
            {
                ReadAll(reader);

                // act
                var testResult = reader.PhysicalLinesRead;

                // assert
                testResult.Should().Be(3);
            }
        }

        [Fact]
        public void ReadLast_SoftLineBreakThenLineWithNoLineBreak_Throws()
        {
            // arrange
            using (var reader = CreateReader("BEGIN:VCARD\r\nVERSION:2.1\r\nNOTE;ENCODING=QUOTED-PRINTABLE:a=\r\nb"))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("Expected a CRLF line break at the end of the last content line.");
            }
        }

        [Theory]
        [InlineData("NOTE:a=")]
        [InlineData("NOTE;ENCODING=8BIT:a=")]
        [InlineData("NOTE;X-P=QUOTED-PRINTABLE:a=")]
        [InlineData("NOTE;X-P=\"ENCODING=QUOTED-PRINTABLE\":a=")]
        [InlineData("NOTE;ENCODING=8BIT;ENCODING=QUOTED-PRINTABLE:a=")]
        [InlineData("NOTE;8BIT;QUOTED-PRINTABLE:a=")]
        [InlineData("NOTE;ENCODING=7BIT,QUOTED-PRINTABLE:a=")]
        public void ReadLines_EqualsAtEndOfOtherValueInVersion21Card_IsNotASoftLineBreak(string line)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, line, NextProperty)))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(4);
            }
        }

        [Fact]
        public void ReadLines_EqualsInQuotedPrintableParameterOnly_IsNotASoftLineBreak()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE;ENCODING=QUOTED-PRINTABLE;X-P=a:b", NextProperty)))
            {
                // act
                var testResult = ReadAll(reader)[3].Value;

                // assert
                testResult.Should().Be("next");
            }
        }

        [Theory]
        [MemberData(nameof(LineEndingInEqualsOutsideVersion21Data))]
        public void ReadLines_LineEndingInEqualsOutsideVersion21Card_IsNotJoined(string text)
        {
            // arrange
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().EndWith("NOTE:next");
            }
        }

        [Theory]
        [MemberData(nameof(SoftBreakOutsideVersion21Data))]
        public void ReadLines_SoftLineBreakOutsideVersion21CardWithAcceptLegacySyntax_IsJoined(string text)
        {
            // arrange
            using (var reader = CreateReader(text, AcceptLegacy))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("ab");
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLines_Base64Value_JoinsItsLinesAndConsumesTheEmptyLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Base64Card, bufferSize: bufferSize))
            {
                // act
                var testResult = DescribeViews(reader);

                // assert
                testResult.Should().Equal(
                    "1/1 BEGIN:VCARD",
                    "2/2 VERSION:2.1",
                    Base64Description,
                    "4/7 NOTE:next");
            }
        }

        [Fact]
        public void GetDecodedValue_Base64Value_GivesTheOctets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, Base64Header, Base64FirstLine, Base64SecondLine, string.Empty)))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.GetDecodedValue();

                // assert
                testResult.Should().Equal(HelloOctets);
            }
        }

        [Fact]
        public void GetDecodedValue_ViewOfBase64Value_GivesTheOctets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, Base64Header, Base64FirstLine, Base64SecondLine, string.Empty)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetDecodedValue();

                // assert
                testResult.Should().Equal(HelloOctets);
            }
        }

        [Fact]
        public void ReadLast_Base64ValueStartingOnThePropertysLine_KeepsIt()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "KEY;ENCODING=BASE64:SGVs", "bG8=", string.Empty)))
            {
                // act
                var testResult = ReadLast(reader).GetDecodedValue();

                // assert
                testResult.Should().Equal(HelloOctets);
            }
        }

        [Fact]
        public void ReadLast_EmptyBase64Value_GivesNoOctets()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "KEY;ENCODING=BASE64:", string.Empty)))
            {
                // act
                var testResult = ReadLast(reader).GetDecodedValue();

                // assert
                testResult.Should().BeEmpty();
            }
        }

        [Theory]
        [InlineData("KEY;BASE64:")]
        [InlineData("KEY;BASE64:SGVs")]
        public void ReadLast_Base64ValueThenEndOfData_Throws(string firstLine)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, firstLine, "    bG8=")))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("The data ends inside a BASE64 value, before the empty line that must end it.");
            }
        }

        [Fact]
        public void ReadLast_Base64ValueWithoutItsEmptyLine_ThrowsAtTheLineThatIsNotBase64()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, Base64Header, Base64FirstLine, EndCard, string.Empty)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("Found a line that isn't base64 inside a BASE64 value, which an empty line must end.");
            }
        }

        [Fact]
        public void ReadLine_AfterLineThatIsNotBase64InBase64Value_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, Base64Header, EndCard, string.Empty)))
            {
                reader.ReadLine();
                reader.ReadLine();
                ReadFailingLine(reader);

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLast_Base64ValueOutsideVersion21Card_IsNotContinued()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, "KEY;ENCODING=BASE64:SGVsbG8=", NextProperty)))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("next");
            }
        }

        [Fact]
        public void ReadLast_Base64ValueOutsideVersion21CardWithAcceptLegacySyntax_IsContinued()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, "KEY;ENCODING=BASE64:SGVs", "bG8=", string.Empty), AcceptLegacy))
            {
                // act
                var testResult = ReadLast(reader).GetDecodedValue();

                // assert
                testResult.Should().Equal(HelloOctets);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadLast_FoldInVersion21Card_KeepsTheWhitespace(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE:a", " b"), bufferSize: bufferSize))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().Be("3/3 NOTE:a b");
            }
        }

        [Fact]
        public void ReadLast_FoldInVersion21CardWithAcceptRfc6350Folds_RemovesOneWhitespaceCharacter()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE:a", "  b"), Rfc6350Folds))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("a b");
            }
        }

        [Theory]
        [InlineData(Version30)]
        [InlineData(Version40)]
        public void ReadLast_FoldInLaterVersion_RemovesOneWhitespaceCharacter(string version)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, version, "NOTE:a", "  b")))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("a b");
            }
        }

        [Fact]
        public void ReadLast_SoftLineBreakWithAcceptRfc6350Folds_KeepsTheWhitespace()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, LineEndingInEquals, " b"), Rfc6350Folds))
            {
                // act
                var testResult = ReadLast(reader).Value;

                // assert
                testResult.Should().Be("a b");
            }
        }

        [Fact]
        public void ReadLast_MoreJoinedLinesThanMaxLineLength_Throws()
        {
            // arrange
            using (var reader = CreateReader(ManySoftLineBreaks, new VcardOptions { MaxLineLength = 20 }))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("The content line is longer than the maximum of 20 characters.");
            }
        }

        [Fact]
        public void GetTextValue_BackslashInVersion21Card_IsAnOrdinaryCharacter()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, @"NOTE:C:\temp")))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.GetTextValue();

                // assert
                testResult.Should().Be(@"C:\temp");
            }
        }

        [Fact]
        public void GetTextValue_ViewOfBackslashInVersion21Card_IsAnOrdinaryCharacter()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, @"NOTE:C:\temp")))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetTextValue().ToString();

                // assert
                testResult.Should().Be(@"C:\temp");
            }
        }

        [Theory]
        [InlineData(Version30)]
        [InlineData(Version40)]
        public void GetTextValue_OtherEscapeInLaterVersion_Throws(string version)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, version, @"NOTE:a\:b")))
            {
                var line = ReadLast(reader);

                // act
                Action testAction = () => line.GetTextValue();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Theory]
        [InlineData(@"NOTE:a\:b", "a:b")]
        [InlineData(@"NOTE:a\,b\n", "a,b\n")]
        [InlineData(@"NOTE:a\", @"a\")]
        public void GetTextValue_OtherEscapeWithAcceptLegacySyntax_GivesTheCharacter(string text, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, text), AcceptLegacy))
            {
                var line = ReadLast(reader);

                // act
                var testResult = line.GetTextValue();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void GetTextValue_ViewOfOtherEscapeWithAcceptLegacySyntax_GivesTheCharacter()
        {
            // arrange
            using (var reader = CreateReader(Crlf(@"NOTE:a\:b"), AcceptLegacy))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetTextValue().ToString();

                // assert
                testResult.Should().Be("a:b");
            }
        }

        [Fact]
        public void ReadLines_NestedCardOnTheNextLine_LeavesTheOuterCardVersion21()
        {
            // arrange
            // ref. vCard 2.1 §2.5.4: the example of an AGENT property.
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:", BeginCard, Version21, "TEL;WORK;VOICE:1", EndCard, "TEL;HOME:2", EndCard, "TEL;TYPE=WORK:3")))
            {
                // act
                var testResult = DescribeViews(reader);

                // assert
                testResult.Should().Equal(
                    "1/1 BEGIN:VCARD",
                    "2/2 VERSION:2.1",
                    "3/3 AGENT:",
                    "4/4 BEGIN:VCARD",
                    "5/5 VERSION:2.1",
                    "6/6 TEL;TYPE=WORK;TYPE=VOICE:1",
                    "7/7 END:VCARD",
                    "8/8 TEL;TYPE=HOME:2",
                    "9/9 END:VCARD",
                    "10/10 TEL;TYPE=WORK:3");
            }
        }

        [Fact]
        public void ReadLast_NestedCardOnTheAgentLine_LeavesTheOuterCardVersion21()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:BEGIN:VCARD", EndCard, "TEL;HOME:2")))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().Be("5/5 TEL;TYPE=HOME:2");
            }
        }

        [Fact]
        public void ReadLast_AfterTheOuterCardOfANestedCard_IsNoLongerVersion21()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:", BeginCard, EndCard, EndCard, BareTypesLine)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLast_ErrorInTheSecondCardAfterANestedCard_ReportsItsIndex()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:", BeginCard, EndCard, EndCard, BeginCard, Version21, "TEL;NOTATYPE:1")))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.CardIndex].Should().Be(1);
            }
        }

        [Fact]
        public void ReadLast_NestedCardOfLaterVersion_UsesItsRules()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:", BeginCard, Version40, BareTypesLine)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLast_NewCardInsideANestedCardOfLaterVersion_EndsTheNesting()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT:", BeginCard, Version40, BeginCard, EndCard, BareTypesLine)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Theory]
        [InlineData("AGENT;VALUE=URL:BEGIN:VCARD")]
        [InlineData("AGENT;VALUE=CID:BEGIN:VCARD")]
        public void ReadLast_AgentValueThatIsNotInline_DoesNotNest(string agentLine)
        {
            // arrange
            // So END:VCARD ends the card, and the line after it isn't in vCard 2.1.
            using (var reader = CreateReader(Crlf(BeginCard, Version21, agentLine, EndCard, BareTypesLine)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLast_AgentValueLocationWithNoName_DoesNotNest()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT;URL:BEGIN:VCARD", EndCard, BareTypesLine), AcceptLegacy))
            {
                // act
                var testResult = ReadLast(reader).CardIndex;

                // assert
                testResult.Should().Be(-1);
            }
        }

        [Fact]
        public void ReadLast_AgentValueSaidToBeInline_Nests()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "AGENT;VALUE=INLINE:BEGIN:VCARD", EndCard, "TEL;HOME:2")))
            {
                // act
                var testResult = LineDescription.Of(ReadLast(reader));

                // assert
                testResult.Should().Be("5/5 TEL;TYPE=HOME:2");
            }
        }

        [Fact]
        public void ReadLast_BeginInLaterVersion_StartsANewCardThatIsNotVersion21()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, BeginCard, BareTypesLine)))
            {
                // act
                Action testAction = () => ReadLast(reader);

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public async Task ReadContentLineAsync_LegacyForms_GivesWhatTheSyncReadGives(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, BareTypesLine, SoftBreakFirst, SoftBreakThird, Base64Header, Base64FirstLine, string.Empty, EndCard), bufferSize: bufferSize))
            {
                // act
                var testResult = await DescribeViewsAsync(reader);

                // assert
                testResult.Should().Equal(
                    "1/1 BEGIN:VCARD",
                    "2/2 VERSION:2.1",
                    "3/3 TEL;TYPE=WORK;TYPE=VOICE:+1-800-555-1234",
                    "4/4 LABEL;ENCODING=QUOTED-PRINTABLE:123 Winding Way=0D=0A  USA",
                    "5/6 SOUND;TYPE=WAVE;ENCODING=BASE64:    SGVs",
                    "6/9 END:VCARD");
            }
        }
    }
}
