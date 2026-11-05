using System;
using System.IO;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the shared scanner's settings, which File.iCal leaves off.  The errors are recorded by kind, so these don't depend on File.iCal's
    /// messages.
    /// </summary>
    public class ContentLineScannerTests
    {
        private const string Group = "item1";
        private const string Telephone = "TEL";

        private const int DefaultBufferSize = 1024;
        private const int SmallestBufferSize = 1;

        // ref. RFC 6350 §3.3: contentline = [group "."] name *(";" param) ":" value CRLF
        private const string GroupedLine = "item1.TEL:x";
        private const string HyphenatedGroupLine = "a-b.X-FOO:x";
        private const string MissingGroupLine = ".TEL:x";
        private const string MissingNameLine = "item1.";
        private const string TwoGroupsLine = "a.b.TEL:x";
        private const string GroupedLineWithParameter = "item1.TEL;TYPE=home:123";
        private const int GroupedLineWithParameterValueOffset = 20;

        // U+FEFF, the byte order mark as a character, before a line.
        private const string ByteOrderMarkLine = "\xFEFFTEL:x";

        public static TheoryData<string, string, string> GroupedLineData()
        {
            return new TheoryData<string, string, string>
            {
                { GroupedLine, Group, Telephone },
                { HyphenatedGroupLine, "a-b", "X-FOO" },
                { "TEL:x", string.Empty, Telephone }
            };
        }

        public static TheoryData<string, string> MalformedGroupData()
        {
            return new TheoryData<string, string>
            {
                { MissingGroupLine, nameof(ContentLineErrorKind.ExpectedPropertyName) },
                { TwoGroupsLine, nameof(ContentLineErrorKind.ExpectedNameEnd) },
                { MissingNameLine, nameof(ContentLineErrorKind.ExpectedPropertyName) },
                { "item1.:x", nameof(ContentLineErrorKind.ExpectedPropertyName) }
            };
        }

        public static TheoryData<string, string> GroupNotAllowedData()
        {
            return new TheoryData<string, string>
            {
                { GroupedLine, nameof(ContentLineErrorKind.ExpectedNameEnd) },
                { HyphenatedGroupLine, nameof(ContentLineErrorKind.ExpectedNameEnd) },
                { MissingGroupLine, nameof(ContentLineErrorKind.ExpectedPropertyName) },
                { TwoGroupsLine, nameof(ContentLineErrorKind.ExpectedNameEnd) },
                { MissingNameLine, nameof(ContentLineErrorKind.ExpectedNameEnd) }
            };
        }

        [Theory]
        [MemberData(nameof(GroupedLineData))]
        public void ParseLine_GroupPrefixAllowed_SplitsGroupAndName(string text, string expectedGroup, string expectedName)
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            var scanner = ReadLastLine(text, allowGroupPrefix, DefaultBufferSize);

            // assert
            scanner.Group.ToString().Should().Be(expectedGroup);
            scanner.Name.ToString().Should().Be(expectedName);
        }

        [Fact]
        public void ParseLine_GroupPrefixAllowedAndLineSplitAcrossRefills_SplitsGroupAndName()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            var scanner = ReadLastLine(GroupedLine, allowGroupPrefix, SmallestBufferSize);

            // assert
            scanner.Group.ToString().Should().Be(Group);
            scanner.Name.ToString().Should().Be(Telephone);
        }

        [Fact]
        public void ParseLine_GroupPrefixAllowedAndLineHasParameter_KeepsTheOtherSlices()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            var scanner = ReadLastLine(GroupedLineWithParameter, allowGroupPrefix, DefaultBufferSize);

            // assert
            scanner.GetParameterName(0).ToString().Should().Be("TYPE");
            scanner.GetParameterValue(0, 0).ToString().Should().Be("home");
            scanner.RawValue.ToString().Should().Be("123");
            scanner.ValueOffsetInLine.Should().Be(GroupedLineWithParameterValueOffset);
            scanner.RawLine.ToString().Should().Be(GroupedLineWithParameter);
        }

        [Fact]
        public void ParseLine_LineAfterGroupedLine_HasNoGroup()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            var scanner = ReadLastLine("item1.TEL:x\r\nNOTE:y", allowGroupPrefix, DefaultBufferSize);

            // assert
            scanner.Group.ToString().Should().BeEmpty();
            scanner.Name.ToString().Should().Be("NOTE");
        }

        [Theory]
        [MemberData(nameof(MalformedGroupData))]
        public void ParseLine_GroupPrefixAllowedAndGroupMalformed_ReportsError(string text, string expectedKind)
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            Action testAction = () => ReadLastLine(text, allowGroupPrefix, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.ToString().Should().Be(expectedKind);
        }

        [Fact]
        public void ParseLine_GroupPrefixAllowedAndErrorAfterName_ReportsNameWithoutGroup()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            Action testAction = () => ReadLastLine("item1.TEL;x:1", allowGroupPrefix, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.PropertyName.Should().Be(Telephone);
        }

        [Theory]
        [MemberData(nameof(GroupNotAllowedData))]
        public void ParseLine_GroupPrefixNotAllowed_ReportsError(string text, string expectedKind)
        {
            // arrange
            const bool allowGroupPrefix = false;

            // act
            Action testAction = () => ReadLastLine(text, allowGroupPrefix, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.ToString().Should().Be(expectedKind);
        }

        [Fact]
        public void Constructor_GroupPrefixNotSpecified_LeavesItOff()
        {
            // arrange
            const int maxLineLength = 1;

            // act
            var settings = new ContentLineScannerSettings(false, maxLineLength);

            // assert
            settings.AllowGroupPrefix.Should().BeFalse();
        }

        [Fact]
        public void Constructor_NewRulesNotSpecified_LeavesThemOff()
        {
            // arrange
            const int maxLineLength = 1;

            // act
            var settings = new ContentLineScannerSettings(false, maxLineLength);

            // assert
            new[] { settings.RejectByteOrderMark, settings.RequireFinalLineBreak }.Should().Equal(false, false);
        }

        [Fact]
        public void ParseLine_ErrorAfterGroup_ReportsGroup()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            Action testAction = () => ReadLastLine("item1.TEL;x:1", allowGroupPrefix, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Group.Should().Be(Group);
        }

        [Fact]
        public void ParseLine_ErrorOnLineWithoutGroupAfterGroupedLine_ReportsNoGroup()
        {
            // arrange
            const bool allowGroupPrefix = true;

            // act
            Action testAction = () => ReadLastLine("item1.TEL:1\r\nTEL;x:1\r\n", allowGroupPrefix, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Group.Should().BeNull();
        }

        [Fact]
        public void Scan_ByteOrderMarkAndNotRejected_SkipsIt()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            var scanner = ReadLastLine(ByteOrderMarkLine, settings, DefaultBufferSize);

            // assert
            scanner.Name.ToString().Should().Be(Telephone);
        }

        [Fact]
        public void ParseLine_ByteOrderMarkAndRejected_ReportsExpectedPropertyName()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue, rejectByteOrderMark: true);

            // act
            Action testAction = () => ReadLastLine(ByteOrderMarkLine, settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.ExpectedPropertyName);
        }

        [Fact]
        public void ParseLine_NoFinalLineBreakAndNotRequired_ReadsLine()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue);

            // act
            var scanner = ReadLastLine("TEL:x", settings, DefaultBufferSize);

            // assert
            scanner.RawValue.ToString().Should().Be("x");
        }

        [Theory]
        [InlineData("TEL:x", 1L)]
        [InlineData("TEL:a\r\nTEL:x", 2L)]
        [InlineData("TEL:x\r\n ", 2L)]
        public void ParseLine_NoFinalLineBreakAndRequired_ReportsMissingLineBreak(string text, long expectedPhysicalLineNumber)
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue, requireFinalLineBreak: true);

            // act
            Action testAction = () => ReadLastLine(text, settings, DefaultBufferSize);

            // assert
            var error = testAction.Should().Throw<RecordedErrorException>().Which.Error;
            error.Kind.Should().Be(ContentLineErrorKind.MissingLineBreak);
            error.PropertyName.Should().Be(Telephone);
            error.PhysicalLineNumber.Should().Be(expectedPhysicalLineNumber);
        }

        [Fact]
        public void ParseLine_NoFinalLineBreakRequiredAndSyntaxError_ReportsSyntaxError()
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue, requireFinalLineBreak: true);

            // act
            Action testAction = () => ReadLastLine("TEL", settings, DefaultBufferSize);

            // assert
            testAction.Should().Throw<RecordedErrorException>().Which.Error.Kind.Should().Be(ContentLineErrorKind.MissingValue);
        }

        [Theory]
        [InlineData(DefaultBufferSize)]
        [InlineData(SmallestBufferSize)]
        public void ParseLine_FinalLineBreakRequiredAndPresent_ReadsLine(int bufferSize)
        {
            // arrange
            var settings = new ContentLineScannerSettings(false, int.MaxValue, requireFinalLineBreak: true);

            // act
            var scanner = ReadLastLine("TEL:x\r\n", settings, bufferSize);

            // assert
            scanner.RawValue.ToString().Should().Be("x");
        }

        /// <summary>
        /// Scans and parses every line of a text, and returns the scanner holding the last.
        /// </summary>
        private static ContentLineScanner ReadLastLine(string text, bool allowGroupPrefix, int bufferSize)
        {
            return ReadLastLine(text, new ContentLineScannerSettings(false, int.MaxValue, allowGroupPrefix), bufferSize);
        }

        private static ContentLineScanner ReadLastLine(string text, ContentLineScannerSettings settings, int bufferSize)
        {
            var scanner = new ContentLineScanner(bufferSize, settings, new RecordingErrorFactory());
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
