using System;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the shared formatter's groups, caret modes and fold modes, which File.iCal doesn't use.  The errors are recorded by kind, so these
    /// don't depend on File.iCal's messages.
    /// </summary>
    public class ContentLineFormatterTests
    {
        private const string CrLf = "\r\n";
        private const string Group = "item1";
        private const string Name = "TEL";
        private const string ParameterName = "P";
        private const int DefaultFoldLength = 75;
        private const int ShortFoldLength = 10;
        private const string Name73Characters = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string Name74Characters = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string Name75Characters = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // ref. RFC 6350 §3.3: contentline = [group "."] name *(";" param) ":" value CRLF
        private const string GroupedLine = "item1.TEL:x\r\n";

        // "item1.TEL:" is exactly the short fold length, so the group counts towards where the line folds.
        private const string GroupedLineFoldedAtShortLength = "item1.TEL:\r\n abcdef\r\n";

        private const string UnfoldedValue = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";
        private const string UnfoldedLine = "TEL:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz\r\n";

        // A shorter line, formatted after UnfoldedLine into the same buffer.
        private const string UnfoldedShortLine = "TEL:abcdefghijkl\r\n";

        [Fact]
        public void Format_AlwaysQuotedParameter_QuotesAValueThatDoesNotNeedIt()
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, "Ab");

            // act
            var length = formatter.Format(line, 1, ContentLineCaretMode.Encode, ContentLineFoldMode.Fold, name => name == ParameterName);

            // assert
            new string(formatter.Buffer, 0, length).Should().Be("TEL;P=\"Ab\":x\r\n");
        }

        [Fact]
        public void Format_AlwaysQuotedParameterHoldingAComma_QuotesItOnce()
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, "a,b");

            // act
            var length = formatter.Format(line, 1, ContentLineCaretMode.Encode, ContentLineFoldMode.Fold, name => name == ParameterName);

            // assert
            new string(formatter.Buffer, 0, length).Should().Be("TEL;P=\"a,b\":x\r\n");
        }

        [Fact]
        public void Format_OtherParameter_QuotesAValueOnlyIfItHasTo()
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, "Ab");

            // act
            var length = formatter.Format(line, 1, ContentLineCaretMode.Encode, ContentLineFoldMode.Fold, name => name == "OTHER");

            // assert
            new string(formatter.Buffer, 0, length).Should().Be("TEL;P=Ab:x\r\n");
        }

        [Fact]
        public void Format_Group_IsWrittenBeforeTheName()
        {
            // arrange
            var formatter = CreateFormatter();

            // act
            var testResult = Format(formatter, new TestLine(Group, Name, "x"));

            // assert
            testResult.Should().Be(GroupedLine);
        }

        [Fact]
        public void Format_Group_CountsTowardsTheFold()
        {
            // arrange
            var formatter = CreateFormatter(ShortFoldLength);

            // act
            var testResult = Format(formatter, new TestLine(Group, Name, "abcdef"));

            // assert
            testResult.Should().Be(GroupedLineFoldedAtShortLength);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a_b")]
        [InlineData("a.b")]
        [InlineData("a b")]
        public void Format_InvalidGroup_ReportsIt(string group)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(group, Name, "x");

            // act
            Action testAction = () => Format(formatter, line);

            // assert
            var error = testAction.Should().Throw<RecordedErrorException>().Which.Error;
            error.Kind.Should().Be(ContentLineErrorKind.InvalidGroupToWrite);
            error.SubjectName.Should().Be(group);
        }

        [Fact]
        public void Format_InvalidValueOnGroupedLine_ReportsTheGroup()
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(Group, Name, "\0");

            // act
            Action testAction = () => Format(formatter, line);

            // assert
            var error = testAction.Should().Throw<RecordedErrorException>().Which.Error;
            error.Kind.Should().Be(ContentLineErrorKind.InvalidValueCharacter);
            error.Group.Should().Be(Group);
        }

        [Theory]
        [InlineData("a^b", "TEL;P=a^^b:x\r\n")]
        [InlineData("a\"b", "TEL;P=a^'b:x\r\n")]
        [InlineData("a\r\nb", "TEL;P=a^nb:x\r\n")]
        [InlineData("a\nb:c", "TEL;P=\"a^nb:c\":x\r\n")]
        public void Format_DefaultCaretMode_EncodesCarets(string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, value);

            // act
            var testResult = Format(formatter, line);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("a^b", "TEL;P=a^b:x\r\n")]
        [InlineData("^n^'^^", "TEL;P=^n^'^^:x\r\n")]
        [InlineData("a:^", "TEL;P=\"a:^\":x\r\n")]
        public void Format_RejectCaretMode_WritesCaretAsOrdinaryCharacter(string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, value);

            // act
            var testResult = Format(formatter, line, ContentLineCaretMode.Reject);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("a\"b", '"')]
        [InlineData("a\rb", '\r')]
        [InlineData("a\nb", '\n')]
        [InlineData("a\r\nb", '\r')]
        public void Format_RejectCaretMode_ReportsCharactersThatNeedCarets(string value, char expectedCharacter)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(Group, Name, "x", ParameterName, value);

            // act
            Action testAction = () => Format(formatter, line, ContentLineCaretMode.Reject);

            // assert
            var error = testAction.Should().Throw<RecordedErrorException>().Which.Error;
            error.Kind.Should().Be(ContentLineErrorKind.ParameterValueNeedsCaretEncoding);
            error.SubjectName.Should().Be(ParameterName);
            error.Character.Should().Be(expectedCharacter);
            error.Group.Should().Be(Group);
        }

        [Theory]
        [InlineData("a\"b", "TEL;P=ab:x\r\n")]
        [InlineData("\"", "TEL;P=:x\r\n")]
        [InlineData("a\nb", "TEL;P=a b:x\r\n")]
        [InlineData("a\rb", "TEL;P=a b:x\r\n")]
        [InlineData("a\r\nb", "TEL;P=a b:x\r\n")]
        [InlineData("a\n\nb", "TEL;P=a  b:x\r\n")]
        [InlineData("a^b", "TEL;P=a^b:x\r\n")]
        [InlineData("a\"b:c", "TEL;P=\"ab:c\":x\r\n")]
        public void Format_OmitCaretMode_WritesWithoutCarets(string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, value);

            // act
            var testResult = Format(formatter, line, ContentLineCaretMode.Omit);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("\0")]
        [InlineData("a\x7F")]
        public void Format_OmitCaretMode_StillReportsControlCharacters(string value)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, "x", ParameterName, value);

            // act
            Action testAction = () => Format(formatter, line, ContentLineCaretMode.Omit);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.InvalidParameterValueCharacterToWrite);
        }

        [Fact]
        public void Format_FoldModeNone_LeavesLongLineWhole()
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, UnfoldedValue);

            // act
            var testResult = Format(formatter, line, foldMode: ContentLineFoldMode.None);

            // assert
            testResult.Should().Be(UnfoldedLine);
        }

        [Fact]
        public void Format_FoldModeNoneAfterLongerLine_WritesOnlyTheNewLine()
        {
            // arrange
            var formatter = CreateFormatter();
            Format(formatter, new TestLine(null, Name, UnfoldedValue), foldMode: ContentLineFoldMode.None);
            var line = new TestLine(null, Name, "abcdefghijkl");

            // act
            var testResult = Format(formatter, line, foldMode: ContentLineFoldMode.None);

            // assert
            testResult.Should().Be(UnfoldedShortLine);
        }

        [Theory]
        [InlineData("short", "TEL:short\r\n")]
        [InlineData("", "TEL:\r\n")]
        // 71 characters, which with "TEL:" make 75, the longest last line.
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrs", "TEL:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrs\r\n")]
        // One more: 70 characters and a '=' are the longest line before a soft line break.
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrst", "TEL:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr=\r\nst\r\n")]
        // =0D stays whole, moving to the next line.
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopq=0Dz", "TEL:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopq=\r\n=0Dz\r\n")]
        public void Format_QuotedPrintableSoftBreak_BreaksTheValueBelow76Characters(string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, value);

            // act
            var testResult = Format(formatter, line, foldMode: ContentLineFoldMode.QuotedPrintableSoftBreak);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        // With its ':', the name makes 74 characters, and the character or '=' after it the 75th, the longest line.
        [InlineData(Name73Characters, "a", Name73Characters + ":a\r\n")]
        [InlineData(Name73Characters, "ab", Name73Characters + ":=\r\nab\r\n")]
        // With its ':', the name makes 75 characters, which leaves room only if there is no value.
        [InlineData(Name74Characters, "", Name74Characters + ":\r\n")]
        public void Format_QuotedPrintableSoftBreakWithLongName_WritesWhatFits(string name, string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, name, value);

            // act
            var testResult = Format(formatter, line, foldMode: ContentLineFoldMode.QuotedPrintableSoftBreak);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(Name74Characters, "a")]
        [InlineData(Name75Characters, "")]
        public void Format_QuotedPrintableSoftBreakWithNoRoomOnTheFirstLine_ReportsLineTooLongToWrite(string name, string value)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, name, value);

            // act
            Action testAction = () => Format(formatter, line, foldMode: ContentLineFoldMode.QuotedPrintableSoftBreak);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.LineTooLongToWrite);
        }

        [Theory]
        [InlineData("", "TEL:\r\n\r\n")]
        [InlineData("SGVsbG8=", "TEL:\r\n    SGVsbG8=\r\n\r\n")]
        [InlineData("  SGVs\tbG8=", "TEL:\r\n    SGVsbG8=\r\n\r\n")]
        // 65 characters: 64 on a line, the longest, and one more.
        [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "TEL:\r\n    AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n    A\r\n\r\n")]
        // 64 characters, which fill their line.
        [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "TEL:\r\n    AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n\r\n")]
        public void Format_Base64Lines_LaysOutTheValueAndAnEmptyLine(string value, string expected)
        {
            // arrange
            var formatter = CreateFormatter();
            var line = new TestLine(null, Name, value);

            // act
            var testResult = Format(formatter, line, foldMode: ContentLineFoldMode.Base64Lines);

            // assert
            testResult.Should().Be(expected);
        }

        private static ContentLineFormatter CreateFormatter(int foldLength = DefaultFoldLength)
        {
            return new ContentLineFormatter(foldLength, ' ', CrLf, new RecordingErrorFactory());
        }

        private static string Format(ContentLineFormatter formatter, TestLine line, ContentLineCaretMode caretMode = ContentLineCaretMode.Encode,
            ContentLineFoldMode foldMode = ContentLineFoldMode.Fold)
        {
            var length = formatter.Format(line, 1, caretMode, foldMode);
            return new string(formatter.Buffer, 0, length);
        }

        private readonly struct TestLine : IContentLineData
        {
            private readonly string _parameterName;
            private readonly string _parameterValue;

            internal TestLine(string group, string name, string value, string parameterName = null, string parameterValue = null)
            {
                Group = group;
                Name = name;
                Value = value;
                _parameterName = parameterName;
                _parameterValue = parameterValue;
            }

            public string Group { get; }

            public string Name { get; }

            public int ParameterCount => _parameterName is null ? 0 : 1;

            public string RawParameters => null;

            public string Value { get; }

            public string GetParameterName(int parameterIndex)
            {
                return _parameterName;
            }

            public string GetParameterValue(int parameterIndex, int valueIndex)
            {
                return _parameterValue;
            }

            public int GetParameterValueCount(int parameterIndex)
            {
                return 1;
            }
        }

        private sealed class RecordingErrorFactory : IContentLineErrorFactory
        {
            public Exception CreateException(ContentLineError error)
            {
                return new RecordedErrorException(error);
            }
        }

        private sealed class RecordedErrorException : Exception
        {
            internal RecordedErrorException(ContentLineError error)
            {
                Error = error;
            }

            internal ContentLineError Error { get; }
        }
    }
}
