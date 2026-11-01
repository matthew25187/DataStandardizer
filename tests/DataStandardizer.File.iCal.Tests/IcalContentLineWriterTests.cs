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
    public class IcalContentLineWriterTests
    {
        private const string Summary = "SUMMARY";
        private const string Description = "DESCRIPTION";
        private const string ParameterP = "X-P";
        
        private const string LineAWithCrlf = "A:1\r\n";
        private const string LineAThenLineC = "A:1\r\nC:3\r\n";

        // A line whose parameters are quoted, caret-encoded and plain as they would not be written from the model.
        private const string RawParametersLine = "ATTENDEE;CN=\"John\";X-A=a^xb;ROLE=CHAIR;MEMBER=\"mailto:a@b.c\",x:mailto:x\r\n";

        #region Content line structure

        [Fact]
        public void WriteLine_NameAndValue_WritesLineWithCrlf()
        {
            // arrange
            var line = new IcalContentLine(Summary, "Team meeting");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("SUMMARY:Team meeting\r\n");
        }

        [Fact]
        public void WriteLine_EmptyValue_WritesColonOnly()
        {
            // act
            var testResult = WriteAll(new IcalContentLine("X-EMPTY", string.Empty));

            // assert
            testResult.Should().Be("X-EMPTY:\r\n");
        }

        [Fact]
        public void WriteLine_ValueWithEscapesAndDelimiters_IsWrittenAsItIs()
        {
            // arrange
            var line = new IcalContentLine(Description, "a\\, b\\; c\\nd:e;f,g\th");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("DESCRIPTION:a\\, b\\; c\\nd:e;f,g\th\r\n");
        }

        [Fact]
        public void WriteLine_NonAsciiValue_IsWritten()
        {
            // act
            var testResult = WriteAll(new IcalContentLine(Summary, "Café 会議 😀"));

            // assert
            testResult.Should().Be("SUMMARY:Café 会議 😀\r\n");
        }

        [Fact]
        public void WriteLines_SeveralLines_WritesEachOnItsOwnLine()
        {
            // act
            var testResult = WriteAll(new IcalContentLine("BEGIN", "VCALENDAR"), new IcalContentLine("VERSION", "2.0"), new IcalContentLine("END", "VCALENDAR"));

            // assert
            testResult.Should().Be(Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "END:VCALENDAR"));
        }

        #endregion

        #region Parameters

        [Fact]
        public void WriteLine_PlainParameterValues_AreNotQuoted()
        {
            // arrange
            var line = new IcalContentLine("ATTENDEE",
                new[] { new IcalContentLineParameter("ROLE", "REQ-PARTICIPANT"), new IcalContentLineParameter("CN", "Jane Doe") }, "mailto:jane@example.com");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("ATTENDEE;ROLE=REQ-PARTICIPANT;CN=Jane Doe:mailto:jane@example.com\r\n");
        }

        [Theory]
        [InlineData("mailto:a@example.com", "\"mailto:a@example.com\"")]
        [InlineData("a;b", "\"a;b\"")]
        [InlineData("Doe, Jane", "\"Doe, Jane\"")]
        public void WriteLine_ParameterValueWithDelimiter_IsQuoted(string value, string expectedValue)
        {
            // arrange
            var line = new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter(ParameterP, value) }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be($"ATTENDEE;X-P={expectedValue}:x\r\n");
        }

        [Fact]
        public void WriteLine_MultipleParameterValues_AreJoinedAndQuotedEachAsNeeded()
        {
            // arrange
            var line = new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter("MEMBER", "mailto:a@example.com", "group", "") }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("ATTENDEE;MEMBER=\"mailto:a@example.com\",group,:x\r\n");
        }

        [Theory]
        [InlineData("a^b", "a^^b")]
        [InlineData("say \"hi\"", "say ^'hi^'")]
        [InlineData("one\ntwo", "one^ntwo")]
        [InlineData("one\r\ntwo", "one^ntwo")]
        [InlineData("one\rtwo", "one^ntwo")]
        [InlineData("^^\n\"", "^^^^^n^'")]
        [InlineData("tab\there", "tab\there")]
        public void WriteLine_ParameterValue_IsCaretEncoded(string value, string expectedValue)
        {
            // arrange
            var line = new IcalContentLine("X-A", new[] { new IcalContentLineParameter(ParameterP, value) }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be($"X-A;X-P={expectedValue}:x\r\n");
        }

        [Fact]
        public void WriteLine_CaretEncodedValueWithDelimiter_IsEncodedAndQuoted()
        {
            // arrange
            var line = new IcalContentLine("X-A", new[] { new IcalContentLineParameter(ParameterP, "\"Doe, J\"\n^") }, "x");

            // act
            var testResult = WriteAll(line);

            // assert
            testResult.Should().Be("X-A;X-P=\"^'Doe, J^'^n^^\":x\r\n");
        }

        [Theory]
        [InlineData("a^b")]
        [InlineData("say \"hi\"")]
        [InlineData("one\ntwo")]
        [InlineData("mailto:a@example.com")]
        [InlineData("\"quoted; with, all: of them\"\n^^")]
        [InlineData("Café 😀")]
        [InlineData("")]
        public void WriteLine_ParameterValue_RoundTripsThroughReader(string value)
        {
            // arrange
            var line = new IcalContentLine("X-A", new[] { new IcalContentLineParameter(ParameterP, value, "second") }, "x");

            // act
            var testResult = ReadFirstLine(WriteAll(line));

            // assert
            testResult.Parameters.Should().ContainSingle();
            testResult.Parameters[0].Values.Should().Equal(value, "second");
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
            using (var writer = new IcalContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new IcalContentLine(name, "x"));

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("property name");
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
                exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
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
            using (var writer = new IcalContentLineWriter(text))
            {
                var line = new IcalContentLine(Summary, new[] { new IcalContentLineParameter(parameterName, "v") }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("parameter name");
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Summary);
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
            using (var writer = new IcalContentLineWriter(text))
            {
                var line = new IcalContentLine(Summary, new[] { new IcalContentLineParameter(ParameterP, "ok", value) }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("parameter X-P");
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Summary);
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
            using (var writer = new IcalContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new IcalContentLine(Description, value));

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("property value");
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Description);
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
            using (var writer = new IcalContentLineWriter(text))
            {
                var line = new IcalContentLine(Summary, new[] { new IcalContentLineParameter(ParameterP, "ok", value) }, "x");

                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("parameter X-P");
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Summary);
                text.ToString().Should().BeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(LoneSurrogateValues), DisableDiscoveryEnumeration = true)]
        public void WriteLine_LoneSurrogateInValue_ThrowsAndWritesNothing(string value)
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLine(new IcalContentLine(Description, value));

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Contain("property value");
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Description);
                text.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_InvalidLineAfterAValidLine_ThrowsWithItsLineNumber()
        {
            // arrange
            using (var writer = new IcalContentLineWriter(new StringWriter()))
            {
                writer.WriteLine(new IcalContentLine("A", "1"));

                // act
                Action testAction = () => writer.WriteLine(new IcalContentLine("B", "\n"));

                // assert
                testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
            }
        }

        [Fact]
        public void WriteLine_AfterInvalidLine_WriterContinues()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                writer.WriteLine(new IcalContentLine("A", "1"));
                Record.Exception(() => writer.WriteLine(new IcalContentLine("B", "\n")));

                // act
                writer.WriteLine(new IcalContentLine("C", "3"));

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
            using (var writer = new IcalContentLineWriter(text))
            {
                var lines = new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "\0"), new IcalContentLine("C", "3") };

                // act
                Action testAction = () => writer.WriteLines(lines);

                // assert
                testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                text.ToString().Should().Be(LineAWithCrlf);
            }
        }

        [Fact]
        public void WriteLines_NullLine_ThrowsAfterWritingLinesBeforeIt()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                // act
                Action testAction = () => writer.WriteLines(new[] { new IcalContentLine("A", "1"), null });

                // assert
                testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lines");
                text.ToString().Should().Be(LineAWithCrlf);
            }
        }

        public static TheoryData<Action<IcalContentLineWriter>> NullArgumentCalls => new TheoryData<Action<IcalContentLineWriter>>
        {
            writer => writer.WriteLine(null),
            writer => writer.WriteLines(null),
        };

        [Theory]
        [MemberData(nameof(NullArgumentCalls), DisableDiscoveryEnumeration = true)]
        public void Write_NullArgument_ThrowsArgumentNull(Action<IcalContentLineWriter> call)
        {
            // arrange
            using (var writer = new IcalContentLineWriter(new StringWriter()))
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
            var value = new string('a', 75 - "DESCRIPTION:".Length);

            // act
            var testResult = WriteAll(new IcalContentLine(Description, value));

            // assert
            testResult.Should().Be($"DESCRIPTION:{value}\r\n");
        }

        [Fact]
        public void WriteLine_OneOctetOverFoldLength_IsFoldedAtFoldLength()
        {
            // arrange
            var value = new string('a', 75 - "DESCRIPTION:".Length) + "b";

            // act
            var testResult = WriteAll(new IcalContentLine(Description, value));

            // assert
            testResult.Should().Be(Crlf("DESCRIPTION:" + value.Substring(0, value.Length - 1), " b"));
        }

        [Fact]
        public void WriteLine_LongLine_EveryPhysicalLineIsFoldLength()
        {
            // arrange
            var value = new string('a', 63) + new string('b', 74) + new string('c', 74) + "dd";

            // act
            var testResult = WriteAll(new IcalContentLine(Description, value));

            // assert
            testResult.Should().Be(Crlf("DESCRIPTION:" + new string('a', 63), " " + new string('b', 74), " " + new string('c', 74), " dd"));
        }

        [Theory]
        [InlineData("é", 72, false)] // 2 + 72 + 2 = 76 octets
        [InlineData("é", 71, true)] // 2 + 71 + 2 = 75 octets
        [InlineData("€", 71, false)] // 2 + 71 + 3 = 76 octets
        [InlineData("€", 70, true)] // 2 + 70 + 3 = 75 octets
        [InlineData("😀", 70, false)] // 2 + 70 + 4 = 76 octets
        [InlineData("😀", 69, true)] // 2 + 69 + 4 = 75 octets
        public void WriteLine_MultiOctetCharacterAtFoldLength_IsNeverSplit(string character, int asciiLength, bool fits)
        {
            // arrange
            var prefix = new string('a', asciiLength);

            // act
            var testResult = WriteAll(new IcalContentLine("X", prefix + character + "z"));

            // assert
            var expected = fits ? Crlf("X:" + prefix + character, " z") : Crlf("X:" + prefix, " " + character + "z");
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_FoldInsideParameters_IsAllowed()
        {
            // arrange
            var line = new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter("CN", new string('n', 80)) }, "mailto:a@example.com");

            // act
            var testResult = WriteAll(line);

            // assert
            AssertFolded(testResult, "ATTENDEE;CN=" + new string('n', 80) + ":mailto:a@example.com", new IcalOptions());
        }

        [Fact]
        public void WriteLine_TabFoldWhitespace_StartsContinuationLinesWithTab()
        {
            // arrange
            var options = new IcalOptions { FoldWhitespace = '\t' };
            var value = new string('a', 100);

            // act
            var testResult = WriteAll(options, new IcalContentLine("X", value));

            // assert
            testResult.Should().Be(Crlf("X:" + new string('a', 73), "\t" + new string('a', 27)));
        }

        [Fact]
        public void WriteLine_BareLineFeed_EndsAndFoldsLinesWithLineFeed()
        {
            // arrange
            var options = new IcalOptions { LineBreak = "\n" };
            var value = new string('a', 100);

            // act
            var testResult = WriteAll(options, new IcalContentLine("X", value), new IcalContentLine("Y", "1"));

            // assert
            testResult.Should().Be("X:" + new string('a', 73) + "\n " + new string('a', 27) + "\nY:1\n");
        }

        [Fact]
        public void WriteLine_BareLineFeed_ReadsBackWhenAccepted()
        {
            // arrange
            var options = new IcalOptions { LineBreak = "\n" };
            var line = new IcalContentLine(Description, string.Concat(Enumerable.Repeat("Café 😀 ", 40)));

            // act
            using (var reader = CreateReader(WriteAll(options, line), new IcalOptions { AcceptBareLineFeed = true }))
            {
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(line.Value);
                reader.PhysicalLinesRead.Should().BeGreaterThan(1);
            }
        }

        [Fact]
        public void WriteLine_CustomFoldLength_FoldsAtThatLength()
        {
            // arrange
            var options = new IcalOptions { FoldLength = 10 };

            // act
            var testResult = WriteAll(options, new IcalContentLine(Summary, "abcdefghijkl"));

            // assert
            testResult.Should().Be(Crlf("SUMMARY:ab", " cdefghijk", " l"));
        }

        [Fact]
        public void WriteLine_MinimumFoldLength_PutsOneCharacterOnEachLineThatCannotHoldMore()
        {
            // arrange
            var options = new IcalOptions { FoldLength = 4 };

            // act
            var testResult = WriteAll(options, new IcalContentLine("X", "😀😀ab€"));

            // assert
            // A continuation line holds 3 octets after the white space, so a 4-octet character goes on a line of its own, one octet over.
            testResult.Should().Be(Crlf("X:", " 😀", " 😀", " ab", " €"));
        }

        public static TheoryData<int, string, char> FoldOptionsData()
        {
            var data = new TheoryData<int, string, char>();
            foreach (var foldLength in new[] { 5, 6, 7, 8, 13, 75, 76 })
            {
                data.Add(foldLength, "\r\n", ' ');
            }

            data.Add(75, "\n", ' ');
            data.Add(75, "\r\n", '\t');
            data.Add(9, "\n", '\t');
            return data;
        }

        [Theory]
        [MemberData(nameof(FoldOptionsData))]
        public void WriteLine_RandomLines_FoldGreedilyWithoutSplittingCharacters(int foldLength, string lineBreak, char foldWhitespace)
        {
            // arrange
            var options = new IcalOptions { FoldLength = foldLength, LineBreak = lineBreak, FoldWhitespace = foldWhitespace };
            string[] pieces = new[] { "a", "Z", " ", "\t", "é", "€", "会", "😀", "^", "\"", ":", ";", ",", "\\n" };
            var random = new Random(181);
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text, options))
            {
                for (var lineIndex = 0; lineIndex < 200; lineIndex++)
                {
                    var value = string.Concat(Enumerable.Range(0, random.Next(0, 120)).Select(_ => pieces[random.Next(pieces.Length)]));
                    text.GetStringBuilder().Clear();

                    // act
                    writer.WriteLine(new IcalContentLine("X-TEST", value));

                    // assert
                    AssertFolded(text.ToString(), "X-TEST:" + value, options);
                }
            }
        }

        #endregion

        #region Round trips

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public void WriteLines_EveryFixture_ReadsBackTheSameLines(string fixtureName, int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.All[fixtureName], bufferSize: bufferSize))
            {
                var lines = ReadAll(reader);
                var expected = lines.Select(LineSnapshot.FromLine).ToList();

                // act
                var testResult = ReadAll(CreateReader(WriteAll(lines.ToArray()), bufferSize: bufferSize)).Select(LineSnapshot.FromLine).ToList();

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering().Excluding(snapshot => snapshot.PhysicalLineNumber));
            }
        }

        [Fact]
        public void WriteLines_LineReadFromSource_KeepsItsRawParameters()
        {
            // arrange
            using (var reader = CreateReader(RawParametersLine))
            {
                var line = reader.ReadLine();

                // act
                var testResult = WriteAll(line);

                // assert
                testResult.Should().Be(RawParametersLine);
            }
        }

        [Fact]
        public void WriteLines_LineBuiltFromParametersRead_EncodesThem()
        {
            // arrange
            using (var reader = CreateReader(RawParametersLine))
            {
                var line = reader.ReadLine();
                var constructed = new IcalContentLine(line.Name, line.Parameters, line.Value);

                // act
                var testResult = WriteAll(constructed);

                // assert
                testResult.Should().Be("ATTENDEE;CN=John;X-A=a^^xb;ROLE=CHAIR;MEMBER=\"mailto:a@b.c\",x:mailto:x\r\n");
            }
        }

        [Fact]
        public void WriteLines_InputFoldedAtFoldLength_IsWrittenBackByteForByte()
        {
            // arrange
            var text = Crlf(
                "BEGIN:VEVENT",
                "ATTENDEE;CN=\"Jane Doe\";X-NOTE=a^xb:mailto:jane.doe@example.com",
                "DESCRIPTION:" + new string('a', 63),
                " " + new string('b', 74),
                " ccc",
                "SUMMARY:" + new string('x', 66),
                " é€😀",
                "ORGANIZER;CN=" + new string('o', 62),
                " ;ROLE=CHAIR:mailto:o@example.com",
                "END:VEVENT");
            var data = new UTF8Encoding(false).GetBytes(text);
            using (var reader = CreateStreamReader(data))
            {
                var lines = ReadAll(reader);
                using (var stream = new MemoryStream())
                {
                    // act
                    using (var writer = new IcalContentLineWriter(stream))
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
        public static TheoryData<string, IcalOptions, string, Func<IcalOptions, IcalContentLineWriter>> InvalidOptions()
        {
            var options = new List<(string, IcalOptions, string)>
            {
                ("FoldLength = 3", new IcalOptions { FoldLength = 3 }, "fold length"),
                ("FoldLength = 0", new IcalOptions { FoldLength = 0 }, "fold length"),
                ("FoldLength = -1", new IcalOptions { FoldLength = -1 }, "fold length"),
                ("FoldWhitespace = x", new IcalOptions { FoldWhitespace = 'x' }, "fold white space"),
                ("FoldWhitespace = LF", new IcalOptions { FoldWhitespace = '\n' }, "fold white space"),
                ("FoldWhitespace = CR", new IcalOptions { FoldWhitespace = '\r' }, "fold white space"),
                ("FoldWhitespace = no-break space", new IcalOptions { FoldWhitespace = '\xA0' }, "fold white space"),
                ("LineBreak = CR", new IcalOptions { LineBreak = "\r" }, "line break"),
                ("LineBreak empty", new IcalOptions { LineBreak = "" }, "line break"),
                ("LineBreak = LF CR", new IcalOptions { LineBreak = "\n\r" }, "line break"),
                ("LineBreak = CRLF CRLF", new IcalOptions { LineBreak = "\r\n\r\n" }, "line break"),
                ("LineBreak = space LF", new IcalOptions { LineBreak = " \n" }, "line break"),
                ("LineBreak null", new IcalOptions { LineBreak = null }, "line break"),
            };
            var constructors = new Func<IcalOptions, IcalContentLineWriter>[]
            {
                invalidOptions => new IcalContentLineWriter(new StringWriter(), invalidOptions),
                invalidOptions => new IcalContentLineWriter(new MemoryStream(), invalidOptions),
            };
            var data = new TheoryData<string, IcalOptions, string, Func<IcalOptions, IcalContentLineWriter>>();
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
        public void Constructor_InvalidOption_ThrowsArgumentException(string option, IcalOptions options, string expectedMessagePart,
            Func<IcalOptions, IcalContentLineWriter> constructor)
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
            var options = new IcalOptions { FoldLength = 4, FoldWhitespace = '\t', LineBreak = "\n" };

            // act
            Action testAction = () => new IcalContentLineWriter(new StringWriter(), options).Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Constructor_DefaultOptions_FollowRfc5545()
        {
            // act
            var testResult = new IcalOptions();

            // assert
            testResult.FoldLength.Should().Be(75);
            testResult.FoldWhitespace.Should().Be(' ');
            testResult.LineBreak.Should().Be("\r\n");
        }

        [Fact]
        public void Constructor_TextWriterWithEncoding_Throws()
        {
            // act
            Action testAction = () => new IcalContentLineWriter(new StringWriter(), new IcalOptions { Encoding = Encoding.UTF8 });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("options");
        }

        [Fact]
        public void Constructor_NullArguments_Throw()
        {
            // act
            var testActions = new Action[]
            {
                () => _ = new IcalContentLineWriter((Stream)null),
                () => _ = new IcalContentLineWriter((TextWriter)null),
                () => _ = new IcalContentLineWriter((string)null),
                () => _ = new IcalContentLineWriter(new MemoryStream(), null),
                () => _ = new IcalContentLineWriter(new StringWriter(), null),
                () => _ = new IcalContentLineWriter("unused.ics", null)
            };

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void Constructor_ReadOnlyStream_Throws()
        {
            // act
            Action testAction = () => new IcalContentLineWriter(new MemoryStream(new byte[0], false));

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("icalStream");
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
                using (var writer = new IcalContentLineWriter(stream))
                {
                    writer.WriteLine(new IcalContentLine(Summary, "Café"));
                }

                // assert
                stream.ToArray().Should().Equal(new UTF8Encoding(false).GetBytes("SUMMARY:Café\r\n"));
            }
        }

        [Fact]
        public void Stream_EncodingOption_IsApplied()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var encoding = new UnicodeEncoding(false, false);
                var expected = encoding.GetBytes("SUMMARY:Caf\xE9\r\n");

                // act
                using (var writer = new IcalContentLineWriter(stream, new IcalOptions { Encoding = encoding }))
                {
                    writer.WriteLine(new IcalContentLine(Summary, "Caf\xE9"));
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
                var writer = new IcalContentLineWriter(stream);
                writer.WriteLine(new IcalContentLine("A", "1"));

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
            using (var writer = new IcalContentLineWriter(new StringWriter()))
            {
                // act
                writer.WriteLine(new IcalContentLine(Description, new string('a', 200)));
                var afterFirst = writer.LinesWritten;
                writer.WriteLines(new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "2") });
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
            using (var writer = new IcalContentLineWriter(new StringWriter()))
            {
                var progress = new RecordingProgress();
                writer.WriteLine(new IcalContentLine("A", "0"));

                // act
                writer.WriteLines(new[] { new IcalContentLine("A", "1"), new IcalContentLine("A", "2") }, progress);
                writer.WriteLines(new[] { new IcalContentLine("A", "3") }, progress);

                // assert
                progress.Reports.Should().Equal(new IcalWriteProgress(2), new IcalWriteProgress(3), new IcalWriteProgress(4));
            }
        }

        [Fact]
        public void WriteLines_Progress_IsReportedAfterTheLineIsWritten()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                var written = new List<string>();
                var progress = new RecordingProgress(_ => written.Add(text.ToString()));

                // act
                writer.WriteLines(new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "2") }, progress);

                // assert
                written.Should().Equal(LineAWithCrlf, Crlf("A:1", "B:2"));
            }
        }

        [Fact]
        public void WriteLines_ProgressThrows_EndsCallAfterLineIsWritten()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                var progress = new RecordingProgress(_ => throw new ApplicationException("stop"));

                // act
                Action testAction = () => writer.WriteLines(new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "2") }, progress);

                // assert
                testAction.Should().Throw<ApplicationException>();
                text.ToString().Should().Be(LineAWithCrlf);
                writer.LinesWritten.Should().Be(1);
            }
        }

        // Each comparison of the progress of three lines, with whether it is true.
        public static TheoryData<Func<IcalWriteProgress, bool>, bool> ProgressComparisons => new TheoryData<Func<IcalWriteProgress, bool>, bool>
        {
            { progress => progress.Equals(new IcalWriteProgress(3)), true },
            { progress => progress == new IcalWriteProgress(3), true },
            { progress => progress != new IcalWriteProgress(4), true },
            { progress => progress.Equals((object)new IcalWriteProgress(3)), true },
            { progress => progress.Equals(null), false },
        };

        [Theory]
        [MemberData(nameof(ProgressComparisons), DisableDiscoveryEnumeration = true)]
        public void WriteProgress_Equality_ComparesCounts(Func<IcalWriteProgress, bool> comparison, bool expected)
        {
            // arrange
            var progress = new IcalWriteProgress(3);

            // act
            var testResult = comparison(progress);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteProgress_GetHashCode_SameCounts_IsTheSame()
        {
            // arrange
            var progress = new IcalWriteProgress(3);
            var expected = new IcalWriteProgress(3).GetHashCode();

            // act
            var testResult = progress.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteProgress_ToString_GivesEachCount()
        {
            // arrange
            var progress = new IcalWriteProgress(3);

            // act
            var testResult = progress.ToString();

            // assert
            testResult.Should().Be("LinesWritten = 3, ComponentsWritten = 0, CalendarsWritten = 0");
        }

        [Fact]
        public void WriteProgress_LinesWritten_Default_IsZero()
        {
            // arrange
            var progress = default(IcalWriteProgress);

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
            using (var writer = new IcalContentLineWriter(text))
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
            using (var writer = new IcalContentLineWriter(stream))
            {
                writer.WriteLine(new IcalContentLine("A", "1"));

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
            using (var writer = new IcalContentLineWriter(new StringWriter()))
            {
                IEnumerable<IcalContentLine> Lines()
                {
                    yield return new IcalContentLine("A", "1");
                    writer.WriteLine(new IcalContentLine("B", "2"));
                }

                // act
                Action testAction = () => writer.WriteLines(Lines());

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
                writer.LinesWritten.Should().Be(1);
            }
        }

        public static TheoryData<Action<IcalContentLineWriter>> MembersAfterDispose()
        {
            var line = new IcalContentLine("A", "1");
            return new TheoryData<Action<IcalContentLineWriter>>
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
        public void Member_AfterDispose_ThrowsObjectDisposed(Action<IcalContentLineWriter> member)
        {
            // arrange
            var writer = new IcalContentLineWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineWriter));
        }

        [Fact]
        public void WriteLines_DisposedDuringEnumeration_ThrowsObjectDisposed()
        {
            // arrange
            using (var text = new TrackingStringWriter())
            {
                var writer = new IcalContentLineWriter(text);

                IEnumerable<IcalContentLine> Lines()
                {
                    yield return new IcalContentLine("A", "1");
                    writer.Dispose();
                    yield return new IcalContentLine("B", "2");
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
            var writer = new IcalContentLineWriter(new MemoryStream());
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
                var writer = new IcalContentLineWriter(text);
                writer.WriteLine(new IcalContentLine("A", "1"));

                // act
                writer.Dispose();

                // assert
                text.FlushCount.Should().Be(0);
                text.IsDisposed.Should().BeFalse();
            }
        }

        #endregion

        /// <summary>
        /// Checks that a written line is folded greedily at the fold length, with no character split across physical lines, and unfolds to the
        /// logical line.
        /// </summary>
        private static void AssertFolded(string written, string logicalLine, IcalOptions options)
        {
            var strictUtf8 = new UTF8Encoding(false, true);
            written.Should().EndWith(options.LineBreak);
            var physicalLines = written.Substring(0, written.Length - options.LineBreak.Length).Split(new[] { options.LineBreak }, StringSplitOptions.None);

            var unfolded = new StringBuilder(physicalLines[0]);
            for (var lineIndex = 0; lineIndex < physicalLines.Length; lineIndex++)
            {
                var physicalLine = physicalLines[lineIndex];

                // Strict encoding throws on a lone surrogate, which is what splitting a pair would leave.
                var octets = strictUtf8.GetByteCount(physicalLine);
                octets.Should().BeLessThanOrEqualTo(options.FoldLength, "physical line {0} is \"{1}\"", lineIndex, physicalLine);
                if (lineIndex > 0)
                {
                    physicalLine[0].Should().Be(options.FoldWhitespace);
                    physicalLine.Length.Should().BeGreaterThan(1);
                    unfolded.Append(physicalLine, 1, physicalLine.Length - 1);
                }

                if (lineIndex < physicalLines.Length - 1)
                {
                    var nextLine = physicalLines[lineIndex + 1];
                    var nextCharacter = nextLine.Substring(1, char.IsSurrogatePair(nextLine, 1) ? 2 : 1);
                    (octets + strictUtf8.GetByteCount(nextCharacter)).Should().BeGreaterThan(options.FoldLength, "lines are folded only when the next character does not fit");
                }
            }

            unfolded.ToString().Should().Be(logicalLine);
        }

        private static IcalContentLine ReadFirstLine(string text)
        {
            using (var reader = CreateReader(text))
            {
                return reader.ReadLine();
            }
        }

        private static string WriteAll(params IcalContentLine[] lines)
        {
            return WriteAll(new IcalOptions(), lines);
        }

        private static string WriteAll(IcalOptions options, params IcalContentLine[] lines)
        {
            using (var text = new StringWriter())
            {
                using (var writer = new IcalContentLineWriter(text, options))
                {
                    writer.WriteLines(lines);
                }

                return text.ToString();
            }
        }
    }
}
