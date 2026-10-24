using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Pins the record tokenizer's handling of edge cases.  Every test runs for each delimiter, terminator and internal buffer size, so buffer refills
    /// land inside fields, inside quotes, between the characters of a two-character terminator and between the two quotes of an escaped quote.
    /// </summary>
    /// <remarks>
    /// Inputs and expected values are written with <c>,</c> as the delimiter and <c>\n</c> as the terminator, and are translated to the delimiter and
    /// terminator under test.
    /// </remarks>
    public class CsvFileReaderTokenizerTests
    {
        public static TheoryData<char, string, int> Formats()
        {
            var data = new TheoryData<char, string, int>();
            foreach (var delimiter in new[] { ',', ';' })
            {
                foreach (var terminator in new[] { "\r\n", "\n", "\r" })
                {
                    foreach (var bufferSize in new[] { 1, 2, 3, 5, 16 * 1024 })
                    {
                        data.Add(delimiter, terminator, bufferSize);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_EmptyStream_ReturnsFalse(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader(string.Empty, delimiter, terminator, bufferSize);

            // act
            var testResult = csvReader.TryReadRecord(new List<string>());

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_TerminatorInsideQuotes_KeepsTerminatorInFieldValue(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("\"a\nb\",c\n\"\n\"\nd", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a\nb", "c" },
                new[] { "\n" },
                new[] { "d" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_EscapedQuotes_ReturnsSingleQuotes(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("\"\"\"a\",\"b\"\"c\",\"d\"\"\",\"\"\"\"\"\"\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "\"a", "b\"c", "d\"", "\"\"" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_EmptyQuotedFields_ReturnsEmptyFieldValues(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("\"\",x,\"\"\n\"\"", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "", "x", "" },
                new[] { "" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_WhiteSpaceAroundQuotedValue_KeepsWhiteSpace(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("  \"a b\"  ,\t\"c\"\n \"d\" ", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "  a b  ", "\tc" },
                new[] { " d " }), options => options.WithStrictOrdering());
            badValues.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuote_CallsBadValueHandlerAndReturnsEmptyFieldValue(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("a,x\"b\",c\n", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a", "", "c" }), options => options.WithStrictOrdering());
            badValues.Should().Equal(("x", 1));
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuote_CallsBadValueHandlerForEachCharacterAndReturnsEmptyFieldValue(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("\"a\"bc ,d\n", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "", "d" }), options => options.WithStrictOrdering());
            badValues.Should().Equal(("ab", 0), ("abc", 0));
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_SecondQuotedSectionAfterQuotedValue_CallsBadValueHandler(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("\"a\" \"b\",c\n", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "", "c" }), options => options.WithStrictOrdering());
            badValues.Should().Equal(("a ", 0));
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_SecondQuotedSectionAfterEmptyQuotedValue_IsValid(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            // Only the field value accumulated so far is checked at an opening quote, and an empty quoted value leaves only whitespace.
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("\"\" \"b\",c\n", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { " b", "c" }), options => options.WithStrictOrdering());
            badValues.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_InvalidLastFieldValue_KeepsRawFieldValue(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            // Unlike other fields, the last field of a record is not replaced with an empty value when it is invalid.
            var badValues = new List<(string?, int?)>();
            using var csvReader = CreateReader("a,x\"b\"\n", delimiter, terminator, bufferSize, CollectBadValues(badValues));

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a", "xb" }), options => options.WithStrictOrdering());
            badValues.Should().Equal(("x", 1));
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_NonWhiteSpaceBeforeOpeningQuoteWithoutBadValueHandler_ThrowsCsvFileExceptionAndSkipsRecord(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a,x\"b\n\",c\nd", delimiter, terminator, bufferSize);
            var fieldValues = new List<string>();

            // act
            Action testAction = () => csvReader.TryReadRecord(fieldValues);

            // assert
            testAction.Should().Throw<CsvFileException>()
                .WithMessage("Encountered invalid value at field index 1.")
                .Which.Data["FieldIndex"].Should().Be(1);
            csvReader.TryReadRecord(fieldValues).Should().BeTrue();
            fieldValues.Should().Equal("d");
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_NonWhiteSpaceAfterClosingQuoteWithoutBadValueHandler_ThrowsCsvFileExceptionAndSkipsRecord(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a,b,\"c\"d\ne", delimiter, terminator, bufferSize);
            var fieldValues = new List<string>();

            // act
            Action testAction = () => csvReader.TryReadRecord(fieldValues);

            // assert
            testAction.Should().Throw<CsvFileException>()
                .WithMessage("Encountered invalid value at field index 2.")
                .Which.Data["FieldIndex"].Should().Be(2);
            csvReader.TryReadRecord(fieldValues).Should().BeTrue();
            fieldValues.Should().Equal("e");
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_EmptyFieldsAndTrailingDelimiter_ReturnsEmptyFieldValues(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader(",a,,\n,\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "", "a", "", "" },
                new[] { "", "" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_BlankLines_ReturnsSingleEmptyFieldValue(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a\n\n\nb\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a" },
                new[] { "" },
                new[] { "" },
                new[] { "b" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_FieldValuesLongerThanBuffer_ReturnsWholeFieldValues(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            var unquotedValue = new string('u', 100);
            var quotedValue = string.Concat(Enumerable.Repeat("q\"\"\nq ", 20));
            using var csvReader = CreateReader($"{unquotedValue},\"{quotedValue}\",{unquotedValue}\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { unquotedValue, quotedValue.Replace("\"\"", "\""), unquotedValue }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_FinalRecordWithTerminator_ReturnsNoFurtherRecord(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a,b\nc,\"d\"\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a", "b" },
                new[] { "c", "d" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_FinalRecordWithoutTerminator_ReturnsFinalRecord(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a,b\nc,\"d\"", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a", "b" },
                new[] { "c", "d" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void TryReadRecord_UnterminatedQuote_RunsToEndOfStream(char delimiter, string terminator, int bufferSize)
        {
            // arrange
            using var csvReader = CreateReader("a,b\nc,\"d\ne,f\n", delimiter, terminator, bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, terminator,
                new[] { "a", "b" },
                new[] { "c", "d\ne,f\n" }), options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(',', 1), InlineData(',', 2), InlineData(',', 3), InlineData(',', 16 * 1024)]
        [InlineData(';', 1), InlineData(';', 2), InlineData(';', 3), InlineData(';', 16 * 1024)]
        public void TryReadRecord_CarriageReturnWithoutLineFeed_KeepsCarriageReturnInFieldValue(char delimiter, int bufferSize)
        {
            // arrange
            // With a two-character terminator, a lone first character is ordinary content, inside or after a quoted value.
            using var csvReader = CreateReader("a\rb,\"c\"\r,\"d\" \ne\r", delimiter, "\r\n", bufferSize);

            // act
            var testResult = ReadAllRecords(csvReader);

            // assert
            testResult.Should().BeEquivalentTo(Records(delimiter, "\r\n",
                new[] { "a\rb", "c\r", "d " },
                new[] { "e\r" }), options => options.WithStrictOrdering());
        }

        private static CsvFieldBadValue<CsvFileRecordLine> CollectBadValues(List<(string?, int?)> badValues) =>
            context => badValues.Add((context.RawFieldValue, context.FieldIndex));

        private static CsvFileReader<CsvFileRecordLine> CreateReader(string input, char delimiter, string terminator, int bufferSize,
            CsvFieldBadValue<CsvFileRecordLine>? badValueHandler = null)
        {
            var options = new CsvFileOptions
            {
                FieldDelimiterCharacter = delimiter,
                TerminatorLineBreak = terminator,
                BadValueHandler = badValueHandler
            };

            return new CsvFileReader<CsvFileRecordLine>(new StringReader(Translate(input, delimiter, terminator)), options, bufferSize);
        }

        private static List<string[]> ReadAllRecords(CsvFileReader<CsvFileRecordLine> csvReader)
        {
            var records = new List<string[]>();
            var fieldValues = new List<string>();
            while (csvReader.TryReadRecord(fieldValues))
            {
                records.Add(fieldValues.ToArray());
            }

            return records;
        }

        private static List<string[]> Records(char delimiter, string terminator, params string[][] records) =>
            records.Select(fieldValues => fieldValues.Select(fieldValue => Translate(fieldValue, delimiter, terminator)).ToArray()).ToList();

        private static string Translate(string template, char delimiter, string terminator) =>
            template.Replace(',', delimiter).Replace("\n", terminator);
    }
}
