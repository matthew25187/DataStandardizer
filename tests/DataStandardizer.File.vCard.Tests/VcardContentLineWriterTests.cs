using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardContentLineWriterTests
    {
        private const string Related = "RELATED";
        private const string ContactUri = "CONTACT-URI";
        private const string ParameterP = "X-P";
        
        private const string LineAWithCrlf = "A:1\r\n";
        private const string LineAThenLineC = "A:1\r\nC:3\r\n";

        // ref. RFC 6350 §3.2: lines of exactly 75 octets, one octet more, and longer, as the writer folds them.
        private static readonly string SixtyThreeAs = new string('a', 63);
        private static readonly string ContactUriOfSixtyThreeAs = "CONTACT-URI:" + SixtyThreeAs + "\r\n";
        private static readonly string SixtyThreeAsAndB = SixtyThreeAs + "b";
        private static readonly string ContactUriOfSixtyThreeAsAndBFolded = "CONTACT-URI:" + SixtyThreeAs + "\r\n b\r\n";
        private static readonly string LongValue = new string('a', 63) + new string('b', 74) + new string('c', 74) + "dd";
        private static readonly string LongContactUriFolded =
            "CONTACT-URI:" + new string('a', 63) + "\r\n " + new string('b', 74) + "\r\n " + new string('c', 74) + "\r\n dd\r\n";

        // An NICKNAME whose CN of 80 octets crosses the fold length, as the writer folds it.
        private static readonly string EightyNs = new string('n', 80);
        private static readonly string AttendeeWithEightyNsFolded =
            "NICKNAME;CN=" + new string('n', 63) + "\r\n " + new string('n', 17) + ":mailto:a@example.com\r\n";

        // A value of 100 octets, written as X with a tab as the fold white space, and with a bare line feed as the line break before a Y line.
        private static readonly string HundredAs = new string('a', 100);
        private static readonly string HundredAsFoldedWithTab = "X:" + new string('a', 73) + "\r\n\t" + new string('a', 27) + "\r\n";
        private static readonly string HundredAsFoldedWithLineFeedThenY = "X:" + new string('a', 73) + "\n " + new string('a', 27) + "\nY:1\n";

        // A value of 40 repeats of characters of one, two and four octets, whose CONTACT-URI of 452 octets takes 7 physical lines once folded.
        private static readonly string RepeatedMultiOctetValue = string.Concat(Enumerable.Repeat("Caf\xE9 \xD83D\xDE00 ", 40));
        private const int RepeatedMultiOctetContactUriPhysicalLines = 7;

        // Each value of an X property with a character of two, three or four octets where the line reaches 75 octets, and the line written:
        // the character is written on the first physical line if it fits there, and starts the next one if not.
        public static TheoryData<string, string> MultiOctetCharactersAtFoldLength => new TheoryData<string, string>
        {
            { new string('a', 72) + "\xE9z", "X:" + new string('a', 72) + "\r\n \xE9z\r\n" },
            { new string('a', 71) + "\xE9z", "X:" + new string('a', 71) + "\xE9\r\n z\r\n" },
            { new string('a', 71) + "\x20ACz", "X:" + new string('a', 71) + "\r\n \x20ACz\r\n" },
            { new string('a', 70) + "\x20ACz", "X:" + new string('a', 70) + "\x20AC\r\n z\r\n" },
            { new string('a', 70) + "\xD83D\xDE00z", "X:" + new string('a', 70) + "\r\n \xD83D\xDE00z\r\n" },
            { new string('a', 69) + "\xD83D\xDE00z", "X:" + new string('a', 69) + "\xD83D\xDE00\r\n z\r\n" },
        };

        // A line sequence whose long lines are folded at 75 octets, in a value, before a multi-octet character, and between parameters.  It has no
        // VERSION line, so its caret is an ordinary character.
        private static readonly string EventFoldedAtFoldLength = Crlf(
            "BEGIN:VCARD",
            "NICKNAME;CN=Jane Doe;X-NOTE=a^xb:mailto:jane.doe@example.com",
            "CONTACT-URI:" + new string('a', 63),
            " " + new string('b', 74),
            " ccc",
            "RELATED:" + new string('x', 66),
            " \xE9\x20AC\xD83D\xDE00",
            "CALADRURI;CN=" + new string('o', 62),
            " ;ROLE=CHAIR:mailto:o@example.com",
            "END:VCARD");

        #region Content line structure

        [Fact]
        public void WriteLine_NameAndValue_WritesLineWithCrlf()
        {
            // arrange
            var line = new VcardContentLine(Related, "Team meeting");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("RELATED:Team meeting\r\n");
        }

        [Fact]
        public void WriteLine_EmptyValue_WritesColonOnly()
        {
            // act
            var testResult = WriteAll(new VcardContentLine("X-EMPTY", string.Empty));

            // assert
            testResult.Should().Be("X-EMPTY:\r\n");
        }

        [Fact]
        public void WriteLine_ValueWithEscapesAndDelimiters_IsWrittenAsItIs()
        {
            // arrange
            var line = new VcardContentLine(ContactUri, "a\\, b\\; c\\nd:e;f,g\th");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("CONTACT-URI:a\\, b\\; c\\nd:e;f,g\th\r\n");
        }

        [Fact]
        public void WriteLine_NonAsciiValue_IsWritten()
        {
            // act
            var testResult = WriteAll(new VcardContentLine(Related, "Café 会議 😀"));

            // assert
            testResult.Should().Be("RELATED:Café 会議 😀\r\n");
        }

        [Fact]
        public void WriteLines_SeveralLines_WritesEachOnItsOwnLine()
        {
            // act
            var testResult = WriteAll(new VcardContentLine("BEGIN", "VCARD"), new VcardContentLine("VERSION", "4.0"), new VcardContentLine("END", "VCARD"));

            // assert
            testResult.Should().Be(Crlf("BEGIN:VCARD", "VERSION:4.0", "END:VCARD"));
        }

        #endregion

        #region Parameters

        [Fact]
        public void WriteLine_PlainParameterValues_AreNotQuoted()
        {
            // arrange
            var line = new VcardContentLine("NICKNAME",
                new[] { new VcardContentLineParameter("ROLE", "REQ-PARTICIPANT"), new VcardContentLineParameter("CN", "Jane Doe") }, "mailto:jane@example.com");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("NICKNAME;ROLE=REQ-PARTICIPANT;CN=Jane Doe:mailto:jane@example.com\r\n");
        }

        [Theory]
        [InlineData("mailto:a@example.com", "NICKNAME;X-P=\"mailto:a@example.com\":x\r\n")]
        [InlineData("a;b", "NICKNAME;X-P=\"a;b\":x\r\n")]
        [InlineData("Doe, Jane", "NICKNAME;X-P=\"Doe, Jane\":x\r\n")]
        public void WriteLine_ParameterValueWithDelimiter_IsQuoted(string value, string expected)
        {
            // arrange
            var line = new VcardContentLine("NICKNAME", new[] { new VcardContentLineParameter(ParameterP, value) }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_MultipleParameterValues_AreJoinedAndQuotedEachAsNeeded()
        {
            // arrange
            var line = new VcardContentLine("NICKNAME", new[] { new VcardContentLineParameter("MEMBER", "mailto:a@example.com", "group", "") }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("NICKNAME;MEMBER=\"mailto:a@example.com\",group,:x\r\n");
        }

        #endregion

        #region Invalid lines

        [Theory]
        [InlineData("")]
        [InlineData("X_Y")]
        [InlineData("SUM MARY")]
        [InlineData("SUMMARÉ")]
        [InlineData("A:B")]
        public void WriteLine_InvalidPropertyName_ThrowsAndWritesNothing(string name)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new VcardContentLine(name, "x"));

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("property name");
                exception.Data[VcardFileException.DataItemName.LineNumber].Should().Be(1L);
                exception.Data.Contains(VcardFileException.DataItemName.PropertyName).Should().BeFalse();
                text.ToString().Should().BeEmpty();
                writer.LinesWritten.Should().Be(0);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("X_P")]
        [InlineData("X P")]
        public void WriteLine_InvalidParameterName_ThrowsAndWritesNothing(string parameterName)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var line = new VcardContentLine(Related, new[] { new VcardContentLineParameter(parameterName, "v") }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("parameter name");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Related);
                text.ToString().Should().BeEmpty();
            }
        }

        [Theory]
        [InlineData("\0")]
        [InlineData("a\u0001b")]
        [InlineData("\u007F")]
        public void WriteLine_InvalidParameterValueCharacter_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var line = new VcardContentLine(Related, new[] { new VcardContentLineParameter(ParameterP, "ok", value) }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("parameter X-P");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Related);
                text.ToString().Should().BeEmpty();
            }
        }

        [Theory]
        [InlineData("\r")]
        [InlineData("\n")]
        [InlineData("a\r\nb")]
        [InlineData("\0")]
        [InlineData("\u007F")]
        public void WriteLine_InvalidValueCharacter_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new VcardContentLine(ContactUri, value));

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("property value");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(ContactUri);
                text.ToString().Should().BeEmpty();
            }
        }

        /// <summary>
        /// Values with a lone surrogate.  These cannot go in <see cref="InlineDataAttribute"/>, which replaces lone surrogates when tests are
        /// discovered.
        /// </summary>
        public static TheoryData<string> LoneSurrogateValues => new TheoryData<string> { "\xD800", "\xDC00x", "x\xD83D", "a\xDE00\xD83Db" };

        [Theory]
        [MemberData(nameof(LoneSurrogateValues), DisableDiscoveryEnumeration = true)]
        public void WriteLine_LoneSurrogateInParameterValue_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var line = new VcardContentLine(Related, new[] { new VcardContentLineParameter(ParameterP, "ok", value) }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("parameter X-P");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(Related);
                text.ToString().Should().BeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(LoneSurrogateValues), DisableDiscoveryEnumeration = true)]
        public void WriteLine_LoneSurrogateInValue_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new VcardContentLine(ContactUri, value));

                // assert
                var exception = testAction.Should().Throw<VcardFileException>().Which;
                exception.Message.Should().Contain("property value");
                exception.Data[VcardFileException.DataItemName.PropertyName].Should().Be(ContactUri);
                text.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_InvalidLineAfterAValidLine_ThrowsWithItsLineNumber()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                writer.WriteLine(new VcardContentLine("A", "1"));

                // act
                Action testAction = () => writer.WriteLine(new VcardContentLine("B", "\n"));

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.LineNumber].Should().Be(2L);
            }
        }

        [Fact]
        public void WriteLine_AfterInvalidLine_WriterContinues()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                writer.WriteLine(new VcardContentLine("A", "1"));
                Record.Exception(() => writer.WriteLine(new VcardContentLine("B", "\n")));

                // act
                writer.WriteLine(new VcardContentLine("C", "3"));

                // assert
                text.ToString().Should().Be(LineAThenLineC);
                writer.LinesWritten.Should().Be(2);
            }
        }

        [Fact]
        public void WriteLines_InvalidLine_LinesBeforeItAreWritten()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var lines = new[] { new VcardContentLine("A", "1"), new VcardContentLine("B", "\0"), new VcardContentLine("C", "3") };

                // act
                Action testAction = () => writer.WriteLines(lines);

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.LineNumber].Should().Be(2L);
                text.ToString().Should().Be(LineAWithCrlf);
            }
        }

        [Fact]
        public void WriteLines_NullLine_ThrowsAfterWritingLinesBeforeIt()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLines(new[] { new VcardContentLine("A", "1"), null });

                // assert
                testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lines");
                text.ToString().Should().Be(LineAWithCrlf);
            }
        }

        public static TheoryData<Action<VcardContentLineWriter>> NullArgumentCalls => new TheoryData<Action<VcardContentLineWriter>>
        {
            writer => writer.WriteLine(null),
            writer => writer.WriteLines(null),
        };

        [Theory]
        [MemberData(nameof(NullArgumentCalls), DisableDiscoveryEnumeration = true)]
        public void Write_NullArgument_ThrowsArgumentNull(Action<VcardContentLineWriter> call)
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                // act
                Action testAction = () => call(writer);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        #endregion

        #region Folding

        [Fact]
        public void WriteLine_ExactlyFoldLength_IsNotFolded()
        {
            // arrange
            var line = new VcardContentLine(ContactUri, SixtyThreeAs);

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(ContactUriOfSixtyThreeAs);
        }

        [Fact]
        public void WriteLine_OneOctetOverFoldLength_IsFoldedAtFoldLength()
        {
            // arrange
            var line = new VcardContentLine(ContactUri, SixtyThreeAsAndB);

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(ContactUriOfSixtyThreeAsAndBFolded);
        }

        [Fact]
        public void WriteLine_LongLine_EveryPhysicalLineIsFoldLength()
        {
            // arrange
            var line = new VcardContentLine(ContactUri, LongValue);

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(LongContactUriFolded);
        }

        [Theory]
        [MemberData(nameof(MultiOctetCharactersAtFoldLength))]
        public void WriteLine_MultiOctetCharacterAtFoldLength_IsNeverSplit(string value, string expected)
        {
            // act
            var testResult = WriteAll(new VcardContentLine("X", value));

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_FoldInsideParameters_IsAllowed()
        {
            // arrange
            var line = new VcardContentLine("NICKNAME", new[] { new VcardContentLineParameter("CN", EightyNs) }, "mailto:a@example.com");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be(AttendeeWithEightyNsFolded);
        }

        [Fact]
        public void WriteLine_TabFoldWhitespace_StartsContinuationLinesWithTab()
        {
            // arrange
            var options = new VcardOptions { FoldWhitespace = '\t' };

            // act
            var testResult = WriteAll(options, new VcardContentLine("X", HundredAs));

            // assert
            testResult.Should().Be(HundredAsFoldedWithTab);
        }

        [Fact]
        public void WriteLine_BareLineFeed_EndsAndFoldsLinesWithLineFeed()
        {
            // arrange
            var options = new VcardOptions { LineBreak = "\n" };

            // act
            var testResult = WriteAll(options, new VcardContentLine("X", HundredAs), new VcardContentLine("Y", "1"));

            // assert
            testResult.Should().Be(HundredAsFoldedWithLineFeedThenY);
        }

        [Fact]
        public void ReadLine_WrittenWithBareLineFeedAndAccepted_IsTheLineWritten()
        {
            // arrange
            var options = new VcardOptions { LineBreak = "\n" };
            var line = new VcardContentLine(ContactUri, RepeatedMultiOctetValue);
            using (var reader = CreateReader(WriteAll(options, line), new VcardOptions { AcceptBareLineFeed = true }))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(RepeatedMultiOctetValue);
                reader.PhysicalLinesRead.Should().Be(RepeatedMultiOctetContactUriPhysicalLines);
            }
        }

        [Fact]
        public void WriteLine_CustomFoldLength_FoldsAtThatLength()
        {
            // arrange
            var options = new VcardOptions { FoldLength = 10 };

            // act
            var testResult = WriteAll(options, new VcardContentLine(Related, "abcdefghijkl"));

            // assert
            testResult.Should().Be(Crlf("RELATED:ab", " cdefghijk", " l"));
        }

        [Fact]
        public void WriteLine_MinimumFoldLength_PutsOneCharacterOnEachLineThatCannotHoldMore()
        {
            // arrange
            var options = new VcardOptions { FoldLength = 4 };

            // act
            var testResult = WriteAll(options, new VcardContentLine("X", "😀😀ab€"));

            // assert
            // A continuation line holds 3 octets after the white space, so a 4-octet character goes on a line of its own, one octet over.
            testResult.Should().Be(Crlf("X:", " 😀", " 😀", " ab", " €"));
        }

        #endregion

        #region Round trips

        [Fact]
        public void WriteLines_InputFoldedAtFoldLength_IsWrittenBackByteForByte()
        {
            // arrange
            var data = new UTF8Encoding(false).GetBytes(EventFoldedAtFoldLength);
            using (var reader = CreateStreamReader(data))
            {
                var lines = ReadAll(reader);
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
        }

        #endregion

        #region Options

        // Each option that is not valid, with the text its message has, and each constructor it is given to.
        public static TheoryData<string, VcardOptions, string, Func<VcardOptions, VcardContentLineWriter>> InvalidOptions()
        {
            var options = new List<(string, VcardOptions, string)>
            {
                ("FoldLength = 3", new VcardOptions { FoldLength = 3 }, "fold length"),
                ("FoldLength = 0", new VcardOptions { FoldLength = 0 }, "fold length"),
                ("FoldLength = -1", new VcardOptions { FoldLength = -1 }, "fold length"),
                ("FoldWhitespace = x", new VcardOptions { FoldWhitespace = 'x' }, "fold white space"),
                ("FoldWhitespace = LF", new VcardOptions { FoldWhitespace = '\n' }, "fold white space"),
                ("FoldWhitespace = CR", new VcardOptions { FoldWhitespace = '\r' }, "fold white space"),
                ("FoldWhitespace = no-break space", new VcardOptions { FoldWhitespace = '\xA0' }, "fold white space"),
                ("LineBreak = CR", new VcardOptions { LineBreak = "\r" }, "line break"),
                ("LineBreak empty", new VcardOptions { LineBreak = "" }, "line break"),
                ("LineBreak = LF CR", new VcardOptions { LineBreak = "\n\r" }, "line break"),
                ("LineBreak = CRLF CRLF", new VcardOptions { LineBreak = "\r\n\r\n" }, "line break"),
                ("LineBreak = space LF", new VcardOptions { LineBreak = " \n" }, "line break"),
                ("LineBreak null", new VcardOptions { LineBreak = null }, "line break"),
            };
            var constructors = new Func<VcardOptions, VcardContentLineWriter>[]
            {
                invalidOptions => new VcardContentLineWriter(new StringWriter(), invalidOptions),
                invalidOptions => new VcardContentLineWriter(new MemoryStream(), invalidOptions),
            };
            var data = new TheoryData<string, VcardOptions, string, Func<VcardOptions, VcardContentLineWriter>>();
            foreach (var (name, invalid, message) in options)
            {
                foreach (var constructor in constructors)
                {
                    data.Add(name, invalid, message, constructor);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(InvalidOptions), DisableDiscoveryEnumeration = true)]
        public void Constructor_InvalidOption_ThrowsArgumentException(string option, VcardOptions options, string expectedMessagePart,
            Func<VcardOptions, VcardContentLineWriter> constructor)
        {
            // arrange
            _ = option;

            // act
            Action testAction = () => constructor(options);

            // assert
            var exception = testAction.Should().Throw<ArgumentException>().Which;
            exception.ParamName.Should().Be("options");
            exception.Message.Should().Contain(expectedMessagePart);
        }

        [Fact]
        public void Constructor_ValidOptions_DoNotThrow()
        {
            // arrange
            var options = new VcardOptions { FoldLength = 4, FoldWhitespace = '\t', LineBreak = "\n" };

            // act
            Action testAction = () => new VcardContentLineWriter(new StringWriter(), options).Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Constructor_DefaultOptions_FollowRfc6350()
        {
            // act
            var testResult = new VcardOptions();

            // assert
            testResult.FoldLength.Should().Be(75);
            testResult.FoldWhitespace.Should().Be(' ');
            testResult.LineBreak.Should().Be("\r\n");
            testResult.AllowLossyVersionConversion.Should().BeFalse();
        }

        [Fact]
        public void Constructor_TextWriterWithEncoding_Throws()
        {
            // act
            Action testAction = () => new VcardContentLineWriter(new StringWriter(), new VcardOptions { Encoding = Encoding.UTF8 });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("options");
        }

        public static TheoryData<Action> NullConstructorArguments => new TheoryData<Action>
        {
            () => _ = new VcardContentLineWriter((Stream)null),
            () => _ = new VcardContentLineWriter((TextWriter)null),
            () => _ = new VcardContentLineWriter((string)null),
            () => _ = new VcardContentLineWriter(new MemoryStream(), null),
            () => _ = new VcardContentLineWriter(new StringWriter(), null),
            () => _ = new VcardContentLineWriter("unused.vcf", null)
        };

        [Theory]
        [MemberData(nameof(NullConstructorArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_NullArgument_ThrowsArgumentNull(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ReadOnlyStream_Throws()
        {
            // act
            Action testAction = () => new VcardContentLineWriter(new MemoryStream(new byte[0], false));

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("vcardStream");
        }

        #endregion

        #region Streams and files

        [Fact]
        public void Stream_DefaultEncoding_IsUtf8WithoutByteOrderMark()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                // act
                using (var writer = new VcardContentLineWriter(stream))
                {
                    writer.WriteLine(new VcardContentLine(Related, "Café"));
                }

                // assert
                stream.ToArray().Should().Equal(new UTF8Encoding(false).GetBytes("RELATED:Café\r\n"));
            }
        }

        [Fact]
        public void Stream_EncodingOption_IsApplied()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var encoding = new UnicodeEncoding(false, false);
                var expected = encoding.GetBytes("RELATED:Caf\xE9\r\n");

                // act
                using (var writer = new VcardContentLineWriter(stream, new VcardOptions { Encoding = encoding }))
                {
                    writer.WriteLine(new VcardContentLine(Related, "Caf\xE9"));
                }

                // assert
                stream.ToArray().Should().Equal(expected);
            }
        }

        [Fact]
        public void Dispose_Stream_FlushesAndLeavesStreamOpen()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var writer = new VcardContentLineWriter(stream);
                writer.WriteLine(new VcardContentLine("A", "1"));

                // act
                writer.Dispose();

                // assert
                stream.CanWrite.Should().BeTrue();
                stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
            }
        }

        #endregion

        #region Counters and progress

        [Fact]
        public void LinesWritten_AfterEachWrite_CountsLogicalLines()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                // act
                writer.WriteLine(new VcardContentLine(ContactUri, new string('a', 200)));
                var afterFirst = writer.LinesWritten;
                writer.WriteLines(new[] { new VcardContentLine("A", "1"), new VcardContentLine("B", "2") });
                var afterSecond = writer.LinesWritten;

                // assert
                afterFirst.Should().Be(1);
                afterSecond.Should().Be(3);
            }
        }

        [Fact]
        public void WriteLines_Progress_IsCumulativeAcrossCalls()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                var progress = new RecordingProgress();
                writer.WriteLine(new VcardContentLine("A", "0"));

                // act
                writer.WriteLines(new[] { new VcardContentLine("A", "1"), new VcardContentLine("A", "2") }, progress);
                writer.WriteLines(new[] { new VcardContentLine("A", "3") }, progress);

                // assert
                progress.Reports.Should().Equal(new VcardWriteProgress(2), new VcardWriteProgress(3), new VcardWriteProgress(4));
            }
        }

        [Fact]
        public void WriteLines_Progress_IsReportedAfterTheLineIsWritten()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var written = new List<string>();
                var progress = new RecordingProgress(_ => written.Add(text.ToString()));

                // act
                writer.WriteLines(new[] { new VcardContentLine("A", "1"), new VcardContentLine("B", "2") }, progress);

                // assert
                written.Should().Equal(LineAWithCrlf, Crlf("A:1", "B:2"));
            }
        }

        [Fact]
        public void WriteLines_ProgressThrows_EndsCallAfterLineIsWritten()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                var progress = new RecordingProgress(_ => throw new ApplicationException("stop"));

                // act
                Action testAction = () => writer.WriteLines(new[] { new VcardContentLine("A", "1"), new VcardContentLine("B", "2") }, progress);

                // assert
                testAction.Should().Throw<ApplicationException>();
                text.ToString().Should().Be(LineAWithCrlf);
                writer.LinesWritten.Should().Be(1);
            }
        }

        // Each comparison of the progress of three lines, with whether it is true.
        public static TheoryData<Func<VcardWriteProgress, bool>, bool> ProgressComparisons => new TheoryData<Func<VcardWriteProgress, bool>, bool>
        {
            { progress => progress.Equals(new VcardWriteProgress(3)), true },
            { progress => progress == new VcardWriteProgress(3), true },
            { progress => progress != new VcardWriteProgress(4), true },
            { progress => progress.Equals((object)new VcardWriteProgress(3)), true },
            { progress => progress.Equals(null), false },
            { progress => progress.Equals(new VcardWriteProgress(3, 1)), false },
        };

        [Theory]
        [MemberData(nameof(ProgressComparisons), DisableDiscoveryEnumeration = true)]
        public void WriteProgress_Equality_ComparesCounts(Func<VcardWriteProgress, bool> comparison, bool expected)
        {
            // arrange
            var progress = new VcardWriteProgress(3);

            // act
            var testResult = comparison(progress);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteProgress_GetHashCode_SameCounts_IsTheSame()
        {
            // arrange
            var progress = new VcardWriteProgress(3);
            var expected = new VcardWriteProgress(3).GetHashCode();

            // act
            var testResult = progress.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteProgress_ToString_GivesEachCount()
        {
            // arrange
            var progress = new VcardWriteProgress(3);

            // act
            var testResult = progress.ToString();

            // assert
            testResult.Should().Be("LinesWritten = 3, CardsWritten = 0");
        }

        [Fact]
        public void WriteProgress_LinesWritten_Default_IsZero()
        {
            // arrange
            var progress = default(VcardWriteProgress);

            // act
            var testResult = progress.LinesWritten;

            // assert
            testResult.Should().Be(0);
        }

        #endregion

        #region Flush, concurrency and disposal

        [Fact]
        public void Flush_CallerSuppliedWriter_IsFlushed()
        {
            // arrange
            using (var text = new TrackingStringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                writer.Flush();

                // assert
                text.FlushCount.Should().Be(1);
            }
        }

        [Fact]
        public void Flush_OwnedWriter_WritesThroughToStream()
        {
            // arrange
            using (var stream = new MemoryStream())
            using (var writer = new VcardContentLineWriter(stream))
            {
                writer.WriteLine(new VcardContentLine("A", "1"));

                // act
                writer.Flush();

                // assert
                stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
            }
        }

        [Fact]
        public void WriteLines_EnumerationCallsWriter_ThrowsInvalidOperation()
        {
            // arrange
            using (var writer = new VcardContentLineWriter(new StringWriter()))
            {
                IEnumerable<VcardContentLine> Lines()
                {
                    yield return new VcardContentLine("A", "1");
                    writer.WriteLine(new VcardContentLine("B", "2"));
                }

                // act
                Action testAction = () => writer.WriteLines(Lines());

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
                writer.LinesWritten.Should().Be(1);
            }
        }

        public static TheoryData<Action<VcardContentLineWriter>> MembersAfterDispose()
        {
            var line = new VcardContentLine("A", "1");
            return new TheoryData<Action<VcardContentLineWriter>>
            {
                writer => _ = writer.LinesWritten,
                writer => writer.WriteLine(line),
                writer => writer.WriteLine(null),
                writer => writer.WriteLines(new[] { line }),
                writer => writer.Flush(),
            };
        }

        [Theory]
        [MemberData(nameof(MembersAfterDispose), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Action<VcardContentLineWriter> member)
        {
            // arrange
            var writer = new VcardContentLineWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(VcardContentLineWriter));
        }

        [Fact]
        public void WriteLines_DisposedDuringEnumeration_ThrowsObjectDisposed()
        {
            // arrange
            using (var text = new TrackingStringWriter())
            {
                var writer = new VcardContentLineWriter(text);

                IEnumerable<VcardContentLine> Lines()
                {
                    yield return new VcardContentLine("A", "1");
                    writer.Dispose();
                    yield return new VcardContentLine("B", "2");
                }

                // act
                Action testAction = () => writer.WriteLines(Lines());

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
                text.ToString().Should().Be(LineAWithCrlf);
                text.IsDisposed.Should().BeFalse();
            }
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var writer = new VcardContentLineWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerSuppliedWriter_IsNeitherFlushedNorDisposed()
        {
            // arrange
            using (var text = new TrackingStringWriter())
            {
                var writer = new VcardContentLineWriter(text);
                writer.WriteLine(new VcardContentLine("A", "1"));

                // act
                writer.Dispose();

                // assert
                text.FlushCount.Should().Be(0);
                text.IsDisposed.Should().BeFalse();
            }
        }

        #endregion

        private static string WriteAll(params VcardContentLine[] lines)
        {
            return WriteAll(new VcardOptions(), lines);
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
