using System;
using System.IO;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the shared engine's hooks for a format whose lines continue other than by folding, which File.iCal leaves off.  The errors are recorded by kind, so
    /// these don't depend on File.iCal's messages.
    /// </summary>
    public class ContentLineContinuationTests
    {
        private const int DefaultBufferSize = 1024;
        private const int SmallestBufferSize = 1;

        // ScriptedContinuation reads these from the end of each physical line.
        private const string JoinedLines = "A:b>\r\nc\r\n";
        private const string JoinedLinesKeepingWhitespace = "A:b>\r\n c\r\n";
        private const string JoinedLinesThenNext = "A:b>\r\nc\r\nD:e\r\n";
        private const string JoinUntilEndLines = "A:b+\r\nc+\r\n\r\nD:e\r\n";
        private const string JoinUntilEndThenEndOfData = "A:b+\r\nc+\r\n";
        private const string LinesWithInvalidLine = "A:b!\r\nD:e\r\n";

        private static ContentLineScannerSettings ScriptedSettings(int maxLineLength = int.MaxValue)
        {
            return new ContentLineScannerSettings(false, maxLineLength, continuation: new ScriptedContinuation());
        }

        [Theory]
        [InlineData(DefaultBufferSize)]
        [InlineData(SmallestBufferSize)]
        public void Scan_Join_RemovesWhatTheHookSaysAndKeepsTheNextLine(int bufferSize)
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            var scanner = ReadLastLine(JoinedLinesKeepingWhitespace, settings, bufferSize);

            // assert
            scanner.RawValue.ToString().Should().Be("b c");
        }

        [Fact]
        public void Scan_Join_CountsThePhysicalLines()
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            var scanner = ReadLastLine(JoinedLines, settings, DefaultBufferSize);

            // assert
            new[] { scanner.LinesRead, scanner.PhysicalLinesRead }.Should().Equal(1, 2);
        }

        [Fact]
        public void Scan_LineAfterJoin_StartsOnTheRightPhysicalLine()
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            var scanner = ReadLastLine(JoinedLinesThenNext, settings, DefaultBufferSize);

            // assert
            scanner.PhysicalLineNumber.Should().Be(3);
        }

        [Fact]
        public void Scan_JoinThenEndOfData_CompletesTheLineWithItsLineBreak()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue, requireFinalLineBreak: true, continuation: new ScriptedContinuation());

            // act
            var scanner = ReadLastLine("A:b>\r\n", settings, DefaultBufferSize);

            // assert
            scanner.RawValue.ToString().Should().Be("b");
        }

        [Theory]
        [InlineData(DefaultBufferSize)]
        [InlineData(SmallestBufferSize)]
        public void Scan_JoinUntilEndThenEnd_EndsAtTheLineThatSaysSo(int bufferSize)
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            var scanner = ReadLastLine(JoinUntilEndLines, settings, bufferSize);

            // assert
            scanner.PhysicalLineNumber.Should().Be(4);
        }

        [Fact]
        public void ParseLine_JoinUntilEndThenEndOfData_ReportsMissingContinuationEnd()
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            Action testAction = () => ReadLastLine(JoinUntilEndThenEndOfData, settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.MissingContinuationEnd);
        }

        [Fact]
        public void Scan_Invalid_ReportsItAndFaultsTheScanner()
        {
            // arrange
            var settings = ScriptedSettings();

            // act
            Action testAction = () => ReadLastLine(LinesWithInvalidLine, settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.InvalidContinuationLine);
        }

        [Fact]
        public void Scan_MoreJoinsThanMaxLineLength_ReportsLineTooLong()
        {
            // arrange
            const int maxLineLength = 4;
            var settings = ScriptedSettings(maxLineLength);

            // act
            Action testAction = () => ReadLastLine("A:b>\r\n>\r\n>\r\n>\r\n>\r\n", settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.LineTooLong);
        }

        [Theory]
        [InlineData(DefaultBufferSize)]
        [InlineData(SmallestBufferSize)]
        public void Scan_KeepFoldWhitespace_RemovesOnlyTheLineBreak(int bufferSize)
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            var scanner = ReadLastLine("A:b\r\n c\r\n", settings, bufferSize, keepFoldWhitespace: true);

            // assert
            scanner.RawValue.ToString().Should().Be("b c");
        }

        [Fact]
        public void ParseLine_AllowValuelessParameters_GivesTheNameAsItsValue()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            var scanner = ReadLastLine("TEL;HOME;P=a:1\r\n", settings, DefaultBufferSize, allowValuelessParameters: true);

            // assert
            new[]
            {
                scanner.GetParameterName(0).ToString(),
                scanner.GetRawParameterValue(0, 0).ToString(),
                scanner.GetParameterName(1).ToString(),
                scanner.GetRawParameterValue(1, 0).ToString()
            }.Should().Equal("HOME", "HOME", "P", "a");
        }

        [Fact]
        public void IsParameterValueless_ParameterWithAndWithoutValue_SaysWhich()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            var scanner = ReadLastLine("TEL;HOME;P=a:1\r\n", settings, DefaultBufferSize, allowValuelessParameters: true);

            // assert
            new[] { scanner.IsParameterValueless(0), scanner.IsParameterValueless(1) }.Should().Equal(true, false);
        }

        [Fact]
        public void ParseLine_ValuelessParameterWithHookOff_ReportsExpectedParameterEquals()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            Action testAction = () => ReadLastLine("TEL;HOME:1\r\n", settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.ExpectedParameterEquals);
        }

        [Fact]
        public void BuildValuelessParameterError_Parameter_ReportsExpectedParameterEquals()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);
            var scanner = ReadLastLine("TEL;HOME:1\r\n", settings, DefaultBufferSize, allowValuelessParameters: true);

            // act
            var testResult = scanner.BuildValuelessParameterError(0);

            // assert
            testResult.Should().BeOfType<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.ExpectedParameterEquals);
        }

        [Theory]
        [InlineData(@"a\:b", "a:b")]
        [InlineData(@"a\", @"a\")]
        [InlineData(@"a\nb", "a\nb")]
        public void TryUnescapeText_AcceptOtherEscapes_GivesTheCharacter(string source, string expected)
        {
            // arrange
            var destination = new char[source.Length];

            // act
            ContentLineEscaping.TryUnescapeText(source.AsSpan(), destination, out var written, out _, acceptOtherEscapes: true);

            // assert
            new string(destination, 0, written).Should().Be(expected);
        }

        [Fact]
        public void TryUnescapeText_OtherEscapeByDefault_Fails()
        {
            // arrange
            var destination = new char[4];

            // act
            var testResult = ContentLineEscaping.TryUnescapeText(@"a\:b".AsSpan(), destination, out _, out _);

            // assert
            testResult.Should().BeFalse();
        }

        /// <summary>
        /// Scans and parses every line of a text, and returns the scanner holding the last.
        /// </summary>
        private static ContentLineScanner ReadLastLine(string text, ContentLineScannerSettings settings, int bufferSize, bool keepFoldWhitespace = false, bool allowValuelessParameters = false)
        {
            var scanner = new ContentLineScanner(bufferSize, settings, new RecordingErrorFactory())
            {
                KeepFoldWhitespace = keepFoldWhitespace,
                AllowValuelessParameters = allowValuelessParameters
            };

            using (var reader = new StringReader(text))
            {
                while (true)
                {
                    var status = scanner.Scan();
                    if (status == ContentLineScanner.ScanStatus.NeedMoreInput)
                    {
                        int offset, count;
                        scanner.PrepareRefill(out offset, out count);
                        scanner.CompleteRefill(reader.Read(scanner.Buffer, offset, count));
                    }
                    else if (status == ContentLineScanner.ScanStatus.LineRead)
                    {
                        scanner.ParseLine();
                    }
                    else
                    {
                        return scanner;
                    }
                }
            }
        }

        /// <summary>
        /// Decides from the last character of each physical line: <c>&gt;</c> joins and is removed, <c>+</c> joins until an empty line, and <c>!</c> is invalid.
        /// </summary>
        private sealed class ScriptedContinuation : IContentLineContinuation
        {
            public ContentLineContinuationKind GetContinuation(ReadOnlySpan<char> line, int physicalLineStart, out int dataLength)
            {
                dataLength = line.Length;
                if (physicalLineStart > 0 && physicalLineStart == line.Length) return ContentLineContinuationKind.End;
                if (line.Length == 0) return ContentLineContinuationKind.None;

                switch (line[line.Length - 1])
                {
                    case '>':
                        dataLength = line.Length - 1;
                        return ContentLineContinuationKind.Join;
                    case '+':
                        dataLength = line.Length - 1;
                        return ContentLineContinuationKind.JoinUntilEnd;
                    case '!':
                        return ContentLineContinuationKind.Invalid;
                    default:
                        return ContentLineContinuationKind.None;
                }
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
