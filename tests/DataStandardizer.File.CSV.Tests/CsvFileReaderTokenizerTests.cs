using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Pins the record tokenizer's handling of edge cases.  Every test runs for each delimiter, terminator and internal buffer size, so buffer refills
    /// land inside fields, inside quotes, between the characters of a two-character terminator and between the two quotes of an escaped quote.
    /// </summary>
    /// <remarks>
    /// The case factories take inputs and expected values written with <c>,</c> as the delimiter and <c>\n</c> as the terminator, and translate them to
    /// each delimiter and terminator under test.
    /// </remarks>
    public class CsvFileReaderTokenizerTests
    {
        /// <summary>
        /// Size of the internal character buffer of a reader that is not given one (16 Ki characters).
        /// </summary>
        private const int DefaultBufferSize = 16384;

        /// <summary>
        /// Two-character terminator, the line break of RFC 4180 section 2.
        /// </summary>
        private const string CrLf = "\r\n";

        private const string FieldIndexDataItem = "FieldIndex";
        private const string InvalidValueAtFieldIndex1Message = "Encountered invalid value at field index 1.";

        private static readonly char[] Delimiters = new char[] { ',', ';' };
        private static readonly string[] Terminators = new string[] { CrLf, "\n", "\r" };
        private static readonly int[] BufferSizes = new int[] { 1, 2, 3, 5, DefaultBufferSize };

        // Each terminator with each CR or LF that isn't the terminator, which a non-escaped field can't hold (RFC 4180 section 2, TEXTDATA).
        private static readonly KeyValuePair<string, string>[] BareLineBreaks = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>(CrLf, "\r"),
            new KeyValuePair<string, string>(CrLf, "\n"),
            new KeyValuePair<string, string>("\n", "\r"),
            new KeyValuePair<string, string>("\r", "\n"),
        };

        // Field values longer than the smaller buffers.  The quoted value has escaped quotes and terminators inside it.
        private static readonly string LongUnquotedValue = new string('u', 100);
        private static readonly string LongQuotedValue = string.Concat(Enumerable.Repeat("q\"\"\nq ", 20));
        private static readonly string LongUnescapedQuotedValue = string.Concat(Enumerable.Repeat("q\"\nq ", 20));

        // One row for each way a record can end on an invalid field, read by both invalid last field theories.  The rows that end with a terminator
        // have a record after it, to show where the stream is left.
        private static readonly InvalidLastFieldValue[] InvalidLastFieldValues = new InvalidLastFieldValue[]
        {
            // Unterminated quote at the end of the stream.
            new InvalidLastFieldValue("a,\"b", new[] { new[] { "a", "" } }, new BadValue("b", 1)),
            // Text before an unterminated quote at the end of the stream: one violation at the quote, and another at the end of the stream.
            new InvalidLastFieldValue("a,x\"b", new[] { new[] { "a", "" } }, new BadValue("x", 1), new BadValue("xb", 1)),
            // Closing quote at the end of the stream.
            new InvalidLastFieldValue("a,x\"b\"", new[] { new[] { "a", "" } }, new BadValue("x", 1)),
            // Closing quote followed by the terminator.
            new InvalidLastFieldValue("a,x\"b\"\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("x", 1)),
            // Text after the closing quote, then the end of the stream.
            new InvalidLastFieldValue("a,\"b\"c", new[] { new[] { "a", "" } }, new BadValue("bc", 1)),
            // Text after the closing quote, then the terminator.
            new InvalidLastFieldValue("a,\"b\"c\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("bc", 1)),
        };

        public static TheoryData<char, string, int> Formats()
        {
            var data = new TheoryData<char, string, int>();
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminator in Terminators)
                {
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(delimiter, terminator, bufferSize);
                    }
                }
            }

            return data;
        }

        public static TheoryData<string, char, string, int, string[][]> TerminatorInsideQuotesCases =>
            RecordCases("\"a\nb\",c\n\"\n\"\nd", new[] { "a\nb", "c" }, new[] { "\n" }, new[] { "d" });

        public static TheoryData<string, char, string, int, string[][]> EscapedQuotesCases =>
            RecordCases("\"\"\"a\",\"b\"\"c\",\"d\"\"\",\"\"\"\"\"\"\n", new[] { "\"a", "b\"c", "d\"", "\"\"" });

        public static TheoryData<string, char, string, int, string[][]> EmptyQuotedFieldsCases =>
            RecordCases("\"\",x,\"\"\n\"\"", new[] { "", "x", "" }, new[] { "" });

        public static TheoryData<string, char, string, int, string[][]> WhiteSpaceBeforeOpeningQuoteCases =>
            RecordCases(WhiteSpaceBeforeOpeningQuoteInput, new[] { "a", "", "c" });

        public static TheoryData<string, char, string, int, string[][]> WhiteSpaceAfterClosingQuoteCases =>
            RecordCases(WhiteSpaceAfterClosingQuoteInput, new[] { "", "d" });

        public static TheoryData<string, char, string, int, string[][]> NonWhiteSpaceBeforeOpeningQuoteCases =>
            RecordCases(NonWhiteSpaceBeforeOpeningQuoteInput, new[] { "a", "", "c" });

        public static TheoryData<string, char, string, int, string[][]> NonWhiteSpaceAfterClosingQuoteCases =>
            RecordCases(NonWhiteSpaceAfterClosingQuoteInput, new[] { "", "d" });

        public static TheoryData<string, char, string, int, string[][]> SecondQuotedSectionAfterQuotedValueCases =>
            RecordCases(SecondQuotedSectionAfterQuotedValueInput, new[] { "", "c" });

        public static TheoryData<string, char, string, int, string[][]> SecondQuotedSectionAfterEmptyQuotedValueCases =>
            RecordCases(SecondQuotedSectionAfterEmptyQuotedValueInput, new[] { "", "c" });

        public static TheoryData<string, char, string, int, string[][]> InvalidLastFieldValueCases()
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
            foreach (var invalidLastFieldValue in InvalidLastFieldValues)
            {
                AddRecordCases(data, Terminators, invalidLastFieldValue.Input, invalidLastFieldValue.ExpectedRecords);
            }

            return data;
        }

        public static TheoryData<string, char, string, int, string[][]> RecordAfterNonWhiteSpaceBeforeOpeningQuoteCases =>
            RecordCases(UnhandledNonWhiteSpaceBeforeOpeningQuoteInput, new[] { "d" });

        public static TheoryData<string, char, string, int, string[][]> RecordAfterNonWhiteSpaceAfterClosingQuoteCases =>
            RecordCases(UnhandledNonWhiteSpaceAfterClosingQuoteInput, new[] { "e" });

        public static TheoryData<string, char, string, int, string[][]> EmptyFieldsAndTrailingDelimiterCases =>
            RecordCases(",a,,\n,\n", new[] { "", "a", "", "" }, new[] { "", "" });

        public static TheoryData<string, char, string, int, string[][]> BlankLinesCases =>
            RecordCases("a\n\n\nb\n", new[] { "a" }, new[] { "" }, new[] { "" }, new[] { "b" });

        public static TheoryData<string, char, string, int, string[][]> FieldValuesLongerThanBufferCases =>
            RecordCases(LongUnquotedValue + ",\"" + LongQuotedValue + "\"," + LongUnquotedValue + "\n",
                new[] { LongUnquotedValue, LongUnescapedQuotedValue, LongUnquotedValue });

        public static TheoryData<string, char, string, int, string[][]> FinalRecordWithTerminatorCases =>
            RecordCases("a,b\nc,\"d\"\n", new[] { "a", "b" }, new[] { "c", "d" });

        public static TheoryData<string, char, string, int, string[][]> FinalRecordWithoutTerminatorCases =>
            RecordCases("a,b\nc,\"d\"", new[] { "a", "b" }, new[] { "c", "d" });

        public static TheoryData<string, char, string, int, string[][]> UnterminatedQuoteCases =>
            RecordCases(UnterminatedQuoteInput, new[] { "a", "b" }, new[] { "c", "" });

        // The raw value runs to the end of the stream, so it holds a delimiter and terminators and is translated like the input.
        public static TheoryData<string, char, string, int, BadValue[]> UnterminatedQuoteBadValueCases =>
            TranslatedBadValueCases(UnterminatedQuoteInput, new BadValue("d\ne,f\n", 1));

        public static TheoryData<string, char, string, int> UnhandledUnterminatedQuoteCases =>
            InputCases(UnhandledUnterminatedQuoteInput);

        // With a two-character terminator, a lone first character inside a quoted value is part of the value.
        public static TheoryData<string, char, string, int, string[][]> CarriageReturnWithoutLineFeedInsideQuotesCases =>
            RecordCases(new[] { CrLf }, "\"a\rb\",c\n\"d\r\"", new[] { "a\rb", "c" }, new[] { "d\r" });

        public static TheoryData<string, char, string, int, string[][]> BareLineBreakInUnquotedFieldCases =>
            LineBreakRecordCases(BareLineBreakInUnquotedFieldTemplate, new[] { "a", "" }, new[] { "d" });

        public static TheoryData<string, char, string, int, BadValue[]> BareLineBreakInUnquotedFieldBadValueCases =>
            LineBreakBadValueCases(BareLineBreakInUnquotedFieldTemplate, new BadValue("b" + LineBreakPlaceholder, 1));

        public static TheoryData<string, char, string, int> UnhandledBareLineBreakInUnquotedFieldCases =>
            LineBreakInputCases(BareLineBreakInUnquotedFieldTemplate);

        public static TheoryData<string, char, string, int, string[][]> LineBreakAfterClosingQuoteCases =>
            LineBreakRecordCases(LineBreakAfterClosingQuoteTemplate, new[] { "a", "" }, new[] { "d" });

        // The line break and the character after it are each a violation.
        public static TheoryData<string, char, string, int, BadValue[]> LineBreakAfterClosingQuoteBadValueCases =>
            LineBreakBadValueCases(LineBreakAfterClosingQuoteTemplate, new BadValue("b" + LineBreakPlaceholder, 1),
                new BadValue("b" + LineBreakPlaceholder + "c", 1));

        public static TheoryData<string, char, string, int, BadValue[]> WhiteSpaceBeforeOpeningQuoteBadValueCases =>
            BadValueCases(WhiteSpaceBeforeOpeningQuoteInput, new BadValue(" ", 1));

        // Each whitespace character after the closing quote is a violation.
        public static TheoryData<string, char, string, int, BadValue[]> WhiteSpaceAfterClosingQuoteBadValueCases =>
            BadValueCases(WhiteSpaceAfterClosingQuoteInput, new BadValue("a ", 0), new BadValue("a \t", 0));

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceBeforeOpeningQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceBeforeOpeningQuoteInput, new BadValue("x", 1));

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceAfterClosingQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceAfterClosingQuoteInput, new BadValue("ab", 0), new BadValue("abc", 0), new BadValue("abc ", 0));

        // The whitespace and the second opening quote are each a violation.
        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterQuotedValueInput, new BadValue("a ", 0), new BadValue("a ", 0));

        // An empty quoted value is no exception: the whitespace and the second opening quote are each a violation.
        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterEmptyQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterEmptyQuotedValueInput, new BadValue(" ", 0), new BadValue(" ", 0));

        public static TheoryData<string, char, string, int, BadValue[]> InvalidLastFieldValueBadValueCases()
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            foreach (var invalidLastFieldValue in InvalidLastFieldValues)
            {
                AddBadValueCases(data, invalidLastFieldValue.Input, invalidLastFieldValue.ExpectedBadValues);
            }

            return data;
        }

        public static TheoryData<string, char, string, int> UnhandledNonWhiteSpaceBeforeOpeningQuoteCases =>
            InputCases(UnhandledNonWhiteSpaceBeforeOpeningQuoteInput);

        public static TheoryData<string, char, string, int> UnhandledNonWhiteSpaceAfterClosingQuoteCases =>
            InputCases(UnhandledNonWhiteSpaceAfterClosingQuoteInput);

        public static TheoryData<string, char, string, int> UnhandledWhiteSpaceBeforeOpeningQuoteCases =>
            InputCases(UnhandledWhiteSpaceBeforeOpeningQuoteInput);

        public static TheoryData<string, char, string, int> UnhandledWhiteSpaceAfterClosingQuoteCases =>
            InputCases(UnhandledWhiteSpaceAfterClosingQuoteInput);

        // Inputs used by more than one test.
        private const string WhiteSpaceBeforeOpeningQuoteInput = "a, \"b\",c\n";
        private const string WhiteSpaceAfterClosingQuoteInput = "\"a\" \t,d\n";
        private const string UnhandledWhiteSpaceBeforeOpeningQuoteInput = "a, \"b\"\nc";
        private const string UnhandledWhiteSpaceAfterClosingQuoteInput = "a,\"b\" \nc";
        private const string NonWhiteSpaceBeforeOpeningQuoteInput = "a,x\"b\",c\n";
        private const string NonWhiteSpaceAfterClosingQuoteInput = "\"a\"bc ,d\n";
        private const string SecondQuotedSectionAfterQuotedValueInput = "\"a\" \"b\",c\n";
        private const string SecondQuotedSectionAfterEmptyQuotedValueInput = "\"\" \"b\",c\n";
        private const string UnhandledNonWhiteSpaceBeforeOpeningQuoteInput = "a,x\"b\n\",c\nd";
        private const string UnhandledNonWhiteSpaceAfterClosingQuoteInput = "a,b,\"c\"d\ne";
        private const string UnterminatedQuoteInput = "a,b\nc,\"d\ne,f\n";
        private const string UnhandledUnterminatedQuoteInput = "a,\"b\nc,d\n";

        // Inputs with a line break that isn't the terminator, written with LineBreakPlaceholder in its place.  The line break cases give each
        // terminator each line break that isn't it.
        private const string BareLineBreakInUnquotedFieldTemplate = "a,b" + LineBreakPlaceholder + "c\nd";
        private const string LineBreakAfterClosingQuoteTemplate = "a,\"b\"" + LineBreakPlaceholder + "c\nd";
        private const string LineBreakPlaceholder = "~";

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_EmptyStream_ReturnsFalse(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(string.Empty, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = csvReader.TryReadRecord(new List<string>());

                // assert
                testResult.Should().BeFalse();
            }
        }

        [Theory]
        [MemberData(nameof(TerminatorInsideQuotesCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_TerminatorInsideQuotes_KeepsTerminatorInFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(EscapedQuotesCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_EscapedQuotes_ReturnsSingleQuotes(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(EmptyQuotedFieldsCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_EmptyQuotedFields_ReturnsEmptyFieldValues(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceBeforeOpeningQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceBeforeOpeningQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceBeforeOpeningQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceBeforeOpeningQuote_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledWhiteSpaceBeforeOpeningQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceBeforeOpeningQuoteWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage(InvalidValueAtFieldIndex1Message)
                    .Which.Data[FieldIndexDataItem].Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceAfterClosingQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(WhiteSpaceAfterClosingQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceAfterClosingQuote_CallsBadValueHandlerForEachCharacter(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledWhiteSpaceAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceAfterClosingQuoteWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage(InvalidValueAtFieldIndex1Message)
                    .Which.Data[FieldIndexDataItem].Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(NonWhiteSpaceBeforeOpeningQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(NonWhiteSpaceBeforeOpeningQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuote_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(NonWhiteSpaceAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(NonWhiteSpaceAfterClosingQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuote_CallsBadValueHandlerForEachCharacter(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(SecondQuotedSectionAfterQuotedValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_SecondQuotedSectionAfterQuotedValue_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(SecondQuotedSectionAfterQuotedValueBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_SecondQuotedSectionAfterQuotedValue_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(SecondQuotedSectionAfterEmptyQuotedValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_SecondQuotedSectionAfterEmptyQuotedValue_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(SecondQuotedSectionAfterEmptyQuotedValueBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_SecondQuotedSectionAfterEmptyQuotedValue_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidLastFieldValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_InvalidLastFieldValue_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            // Like every other field, the last field of a record is replaced with an empty value when it is invalid.
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(InvalidLastFieldValueBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_InvalidLastFieldValue_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledNonWhiteSpaceBeforeOpeningQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuoteWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage(InvalidValueAtFieldIndex1Message)
                    .Which.Data[FieldIndexDataItem].Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(RecordAfterNonWhiteSpaceBeforeOpeningQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuoteWithoutBadValueHandler_SkipsRecord(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                _ = Record.Exception(() => csvReader.TryReadRecord(new List<string>()));

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledNonWhiteSpaceAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuoteWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage("Encountered invalid value at field index 2.")
                    .Which.Data[FieldIndexDataItem].Should().Be(2);
            }
        }

        [Theory]
        [MemberData(nameof(RecordAfterNonWhiteSpaceAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuoteWithoutBadValueHandler_SkipsRecord(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                _ = Record.Exception(() => csvReader.TryReadRecord(new List<string>()));

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(EmptyFieldsAndTrailingDelimiterCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_EmptyFieldsAndTrailingDelimiter_ReturnsEmptyFieldValues(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(BlankLinesCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_BlankLines_ReturnsSingleEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(FieldValuesLongerThanBufferCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_FieldValuesLongerThanBuffer_ReturnsWholeFieldValues(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(FinalRecordWithTerminatorCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_FinalRecordWithTerminator_ReturnsNoFurtherRecord(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(FinalRecordWithoutTerminatorCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_FinalRecordWithoutTerminator_ReturnsFinalRecord(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(UnterminatedQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_UnterminatedQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledUnterminatedQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_UnterminatedQuoteWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage(InvalidValueAtFieldIndex1Message)
                    .Which.Data[FieldIndexDataItem].Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(UnterminatedQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_UnterminatedQuote_CallsBadValueHandlerWithRestOfStream(string input, char delimiter, string terminator, int bufferSize,
            BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(CarriageReturnWithoutLineFeedInsideQuotesCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CarriageReturnWithoutLineFeedInsideQuotes_KeepsCarriageReturnInFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(BareLineBreakInUnquotedFieldCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_BareLineBreakInUnquotedField_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(BareLineBreakInUnquotedFieldBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_BareLineBreakInUnquotedField_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledBareLineBreakInUnquotedFieldCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_BareLineBreakInUnquotedFieldWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize))
            {
                var fieldValues = new List<string>();

                // act
                Action testAction = () => csvReader.TryReadRecord(fieldValues);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .WithMessage(InvalidValueAtFieldIndex1Message)
                    .Which.Data[FieldIndexDataItem].Should().Be(1);
            }
        }

        [Theory]
        [MemberData(nameof(LineBreakAfterClosingQuoteCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_LineBreakAfterClosingQuote_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>())))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(LineBreakAfterClosingQuoteBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_LineBreakAfterClosingQuote_CallsBadValueHandlerForEachCharacter(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        /// <summary>
        /// A call to the bad value handler: the raw field value and field index it was given.
        /// </summary>
        public sealed class BadValue : IEquatable<BadValue>
        {
            public BadValue(string rawFieldValue, int? fieldIndex)
            {
                RawFieldValue = rawFieldValue;
                FieldIndex = fieldIndex;
            }

            public string RawFieldValue { get; }

            public int? FieldIndex { get; }

            public bool Equals(BadValue other) => other != null && RawFieldValue == other.RawFieldValue && FieldIndex == other.FieldIndex;

            public override bool Equals(object obj) => Equals(obj as BadValue);

            public override int GetHashCode() => ((RawFieldValue?.GetHashCode() ?? 0) * 397) ^ FieldIndex.GetHashCode();

            public override string ToString() => $"BadValue {{ RawFieldValue = {RawFieldValue}, FieldIndex = {FieldIndex} }}";
        }

        /// <summary>
        /// A record that ends on an invalid field, the records expected from it and the bad values expected for it.
        /// </summary>
        private sealed class InvalidLastFieldValue
        {
            public InvalidLastFieldValue(string input, string[][] expectedRecords, params BadValue[] expectedBadValues)
            {
                Input = input;
                ExpectedRecords = expectedRecords;
                ExpectedBadValues = expectedBadValues;
            }

            public string Input { get; }

            public string[][] ExpectedRecords { get; }

            public BadValue[] ExpectedBadValues { get; }
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the records expected from it.
        /// </summary>
        private static TheoryData<string, char, string, int, string[][]> RecordCases(string input, params string[][] expectedRecords) =>
            RecordCases(Terminators, input, expectedRecords);

        private static TheoryData<string, char, string, int, string[][]> RecordCases(string[] terminators, string input, params string[][] expectedRecords)
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
            AddRecordCases(data, terminators, input, expectedRecords);

            return data;
        }

        private static void AddRecordCases(TheoryData<string, char, string, int, string[][]> data, string[] terminators, string input, string[][] expectedRecords)
        {
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminator in terminators)
                {
                    var translatedRecords = expectedRecords
                        .Select(fieldValues => fieldValues.Select(fieldValue => Translate(fieldValue, delimiter, terminator)).ToArray())
                        .ToArray();
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(Translate(input, delimiter, terminator), delimiter, terminator, bufferSize, translatedRecords);
                    }
                }
            }
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the bad values expected from it.  The expected raw field
        /// values hold no delimiter or terminator, so they aren't translated.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> BadValueCases(string input, params BadValue[] expectedBadValues)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            AddBadValueCases(data, input, expectedBadValues);

            return data;
        }

        private static void AddBadValueCases(TheoryData<string, char, string, int, BadValue[]> data, string input, BadValue[] expectedBadValues)
        {
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminator in Terminators)
                {
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(Translate(input, delimiter, terminator), delimiter, terminator, bufferSize, expectedBadValues);
                    }
                }
            }
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the bad values expected from it.  The expected raw field
        /// values are translated like the input.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> TranslatedBadValueCases(string input, params BadValue[] expectedBadValues)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminator in Terminators)
                {
                    var translatedBadValues = expectedBadValues
                        .Select(badValue => new BadValue(Translate(badValue.RawFieldValue, delimiter, terminator), badValue.FieldIndex))
                        .ToArray();
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(Translate(input, delimiter, terminator), delimiter, terminator, bufferSize, translatedBadValues);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input.
        /// </summary>
        private static TheoryData<string, char, string, int> InputCases(string input)
        {
            var data = new TheoryData<string, char, string, int>();
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminator in Terminators)
                {
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(Translate(input, delimiter, terminator), delimiter, terminator, bufferSize);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator, line break that isn't the terminator, and buffer size, from an input and the records
        /// expected from it.  The line break replaces <see cref="LineBreakPlaceholder"/> in the input once it is translated.
        /// </summary>
        private static TheoryData<string, char, string, int, string[][]> LineBreakRecordCases(string template, params string[][] expectedRecords)
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
            foreach (var terminatorAndLineBreak in BareLineBreaks)
            {
                var terminator = terminatorAndLineBreak.Key;
                foreach (var delimiter in Delimiters)
                {
                    var input = Translate(template, delimiter, terminator).Replace(LineBreakPlaceholder, terminatorAndLineBreak.Value);
                    var translatedRecords = expectedRecords
                        .Select(fieldValues => fieldValues.Select(fieldValue => Translate(fieldValue, delimiter, terminator)).ToArray())
                        .ToArray();
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(input, delimiter, terminator, bufferSize, translatedRecords);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator, line break that isn't the terminator, and buffer size, from an input and the bad values
        /// expected from it.  The line break replaces <see cref="LineBreakPlaceholder"/> in the input once it is translated, and in the expected raw
        /// field values, which hold no delimiter or terminator.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> LineBreakBadValueCases(string template, params BadValue[] expectedBadValues)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            foreach (var terminatorAndLineBreak in BareLineBreaks)
            {
                var terminator = terminatorAndLineBreak.Key;
                var lineBreak = terminatorAndLineBreak.Value;
                var translatedBadValues = expectedBadValues
                    .Select(badValue => new BadValue(badValue.RawFieldValue.Replace(LineBreakPlaceholder, lineBreak), badValue.FieldIndex))
                    .ToArray();
                foreach (var delimiter in Delimiters)
                {
                    var input = Translate(template, delimiter, terminator).Replace(LineBreakPlaceholder, lineBreak);
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(input, delimiter, terminator, bufferSize, translatedBadValues);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator, line break that isn't the terminator, and buffer size, from an input.  The line break
        /// replaces <see cref="LineBreakPlaceholder"/> once the input is translated.
        /// </summary>
        private static TheoryData<string, char, string, int> LineBreakInputCases(string template)
        {
            var data = new TheoryData<string, char, string, int>();
            foreach (var terminatorAndLineBreak in BareLineBreaks)
            {
                var terminator = terminatorAndLineBreak.Key;
                foreach (var delimiter in Delimiters)
                {
                    var input = Translate(template, delimiter, terminator).Replace(LineBreakPlaceholder, terminatorAndLineBreak.Value);
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(input, delimiter, terminator, bufferSize);
                    }
                }
            }

            return data;
        }

        private static string Translate(string template, char delimiter, string terminator) =>
            template.Replace(',', delimiter).Replace("\n", terminator);

        private static CsvFieldBadValue<CsvFileRecordLine> CollectBadValues(List<BadValue> badValues) =>
            context => badValues.Add(new BadValue(context.RawFieldValue, context.FieldIndex));

        private static CsvFileReader<CsvFileRecordLine> CreateReader(string input, char delimiter, string terminator, int bufferSize,
            CsvFieldBadValue<CsvFileRecordLine> badValueHandler = null)
        {
            var options = new CsvFileOptions
            {
                FieldDelimiterCharacter = delimiter,
                TerminatorLineBreak = terminator,
                BadValueHandler = badValueHandler
            };

            return new CsvFileReader<CsvFileRecordLine>(new StringReader(input), options, bufferSize);
        }

        /// <summary>
        /// Reads every remaining record.  The list constructor reads the records through <see cref="RecordSequence"/>, which reads one record per step.
        /// </summary>
        private static List<string[]> ReadAllRecords(CsvFileReader<CsvFileRecordLine> csvReader) => new List<string[]>(new RecordSequence(csvReader));

        /// <summary>
        /// The records that remain in a reader, each read by one call to <see cref="CsvFileReader{TRecordLine}.TryReadRecord"/>.
        /// </summary>
        private sealed class RecordSequence : IEnumerable<string[]>
        {
            private readonly CsvFileReader<CsvFileRecordLine> _csvReader;

            public RecordSequence(CsvFileReader<CsvFileRecordLine> csvReader)
            {
                _csvReader = csvReader;
            }

            public IEnumerator<string[]> GetEnumerator() => new RecordEnumerator(_csvReader);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class RecordEnumerator : IEnumerator<string[]>
        {
            private readonly CsvFileReader<CsvFileRecordLine> _csvReader;
            private readonly List<string> _fieldValues = new List<string>();

            public RecordEnumerator(CsvFileReader<CsvFileRecordLine> csvReader)
            {
                _csvReader = csvReader;
            }

            public string[] Current { get; private set; } = Array.Empty<string>();

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var hasRecord = _csvReader.TryReadRecord(_fieldValues);
                Current = _fieldValues.ToArray();

                return hasRecord;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
