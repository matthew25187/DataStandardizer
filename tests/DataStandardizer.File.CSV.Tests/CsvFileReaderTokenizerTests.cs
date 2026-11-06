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

        private static readonly char[] Delimiters = new char[] { ',', ';' };
        private static readonly string[] Terminators = new string[] { CrLf, "\n", "\r" };
        private static readonly int[] BufferSizes = new int[] { 1, 2, 3, 5, DefaultBufferSize };

        // Field values longer than the smaller buffers.  The quoted value has escaped quotes and terminators inside it.
        private static readonly string LongUnquotedValue = new string('u', 100);
        private static readonly string LongQuotedValue = string.Concat(Enumerable.Repeat("q\"\"\nq ", 20));
        private static readonly string LongUnescapedQuotedValue = string.Concat(Enumerable.Repeat("q\"\nq ", 20));

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

        public static TheoryData<string, char, string, int, string[][]> WhiteSpaceAroundQuotedValueCases =>
            RecordCases(WhiteSpaceAroundQuotedValueInput, new[] { "  a b  ", "\tc" }, new[] { " d " });

        public static TheoryData<string, char, string, int, string[][]> NonWhiteSpaceBeforeOpeningQuoteCases =>
            RecordCases(NonWhiteSpaceBeforeOpeningQuoteInput, new[] { "a", "", "c" });

        public static TheoryData<string, char, string, int, string[][]> NonWhiteSpaceAfterClosingQuoteCases =>
            RecordCases(NonWhiteSpaceAfterClosingQuoteInput, new[] { "", "d" });

        public static TheoryData<string, char, string, int, string[][]> SecondQuotedSectionAfterQuotedValueCases =>
            RecordCases(SecondQuotedSectionAfterQuotedValueInput, new[] { "", "c" });

        public static TheoryData<string, char, string, int, string[][]> SecondQuotedSectionAfterEmptyQuotedValueCases =>
            RecordCases(SecondQuotedSectionAfterEmptyQuotedValueInput, new[] { " b", "c" });

        public static TheoryData<string, char, string, int, string[][]> InvalidLastFieldValueCases =>
            RecordCases(InvalidLastFieldValueInput, new[] { "a", "xb" });

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
            RecordCases("a,b\nc,\"d\ne,f\n", new[] { "a", "b" }, new[] { "c", "d\ne,f\n" });

        // With a two-character terminator, a lone first character is ordinary content, inside or after a quoted value.
        public static TheoryData<string, char, string, int, string[][]> CarriageReturnWithoutLineFeedCases =>
            RecordCases(new[] { CrLf }, "a\rb,\"c\"\r,\"d\" \ne\r", new[] { "a\rb", "c\r", "d " }, new[] { "e\r" });

        public static TheoryData<string, char, string, int, BadValue[]> WhiteSpaceAroundQuotedValueBadValueCases =>
            BadValueCases(WhiteSpaceAroundQuotedValueInput);

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceBeforeOpeningQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceBeforeOpeningQuoteInput, new BadValue("x", 1));

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceAfterClosingQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceAfterClosingQuoteInput, new BadValue("ab", 0), new BadValue("abc", 0));

        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterQuotedValueInput, new BadValue("a ", 0));

        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterEmptyQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterEmptyQuotedValueInput);

        public static TheoryData<string, char, string, int, BadValue[]> InvalidLastFieldValueBadValueCases =>
            BadValueCases(InvalidLastFieldValueInput, new BadValue("x", 1));

        public static TheoryData<string, char, string, int> UnhandledNonWhiteSpaceBeforeOpeningQuoteCases =>
            InputCases(UnhandledNonWhiteSpaceBeforeOpeningQuoteInput);

        public static TheoryData<string, char, string, int> UnhandledNonWhiteSpaceAfterClosingQuoteCases =>
            InputCases(UnhandledNonWhiteSpaceAfterClosingQuoteInput);

        // Inputs used by more than one test.
        private const string WhiteSpaceAroundQuotedValueInput = "  \"a b\"  ,\t\"c\"\n \"d\" ";
        private const string NonWhiteSpaceBeforeOpeningQuoteInput = "a,x\"b\",c\n";
        private const string NonWhiteSpaceAfterClosingQuoteInput = "\"a\"bc ,d\n";
        private const string SecondQuotedSectionAfterQuotedValueInput = "\"a\" \"b\",c\n";
        private const string SecondQuotedSectionAfterEmptyQuotedValueInput = "\"\" \"b\",c\n";
        private const string InvalidLastFieldValueInput = "a,x\"b\"\n";
        private const string UnhandledNonWhiteSpaceBeforeOpeningQuoteInput = "a,x\"b\n\",c\nd";
        private const string UnhandledNonWhiteSpaceAfterClosingQuoteInput = "a,b,\"c\"d\ne";

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
        [MemberData(nameof(WhiteSpaceAroundQuotedValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceAroundQuotedValue_KeepsWhiteSpace(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
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
        [MemberData(nameof(WhiteSpaceAroundQuotedValueBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_WhiteSpaceAroundQuotedValue_DoesNotCallBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
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
        public void TryReadRecord_SecondQuotedSectionAfterEmptyQuotedValue_ReturnsBothSections(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            // Only the field value accumulated so far is checked at an opening quote, and an empty quoted value leaves only whitespace.
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
        public void TryReadRecord_SecondQuotedSectionAfterEmptyQuotedValue_DoesNotCallBadValueHandler(string input, char delimiter, string terminator, int bufferSize, BadValue[] expectedBadValues)
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
        public void TryReadRecord_InvalidLastFieldValue_KeepsRawFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
        {
            // arrange
            // Unlike other fields, the last field of a record is not replaced with an empty value when it is invalid.
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
                    .WithMessage("Encountered invalid value at field index 1.")
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
        public void TryReadRecord_UnterminatedQuote_RunsToEndOfStream(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
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
        [MemberData(nameof(CarriageReturnWithoutLineFeedCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CarriageReturnWithoutLineFeed_KeepsCarriageReturnInFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
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
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the records expected from it.
        /// </summary>
        private static TheoryData<string, char, string, int, string[][]> RecordCases(string input, params string[][] expectedRecords) =>
            RecordCases(Terminators, input, expectedRecords);

        private static TheoryData<string, char, string, int, string[][]> RecordCases(string[] terminators, string input, params string[][] expectedRecords)
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
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

            return data;
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the bad values expected from it.  The expected raw field
        /// values hold no delimiter or terminator, so they aren't translated.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> BadValueCases(string input, params BadValue[] expectedBadValues)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
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
