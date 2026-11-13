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

        // Each terminator, as the key, with each CR or LF that isn't the terminator, as the value, which replaces LineBreakPlaceholder.  A
        // non-escaped field can't hold a CR or LF (RFC 4180 section 2, TEXTDATA).
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
        private static readonly InvalidFieldCase[] InvalidLastFieldValues = new InvalidFieldCase[]
        {
            // Unterminated quote at the end of the stream.
            new InvalidFieldCase("a,\"b", new[] { new[] { "a", "" } }, new BadValue("b", 1)),
            // Text before an unterminated quote at the end of the stream: one violation at the quote, and another at the end of the stream.
            new InvalidFieldCase("a,x\"b", new[] { new[] { "a", "" } }, new BadValue("x", 1), new BadValue("xb", 1)),
            // Closing quote at the end of the stream.
            new InvalidFieldCase("a,x\"b\"", new[] { new[] { "a", "" } }, new BadValue("x", 1)),
            // Closing quote followed by the terminator.
            new InvalidFieldCase("a,x\"b\"\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("x", 1)),
            // Text after the closing quote, then the end of the stream.
            new InvalidFieldCase("a,\"b\"c", new[] { new[] { "a", "" } }, new BadValue("bc", 1)),
            // Text after the closing quote, then the terminator.
            new InvalidFieldCase("a,\"b\"c\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("bc", 1)),
        };

        // One row for each place in an unquoted field that a CR or LF that isn't the terminator can be, read by both bare line break theories.
        private static readonly InvalidFieldCase[] BareLineBreakInUnquotedFieldValues = new InvalidFieldCase[]
        {
            // Inside the field.
            new InvalidFieldCase("a,b" + LineBreakPlaceholder + "c\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("b" + LineBreakPlaceholder, 1)),
            // Just before a delimiter.
            new InvalidFieldCase("a,b" + LineBreakPlaceholder + ",c\nd", new[] { new[] { "a", "", "c" }, new[] { "d" } },
                new BadValue("b" + LineBreakPlaceholder, 1)),
            // Just before the terminator.
            new InvalidFieldCase("a,b" + LineBreakPlaceholder + "\nd", new[] { new[] { "a", "" }, new[] { "d" } }, new BadValue("b" + LineBreakPlaceholder, 1)),
            // At the end of the stream.
            new InvalidFieldCase("a,b" + LineBreakPlaceholder, new[] { new[] { "a", "" } }, new BadValue("b" + LineBreakPlaceholder, 1)),
        };

        // One row for each way a CR or LF that isn't the terminator can follow a closing quote, read by both line break after closing quote
        // theories.
        private static readonly InvalidFieldCase[] LineBreakAfterClosingQuoteValues = new InvalidFieldCase[]
        {
            // Then more text: the line break and the character after it are each a violation.
            new InvalidFieldCase("a,\"b\"" + LineBreakPlaceholder + "c\nd", new[] { new[] { "a", "" }, new[] { "d" } },
                new BadValue("b" + LineBreakPlaceholder, 1), new BadValue("b" + LineBreakPlaceholder + "c", 1)),
            // Then the end of the stream.
            new InvalidFieldCase("a,\"b\"" + LineBreakPlaceholder, new[] { new[] { "a", "" } }, new BadValue("b" + LineBreakPlaceholder, 1)),
        };

        // Each character outside TEXTDATA, as the key, with the extended characters it needs, as the value.
        private static readonly KeyValuePair<char, CsvExtendedCharacters>[] OutsideTextDataAllowed = new KeyValuePair<char, CsvExtendedCharacters>[]
        {
            new KeyValuePair<char, CsvExtendedCharacters>(Tab, CsvExtendedCharacters.Control),
            new KeyValuePair<char, CsvExtendedCharacters>(Null, CsvExtendedCharacters.Control),
            new KeyValuePair<char, CsvExtendedCharacters>(Delete, CsvExtendedCharacters.Control),
            new KeyValuePair<char, CsvExtendedCharacters>(LatinSmallLetterEWithAcute, CsvExtendedCharacters.NonAscii),
            new KeyValuePair<char, CsvExtendedCharacters>(NextLine, CsvExtendedCharacters.Control | CsvExtendedCharacters.NonAscii),
            new KeyValuePair<char, CsvExtendedCharacters>(NextLine, AllFlagsAndUndefinedBit),
        };

        // Each character outside TEXTDATA, as the key, with extended characters that don't include all it needs, as the value.  The first rows
        // are the strict default.
        private static readonly KeyValuePair<char, CsvExtendedCharacters>[] OutsideTextDataNotAllowed = new KeyValuePair<char, CsvExtendedCharacters>[]
        {
            new KeyValuePair<char, CsvExtendedCharacters>(Tab, CsvExtendedCharacters.None),
            new KeyValuePair<char, CsvExtendedCharacters>(Null, CsvExtendedCharacters.None),
            new KeyValuePair<char, CsvExtendedCharacters>(Delete, CsvExtendedCharacters.None),
            new KeyValuePair<char, CsvExtendedCharacters>(LatinSmallLetterEWithAcute, CsvExtendedCharacters.None),
            new KeyValuePair<char, CsvExtendedCharacters>(NextLine, CsvExtendedCharacters.None),
            new KeyValuePair<char, CsvExtendedCharacters>(Tab, CsvExtendedCharacters.NonAscii),
            new KeyValuePair<char, CsvExtendedCharacters>(LatinSmallLetterEWithAcute, CsvExtendedCharacters.Control),
            new KeyValuePair<char, CsvExtendedCharacters>(NextLine, CsvExtendedCharacters.Control),
            new KeyValuePair<char, CsvExtendedCharacters>(NextLine, CsvExtendedCharacters.NonAscii),
        };

        // One row for each place in a field that a character outside TEXTDATA can be, read by both outside TEXTDATA theories.
        private static readonly InvalidFieldCase[] OutsideTextDataValues = new InvalidFieldCase[]
        {
            // Inside an unquoted field.
            new InvalidFieldCase("a,b" + OutsideTextDataPlaceholder + "c\nd", new[] { new[] { "a", "" }, new[] { "d" } },
                new BadValue("b" + OutsideTextDataPlaceholder, 1)),
            // Alone in an unquoted field, before a delimiter.
            new InvalidFieldCase("a," + OutsideTextDataPlaceholder + ",c\nd", new[] { new[] { "a", "", "c" }, new[] { "d" } },
                new BadValue(OutsideTextDataPlaceholder, 1)),
            // Twice in an unquoted field: only the first is reported, as the value is already invalid.
            new InvalidFieldCase("a,b" + OutsideTextDataPlaceholder + "c" + OutsideTextDataPlaceholder + "\nd", new[] { new[] { "a", "" }, new[] { "d" } },
                new BadValue("b" + OutsideTextDataPlaceholder, 1)),
            // Twice in a quoted field.
            new InvalidFieldCase("a,\"b" + OutsideTextDataPlaceholder + "c" + OutsideTextDataPlaceholder + "\"\nd", new[] { new[] { "a", "" }, new[] { "d" } },
                new BadValue("b" + OutsideTextDataPlaceholder, 1)),
            // In two fields: each field reports its own.
            new InvalidFieldCase(OutsideTextDataPlaceholder + "," + OutsideTextDataPlaceholder + "\nd", new[] { new[] { "", "" }, new[] { "d" } },
                new BadValue(OutsideTextDataPlaceholder, 0), new BadValue(OutsideTextDataPlaceholder, 1)),
            // At the end of the stream.
            new InvalidFieldCase("a,b" + OutsideTextDataPlaceholder, new[] { new[] { "a", "" } }, new BadValue("b" + OutsideTextDataPlaceholder, 1)),
            // Inside a quoted field.
            new InvalidFieldCase("a,\"b" + OutsideTextDataPlaceholder + "c\"\nd", new[] { new[] { "a", "" }, new[] { "d" } },
                new BadValue("b" + OutsideTextDataPlaceholder, 1)),
            // Just before the closing quote.
            new InvalidFieldCase("a,\"b" + OutsideTextDataPlaceholder + "\",c\nd", new[] { new[] { "a", "", "c" }, new[] { "d" } },
                new BadValue("b" + OutsideTextDataPlaceholder, 1)),
        };

        public static TheoryData<string, char, string, int, CsvExtendedCharacters, string[][]> OutsideTextDataCases =>
            OutsideTextDataRecordCases(OutsideTextDataNotAllowed, OutsideTextDataValues);

        public static TheoryData<string, char, string, int, CsvExtendedCharacters, BadValue[]> OutsideTextDataBadValueCases =>
            OutsideTextDataBadValueCasesOf(OutsideTextDataNotAllowed, OutsideTextDataValues);

        public static TheoryData<string, char, string, int, CsvExtendedCharacters> UnhandledOutsideTextDataCases =>
            OutsideTextDataInputCases(OutsideTextDataNotAllowed, UnhandledOutsideTextDataInput);

        public static TheoryData<string, char, string, int, CsvExtendedCharacters, string[][]> AllowedOutsideTextDataCases =>
            OutsideTextDataRecordCases(OutsideTextDataAllowed, new[]
            {
                new InvalidFieldCase(AllowedOutsideTextDataInput,
                    new[] { new[] { "a", "b" + OutsideTextDataPlaceholder + "c" }, new[] { "d" + OutsideTextDataPlaceholder } }),
            });

        public static TheoryData<string, char, string, int, BadValue[]> SurrogatePairOutsideTextDataBadValueCases()
        {
            var data = BadValueCases(SurrogatePairInUnquotedFieldInput, new BadValue(SurrogatePairBadValue, 1));
            foreach (var badValueCase in BadValueCases(SurrogatePairInQuotedFieldInput, new BadValue(SurrogatePairBadValue, 1)))
            {
                data.Add((string)badValueCase[0], (char)badValueCase[1], (string)badValueCase[2], (int)badValueCase[3], (BadValue[])badValueCase[4]);
            }

            return data;
        }

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
            return InvalidFieldRecordCases(WithoutLineBreak(Terminators), InvalidLastFieldValues);
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

        // The raw value runs to the end of the stream, so it holds a delimiter and terminators, which are translated like the input.
        public static TheoryData<string, char, string, int, BadValue[]> UnterminatedQuoteBadValueCases =>
            BadValueCases(UnterminatedQuoteInput, new BadValue("d\ne,f\n", 1));

        public static TheoryData<string, char, string, int> UnhandledUnterminatedQuoteCases =>
            InputCases(UnhandledUnterminatedQuoteInput);

        // With a two-character terminator, a lone first character inside a quoted value is part of the value.
        public static TheoryData<string, char, string, int, string[][]> CarriageReturnWithoutLineFeedInsideQuotesCases =>
            RecordCases(new[] { CrLf }, "\"a\rb\",c\n\"d\r\"", new[] { "a\rb", "c" }, new[] { "d\r" });

        // A lone first character of a terminator that isn't CR or LF is ordinary content in an unquoted field.
        public static TheoryData<string, char, string, int, string[][]> LoneTerminatorStartInUnquotedFieldCases =>
            RecordCases(new[] { PipePairTerminator }, "a|b\nc", new[] { "a|b" }, new[] { "c" });

        public static TheoryData<string, char, string, int, string[][]> BareLineBreakInUnquotedFieldCases =>
            InvalidFieldRecordCases(BareLineBreaks, BareLineBreakInUnquotedFieldValues);

        public static TheoryData<string, char, string, int, BadValue[]> BareLineBreakInUnquotedFieldBadValueCases =>
            InvalidFieldBadValueCases(BareLineBreaks, BareLineBreakInUnquotedFieldValues);

        public static TheoryData<string, char, string, int> UnhandledBareLineBreakInUnquotedFieldCases =>
            InputCases(BareLineBreaks, UnhandledBareLineBreakInUnquotedFieldInput);

        public static TheoryData<string, char, string, int, string[][]> LineBreakAfterClosingQuoteCases =>
            InvalidFieldRecordCases(BareLineBreaks, LineBreakAfterClosingQuoteValues);

        public static TheoryData<string, char, string, int, BadValue[]> LineBreakAfterClosingQuoteBadValueCases =>
            InvalidFieldBadValueCases(BareLineBreaks, LineBreakAfterClosingQuoteValues);

        public static TheoryData<string, char, string, int, BadValue[]> WhiteSpaceBeforeOpeningQuoteBadValueCases =>
            BadValueCases(WhiteSpaceBeforeOpeningQuoteInput, new BadValue(" ", 1));

        // Each whitespace character after the closing quote is a violation.
        public static TheoryData<string, char, string, int, BadValue[]> WhiteSpaceAfterClosingQuoteBadValueCases =>
            BadValueCases(WhiteSpaceAfterClosingQuoteInput, new BadValue("a ", 0), new BadValue("a \t", 0));

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceBeforeOpeningQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceBeforeOpeningQuoteInput, new BadValue("x", 1));

        public static TheoryData<string, char, string, int, BadValue[]> NonWhiteSpaceAfterClosingQuoteBadValueCases =>
            BadValueCases(NonWhiteSpaceAfterClosingQuoteInput, new BadValue("ab", 0), new BadValue("abc", 0), new BadValue("abc ", 0));

        // The whitespace after the closing quote is the violation.  The second opening quote, past it, isn't reported again.
        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterQuotedValueInput, new BadValue("a ", 0));

        // An empty quoted value is no exception.
        public static TheoryData<string, char, string, int, BadValue[]> SecondQuotedSectionAfterEmptyQuotedValueBadValueCases =>
            BadValueCases(SecondQuotedSectionAfterEmptyQuotedValueInput, new BadValue(" ", 0));

        public static TheoryData<string, char, string, int, BadValue[]> InvalidLastFieldValueBadValueCases()
        {
            return InvalidFieldBadValueCases(WithoutLineBreak(Terminators), InvalidLastFieldValues);
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

        private const string UnhandledBareLineBreakInUnquotedFieldInput = "a,b" + LineBreakPlaceholder + "c\nd";

        // Stands in an input for a line break that isn't the terminator.  The line break cases put each of BareLineBreaks in its place.
        private const string LineBreakPlaceholder = "~";

        // A two-character terminator whose first character isn't CR or LF.
        private const string PipePairTerminator = "||";

        // Characters outside TEXTDATA, which is %x20-21 / %x23-2B / %x2D-7E (RFC 4180 section 2).
        private const char Tab = '\x0009';
        private const char Null = '\x0000';
        private const char Delete = '\x007F';
        private const char LatinSmallLetterEWithAcute = '\x00E9';

        // A control character outside US-ASCII (C1), so it needs both kinds of extended character.
        private const char NextLine = '\x0085';

        // A character outside the Basic Multilingual Plane, U+1F600, which is a surrogate pair in UTF-16.  The pair is one character, so it is
        // reported whole, once.
        private const string GrinningFace = "\xD83D\xDE00";
        private const string SurrogatePairInUnquotedFieldInput = "a,b" + GrinningFace + "c" + GrinningFace + "\nd";
        private const string SurrogatePairInQuotedFieldInput = "a,\"b" + GrinningFace + "c\"\nd";
        private const string SurrogatePairBadValue = "b" + GrinningFace;

        // Every flag of CsvExtendedCharacters and a bit it doesn't define.  The undefined bit doesn't stop the flags from allowing a character.
        private const CsvExtendedCharacters AllFlagsAndUndefinedBit = (CsvExtendedCharacters)7;

        // Stands in an input for a character outside TEXTDATA.  The cases put each character under test in its place.
        private const string OutsideTextDataPlaceholder = "^";

        private const string UnhandledOutsideTextDataInput = "a,b" + OutsideTextDataPlaceholder + "c\nd";

        // Read with every kind of extended character the character in its place needs: the character is kept in both fields.
        private const string AllowedOutsideTextDataInput = "a,b" + OutsideTextDataPlaceholder + "c\n\"d" + OutsideTextDataPlaceholder + "\"";

        // Control characters used as the delimiter and terminator, which TEXTDATA leaves out (RFC 4180 section 2).
        private const char TabDelimiter = '\x0009';
        private const string RecordSeparator = "\x001E";
        private const string UnitSeparator = "\x001F";

        // A quoted field holds the delimiter in place of COMMA, and the terminator in place of CRLF (RFC 4180 section 2, escaped).
        private const string TabInQuotedFieldInput = "\"a\x0009b\"\x0009c";
        private const string TabInQuotedFieldValue = "a\x0009b";
        private const string RecordSeparatorInQuotedFieldInput = "\"a" + RecordSeparator + "b\"" + RecordSeparator + "c";
        private const string RecordSeparatorInQuotedFieldValue = "a" + RecordSeparator + "b";

        // A lone first character of a two-character control terminator is ordinary content, so outside TEXTDATA in an unquoted field.
        private const string ControlPairTerminator = RecordSeparator + UnitSeparator;
        private const string LoneControlTerminatorStartInput = "a" + RecordSeparator + "b" + ControlPairTerminator + "c";
        private const string LoneControlTerminatorStartBadValue = "a" + RecordSeparator;

        // With another delimiter, COMMA is TEXTDATA like any other printable character.
        private const char Semicolon = ';';
        private const string CommaInUnquotedFieldInput = "a,b;c";
        private const string CommaInUnquotedFieldValue = "a,b";

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
        [MemberData(nameof(LoneTerminatorStartInUnquotedFieldCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_LoneTerminatorStartInUnquotedField_KeepsCharacterInFieldValue(string input, char delimiter, string terminator, int bufferSize, string[][] expectedRecords)
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

        [Theory]
        [MemberData(nameof(OutsideTextDataCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CharacterOutsideTextData_ReturnsEmptyFieldValue(string input, char delimiter, string terminator, int bufferSize,
            CsvExtendedCharacters extendedCharacters, string[][] expectedRecords)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(new List<BadValue>()), extendedCharacters))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(OutsideTextDataBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CharacterOutsideTextData_CallsBadValueHandler(string input, char delimiter, string terminator, int bufferSize,
            CsvExtendedCharacters extendedCharacters, BadValue[] expectedBadValues)
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, CollectBadValues(badValues), extendedCharacters))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(expectedBadValues);
            }
        }

        [Theory]
        [MemberData(nameof(UnhandledOutsideTextDataCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CharacterOutsideTextDataWithoutBadValueHandler_ThrowsCsvFileException(string input, char delimiter, string terminator,
            int bufferSize, CsvExtendedCharacters extendedCharacters)
        {
            // arrange
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, extendedCharacters: extendedCharacters))
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
        [MemberData(nameof(AllowedOutsideTextDataCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_CharacterOutsideTextDataWithExtendedCharacters_KeepsCharacter(string input, char delimiter, string terminator,
            int bufferSize, CsvExtendedCharacters extendedCharacters, string[][] expectedRecords)
        {
            // arrange
            // Without a bad value handler, a violation would throw.
            using (var csvReader = CreateReader(input, delimiter, terminator, bufferSize, extendedCharacters: extendedCharacters))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(expectedRecords, config => config.WithStrictOrdering());
            }
        }

        [Theory]
        [MemberData(nameof(SurrogatePairOutsideTextDataBadValueCases), DisableDiscoveryEnumeration = true)]
        public void TryReadRecord_SurrogatePairOutsideTextData_CallsBadValueHandlerOnceWithWholePair(string input, char delimiter, string terminator,
            int bufferSize, BadValue[] expectedBadValues)
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

        [Fact]
        public void TryReadRecord_ControlDelimiterInQuotedField_KeepsDelimiter()
        {
            // arrange
            // Without a bad value handler, a violation would throw.
            using (var csvReader = CreateReader(TabInQuotedFieldInput, TabDelimiter, CrLf, DefaultBufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(new[] { new[] { TabInQuotedFieldValue, "c" } }, config => config.WithStrictOrdering());
            }
        }

        [Fact]
        public void TryReadRecord_ControlTerminatorInQuotedField_KeepsTerminator()
        {
            // arrange
            // Without a bad value handler, a violation would throw.
            using (var csvReader = CreateReader(RecordSeparatorInQuotedFieldInput, ',', RecordSeparator, DefaultBufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(new[] { new[] { RecordSeparatorInQuotedFieldValue }, new[] { "c" } }, config => config.WithStrictOrdering());
            }
        }

        [Fact]
        public void TryReadRecord_LoneControlTerminatorStartInUnquotedField_CallsBadValueHandler()
        {
            // arrange
            var badValues = new List<BadValue>();
            using (var csvReader = CreateReader(LoneControlTerminatorStartInput, ',', ControlPairTerminator, DefaultBufferSize, CollectBadValues(badValues)))
            {

                // act
                _ = ReadAllRecords(csvReader);

                // assert
                badValues.Should().Equal(new BadValue(LoneControlTerminatorStartBadValue, 0));
            }
        }

        [Fact]
        public void TryReadRecord_CommaInUnquotedFieldWithAnotherDelimiter_KeepsComma()
        {
            // arrange
            // Without a bad value handler, a violation would throw.
            using (var csvReader = CreateReader(CommaInUnquotedFieldInput, Semicolon, CrLf, DefaultBufferSize))
            {

                // act
                var testResult = ReadAllRecords(csvReader);

                // assert
                testResult.Should().BeEquivalentTo(new[] { new[] { CommaInUnquotedFieldValue, "c" } }, config => config.WithStrictOrdering());
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
        /// An input with an invalid field, the records expected from it and the bad values expected for it.
        /// </summary>
        private sealed class InvalidFieldCase
        {
            public InvalidFieldCase(string input, string[][] expectedRecords, params BadValue[] expectedBadValues)
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
        /// Pairs each terminator with an empty line break, for inputs with no <see cref="LineBreakPlaceholder"/>.
        /// </summary>
        private static KeyValuePair<string, string>[] WithoutLineBreak(string[] terminators) =>
            terminators.Select(terminator => new KeyValuePair<string, string>(terminator, string.Empty)).ToArray();

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the records expected from it.
        /// </summary>
        private static TheoryData<string, char, string, int, string[][]> RecordCases(string input, params string[][] expectedRecords) =>
            RecordCases(Terminators, input, expectedRecords);

        private static TheoryData<string, char, string, int, string[][]> RecordCases(string[] terminators, string input, params string[][] expectedRecords)
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
            AddRecordCases(data, WithoutLineBreak(terminators), input, expectedRecords);

            return data;
        }

        /// <summary>
        /// Makes one case for each row, delimiter, terminator and line break pair, and buffer size, from the input and records of each row.
        /// </summary>
        private static TheoryData<string, char, string, int, string[][]> InvalidFieldRecordCases(KeyValuePair<string, string>[] terminatorsAndLineBreaks,
            InvalidFieldCase[] invalidFieldCases)
        {
            var data = new TheoryData<string, char, string, int, string[][]>();
            foreach (var invalidFieldCase in invalidFieldCases)
            {
                AddRecordCases(data, terminatorsAndLineBreaks, invalidFieldCase.Input, invalidFieldCase.ExpectedRecords);
            }

            return data;
        }

        private static void AddRecordCases(TheoryData<string, char, string, int, string[][]> data, KeyValuePair<string, string>[] terminatorsAndLineBreaks,
            string input, string[][] expectedRecords)
        {
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminatorAndLineBreak in terminatorsAndLineBreaks)
                {
                    var translatedInput = Translate(input, delimiter, terminatorAndLineBreak);
                    var translatedRecords = expectedRecords
                        .Select(fieldValues => fieldValues.Select(fieldValue => Translate(fieldValue, delimiter, terminatorAndLineBreak)).ToArray())
                        .ToArray();
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(translatedInput, delimiter, terminatorAndLineBreak.Key, bufferSize, translatedRecords);
                    }
                }
            }
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input and the bad values expected from it.  The expected raw field
        /// values are translated like the input.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> BadValueCases(string input, params BadValue[] expectedBadValues)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            AddBadValueCases(data, WithoutLineBreak(Terminators), input, expectedBadValues);

            return data;
        }

        /// <summary>
        /// Makes one case for each row, delimiter, terminator and line break pair, and buffer size, from the input and bad values of each row.
        /// </summary>
        private static TheoryData<string, char, string, int, BadValue[]> InvalidFieldBadValueCases(KeyValuePair<string, string>[] terminatorsAndLineBreaks,
            InvalidFieldCase[] invalidFieldCases)
        {
            var data = new TheoryData<string, char, string, int, BadValue[]>();
            foreach (var invalidFieldCase in invalidFieldCases)
            {
                AddBadValueCases(data, terminatorsAndLineBreaks, invalidFieldCase.Input, invalidFieldCase.ExpectedBadValues);
            }

            return data;
        }

        private static void AddBadValueCases(TheoryData<string, char, string, int, BadValue[]> data, KeyValuePair<string, string>[] terminatorsAndLineBreaks,
            string input, BadValue[] expectedBadValues)
        {
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminatorAndLineBreak in terminatorsAndLineBreaks)
                {
                    var translatedInput = Translate(input, delimiter, terminatorAndLineBreak);
                    var translatedBadValues = expectedBadValues
                        .Select(badValue => new BadValue(Translate(badValue.RawFieldValue, delimiter, terminatorAndLineBreak), badValue.FieldIndex))
                        .ToArray();
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(translatedInput, delimiter, terminatorAndLineBreak.Key, bufferSize, translatedBadValues);
                    }
                }
            }
        }

        /// <summary>
        /// Makes one case for each delimiter, terminator and buffer size, from an input.
        /// </summary>
        private static TheoryData<string, char, string, int> InputCases(string input) => InputCases(WithoutLineBreak(Terminators), input);

        private static TheoryData<string, char, string, int> InputCases(KeyValuePair<string, string>[] terminatorsAndLineBreaks, string input)
        {
            var data = new TheoryData<string, char, string, int>();
            foreach (var delimiter in Delimiters)
            {
                foreach (var terminatorAndLineBreak in terminatorsAndLineBreaks)
                {
                    var translatedInput = Translate(input, delimiter, terminatorAndLineBreak);
                    foreach (var bufferSize in BufferSizes)
                    {
                        data.Add(translatedInput, delimiter, terminatorAndLineBreak.Key, bufferSize);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Translates a template written with <c>,</c> as the delimiter and <c>\n</c> as the terminator, then puts the line break of the pair in
        /// place of <see cref="LineBreakPlaceholder"/>, so a line break isn't translated as the terminator.
        /// </summary>
        private static string Translate(string template, char delimiter, KeyValuePair<string, string> terminatorAndLineBreak) =>
            template.Replace(',', delimiter).Replace("\n", terminatorAndLineBreak.Key).Replace(LineBreakPlaceholder, terminatorAndLineBreak.Value);

        /// <summary>
        /// Makes one case for each character and its extended characters, row, delimiter, terminator and buffer size, from the input and records of
        /// each row.  The character takes the place of <see cref="OutsideTextDataPlaceholder"/>.
        /// </summary>
        private static TheoryData<string, char, string, int, CsvExtendedCharacters, string[][]> OutsideTextDataRecordCases(
            KeyValuePair<char, CsvExtendedCharacters>[] charactersAndExtendedCharacters, InvalidFieldCase[] invalidFieldCases)
        {
            var data = new TheoryData<string, char, string, int, CsvExtendedCharacters, string[][]>();
            foreach (var characterAndExtendedCharacters in charactersAndExtendedCharacters)
            {
                var character = characterAndExtendedCharacters.Key.ToString();
                foreach (var invalidFieldCase in invalidFieldCases)
                {
                    var records = invalidFieldCase.ExpectedRecords
                        .Select(fieldValues => fieldValues.Select(fieldValue => fieldValue.Replace(OutsideTextDataPlaceholder, character)).ToArray())
                        .ToArray();
                    var recordCases = new TheoryData<string, char, string, int, string[][]>();
                    AddRecordCases(recordCases, WithoutLineBreak(Terminators), invalidFieldCase.Input.Replace(OutsideTextDataPlaceholder, character), records);
                    foreach (var recordCase in recordCases)
                    {
                        data.Add((string)recordCase[0], (char)recordCase[1], (string)recordCase[2], (int)recordCase[3], characterAndExtendedCharacters.Value,
                            (string[][])recordCase[4]);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each character and its extended characters, row, delimiter, terminator and buffer size, from the input and bad values of
        /// each row.  The character takes the place of <see cref="OutsideTextDataPlaceholder"/>.
        /// </summary>
        private static TheoryData<string, char, string, int, CsvExtendedCharacters, BadValue[]> OutsideTextDataBadValueCasesOf(
            KeyValuePair<char, CsvExtendedCharacters>[] charactersAndExtendedCharacters, InvalidFieldCase[] invalidFieldCases)
        {
            var data = new TheoryData<string, char, string, int, CsvExtendedCharacters, BadValue[]>();
            foreach (var characterAndExtendedCharacters in charactersAndExtendedCharacters)
            {
                var character = characterAndExtendedCharacters.Key.ToString();
                foreach (var invalidFieldCase in invalidFieldCases)
                {
                    var badValues = invalidFieldCase.ExpectedBadValues
                        .Select(badValue => new BadValue(badValue.RawFieldValue.Replace(OutsideTextDataPlaceholder, character), badValue.FieldIndex))
                        .ToArray();
                    var badValueCases = new TheoryData<string, char, string, int, BadValue[]>();
                    AddBadValueCases(badValueCases, WithoutLineBreak(Terminators), invalidFieldCase.Input.Replace(OutsideTextDataPlaceholder, character), badValues);
                    foreach (var badValueCase in badValueCases)
                    {
                        data.Add((string)badValueCase[0], (char)badValueCase[1], (string)badValueCase[2], (int)badValueCase[3], characterAndExtendedCharacters.Value,
                            (BadValue[])badValueCase[4]);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Makes one case for each character and its extended characters, delimiter, terminator and buffer size, from an input.  The character takes
        /// the place of <see cref="OutsideTextDataPlaceholder"/>.
        /// </summary>
        private static TheoryData<string, char, string, int, CsvExtendedCharacters> OutsideTextDataInputCases(
            KeyValuePair<char, CsvExtendedCharacters>[] charactersAndExtendedCharacters, string input)
        {
            var data = new TheoryData<string, char, string, int, CsvExtendedCharacters>();
            foreach (var characterAndExtendedCharacters in charactersAndExtendedCharacters)
            {
                foreach (var inputCase in InputCases(input.Replace(OutsideTextDataPlaceholder, characterAndExtendedCharacters.Key.ToString())))
                {
                    data.Add((string)inputCase[0], (char)inputCase[1], (string)inputCase[2], (int)inputCase[3], characterAndExtendedCharacters.Value);
                }
            }

            return data;
        }

        private static CsvFieldBadValue<CsvFileRecordLine> CollectBadValues(List<BadValue> badValues) =>
            context => badValues.Add(new BadValue(context.RawFieldValue, context.FieldIndex));

        private static CsvFileReader<CsvFileRecordLine> CreateReader(string input, char delimiter, string terminator, int bufferSize,
            CsvFieldBadValue<CsvFileRecordLine> badValueHandler = null, CsvExtendedCharacters extendedCharacters = CsvExtendedCharacters.None)
        {
            var options = new CsvFileOptions
            {
                FieldDelimiterCharacter = delimiter,
                TerminatorLineBreak = terminator,
                BadValueHandler = badValueHandler,
                ExtendedCharacters = extendedCharacters
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
