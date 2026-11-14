using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{

    /// <remarks>
    /// Mappers built from attributes are cached process-wide, by record-line type, and <see cref="CsvMapperCache.Clear"/>
    /// clears them.  Clearing never changes how a line is written, so tests that clear the cache can run alongside the others.
    /// </remarks>
    public class CsvFileWriterTests
    {
        /// <summary>
        /// Size, in characters, of the buffer of the <see cref="StreamWriter"/> that a writer creates over a stream
        /// (16 Ki characters).
        /// </summary>
        private const int StreamBufferSize = 16384;

        /// <summary>
        /// Code page of ISO 8859-1 (Latin-1), which every target framework has built in.
        /// </summary>
        private const int Latin1CodePage = 28591;

        // A field value as long as the stream buffer, so that writing it and a line break fills the buffer once.
        private static readonly string StreamBufferLengthFieldValue = new string('a', StreamBufferSize);

        // A negative sign that no culture uses, so that a value formatted with the culture under test can be told apart
        // from one formatted with any other culture.
        private const string TestNegativeSign = "~";

        // Field names of the lines written.
        private const string FieldName1 = "One";
        private const string FieldName2 = "Two";
        private const string FieldName3 = "Three";
        private const string FieldName4 = "Four";
        private const string FieldName5 = "Five";
        private const string IdFieldName = "ID";
        private const string NameFieldName = "Name";
        private const string DescriptionFieldName = "Description";

        // Field values of the lines written.
        private const string NumberValue = "1";
        private const string TextValue = "Two";
        private const string FirstName = "One";
        private const string FirstDescription = "First";
        private const string SecondName = "Two";
        private const string SecondDescription = "Second";
        private const string ThirdName = "Three";
        private const string ThirdDescription = "Third";
        private const string FirstLongDescription = "This is the first record";
        private const string SecondLongDescription = "This is the second record";
        private const string ValueWithQuote = "Left\"Right";
        private const string ValueWithCrLf = "Left\r\nRight";
        private const string ValueWithDelimiter = "Left,Right";
        private const string FirstAttributeValue = "1st";
        private const string SecondAttributeValue = "2nd";

        // The record a line mapped to put SecondAttributeValue ahead of FirstAttributeValue writes.
        private const string ReversedAttributeRecord = "2nd,1st\r\n";

        // The record a line mapped to put FirstAttributeValue ahead of SecondAttributeValue writes.
        private const string OrderedAttributeRecord = "1st,2nd\r\n";

        // The record ConstantAndVariableLineMapper writes for a line whose Description is FirstDescription: the generated Id, the constant Name,
        // then the Description.
        private const string ConstantAndVariableRecord = "42,Constant,First\r\n";
        private const string HtmlLineBreak = "<br>";
        private const string FirstId = "1";
        private const string SecondId = "2";
        private const string InvariantDecimalSeparator = ".";

        // Field names of header lines, and of record lines that are given only by position.
        private const string HeaderFieldName1 = "1";
        private const string HeaderFieldName2 = "2";
        private const string HeaderFieldName3 = "3";
        private const string HeaderFieldName4 = "4";
        private const string ZeroFieldName = "0";

        // Lines written.
        private const string SingleNumberLine = "1\r\n";
        private const string FirstRecordLine = "1,One\r\n";
        private const string FirstAndThirdRecordLines = "1,One\r\n3,Three\r\n";
        private const string FirstAndSecondRecordLines = "1,One\r\n2,Two\r\n";

        // Field names that differ from the property keys they are mapped to, the values written to them, and the line
        // that results.
        private const string PersonIdFieldName = "person_id";
        private const string PersonNameFieldName = "person_name";
        private const int RenamedId = 5;
        private const string RenamedName = "bob";
        private const string RenamedRecordLine = "5,bob\r\n";

        // Values held under the field names of a line that also holds values under the property keys, and the line that
        // results.
        private const int FieldNameId = 9;
        private const string FieldNameName = "amy";
        private const string FieldNameRecordLine = "9,amy\r\n";

        // A property key given by CsvFieldAttribute, and a value that replaces RenamedId on a line read from a file.
        private const string PersonKeyFieldName = "pid";
        private const int ChangedId = 6;
        private const string ChangedRecordLine = "6,bob\r\n";

        // A property key that is also the name of another property with a CsvFieldAttribute key of its own, the keys and field
        // names of the two properties, their values, and the line that results.
        private const string CrossKeyedTitleKey = "Name";
        private const string CrossKeyedTitleFieldName = "title_col";
        private const string CrossKeyedNameKey = "full_name";
        private const string CrossKeyedTitle = "T";
        private const string CrossKeyedName = "N";
        private const string CrossKeyedRecordLine = "T,N\r\n";

        // Two properties, each keyed by the other's name, the fields they are mapped to, their values, and the line that results.
        private const string SwappedKeyA = "B";
        private const string SwappedKeyB = "A";
        private const string SwappedFieldNameA = "a_col";
        private const string SwappedFieldNameB = "b_col";
        private const string SwappedValueA = "x";
        private const string SwappedValueB = "y";
        private const string SwappedRecordLine = "x,y\r\n";
        private const string RenamedIdLine = "5\r\n";
        private const string KeyedIdHeaderAndRecordLines = "pid\r\n5\r\n";
        private const string RenamedNameLine = "bob\r\n";

        /// <summary>
        /// The text the converters of <see cref="DelegateConvertedNameLineMapper"/> and <see cref="FixedStringConverter"/> write for every value.
        /// </summary>
        private const string ConvertedName = "CONVERTED";

        private const string ConvertedNameRecordLine = "5,CONVERTED\r\n";
        private const string ConvertedRecordLine = "#5,bob\r\n";

        // The record line that is read and then written.
        private const string RenamedRecordFile = "5,bob";
        private const string FirstLongRecordLine = "1,One,This is the first record\r\n";

        // GapIndexedLineMapper maps Id to index 0, Name by name and Description to index 10, so Name fills position 1 and positions 2 to 9 are blank.
        private const string GapIndexedRecordLine = "1,One,,,,,,,,,This is the first record\r\n";

        // IndexGapLineMapper maps Id, Name and Description to indexes 0, 1 and 3, so position 2 is blank, or holds a field no mapping covers.
        private const string IndexGapRecordLine = "1,One,,First\r\n";
        private const string IndexGapUnmappedRecordLine = "1,One,Extra,First\r\n";
        private const string IndexGapUntrimmedRecordLine = "1,One,,\r\n";
        private const string UnmappedFieldName = "Notes";
        private const string UnmappedFieldValue = "Extra";

        // ContactLineMapper maps FirstName, LastName and City by name, Title to index 0 and Country to index 9.
        private const string ContactFirstName = "Ann";
        private const string ContactLastName = "Lee";
        private const string ContactTitle = "Dr";
        private const string ContactCity = "Oslo";
        private const string ContactCountry = "Norway";
        private const string ContactFirstNameFieldName = nameof(ContactLine.FirstName);
        private const string ContactLastNameFieldName = nameof(ContactLine.LastName);
        private const string ContactTitleFieldName = nameof(ContactLine.Title);
        private const string ContactCityFieldName = nameof(ContactLine.City);
        private const string ContactCountryFieldName = nameof(ContactLine.Country);
        private const string ContactRecordLine = "Dr,Ann,Lee,Oslo,,,,,,Norway\r\n";
        private const string ContactHeaderLine = "Title,FirstName,LastName,City,,,,,,Country\r\n";
        private const string ContactHeaderAndRecordLines = "Title,FirstName,LastName,City,,,,,,Country\r\nDr,Ann,Lee,Oslo,,,,,,Norway\r\n";
        private const string ContactPhoneFieldName = "Phone";
        private const string ContactPhone = "555";
        private const string ContactPhoneHeaderAndRecordLines = "Title,FirstName,LastName,City,Phone,,,,,Country\r\nDr,Ann,Lee,Oslo,555,,,,,Norway\r\n";
        private const string ContactNoCityHeaderLine = "Title,FirstName,LastName,,,,,,,Country\r\n";

        // SharedIndexLineMapper maps Name and then Description to index 0, so Description, the last mapping, gives the field's value.
        private const string SharedIndexRecordLine = "First\r\n";

        // NamedAtSharedIndexLineMapper maps Name and Description to index 0, which Name names, and Id by the name Description, so Id takes position 1.
        private const string NamedAtSharedIndexRecordLine = "One,1\r\n";
        private const int NamedAtSharedIndexId = 1;

        private const string OptionsParameterName = "options";

        // Keys of the CsvFileException data items that describe a record line with a field the first record line doesn't have, and the field
        // count of the first record line of the tests that check them.
        private const string FieldNameDataItem = "FieldName";
        private const string ExpectedFieldCountDataItem = "ExpectedFieldCount";
        private const int FirstLineFieldCount = 2;

        // Key of the CsvFileException data item that gives the field count of a line, and the field count of a line with no fields.
        private const string ActualFieldCountDataItem = "ActualFieldCount";
        private const int NoFieldCount = 0;

        // Key of the CsvFileException data item that gives the index of a field.
        private const string FieldIndexDataItem = "FieldIndex";

        // Values of a character, and the line each is written as.  TEXTDATA is %x20-21 / %x23-2B / %x2D-7E (RFC 4180 section 2), read as the
        // printable characters of the character set the writer writes (RFC 4180 section 3, charset).  A control character is never printable.
        private const string TabValue = "\x0009";
        private const string NullValue = "\x0000";
        private const string DeleteValue = "\x007F";
        private const string NextLineValue = "\x0085";
        private const string LatinSmallLetterEWithAcuteValue = "\x00E9";
        private const string CjkUnifiedIdeographOneValue = "\x4E00";
        private const string EuroSignValue = "\x20AC";
        private const string GrinningFaceValue = "\xD83D\xDE00";

        // A surrogate without its other half isn't a character of any character set.
        private const string LoneHighSurrogateValue = "a\xD800";
        private const string LoneLowSurrogateValue = "\xDC00a";
        private const string LoneHighSurrogateMessage = "The value of the field at index 0 holds U+D800, a surrogate without its other half, which isn't a character, so it can't be written.";
        private const string LoneLowSurrogateMessage = "The value of the field at index 0 holds U+DC00, a surrogate without its other half, which isn't a character, so it can't be written.";
        private const string LatinSmallLetterEWithAcuteLine = "\x00E9\r\n";
        private const string CjkUnifiedIdeographOneLine = "\x4E00\r\n";
        private const string GrinningFaceLine = "\xD83D\xDE00\r\n";

        // What a writer throws for each of the values, as the first field of a line, with the name of the writer's character set.
        private const string TabMessage = "The value of the field at index 0 holds U+0009, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string NullMessage = "The value of the field at index 0 holds U+0000, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string DeleteMessage = "The value of the field at index 0 holds U+007F, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string NextLineMessage = "The value of the field at index 0 holds U+0085, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string LeftTabRightMessage = "The value of the field at index 0 holds U+0009, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string SecondFieldTabMessage = "The value of the field at index 1 holds U+0009, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string AsciiLatinSmallLetterEWithAcuteMessage = "The value of the field at index 0 holds U+00E9, which isn't a printable character of the writer's character set, us-ascii, so RFC 4180 doesn't allow it in a field.";
        private const string AsciiGrinningFaceMessage = "The value of the field at index 0 holds U+1F600, which isn't a printable character of the writer's character set, us-ascii, so RFC 4180 doesn't allow it in a field.";
        private const string Latin1EuroSignMessage = "The value of the field at index 0 holds U+20AC, which isn't a printable character of the writer's character set, iso-8859-1, so RFC 4180 doesn't allow it in a field.";
        private const string Latin1GrinningFaceMessage = "The value of the field at index 0 holds U+1F600, which isn't a printable character of the writer's character set, iso-8859-1, so RFC 4180 doesn't allow it in a field.";
        private const string RestrictedAsciiBracketMessage = "The value of the field at index 0 holds U+005B, which isn't a printable character of the writer's character set, RestrictedAsciiEncoding, so RFC 4180 doesn't allow it in a field.";
        private const string AsciiSectionSignDelimiterMessage = "The field delimiter U+00A7 isn't a character of the writer's character set, us-ascii.";
        private const string Latin1LineSeparatorTerminatorMessage = "The terminator line break holds U+2028, which isn't a character of the writer's character set, iso-8859-1.";
        private const string VerticalTabEmbeddedLineBreakMessage = "The embedded line break holds U+000B, which isn't a printable character of the writer's character set, utf-16, so RFC 4180 doesn't allow it in a field.";
        private const string Utf8TabMessage = "The value of the field at index 0 holds U+0009, which isn't a printable character of the writer's character set, utf-8, so RFC 4180 doesn't allow it in a field.";

        // A value with a character that a national variant of ISO 646 without '[' doesn't have, and that character.
        private const string BracketedValue = "a[b";
        private const char LeftSquareBracket = '[';

        // Options characters that a character set may not have: SECTION SIGN, LINE SEPARATOR and a vertical tab, a control character.
        private const char SectionSign = '\x00A7';
        private const string LineSeparator = "\x2028";
        private const string VerticalTab = "\x000B";

        // A tab delimiter, a value that holds it, and the line it is written as: quoted, as the delimiter takes the place of COMMA (RFC 4180 section 2).
        private const char TabDelimiter = '\x0009';
        private const string ValueWithTab = "Left\x0009Right";
        private const string QuotedValueWithTabLine = "\"Left\x0009Right\"\r\n";

        // A header line of the fields FieldName1 and FieldName2, its field count, and the lines that result from writing it and then a record line
        // that has values for both fields, only for FieldName1, or for neither.
        private const string TwoFieldHeaderLine = "One,Two\r\n";
        private const int HeaderLineFieldCount = 2;
        private const string TwoFieldHeaderAndRecordLines = "One,Two\r\n1,Two\r\n";
        private const string TwoFieldHeaderAndBlankRecordLines = "One,Two\r\n1,\r\n";
        private const string TwoFieldHeaderAndAllBlankRecordLines = "One,Two\r\n,\r\n";

        // A header line of the field FieldName1, then a record of one blank field, which is a record (RFC 4180§2 ABNF: field = *TEXTDATA).
        private const string OneFieldHeaderAndBlankRecordLines = "One\r\n\r\n";

        // A record line with the value NumberValue for FieldName1 and TextValue for FieldName2, written with no header line.
        private const string TwoFieldRecordLine = "1,Two\r\n";

        // A header line of the fields that RenamedAttributeLine is mapped to, followed by RenamedRecordLine.
        private const string RenamedHeaderAndRecordLines = "person_id,person_name\r\n5,bob\r\n";

        // The record line that PartlyMappedLineMapper reads, with an unmapped second field.
        private const string PartlyMappedRecordFile = "1,One";

        [Fact]
        public void WriteLine_HeaderLineAndQuoteHandlingAlways_WritesHeaderLineWithAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(NumberValue, NumberValue);
            testLine.Add(TextValue, TextValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"1\",\"Two\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndQuoteHandlingRequired_WritesHeaderLineWithRequiredValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, "One,Two");
            testLine.Add(HeaderFieldName2, "Three\r\nFour");
            testLine.Add(HeaderFieldName3, "Five\"Six");
            testLine.Add(HeaderFieldName4, "Last");

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"One,Two\",\"Three\r\nFour\",\"Five\"\"Six\",Last\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, FieldName1);
            testLine.Add(HeaderFieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("One\r\n");
            }
        }

        [Theory]
        [InlineData("\n", "One,Two\n"), InlineData("\r", "One,Two\r")]
        public void WriteLine_HeaderLineAndCustomLineBreak_WritesHeaderLineTerminatedByLineBreak(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndCustomFieldDelimiter_WritesHeaderLineWithFieldsSeparatedByDelimiter()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("One;Two\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingAlways_WritesRecordLineWithAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, ValueWithQuote);
            testLine.Add(FieldName4, ValueWithCrLf);
            testLine.Add(FieldName5, ValueWithDelimiter);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"1\",\"Two\",\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingAuto_WritesRecordLineWithStringValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1,\"Two\"\r\n");
            }
        }

        [Theory]
        [InlineData("1,000", "\"1,000\"\r\n")]
        [InlineData("1\r\n", "\"1\r\n\"\r\n")]
        public void WriteLine_QuoteHandlingAutoAndNumberWithCharacterRequiringQuotes_WritesValueQuoted(string fieldValue, string expectedLine)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, fieldValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };

            var buffer = new StringBuilder();
            using (new CurrentCultureScope(CultureInfo.InvariantCulture))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingRequired_WritesRecordLineWithRequiredValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, ValueWithQuote);
            testLine.Add(FieldName4, ValueWithCrLf);
            testLine.Add(FieldName5, ValueWithDelimiter);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1,Two,\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueAndNoCulture_WritesRecordWithSerializedValueUsingInvariantCulture()
        {
            // arrange
            ICsvFileLine testLine = new AllIndexedLine { Id = -1 };

            var options = new CsvFileOptions { Culture = null };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("-1,,\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueAndCulture_WritesRecordWithSerializedValueUsingSpecifiedCulture()
        {
            // arrange
            ICsvFileLine testLine = new AllIndexedLine { Id = -100 };

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, InvariantDecimalSeparator) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("~100,,\r\n");
            }
        }

        [Fact]
        public void WriteLine_MapperRegisteredOnAnotherWriter_WritesFieldsInLineOrder()
        {
            // arrange
            ICsvFileLine testLine = new InstanceScopedLine { Id = 1, Name = FirstName };

            var buffer = new StringBuilder();
            using (var otherWriter = new CsvFileWriter<InstanceScopedLine>(new StringWriter()))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<InstanceScopedLine>(stringWriter))
            {
                otherWriter.RegisterMapper<InstanceScopedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_AfterMapperCacheCleared_WritesFieldsInAttributeOrder()
        {
            // arrange
            using (var otherWriter = new CsvFileWriter<ClearedCacheLine>(new StringWriter()))
            {
                otherWriter.WriteLine(new ClearedCacheLine { First = FirstAttributeValue, Second = SecondAttributeValue });
            }

            CsvMapperCache.Clear();

            ICsvFileLine testLine = new ClearedCacheLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ClearedCacheLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ReversedAttributeRecord);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueNoCultureNoMapper_WritesRecordWithSerializedValueUsingInvariantCulture()
        {
            // arrange
            ICsvFileLine testLine = new UnmappedLine { Id = -47, Name = FirstName, Description = FirstDescription };

            // A current culture with another negative sign, which the output must not use.
            var buffer = new StringBuilder();
            using (new CurrentCultureScope(CreateCulture(TestNegativeSign, InvariantDecimalSeparator)))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("-47,One,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueCultureNoMapper_WritesRecordWithSerializedValueUsingSpecifiedCulture()
        {
            // arrange
            ICsvFileLine testLine = new UnmappedLine { Id = -49, Name = FirstName, Description = FirstDescription };

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, InvariantDecimalSeparator) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("~49,One,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(SingleNumberLine);
            }
        }

        /// <summary>
        /// Record lines whose fields are in a different order from the header line One,Two,Three, or lack one of its fields, and the record
        /// line that results when they are written after it with <see cref="ICsvFileOptions.SuppressTrailingBlankFields"/> set.
        /// </summary>
        public static TheoryData<CsvFileRecordLine, string> RecordLinesInAnotherOrderThanTheHeaderLine
        {
            get
            {
                var lastFieldFirst = new CsvFileRecordLine();
                ((ICsvFileLine)lastFieldFirst).Add(FieldName3, "z");
                ((ICsvFileLine)lastFieldFirst).Add(FieldName1, string.Empty);
                ((ICsvFileLine)lastFieldFirst).Add(FieldName2, string.Empty);

                var middleFieldMissing = new CsvFileRecordLine();
                ((ICsvFileLine)middleFieldMissing).Add(FieldName1, "x");
                ((ICsvFileLine)middleFieldMissing).Add(FieldName3, string.Empty);

                var allFieldsBlank = new CsvFileRecordLine();
                ((ICsvFileLine)allFieldsBlank).Add(FieldName2, string.Empty);
                ((ICsvFileLine)allFieldsBlank).Add(FieldName1, string.Empty);
                ((ICsvFileLine)allFieldsBlank).Add(FieldName3, string.Empty);

                return new TheoryData<CsvFileRecordLine, string>
                {
                    { lastFieldFirst, ",,z\r\n" },
                    { middleFieldMissing, "x\r\n" },
                    { allFieldsBlank, ",,\r\n" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RecordLinesInAnotherOrderThanTheHeaderLine))]
        public void WriteLine_RecordLineInAnotherOrderThanHeaderLineAndSuppressTrailingBlankFields_TrimsTrailingBlankFieldsInHeaderLineOrder(
            CsvFileRecordLine testLine, string expectedResult)
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(HeaderFieldName1, FieldName1);
            headerLine.Add(HeaderFieldName2, FieldName2);
            headerLine.Add(HeaderFieldName3, FieldName3);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);
                buffer.Clear();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineInAnotherOrderThanFirstRecordLineAndSuppressTrailingBlankFields_TrimsTrailingBlankFieldsInFirstRecordLineOrder()
        {
            // arrange
            ICsvFileLine firstLine = new CsvFileRecordLine();
            firstLine.Add(FieldName1, NumberValue);
            firstLine.Add(FieldName2, TextValue);
            firstLine.Add(FieldName3, ThirdName);

            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName3, "z");
            testLine.Add(FieldName1, string.Empty);
            testLine.Add(FieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(firstLine);
                buffer.Clear();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(",,z\r\n");
            }
        }

        [Fact]
        public void WriteLine_TrailingMappedFieldConvertedToEmptyTextAndSuppressTrailingBlankFields_LeavesTheFieldOut()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<EmptyIdConverterLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedNameLine);
            }
        }

        [Fact]
        public void WriteLine_TrailingMappedFieldWithConstantValueAndSuppressTrailingBlankFields_WritesTheConstantValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = string.Empty };

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<ConstantNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnStringProperty_WritesConvertedValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<DelegateConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_TypeConverterOnStringProperty_WritesConvertedValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<TypeConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_TypeConverterNotToStringOnStringProperty_WritesValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FromStringOnlyConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_ConstantStringWithTypeConverter_WritesConstantValue()
        {
            // arrange
            var testLine = new ConstantConvertedNameLine { Name = string.Empty };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantConvertedNameLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedNameLine);
            }
        }

        [Theory]
        [InlineData("\n", "1,Two\n"), InlineData("\r", "1,Two\r")]
        public void WriteLine_RecordLineAndCustomLineBreak_WritesRecordLineTerminatedByLineBreak(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);

            var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndCustomFieldDelimiter_WritesRecordLineWithFieldsSeparatedByDelimiter()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, "3");

            var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1;Two;3\r\n");
            }
        }

        [Theory]
        [InlineData("\n", "1,\"Left\nRight\"\r\n"), InlineData("\r", "1,\"Left\rRight\"\r\n")]
        public void WriteLine_RecordLineAndEmbeddedLineBreak_WritesRecordLineWithNormalizedFieldValues(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, ValueWithCrLf);

            var options = new CsvFileOptions { EmbeddedLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndRecordLines_WritesCsvFieldsInOrderByHeaderLine()
        {
            // arrange
            ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
            testHeaderLine.Add(IdFieldName, IdFieldName);
            testHeaderLine.Add(NameFieldName, NameFieldName);
            testHeaderLine.Add(DescriptionFieldName, DescriptionFieldName);

            ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
            testRecordLine1.Add(NameFieldName, FirstName);
            testRecordLine1.Add(DescriptionFieldName, FirstLongDescription);
            testRecordLine1.Add(IdFieldName, FirstId);

            ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
            testRecordLine2.Add(DescriptionFieldName, SecondLongDescription);
            testRecordLine2.Add(IdFieldName, SecondId);
            testRecordLine2.Add(NameFieldName, SecondName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(testHeaderLine);
                csvWriter.WriteLine(testRecordLine1);

                // act
                csvWriter.WriteLine(testRecordLine2);

                // assert
                buffer.ToString().Should().Be("ID,Name,Description\r\n1,One,This is the first record\r\n2,Two,This is the second record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLines_WritesCsvFieldsInOrderByFirstRecordLine()
        {
            // arrange
            ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
            testRecordLine1.Add(IdFieldName, FirstId);
            testRecordLine1.Add(NameFieldName, FirstName);
            testRecordLine1.Add(DescriptionFieldName, FirstLongDescription);

            ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
            testRecordLine2.Add(NameFieldName, SecondName);
            testRecordLine2.Add(DescriptionFieldName, SecondLongDescription);
            testRecordLine2.Add(IdFieldName, SecondId);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(testRecordLine1);

                // act
                csvWriter.WriteLine(testRecordLine2);

                // assert
                buffer.ToString().Should().Be("1,One,This is the first record\r\n2,Two,This is the second record\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndRecordLineWithMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
            testHeaderLine.Add(nameof(TestLine.Id), nameof(TestLine.Id));
            testHeaderLine.Add(nameof(TestLine.Name), nameof(TestLine.Name));
            testHeaderLine.Add(nameof(TestLine.Description), nameof(TestLine.Description));

            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();
                csvWriter.WriteLine(testHeaderLine);

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be("Id,Name,Description\r\n1,One,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapperAndFieldsInsertedOutOfOrder_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new AllIndexedLine();
            testRecordLine.Add(nameof(TestLine.Description), FirstLongDescription);
            testRecordLine.Insert(0, nameof(TestLine.Name), FirstName);
            testRecordLine.Insert(1, nameof(TestLine.Id), 1);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapperAndFieldRemovedAtPosition_WritesEmptyValueForRemovedField()
        {
            // arrange
            // The initializer adds the fields in the order Description, Name, Id, so position 1 is Name.
            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };
            testRecordLine.RemoveAt(1);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be("1,,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFirstIndexMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new FirstIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<FirstIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FirstIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithLastIndexMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new LastIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<LastIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<LastIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithGapInIndexMapping_WritesBlankFieldsInGap()
        {
            // arrange
            ICsvFileLine testRecordLine = new GapIndexedLine { Name = FirstName, Description = FirstLongDescription, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<GapIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<GapIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(GapIndexedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperWithIndexGap_WritesFieldAtMappedIndex()
        {
            // arrange
            var testLine = new IndexGapLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IndexGapLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IndexGapLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(IndexGapRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperWithIndexGapAndUnmappedField_WritesUnmappedFieldInGap()
        {
            // arrange
            var testLine = new IndexGapLine { Id = 1, Name = FirstName, Description = FirstDescription };
            ((ICsvFileLine)testLine).Add(UnmappedFieldName, UnmappedFieldValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IndexGapLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IndexGapLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(IndexGapUnmappedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperWithIndexGapAndBlankLastField_WritesBlankFields()
        {
            // arrange
            var testLine = new IndexGapLine { Id = 1, Name = FirstName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IndexGapLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IndexGapLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(IndexGapUntrimmedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperWithIndexGapAndSuppressTrailingBlankFields_TrimsBlankFieldsOfLayout()
        {
            // arrange
            var testLine = new IndexGapLine { Id = 1, Name = FirstName };
            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IndexGapLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<IndexGapLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void ReadLine_HeaderlessFileWrittenWithIndexGap_ReadsFieldAtMappedIndex()
        {
            // arrange
            var testLine = new IndexGapLine { Id = 1, Name = FirstName, Description = FirstDescription };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IndexGapLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IndexGapLineMapper>();
                csvWriter.WriteLine(testLine);
            }

            IndexGapLine testResult;
            using (var csvReader = new CsvFileReader<IndexGapLine>(new StringReader(buffer.ToString())))
            {
                csvReader.RegisterMapper<IndexGapLineMapper>();

                // act
                testResult = (IndexGapLine)csvReader.ReadLine();
            }

            // assert
            testResult.Description.Should().Be(FirstDescription);
        }

        [Fact]
        public void WriteLine_HeaderlessMapperWithNameMappingsAroundIndexes_FillsFreePositionsInMapperOrder()
        {
            // arrange
            var testLine = new ContactLine
            {
                Country = ContactCountry, City = ContactCity, Title = ContactTitle, LastName = ContactLastName, FirstName = ContactFirstName
            };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithIndexMappedNames_WritesThemAtTheirIndexes()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            testLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            testLine.Add(ContactTitleFieldName, ContactTitleFieldName);
            testLine.Add(ContactCityFieldName, ContactCityFieldName);
            testLine.Add(ContactCountryFieldName, ContactCountryFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithoutIndexMappedNames_WritesMappingNamesAtTheirIndexes()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            testLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            testLine.Add(ContactCityFieldName, ContactCityFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithUnmappedField_WritesUnmappedFieldAfterMappedFields()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            headerLine.Add(ContactPhoneFieldName, ContactPhoneFieldName);
            headerLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            headerLine.Add(ContactTitleFieldName, ContactTitleFieldName);
            headerLine.Add(ContactCityFieldName, ContactCityFieldName);
            headerLine.Add(ContactCountryFieldName, ContactCountryFieldName);
            var testLine = new ContactLine
            {
                Country = ContactCountry, City = ContactCity, Title = ContactTitle, LastName = ContactLastName, FirstName = ContactFirstName
            };
            ((ICsvFileLine)testLine).Add(ContactPhoneFieldName, ContactPhone);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactPhoneHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithoutRequiredNameMappedField_ThrowsCsvFileException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            testLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            testLine.Add(ContactTitleFieldName, ContactTitleFieldName);
            testLine.Add(ContactCountryFieldName, ContactCountryFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<CsvFileException>()
                    .Which.Data[FieldNameDataItem].Should().Be(ContactCityFieldName);
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithoutOptionalNameMappedField_WritesHeaderLineWithoutIt()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            testLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            testLine.Add(ContactTitleFieldName, ContactTitleFieldName);
            testLine.Add(ContactCountryFieldName, ContactCountryFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<OptionalCityContactLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactNoCityHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_MappingsSharingIndex_WritesLastMappingsOwnValue()
        {
            // arrange
            var testLine = new SharedIndexLine { Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<SharedIndexLine>(stringWriter))
            {
                csvWriter.RegisterMapper<SharedIndexLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(SharedIndexRecordLine);
            }
        }

        [Fact]
        public void WriteLine_NameMappingNamedLikeSecondMappingAtIndex_WritesFieldAtFreePosition()
        {
            // arrange
            var testLine = new NamedAtSharedIndexLine { Name = FirstName, Id = NamedAtSharedIndexId };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<NamedAtSharedIndexLine>(stringWriter))
            {
                csvWriter.RegisterMapper<NamedAtSharedIndexLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(NamedAtSharedIndexRecordLine);
            }
        }

        [Fact]
        public void ReadLine_NameMappingNamedLikeSecondMappingAtIndex_ReadsFieldAtFreePosition()
        {
            // arrange
            NamedAtSharedIndexLine testResult;
            using (var csvReader = new CsvFileReader<NamedAtSharedIndexLine>(new StringReader(NamedAtSharedIndexRecordLine)))
            {
                csvReader.RegisterMapper<NamedAtSharedIndexLineMapper>();

                // act
                testResult = (NamedAtSharedIndexLine)csvReader.ReadLine();
            }

            // assert
            testResult.Id.Should().Be(NamedAtSharedIndexId);
        }

        [Fact]
        public void WriteLine_RecordLineAfterHeaderLineWithIndexMappings_WritesFieldsInHeaderLayout()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(ContactFirstNameFieldName, ContactFirstNameFieldName);
            headerLine.Add(ContactLastNameFieldName, ContactLastNameFieldName);
            headerLine.Add(ContactTitleFieldName, ContactTitleFieldName);
            headerLine.Add(ContactCityFieldName, ContactCityFieldName);
            headerLine.Add(ContactCountryFieldName, ContactCountryFieldName);
            var testLine = new ContactLine
            {
                Country = ContactCountry, City = ContactCity, Title = ContactTitle, LastName = ContactLastName, FirstName = ContactFirstName
            };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ContactLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ContactLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ContactHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithIncompleteFieldList_WritesAvailableCsvFieldsInOrderByMapping()
        {
            // arrange
            var testRecordLine = new AllIndexedLine { Description = FirstLongDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(",,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesWithInconsistentFieldCountsAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineHandledByFieldCountDelegate_WritesFieldsOfLinesWritten()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWithInconsistentFieldCountAndNoFieldCountDelegate_WritesFieldsOfLinesWritten()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(recordLine2));
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWhoseValueFailsToFormat_WritesFieldsOfLinesWritten()
        {
            // arrange
            // Without a header line, the fields written are those of the record lines.  The first line isn't written, so the second line's
            // fields are the only ones.
            ICsvFileLine recordLine1 = new CsvFileRecordLine();
            recordLine1.Add(FieldName1, NumberValue);
            recordLine1.Add(FieldName2, TextValue);
            recordLine1.Add(FieldName3, new UnformattableValue());

            ICsvFileLine recordLine2 = new CsvFileRecordLine();
            recordLine2.Add(FieldName1, NumberValue);
            recordLine2.Add(FieldName2, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                _ = Record.Exception(() => csvWriter.WriteLine(recordLine1));
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be("1,Two\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesWithInconsistentFieldCountsAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage("Expected 2 fields; found 3 fields.");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            // Without a header line, the first line sets the fields written, so the second line's Description field isn't one of them.
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>()
                    .WithMessage("Field 'Description' is not one of the 2 fields of the first record line.").Which;
                exception.Data[FieldNameDataItem].Should().Be(nameof(UnmappedLine.Description));
                exception.Data[ExpectedFieldCountDataItem].Should().Be(FirstLineFieldCount);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFewerFieldsAndAnotherFieldName_ThrowsCsvFileException()
        {
            // arrange
            // The second line has a field that the first line doesn't, as well as fewer fields, so its field count is reported.
            ICsvFileLine recordLine1 = new CsvFileRecordLine();
            recordLine1.Add(FieldName1, NumberValue);
            recordLine1.Add(FieldName2, TextValue);
            recordLine1.Add(FieldName3, TextValue);

            ICsvFileLine recordLine2 = new CsvFileRecordLine();
            recordLine2.Add(FieldName1, NumberValue);
            recordLine2.Add(FieldName4, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage("Expected 3 fields; found 2 fields.");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWithAnotherFieldNameHandledByFieldCountDelegate_WritesFieldsOfFirstLine()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);
                csvWriter.WriteLine(recordLine2);

                // act
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithMoreFieldsThanHeaderLine_ThrowsCsvFileException()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName2, TextValue);
            recordLine.Add(FieldName3, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage("Expected 2 fields; found 3 fields.");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithNoFields_ThrowsCsvFileException()
        {
            // arrange
            var headerLine = new CsvFileHeaderLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(headerLine);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>().WithMessage("A header line has no fields.").Which;
                exception.Data[ActualFieldCountDataItem].Should().Be(NoFieldCount);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithNoFields_WritesNothing()
        {
            // arrange
            var headerLine = new CsvFileHeaderLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(headerLine);

                // assert
                testAction.Should().Throw<CsvFileException>();
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            var recordLine = new CsvFileRecordLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(recordLine);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>().WithMessage("A record line has no fields to write.").Which;
                exception.Data[ActualFieldCountDataItem].Should().Be(NoFieldCount);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsAndNoFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine = new CsvFileRecordLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(recordLine);

                // assert
                testAction.Should().Throw<CsvFileException>();
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            var recordLine = new CsvFileRecordLine();

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                // act
                csvWriter.WriteLine(recordLine);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine = new CsvFileRecordLine();

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWithNoFieldsHandledByFieldCountDelegate_WritesFieldsOfLineWritten()
        {
            // arrange
            var emptyRecordLine = new CsvFileRecordLine();
            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName2, TextValue);

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                // act
                csvWriter.WriteLine(emptyRecordLine);
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsAfterHeaderLine_WritesBlankValueForEachHeaderField()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            var recordLine = new CsvFileRecordLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderAndAllBlankRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNoFieldsAfterOneFieldHeaderLine_WritesRecordOfOneBlankField()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);

            var recordLine = new CsvFileRecordLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(OneFieldHeaderAndBlankRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFieldNotInHeaderLineAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName3, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>()
                    .WithMessage("Field 'Three' is not one of the 2 fields of the header line.").Which;
                exception.Data[FieldNameDataItem].Should().Be(FieldName3);
                exception.Data[ExpectedFieldCountDataItem].Should().Be(HeaderLineFieldCount);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFieldNotInHeaderLineAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName3, TextValue);

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFieldNotInHeaderLineHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName3, TextValue);

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithNullFieldNotInHeaderLine_WritesFieldsOfHeaderLine()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName2, TextValue);
            recordLine.Add(FieldName3, null);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithEmptyFieldNotInHeaderLine_WritesFieldsOfHeaderLine()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);
            recordLine.Add(FieldName2, TextValue);
            recordLine.Add(FieldName3, string.Empty);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithConstantMappingOfFieldNotInHeaderLine_WritesFieldsOfHeaderLine()
        {
            // arrange
            // The reader puts the constant value on the line under the mapped field name, which the file's header line doesn't have.
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            ICsvFileLine headerLine;
            ConstantDescriptionLine recordLine;
            using (var csvReader = new CsvFileReader<ConstantDescriptionLine>(new StringReader(TwoFieldHeaderAndRecordLines), options))
            {
                csvReader.RegisterMapper<ConstantDescriptionLineMapper>();
                headerLine = csvReader.ReadLine();
                recordLine = (ConstantDescriptionLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantDescriptionLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ConstantDescriptionLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessLinesOfTypesWithOtherAttributeMappings_WritesMappedFieldsOfEachLine()
        {
            // arrange
            // Each line type has its own mapper, which covers all of the line's fields, so neither line has fields to add to the other.
            var recordLine1 = new IdNameAttributeLine { Id = FirstId, Name = FirstName };
            var recordLine2 = new CodeTitleAttributeLine { Code = SecondId, Title = SecondName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be(FirstAndSecondRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineLackingAFieldOfHeaderLine_WritesBlankValueForField()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(FieldName1, FieldName1);
            headerLine.Add(FieldName2, FieldName2);

            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add(FieldName1, NumberValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(TwoFieldHeaderAndBlankRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithPropertiesMappedToHeaderLineFields_WritesFieldValues()
        {
            // arrange
            // The line holds its values under the property keys, which the mappings resolve to the header line's fields.
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(PersonIdFieldName, PersonIdFieldName);
            headerLine.Add(PersonNameFieldName, PersonNameFieldName);

            var recordLine = new RenamedAttributeLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(RenamedHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperAndUnmappedField_WritesUnmappedFieldAfterMappedFields()
        {
            // arrange
            var recordLine = new PartlyMappedLine { Id = 1, Name = FirstName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<PartlyMappedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<PartlyMappedLineMapper>();

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperAndUnmappedFieldNotOnFirstLine_ThrowsCsvFileException()
        {
            // arrange
            // The first line sets the unmapped fields written, so the second line's Description field isn't one of them.
            var recordLine1 = new PartlyMappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new PartlyMappedLine { Id = 2, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<PartlyMappedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<PartlyMappedLineMapper>();
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>()
                    .WithMessage("Field 'Description' is not one of the 2 fields of the first record line.").Which;
                exception.Data[FieldNameDataItem].Should().Be(nameof(PartlyMappedLine.Description));
                exception.Data[ExpectedFieldCountDataItem].Should().Be(FirstLineFieldCount);
            }
        }

        [Fact]
        public void WriteLine_HeaderlessMapperAndLineReadWithUnmappedField_WritesFieldValues()
        {
            // arrange
            // The reader keeps the unmapped field under a name generated from its position.
            PartlyMappedLine testLine;
            using (var csvReader = new CsvFileReader<PartlyMappedLine>(new StringReader(PartlyMappedRecordFile)))
            {
                csvReader.RegisterMapper<PartlyMappedLineMapper>();
                testLine = (PartlyMappedLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<PartlyMappedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<PartlyMappedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineLackingAFieldOfFirstLine_WritesBlankValueForField()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be("1,One\r\n2,\r\n");
            }
        }

        [Fact]
        public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            // The options are checked before the file is created, so no file is created.
            const string testFilePath = "unused.csv";

            // act
            Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(testFilePath, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
        }

        [Fact]
        public void WriteLine_StreamConstructor_WritesStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, StreamBufferLengthFieldValue);

            using (var testStream = new RecordingStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                // The buffer is full once the field value is in it, so it is written, and the line break is left in the buffer.
                testStream.WriteSizes.Should().Equal(StreamBufferSize);
            }
        }

        [Fact]
        public void WriteLine_StreamWithEncodingOptionConstructor_WritesStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, StreamBufferLengthFieldValue);

            var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
            using (var testStream = new RecordingStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                // The buffer is full once the field value is in it, so it is written, and the line break is left in the buffer.
                testStream.WriteSizes.Should().Equal(StreamBufferSize);
            }
        }

        [Theory]
        [InlineData(TabValue, TabMessage)]
        [InlineData(NullValue, NullMessage)]
        [InlineData(DeleteValue, DeleteMessage)]
        [InlineData(NextLineValue, NextLineMessage)]
        public void WriteLine_RecordValueWithControlCharacter_ThrowsCsvFileException(string fieldValue, string expectedMessage)
        {
            // arrange
            // A string is UTF-16, whose character set has every character, but a control character isn't printable.
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, fieldValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(expectedMessage)
                    .Which.Data[FieldIndexDataItem].Should().Be(0);
            }
        }

        [Fact]
        public void WriteLine_RecordValueWithLoneHighSurrogateToStringWriter_ThrowsCsvFileException()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, LoneHighSurrogateValue);

            using (var stringWriter = new StringWriter())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(LoneHighSurrogateMessage)
                    .Which.Data[FieldIndexDataItem].Should().Be(0);
            }
        }

        [Fact]
        public void WriteLine_RecordValueWithLoneLowSurrogateToStream_ThrowsCsvFileException()
        {
            // arrange
            // The default encoding, UTF-8, would throw EncoderFallbackException for it.
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, LoneLowSurrogateValue);

            using (var testStream = new MemoryStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(LoneLowSurrogateMessage);
            }
        }

        public static TheoryData<string, Encoding, string> ValuesOutsideEncodingsCharacterSet => new TheoryData<string, Encoding, string>
        {
            { LatinSmallLetterEWithAcuteValue, Encoding.ASCII, AsciiLatinSmallLetterEWithAcuteMessage },
            { GrinningFaceValue, Encoding.ASCII, AsciiGrinningFaceMessage },
            { EuroSignValue, Encoding.GetEncoding(Latin1CodePage), Latin1EuroSignMessage },
            { GrinningFaceValue, Encoding.GetEncoding(Latin1CodePage), Latin1GrinningFaceMessage },
            { TabValue, new UTF8Encoding(false), Utf8TabMessage },
        };

        [Theory]
        [MemberData(nameof(ValuesOutsideEncodingsCharacterSet))]
        public void WriteLine_RecordValueOutsideEncodingsCharacterSet_ThrowsCsvFileException(string fieldValue, Encoding encoding, string expectedMessage)
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, fieldValue);

            var options = new CsvFileOptions { Encoding = encoding };
            using (var testStream = new MemoryStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(expectedMessage);
            }
        }

        [Fact]
        public void WriteLine_RecordValueWithCharacterCustomEncodingLacks_ThrowsCsvFileException()
        {
            // arrange
            // The encoding has no web name, and lacks a character of printable US-ASCII, so the writer checks each character against it.
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, BracketedValue);

            var options = new CsvFileOptions { Encoding = new RestrictedAsciiEncoding(LeftSquareBracket) };
            using (var testStream = new MemoryStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(RestrictedAsciiBracketMessage);
            }
        }

        public static TheoryData<CsvFileOptions, string> OptionsOutsideCharacterSet => new TheoryData<CsvFileOptions, string>
        {
            { new CsvFileOptions { Encoding = Encoding.ASCII, FieldDelimiterCharacter = SectionSign }, AsciiSectionSignDelimiterMessage },
            { new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage), TerminatorLineBreak = LineSeparator }, Latin1LineSeparatorTerminatorMessage },
        };

        [Theory]
        [MemberData(nameof(OptionsOutsideCharacterSet))]
        public void WriteLine_OptionsCharacterOutsideCharacterSet_ThrowsInvalidOperationException(CsvFileOptions options, string expectedMessage)
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, NumberValue);

            using (var testStream = new MemoryStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(expectedMessage);
            }
        }

        [Fact]
        public void WriteLine_EmbeddedLineBreakWithControlCharacter_ThrowsInvalidOperationException()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, NumberValue);

            var options = new CsvFileOptions { EmbeddedLineBreak = VerticalTab };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(VerticalTabEmbeddedLineBreakMessage);
            }
        }

        [Fact]
        public void WriteLine_RecordValueOutsideAsciiToWriterWithoutEncoding_ThrowsCsvFileException()
        {
            // arrange
            // A writer that has no encoding writes US-ASCII.
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, LatinSmallLetterEWithAcuteValue);

            using (var unencodedWriter = new UnencodedStringWriter())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(unencodedWriter))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(AsciiLatinSmallLetterEWithAcuteMessage);
            }
        }

        [Fact]
        public void WriteLine_RecordValueWithControlCharacter_WritesNothing()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, NumberValue);
            testRecordLine.Add(FieldName2, TabValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(testRecordLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(SecondFieldTabMessage);
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_HeaderValueWithControlCharacter_ThrowsCsvFileException()
        {
            // arrange
            var headerLine = new CsvFileHeaderLine();
            ((ICsvFileLine)headerLine).Add(ZeroFieldName, ValueWithTab);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(headerLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(LeftTabRightMessage);
            }
        }

        [Theory]
        [InlineData(LatinSmallLetterEWithAcuteValue, LatinSmallLetterEWithAcuteLine)]
        [InlineData(CjkUnifiedIdeographOneValue, CjkUnifiedIdeographOneLine)]
        [InlineData(GrinningFaceValue, GrinningFaceLine)]
        public void WriteLine_RecordValueOutsideAsciiToStringWriter_WritesValue(string fieldValue, string expectedLine)
        {
            // arrange
            // A string is UTF-16, whose character set has every character.
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, fieldValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                buffer.ToString().Should().Be(expectedLine);
            }
        }

        [Fact]
        public void WriteLine_RecordValueWithControlDelimiter_WritesQuotedValue()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, ValueWithTab);

            var options = new CsvFileOptions { FieldDelimiterCharacter = TabDelimiter };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                buffer.ToString().Should().Be(QuotedValueWithTabLine);
            }
        }

        [Fact]
        public void WriteLine_StreamWithEncodingOption_WritesUsingEncoding()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, "Caf\x00E9");

            var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
            using (var testStream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                // "Caf\x00E9" and CRLF in ISO 8859-1.
                testStream.ToArray().Should().Equal(0x43, 0x61, 0x66, 0xE9, 0x0D, 0x0A);
            }
        }

        [Fact]
        public void WriteLine_CallerSuppliedWriter_WritesToCallerSuppliedWriter()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, NumberValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(SingleNumberLine);
            }
        }

        [Fact]
        public void WriteLine_StreamWithoutEncodingOption_WritesUtf8WithoutByteOrderMark()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, "\x00E9");

            using (var testStream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                // "\x00E9" and CRLF in UTF-8, with no byte order mark.
                testStream.ToArray().Should().Equal(0xC3, 0xA9, 0x0D, 0x0A);
            }
        }

        [Fact]
        public void Constructor_StreamWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            using (var stream = new MemoryStream())
            {

                // act
                Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(stream, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithStream_ThrowsObjectDisposedException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithCallerSuppliedWriter_ThrowsObjectDisposedException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            using (var stringWriter = new StringWriter())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithCallerSuppliedWriter_WritesNothing()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);
                csvWriter.Dispose();

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.Dispose();

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void RegisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<AllIndexedLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Theory]
        [InlineData("\"", null, "1,\"\"\"\"\r\n")]
        [InlineData(",", null, "1,\",\"\r\n")]
        [InlineData("Say \"hi\" \"\"", null, "1,\"Say \"\"hi\"\" \"\"\"\"\"\r\n")]
        [InlineData("Left\rRight", null, "1,\"Left\rRight\"\r\n")]
        [InlineData("Left\nRight", null, "1,\"Left\nRight\"\r\n")]
        [InlineData(ValueWithCrLf, null, "1,\"Left\r\nRight\"\r\n")]
        [InlineData("Plain", null, "1,Plain\r\n")]
        [InlineData("Left\rRight", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("Left\nRight", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData(ValueWithCrLf, HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("Left<br>Right", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("bar", HtmlLineBreak, "1,bar\r\n")]
        [InlineData("Left\nRight", "\n", "1,\"Left\nRight\"\r\n")]
        public void WriteLine_RecordLineAndQuoteHandlingRequired_QuotesOnlyValuesRequiringQuotes(string testFieldValue, string testEmbeddedLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, testFieldValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required, EmbeddedLineBreak = testEmbeddedLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingRequiredWithEmptyEmbeddedLineBreak_WritesAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, "Plain");

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required, EmbeddedLineBreak = string.Empty };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                // Every value contains the empty line break sequence, so every value is quoted.
                buffer.ToString().Should().Be("\"1\",\"Plain\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFormattableValueAndNonInvariantCulture_WritesValueFormattedWithCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add("Amount", 1234.5m);
            testLine.Add(NameFieldName, FirstName);

            var options = new CsvFileOptions { Culture = CreateCulture("-", ",") };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                // The culture's decimal separator is the field delimiter, so the value is quoted.
                buffer.ToString().Should().Be("\"1234,5\",One\r\n");
            }
        }

        /// <summary>
        /// A UTC time with fractional seconds, which the general date pattern would write without them and without its <see cref="DateTimeKind"/>.
        /// </summary>
        private static readonly DateTime UtcTimeWithMilliseconds = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);

        /// <summary>
        /// <see cref="UtcTimeWithMilliseconds"/> in the round-trip pattern (<c>"o"</c>).
        /// </summary>
        private const string UtcTimeWithMillisecondsText = "2024-01-02T03:04:05.6780000Z";

        /// <summary>
        /// A line holding only <see cref="UtcTimeWithMillisecondsText"/>.
        /// </summary>
        private const string UtcTimeWithMillisecondsLine = "2024-01-02T03:04:05.6780000Z\r\n";

        /// <summary>
        /// The double nearest to 0.1 + 0.2, which needs 17 significant digits to read back as the same value.  .NET Framework's <c>"R"</c> and
        /// general format write it as <c>0.3</c>.
        /// </summary>
        private const double SumOfOneTenthAndTwoTenths = 0.30000000000000004;

        /// <summary>
        /// <see cref="SumOfOneTenthAndTwoTenths"/> with the fewest significant digits that read back as it.
        /// </summary>
        private const string SumOfOneTenthAndTwoTenthsText = "0.30000000000000004";

        /// <summary>
        /// A double that needs 16 significant digits to read back as the same value, which .NET Framework's <c>"R"</c> writes with 15
        /// (<c>901.742658997766</c>), a different double.
        /// </summary>
        private const double SixteenDigitDouble = 901.7426589977661;

        /// <summary>
        /// A float that needs 8 significant digits to read back as the same value: 0.9000000 is a different float.
        /// </summary>
        private const float EightDigitFloat = 0.90000004f;

        public static TheoryData<string, Func<string>, string> UnconvertedValueTextCases => new TheoryData<string, Func<string>, string>
        {
            { "DateTime, UTC", () => WriteValue(UtcTimeWithMilliseconds), UtcTimeWithMillisecondsText },
            { "DateTime, unspecified kind", () => WriteValue(new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Unspecified)), "2024-01-02T03:04:05.6780000" },
            { "DateTimeOffset", () => WriteValue(new DateTimeOffset(2024, 1, 2, 3, 4, 5, 678, new TimeSpan(13, 0, 0))), "2024-01-02T03:04:05.6780000+13:00" },
#if NET6_0_OR_GREATER
            { "DateOnly", () => WriteValue(new DateOnly(2024, 1, 2)), "2024-01-02" },
            { "TimeOnly", () => WriteValue(new TimeOnly(3, 4, 5, 678)), "03:04:05.6780000" },
#endif
            { "double, needs 17 digits", () => WriteValue(SumOfOneTenthAndTwoTenths), SumOfOneTenthAndTwoTenthsText },
            { "double, needs 16 digits", () => WriteValue(SixteenDigitDouble), "901.7426589977661" },
            { "double, one digit", () => WriteValue(0.1d), "0.1" },
            { "double, negative", () => WriteValue(-1.5d), "-1.5" },
            { "double, negative zero", () => WriteValue(-0.0d), "-0" },
            { "double, whole number of 17 digits", () => WriteValue(1E+16d), "10000000000000000" },
            { "double, whole number of 18 digits", () => WriteValue(1E+17d), "1E+17" },
            { "double, decimal point three places before the first digit", () => WriteValue(0.0001d), "0.0001" },
            { "double, decimal point four places before the first digit", () => WriteValue(0.00001d), "1E-05" },
            { "double, largest", () => WriteValue(double.MaxValue), "1.7976931348623157E+308" },
            { "double, smallest subnormal", () => WriteValue(double.Epsilon), "5E-324" },
            { "double, smallest normal", () => WriteValue(2.2250738585072014E-308), "2.2250738585072014E-308" },
            { "double, NaN", () => WriteValue(double.NaN), "NaN" },
            { "double, positive infinity", () => WriteValue(double.PositiveInfinity), "Infinity" },
            { "double, negative infinity", () => WriteValue(double.NegativeInfinity), "-Infinity" },
            { "float, one digit", () => WriteValue(0.1f), "0.1" },
            { "float, needs 8 digits", () => WriteValue(EightDigitFloat), "0.90000004" },
            { "float, whole number of 9 digits", () => WriteValue(1E+8f), "100000000" },
            { "float, whole number of 10 digits", () => WriteValue(1E+9f), "1E+09" },
            { "float, largest", () => WriteValue(float.MaxValue), "3.4028235E+38" },
            { "float, smallest subnormal", () => WriteValue(float.Epsilon), "1E-45" },
        };

        [Theory]
        [MemberData(nameof(UnconvertedValueTextCases))]
        public void WriteLine_ValueWithoutConverter_WritesTextThatReadsBackAsTheSameValue(string valueType, Func<string> writeValue, string expected)
        {
            // arrange
            _ = valueType;

            // act
            var testResult = writeValue();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLine_DoubleWithoutConverterAndCulture_WritesTextWithCultureSymbols()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, -1.5E-07d);

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, ":") };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("~1:5E~07\r\n");
            }
        }

        [Fact]
        public void WriteLine_DateTimeWithoutConverterAndCulture_WritesRoundTripPattern()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, UtcTimeWithMilliseconds);

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, ":") };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(UtcTimeWithMillisecondsLine);
            }
        }

        public static TheoryData<string, Func<object>, object> RoundTripCases => new TheoryData<string, Func<object>, object>
        {
            { "DateTime", () => WriteAndReadBack(UtcTimeWithMilliseconds), UtcTimeWithMilliseconds },
            { "double", () => WriteAndReadBack(SumOfOneTenthAndTwoTenths), SumOfOneTenthAndTwoTenths },
            { "double, needs 16 digits", () => WriteAndReadBack(SixteenDigitDouble), SixteenDigitDouble },
            { "float", () => WriteAndReadBack(EightDigitFloat), EightDigitFloat },
        };

        [Theory]
        [MemberData(nameof(RoundTripCases))]
        public void WriteLineThenReadLine_ValueWithoutConverter_ReadsTheSameValue(string valueType, Func<object> writeAndReadBack, object expected)
        {
            // arrange
            _ = valueType;

            // act
            var testResult = writeAndReadBack();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteLineThenReadLine_UtcDateTimeWithoutConverter_ReadsUtcKind()
        {
            // arrange

            // act
            var testResult = (DateTime)WriteAndReadBack(UtcTimeWithMilliseconds);

            // assert
            testResult.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void WriteLine_RecordLinesWithToStringConverter_CallsConverterOncePerField()
        {
            // arrange
            var testLine1 = new IdConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };
            var testLine2 = new IdConverterLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var testLine3 = new IdConverterLine { Id = 3, Name = ThirdName, Description = ThirdDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IdConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IdConverterLineMapper>();
                csvWriter.WriteLine(testLine1);
                csvWriter.WriteLine(testLine2);

                // act
                csvWriter.WriteLine(testLine3);

                // assert
                buffer.ToString().Should().Be("#1,One,First\r\n#2,Two,Second\r\n#3,Three,Third\r\n");
                testLine1.ConvertedIds.Should().Equal(1);
                testLine2.ConvertedIds.Should().Equal(2);
                testLine3.ConvertedIds.Should().Equal(3);
            }
        }

        [Fact]
        public void WriteLine_AttributeMappingRenamesField_WritesPropertyValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMapperRenamesField_WritesPropertyValues()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RenamedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_MappingKeyIsNameOfAnotherKeyedProperty_WritesMappedPropertyValue()
        {
            // arrange
            var testLine = new CrossKeyedAttributeLine { Title = CrossKeyedTitle, Name = CrossKeyedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CrossKeyedAttributeLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(CrossKeyedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMappingKeyIsNameOfAnotherKeyedProperty_WritesMappedPropertyValue()
        {
            // arrange
            var testLine = new CrossKeyedBuilderLine { Title = CrossKeyedTitle, Name = CrossKeyedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CrossKeyedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<CrossKeyedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(CrossKeyedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_DerivedLineWithMappingKeyThatIsNameOfAnotherKeyedProperty_WritesMappedPropertyValue()
        {
            // arrange
            var testLine = new DerivedCrossKeyedBuilderLine { Title = CrossKeyedTitle, Name = CrossKeyedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CrossKeyedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<CrossKeyedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(CrossKeyedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_PropertiesKeyedByEachOthersNames_WritesEachPropertysValue()
        {
            // arrange
            var testLine = new SwappedKeyLine { A = SwappedValueA, B = SwappedValueB };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<SwappedKeyLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(SwappedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RenamedMappingAndLineWithFieldNamesOnly_WritesFieldValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine();
            testLine.Add(PersonIdFieldName, RenamedId);
            testLine.Add(PersonNameFieldName, RenamedName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RenamedMappingAndLineWithPropertyKeysAndFieldNames_WritesFieldNameValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine { Id = RenamedId, Name = RenamedName };
            testLine.Add(PersonIdFieldName, FieldNameId);
            testLine.Add(PersonNameFieldName, FieldNameName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FieldNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndNoWriterMapper_WritesFieldValues()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<RenamedBuilderLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndPropertyChanged_WritesChangedValue()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<RenamedBuilderLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            testLine.Id = ChangedId;

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RenamedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ChangedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndConverterReadingContextField_WritesConvertedValue()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<FieldReadingConverterLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FieldReadingConverterLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMapperRenamesFieldOfPropertyWithCsvFieldAttribute_WritesPropertyValue()
        {
            // arrange
            var testLine = new KeyedBuilderLine { Id = RenamedId };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<KeyedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<KeyedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedIdLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMapperMapsPropertyWithCsvFieldAttributeAfterHeaderLine_WritesPropertyValue()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(PersonKeyFieldName, PersonKeyFieldName);

            var recordLine = new KeyedBuilderLine { Id = RenamedId };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<KeyedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<UnrenamedKeyedBuilderLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(KeyedIdHeaderAndRecordLines);
            }
        }

        [Fact]
        public void WriteLine_MapperRegisteredAfterFirstLine_WritesLaterLinesInOrderByMapping()
        {
            // arrange
            var testLine1 = new ReversedLine { Id = 1, Name = FirstName, Description = FirstDescription };
            var testLine2 = new ReversedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ReversedLine>(stringWriter))
            {
                csvWriter.WriteLine(testLine1);
                csvWriter.RegisterMapper<ReversedLineMapper>();

                // act
                csvWriter.WriteLine(testLine2);

                // assert
                buffer.ToString().Should().Be("1,One,First\r\nSecond,Two,2\r\n");
            }
        }

        [Fact]
        public void WriteLine_ConverterThrowsPartWayThroughLine_ThrowsConverterException()
        {
            // arrange
            var testLine = new ThrowingConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ThrowingConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ThrowingConverterLineMapper>();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(ThrowingConverterLineMapper.ConverterMessage);
            }
        }

        [Fact]
        public void WriteLine_ConverterThrowsPartWayThroughLine_WritesNothingForLine()
        {
            // arrange
            var testLine = new ThrowingConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ThrowingConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ThrowingConverterLineMapper>();

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                // The first field converts, but the line is written only once every field has converted.
                buffer.ToString().Should().BeEmpty();
            }
        }

        /// <summary>
        /// Lines that a <see cref="CsvFileWriter{TRecordLine}"/> of <see cref="AllIndexedLine"/> can't write: a plain record line, and a record line of a sibling type.
        /// </summary>
        public static TheoryData<ICsvFileLine> LinesOfAnotherRecordType()
        {
            ICsvFileLine plainLine = new CsvFileRecordLine();
            plainLine.Add(FieldName1, TextValue);

            return new TheoryData<ICsvFileLine>
            {
                plainLine,
                new UnmappedLine { Id = 1, Name = FirstName, Description = FirstDescription }
            };
        }

        [Theory]
        [MemberData(nameof(LinesOfAnotherRecordType))]
        public void WriteLine_LineOfAnotherRecordType_ThrowsArgumentException(ICsvFileLine testLine)
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ArgumentException>().WithParameterName("csvLine");
            }
        }

        [Theory]
        [MemberData(nameof(LinesOfAnotherRecordType))]
        public void WriteLine_LineOfAnotherRecordType_WritesNothing(ICsvFileLine testLine)
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_NullLine_ThrowsArgumentNullException()
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("csvLine");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithConstantAndVariableValueMappings_WritesMappedValues()
        {
            // arrange
            var testLine = new ConstantAndVariableLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantAndVariableLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ConstantAndVariableLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConstantAndVariableRecord);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithoutConstantAndVariableValueMappedFields_WritesMappedValues()
        {
            // arrange
            var testLine = new ConstantAndVariableLine { Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantAndVariableLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ConstantAndVariableLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConstantAndVariableRecord);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithoutConstantAndVariableValueMappedFieldsAfterHeaderLine_WritesMappedValues()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(nameof(ConstantAndVariableLine.Id), nameof(ConstantAndVariableLine.Id));
            headerLine.Add(nameof(ConstantAndVariableLine.Name), nameof(ConstantAndVariableLine.Name));
            headerLine.Add(nameof(ConstantAndVariableLine.Description), nameof(ConstantAndVariableLine.Description));

            var testLine = new ConstantAndVariableLine { Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantAndVariableLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ConstantAndVariableLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("Id,Name,Description\r\n42,Constant,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithoutAttributeConstantValueField_WritesConstantValue()
        {
            // arrange
            var testLine = new ConstantAttributeLine { Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantAttributeLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("bob,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_ConstantPropertyTypeCantHold_WritesConstantAsGiven()
        {
            // arrange
            var testLine = new IntConstantStringLine();

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IntConstantStringLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("5\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesOfDifferentTypesWithoutRegisteredMapper_WritesEachLineWithItsOwnMapping()
        {
            // arrange
            ICsvFileLine testLine1 = new MixedLine();
            testLine1.Add(nameof(AttributeMappedLine.First), FirstAttributeValue);
            testLine1.Add(nameof(AttributeMappedLine.Second), SecondAttributeValue);

            var testLine2 = new AttributeMappedLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<MixedLine>(stringWriter))
            {
                csvWriter.WriteLine(testLine1);

                // act
                csvWriter.WriteLine(testLine2);

                // assert
                buffer.ToString().Should().Be("1st,2nd\r\n2nd,1st\r\n");
            }
        }

        [Fact]
        public void WriteLine_AttributeMappingOnBaseLineType_WritesFieldsInMappedOrder()
        {
            // arrange
            ICsvFileLine testLine = new InheritedAttributeMappedLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<InheritedAttributeMappedLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ReversedAttributeRecord);
            }
        }

        [Fact]
        public void WriteLine_DerivedLineAddsMappedProperty_WritesInheritedFieldFirst()
        {
            // arrange
            ICsvFileLine testLine = new OrderedDerivedLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<OrderedDerivedLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(OrderedAttributeRecord);
            }
        }

        /// <summary>
        /// Writes a value as the only field of a line without a converter, and gives the text of the field.
        /// </summary>
        private static string WriteValue(object value)
        {
            ICsvFileLine csvLine = new CsvFileRecordLine();
            csvLine.Add(FieldName1, value);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(csvLine);
            }

            return buffer.ToString(0, buffer.Length - "\r\n".Length);
        }

        /// <summary>
        /// Writes a value as the only field of a line without a converter, and reads the line back into a property of the value's type.
        /// </summary>
        private static object WriteAndReadBack<T>(T value)
        {
            // Written and read back without a header line, so that the only field is the value.
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RoundTripLine<T>>(stringWriter, options))
            {
                csvWriter.WriteLine(new RoundTripLine<T> { Value = value });
            }

            using (var csvReader = new CsvFileReader<RoundTripLine<T>>(new StringReader(buffer.ToString()), options))
            {
                return ((RoundTripLine<T>)csvReader.ReadLine()).Value;
            }
        }

        private sealed class RoundTripLine<T> : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            public T Value
            {
                get => GetPropertyValue<T>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Creates a culture that formats numbers with the given negative sign and decimal separator, and is otherwise the
        /// invariant culture, so that the result doesn't depend on the culture data of the machine.
        /// </summary>
        private static CultureInfo CreateCulture(string negativeSign, string decimalSeparator)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = negativeSign;
            culture.NumberFormat.NumberDecimalSeparator = decimalSeparator;

            return culture;
        }

        /// <summary>
        /// Writes a line and then disposes the writer, so that everything it wrote reaches its stream.
        /// </summary>
        private static void WriteLineAndDispose(CsvFileWriter<CsvFileRecordLine> csvWriter, ICsvFileLine csvLine)
        {
            using (csvWriter)
            {
                csvWriter.WriteLine(csvLine);
            }
        }

        /// <summary>
        /// The record-line type of a writer that writes lines of several types.
        /// </summary>
        private class MixedLine : CsvFileRecordLine
        {
        }

        /// <summary>
        /// A line whose properties are mapped, by attribute, to fields in the opposite order, for <see cref="InheritedAttributeMappedLine"/> to inherit.
        /// </summary>
        private class ReversedAttributeBaseLine : CsvFileRecordLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that inherits every mapped property from <see cref="ReversedAttributeBaseLine"/>.
        /// </summary>
        private sealed class InheritedAttributeMappedLine : ReversedAttributeBaseLine
        {
        }

        /// <summary>
        /// A line with a property mapped, by attribute, to a field with no index, for <see cref="OrderedDerivedLine"/> to inherit.
        /// </summary>
        private class OrderedBaseLine : CsvFileRecordLine
        {
            [CsvFieldMapping(nameof(First))]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that adds a property mapped to a field with no index to the one it inherits from <see cref="OrderedBaseLine"/>.
        /// </summary>
        private sealed class OrderedDerivedLine : OrderedBaseLine
        {
            [CsvFieldMapping(nameof(Second))]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class AttributeMappedLine : MixedLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private abstract class TestLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Description
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that no mapper is registered for.
        /// </summary>
        private sealed class UnmappedLine : TestLine
        {
        }

        private sealed class AllIndexedLine : TestLine
        {
        }

        private sealed class FirstIndexedLine : TestLine
        {
        }

        private sealed class LastIndexedLine : TestLine
        {
        }

        private sealed class GapIndexedLine : TestLine
        {
        }

        private sealed class ReversedLine : TestLine
        {
        }

        private sealed class ThrowingConverterLine : TestLine
        {
        }

        /// <summary>
        /// A line whose <see cref="TestLine.Description"/> property <see cref="ConstantDescriptionLineMapper"/> gives a constant value.
        /// </summary>
        private sealed class ConstantDescriptionLine : TestLine
        {
        }

        /// <summary>
        /// A line whose two fields are mapped by attribute, to positions that <see cref="CodeTitleAttributeLine"/> maps other properties to.
        /// </summary>
        private sealed class IdNameAttributeLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            public string Id
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(1)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line whose two fields are mapped by attribute, to positions that <see cref="IdNameAttributeLine"/> maps other properties to.
        /// </summary>
        private sealed class CodeTitleAttributeLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            public string Code
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(1)]
            public string Title
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that <see cref="PartlyMappedLineMapper"/> maps only the <see cref="TestLine.Id"/> property of.
        /// </summary>
        private sealed class PartlyMappedLine : TestLine
        {
        }

        /// <summary>
        /// A field value that can't be formatted as text.
        /// </summary>
        private sealed class UnformattableValue
        {
            public override string ToString() => throw new InvalidOperationException();
        }

        private sealed class ConstantAndVariableLine : TestLine
        {
        }

        /// <summary>
        /// A line whose properties are mapped, by attribute, to fields with other names.
        /// </summary>
        private sealed class RenamedAttributeLine : CsvFileRecordLine
        {
            [CsvFieldMapping(PersonIdFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(PersonNameFieldName)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line whose properties <see cref="RenamedBuilderLineMapper"/> maps to fields with other names.
        /// </summary>
        private sealed class RenamedBuilderLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class RenamedBuilderLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public RenamedBuilderLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
            }
        }

        /// <summary>
        /// A line whose Title property is keyed by the name of its Name property, which has a key of its own.
        /// </summary>
        private sealed class CrossKeyedAttributeLine : CsvFileRecordLine
        {
            [CsvField(CrossKeyedTitleKey)]
            [CsvFieldMapping(CrossKeyedTitleFieldName)]
            public string Title
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvField(CrossKeyedNameKey)]
            [CsvFieldMapping(CrossKeyedNameKey)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// <see cref="CrossKeyedAttributeLine"/> with its field names given by <see cref="CrossKeyedBuilderLineMapper"/>.
        /// </summary>
        private class CrossKeyedBuilderLine : CsvFileRecordLine
        {
            [CsvField(CrossKeyedTitleKey)]
            public string Title
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvField(CrossKeyedNameKey)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class CrossKeyedBuilderLineMapper : CsvFileMapperBase<CrossKeyedBuilderLine>
        {
            public CrossKeyedBuilderLineMapper()
            {
                this.Map().Property(x => x.Title).HasFieldName(CrossKeyedTitleFieldName);
                this.Map().Property(x => x.Name).HasFieldName(CrossKeyedNameKey);
            }
        }

        /// <summary>
        /// A line of another type than <see cref="CrossKeyedBuilderLine"/>, written with the mapper registered for it.
        /// </summary>
        private sealed class DerivedCrossKeyedBuilderLine : CrossKeyedBuilderLine
        {
        }

        /// <summary>
        /// A line whose two properties are each keyed by the other's name.
        /// </summary>
        private sealed class SwappedKeyLine : CsvFileRecordLine
        {
            [CsvField(SwappedKeyA)]
            [CsvFieldMapping(SwappedFieldNameA)]
            public string A
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvField(SwappedKeyB)]
            [CsvFieldMapping(SwappedFieldNameB)]
            public string B
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps Id to a field with another name, with a converter that reads the value from the line by the field name the
        /// context gives, and prefixes it with <c>#</c>.
        /// </summary>
        private sealed class FieldReadingConverterLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public FieldReadingConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldName(PersonIdFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(context => "#" + ((ICsvFileLine)context.Model)[context.FieldName]));
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
            }
        }

        /// <summary>
        /// Maps Name and then Id to fields with other names, with a converter that writes empty text for Id.
        /// </summary>
        private sealed class EmptyIdConverterLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public EmptyIdConverterLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldName(PersonIdFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(context => string.Empty));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with a converter that writes <see cref="ConvertedName"/> for Name.
        /// </summary>
        private sealed class DelegateConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public DelegateConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(_ => ConvertedName));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="FixedStringConverter"/> as the type converter of Name.
        /// </summary>
        private sealed class TypeConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public TypeConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(typeof(FixedStringConverter));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="FromStringOnlyConverter"/> as the type converter of Name.
        /// </summary>
        private sealed class FromStringOnlyConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public FromStringOnlyConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(typeof(FromStringOnlyConverter));
            }
        }

        /// <summary>
        /// A line whose Name, mapped by attribute, has <see cref="RenamedName"/> as its constant value, followed by its Description.
        /// </summary>
        private sealed class ConstantAttributeLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = RenamedName)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(1)]
            public string Description
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line whose Name has <see cref="RenamedName"/> as its constant value, and <see cref="FixedStringConverter"/> as its type converter.
        /// </summary>
        private sealed class ConstantConvertedNameLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = RenamedName)]
            [TypeConverter(typeof(FixedStringConverter))]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Converts every string to <see cref="ConvertedName"/>, and doesn't convert to a string.
        /// </summary>
        private sealed class FromStringOnlyConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => false;

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => ConvertedName;
        }

        /// <summary>
        /// Converts every value to <see cref="ConvertedName"/>.
        /// </summary>
        private sealed class FixedStringConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => ConvertedName;
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="RenamedName"/> as the constant value of Name.
        /// </summary>
        private sealed class ConstantNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public ConstantNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .HasConstantValue(RenamedName);
            }
        }

        /// <summary>
        /// A line whose property has a key from <see cref="CsvFieldAttribute"/>, which <see cref="KeyedBuilderLineMapper"/>
        /// maps to a field with another name.
        /// </summary>
        private sealed class KeyedBuilderLine : CsvFileRecordLine
        {
            [CsvField(PersonKeyFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class KeyedBuilderLineMapper : CsvFileMapperBase<KeyedBuilderLine>
        {
            public KeyedBuilderLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
            }
        }

        /// <summary>
        /// Maps the property of <see cref="KeyedBuilderLine"/> without naming a field, so it maps to the field named by its
        /// <see cref="CsvFieldAttribute"/>.
        /// </summary>
        private sealed class UnrenamedKeyedBuilderLineMapper : CsvFileMapperBase<KeyedBuilderLine>
        {
            public UnrenamedKeyedBuilderLineMapper()
            {
                this.Map().Property(x => x.Id);
            }
        }

        private sealed class IdConverterLine : TestLine
        {
            /// <summary>
            /// The IDs that the converter of the Id mapping was called for.  A field, not a property, so that it is not
            /// part of the line.
            /// </summary>
            public readonly List<int> ConvertedIds = new List<int>();
        }

        private sealed class InstanceScopedLine : TestLine
        {
        }

        private sealed class InstanceScopedLineMapper : CsvFileMapperBase<InstanceScopedLine>
        {
            public InstanceScopedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ClearedCacheLine : CsvFileRecordLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class AllIndexedLineMapper : CsvFileMapperBase<AllIndexedLine>
        {
            public AllIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class FirstIndexedLineMapper : CsvFileMapperBase<FirstIndexedLine>
        {
            public FirstIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description);
            }
        }

        private sealed class LastIndexedLineMapper : CsvFileMapperBase<LastIndexedLine>
        {
            public LastIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ConstantDescriptionLineMapper : CsvFileMapperBase<ConstantDescriptionLine>
        {
            public ConstantDescriptionLineMapper()
            {
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName(DescriptionFieldName)
                    .HasConstantValue(FirstDescription);
            }
        }

        private sealed class PartlyMappedLineMapper : CsvFileMapperBase<PartlyMappedLine>
        {
            public PartlyMappedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        private sealed class IndexGapLine : TestLine
        {
        }

        /// <summary>
        /// Maps Id, Name and Description to indexes 0, 1 and 3, so no mapping covers position 2.
        /// </summary>
        private sealed class IndexGapLineMapper : CsvFileMapperBase<IndexGapLine>
        {
            public IndexGapLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(3);
            }
        }

        private sealed class ContactLine : CsvFileRecordLine
        {
            public string FirstName
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string LastName
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Title
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string City
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Country
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps FirstName and LastName by name, then Title to index 0, City by name and Country to index 9.
        /// </summary>
        private sealed class ContactLineMapper : CsvFileMapperBase<ContactLine>
        {
            public ContactLineMapper()
            {
                this.Map().Property(x => x.FirstName);
                this.Map().Property(x => x.LastName);
                this.Map().Property(x => x.Title).HasFieldIndex(0);
                this.Map().Property(x => x.City);
                this.Map().Property(x => x.Country).HasFieldIndex(9);
            }
        }

        /// <summary>
        /// Maps the fields as <see cref="ContactLineMapper"/> does, except that City is optional.
        /// </summary>
        private sealed class OptionalCityContactLineMapper : CsvFileMapperBase<ContactLine>
        {
            public OptionalCityContactLineMapper()
            {
                this.Map().Property(x => x.FirstName);
                this.Map().Property(x => x.LastName);
                this.Map().Property(x => x.Title).HasFieldIndex(0);
                this.Map().Property(x => x.City).IsOptional();
                this.Map().Property(x => x.Country).HasFieldIndex(9);
            }
        }

        private sealed class SharedIndexLine : TestLine
        {
        }

        /// <summary>
        /// Maps Name and then Description to index 0, so they share the field there.
        /// </summary>
        private sealed class SharedIndexLineMapper : CsvFileMapperBase<SharedIndexLine>
        {
            public SharedIndexLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldIndex(0);
                this.Map().Property(x => x.Description).HasFieldIndex(0);
            }
        }

        private sealed class NamedAtSharedIndexLine : TestLine
        {
        }

        /// <summary>
        /// Maps Name and then Description to index 0, which Name, the first, names, and Id by the name Description, which no field has.
        /// </summary>
        private sealed class NamedAtSharedIndexLineMapper : CsvFileMapperBase<NamedAtSharedIndexLine>
        {
            public NamedAtSharedIndexLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldIndex(0);
                this.Map().Property(x => x.Description).HasFieldIndex(0);
                this.Map().Property(x => x.Id).HasFieldName(DescriptionFieldName).ConvertUsing(typeof(Int32Converter));
            }
        }

        private sealed class GapIndexedLineMapper : CsvFileMapperBase<GapIndexedLine>
        {
            public GapIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(10);
            }
        }

        private sealed class IdConverterLineMapper : CsvFileMapperBase<IdConverterLine>
        {
            public IdConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(context =>
                    {
                        context.Model.ConvertedIds.Add(context.Model.Id);
                        return context.Model.Id.ToString("'#'0", CultureInfo.InvariantCulture);
                    });
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ReversedLineMapper : CsvFileMapperBase<ReversedLine>
        {
            public ReversedLineMapper()
            {
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(0);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(2)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        /// <summary>
        /// Maps Name first, then Id with a converter that always throws, so the line fails part of the way through.
        /// </summary>
        private sealed class ThrowingConverterLineMapper : CsvFileMapperBase<ThrowingConverterLine>
        {
            public const string ConverterMessage = "The converter failed.";

            public ThrowingConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(0);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(1)
                    .ConvertUsing(new CsvFieldConvertToString<ThrowingConverterLine>(_ => throw new InvalidOperationException(ConverterMessage)));
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ConstantAndVariableLineMapper : CsvFileMapperBase<ConstantAndVariableLine>
        {
            public ConstantAndVariableLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .HasVariableValue(() => 42);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1)
                    .HasConstantValue("Constant");
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        [Fact]
        public void WriteLine_HeaderNameWithLineBreakAndEmbeddedLineBreakSet_WritesTheNameAsTheReaderWillGiveItBack()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, "One\r\nTwo");
            var options = new CsvFileOptions { EmbeddedLineBreak = "<br>" };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"One<br>Two\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderNameWithLineBreakAndEmbeddedLineBreakSet_StillOrdersRecordFieldsByTheName()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(HeaderFieldName1, "One\r\nTwo");
            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add("One\r\nTwo", TextValue);
            var options = new CsvFileOptions { EmbeddedLineBreak = "<br>" };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().EndWith(TextValue + "\r\n");
            }
        }

        public static TheoryData<string, string, string> ValuesHoldingPartOfTheTerminator => new TheoryData<string, string, string>
        {
            { "|", "a|b", "\"a|b\"|" },
            { "||", "a||b", "\"a||b\"||" },
            { "||", "x|", "\"x|\"||" },
            { "||", "|x", "\"|x\"||" }
        };

        [Theory]
        [MemberData(nameof(ValuesHoldingPartOfTheTerminator))]
        public void WriteLine_RecordValueHoldingPartOfACustomTerminator_QuotesTheValue(string terminator, string value, string expected)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, value);
            var options = new CsvFileOptions { TerminatorLineBreak = terminator };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expected);
            }
        }

        // "1.5.5" is a number where the period groups digits, as in German, and not where it is the decimal separator, as in the invariant culture.
        private const string NumberOnlyWhereThePeriodGroups = "1.5.5";

        [Fact]
        public void WriteLine_AutoQuotingWithCultureOption_DecidesWhichValuesAreNumbersWithThatCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberOnlyWhereThePeriodGroups);
            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto, Culture = new CultureInfo("de-DE") };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(NumberOnlyWhereThePeriodGroups + "\r\n");
            }
        }

        [Fact]
        public void WriteLine_AutoQuotingWithNoCultureOption_IgnoresTheCurrentCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberOnlyWhereThePeriodGroups);
            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };
            var buffer = new StringBuilder();
            using (new CurrentCultureScope(new CultureInfo("de-DE")))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"" + NumberOnlyWhereThePeriodGroups + "\"\r\n");
            }
        }

        [Fact]
        public void Constructor_TextWriterWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            using (var stringWriter = new StringWriter())
            {

                // act
                Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(stringWriter, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
            }
        }

        // Options with an inconsistent field count handler of a type the writer never calls: a delegate for a base type of the line type, or a
        // delegate of another type.
        public static TheoryData<CsvFileOptions> OptionsWithHandlerOfAnotherType => new TheoryData<CsvFileOptions>
        {
            new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(context => { }) },
            new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldBadValue<HandlerLine>(context => { }) },
            new CsvFileOptions { InconsistentFieldCountHandler = new Action<CsvFieldContext<HandlerLine>>(context => { }) }
        };

        [Theory]
        [MemberData(nameof(OptionsWithHandlerOfAnotherType))]
        public void Constructor_HandlerOfAnotherType_ThrowsArgumentException(CsvFileOptions options)
        {
            // act
            Action testAction = () => _ = new CsvFileWriter<HandlerLine>(TextWriter.Null, options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName(OptionsParameterName);
        }

        // Options with a bad-value or header handler that a reader of another line type could call.  The writer never calls either handler.
        public static TheoryData<CsvFileOptions> OptionsWithHandlerTheWriterNeverCalls => new TheoryData<CsvFileOptions>
        {
            new CsvFileOptions { BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(context => { }) },
            new CsvFileOptions { HeaderHandler = new Func<CsvFileHeaderLine, IReadOnlyList<string>>(headerLine => Array.Empty<string>()) }
        };

        [Theory]
        [MemberData(nameof(OptionsWithHandlerTheWriterNeverCalls))]
        public void Constructor_HandlerTheWriterNeverCalls_DoesNotThrow(CsvFileOptions options)
        {
            // act
            Action testAction = () => _ = new CsvFileWriter<HandlerLine>(TextWriter.Null, options);

            // assert
            testAction.Should().NotThrow();
        }

        // The constructors that take options and no file path.  The file path constructor is covered by the integration tests.
        public static TheoryData<Func<CsvFileOptions, IDisposable>> OptionsConstructors => new TheoryData<Func<CsvFileOptions, IDisposable>>
        {
            options => new CsvFileWriter<HandlerLine>(Stream.Null, options),
            options => new CsvFileWriter<HandlerLine>(TextWriter.Null, options)
        };

        [Theory]
        [MemberData(nameof(OptionsConstructors))]
        public void Constructor_AnyOverloadWithHandlerOfAnotherType_ThrowsArgumentException(Func<CsvFileOptions, IDisposable> constructor)
        {
            // arrange
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(context => { }) };

            // act
            Action testAction = () => constructor(options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName(OptionsParameterName);
        }

        [Fact]
        public void Constructor_HandlerOfLineType_DoesNotThrow()
        {
            // arrange
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<HandlerLine>(context => { }) };

            // act
            Action testAction = () => _ = new CsvFileWriter<HandlerLine>(TextWriter.Null, options);

            // assert
            testAction.Should().NotThrow();
        }

        private sealed class HandlerLine : CsvFileRecordLine
        {
        }

        // A line whose constant is an int, which a string property can't hold, so a reader rejects it and a writer writes it as it is given.
        private sealed class IntConstantStringLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = 5)]
            public string Value
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        // The field delimiter and terminator that RFC 4180 gives a CSV file.
        // ref. RFC 4180§2¶1, RFC 4180§2¶4
        private const char Rfc4180FieldDelimiter = ',';
        private const string Rfc4180Terminator = "\r\n";

        // A quote handling value that CsvFieldQuoteHandling doesn't define.
        private const CsvFieldQuoteHandling UndefinedQuoteHandling = (CsvFieldQuoteHandling)99;

        // The messages for options that a writer rejects.  Each ends with a wildcard, as an ArgumentException adds the parameter name to its message.
        private const string EmptyTerminatorMessage = "The terminator line break must have at least one character.*";
        private const string QuoteTerminatorMessage = "The terminator line break can't start with a double quote.*";
        private const string QuoteDelimiterMessage = "The field delimiter can't be a double quote.*";
        private const string TerminatorDelimiterMessage = "The field delimiter can't be the first character of the terminator line break.*";
        private const string UndefinedQuoteHandlingMessage = "The quote handling '99' isn't a defined CsvFieldQuoteHandling value.*";

        // Options with which the lines written couldn't be read back, and the message each is rejected with: no terminator, so the records run
        // together (RFC 4180§2¶1); a terminator that reads as an escaped quote after a quoted field (RFC 4180§2¶7); a delimiter that can't be told
        // apart from a quote (RFC 4180§2¶5) or from the end of the record; or a quote handling value that never quotes (RFC 4180§2¶6).
        public static TheoryData<char, string, CsvFieldQuoteHandling, string> OptionsThatCantBeReadBack =>
            new TheoryData<char, string, CsvFieldQuoteHandling, string>
            {
                { Rfc4180FieldDelimiter, "", CsvFieldQuoteHandling.Required, EmptyTerminatorMessage },
                { Rfc4180FieldDelimiter, null, CsvFieldQuoteHandling.Required, EmptyTerminatorMessage },
                { Rfc4180FieldDelimiter, "\"\n", CsvFieldQuoteHandling.Required, QuoteTerminatorMessage },
                { '"', Rfc4180Terminator, CsvFieldQuoteHandling.Required, QuoteDelimiterMessage },
                { '\n', "\n", CsvFieldQuoteHandling.Required, TerminatorDelimiterMessage },
                { '|', "||", CsvFieldQuoteHandling.Required, TerminatorDelimiterMessage },
                { Rfc4180FieldDelimiter, Rfc4180Terminator, UndefinedQuoteHandling, UndefinedQuoteHandlingMessage }
            };

        [Theory]
        [MemberData(nameof(OptionsThatCantBeReadBack))]
        public void Constructor_OptionsThatCantBeReadBack_ThrowsArgumentException(char fieldDelimiter, string terminator, CsvFieldQuoteHandling quoteHandling,
            string expectedMessage)
        {
            // arrange
            var options = new CsvFileOptions { FieldDelimiterCharacter = fieldDelimiter, TerminatorLineBreak = terminator, QuoteHandling = quoteHandling };

            // act
            Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(TextWriter.Null, options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(expectedMessage).WithParameterName(OptionsParameterName);
        }

        [Theory]
        [MemberData(nameof(OptionsConstructors))]
        public void Constructor_AnyOverloadWithOptionsThatCantBeReadBack_ThrowsArgumentException(Func<CsvFileOptions, IDisposable> constructor)
        {
            // arrange
            var options = new CsvFileOptions { FieldDelimiterCharacter = '"' };

            // act
            Action testAction = () => constructor(options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(QuoteDelimiterMessage).WithParameterName(OptionsParameterName);
        }

        [Fact]
        public void WriteLine_DelimiterInTerminatorAfterItsFirstCharacter_WritesFieldsSeparatedByDelimiter()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { FieldDelimiterCharacter = '\n' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("One\nTwo\r\n");
            }
        }

        [Fact]
        public void WriteLine_DelimiterInTerminatorAfterItsFirstCharacter_ReadsBackAsLineWritten()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { FieldDelimiterCharacter = '\n' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);
                ICsvFileLine readLine;
                using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(buffer.ToString()), options))
                {
                    readLine = csvReader.ReadLine();
                }

                // assert
                readLine.Count.Should().Be(2);
                readLine[0].Should().Be(FieldName1);
                readLine[1].Should().Be(FieldName2);
            }
        }

#if NETFRAMEWORK
        // The options have setters on targets older than .NET 5, so they can be changed after the writer is created.

        [Theory]
        [MemberData(nameof(OptionsThatCantBeReadBack))]
        public void WriteLine_OptionsChangedSoOutputCantBeReadBack_ThrowsInvalidOperationException(char fieldDelimiter, string terminator,
            CsvFieldQuoteHandling quoteHandling, string expectedMessage)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, TextValue);

            var options = new CsvFileOptions();
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(TextWriter.Null, options))
            {
                csvWriter.WriteLine(testLine);
                options.FieldDelimiterCharacter = fieldDelimiter;
                options.TerminatorLineBreak = terminator;
                options.QuoteHandling = quoteHandling;

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(expectedMessage);
            }
        }

        [Theory]
        [MemberData(nameof(OptionsThatCantBeReadBack))]
        public void WriteLine_OptionsChangedSoOutputCantBeReadBack_WritesNothing(char fieldDelimiter, string terminator,
            CsvFieldQuoteHandling quoteHandling, string expectedMessage)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, TextValue);

            var options = new CsvFileOptions();
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(testLine);
                options.FieldDelimiterCharacter = fieldDelimiter;
                options.TerminatorLineBreak = terminator;
                options.QuoteHandling = quoteHandling;

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(expectedMessage);
                buffer.ToString().Should().Be("Two\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAfterOptionsChangedSoOutputCantBeReadBack_ThrowsInvalidOperationException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(FieldName1, FieldName1);

            var options = new CsvFileOptions();
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                options.FieldDelimiterCharacter = '"';

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(QuoteDelimiterMessage);
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_ValueChangesOptionsWhileLineIsConverted_ThrowsInvalidOperationException()
        {
            // arrange
            var options = new CsvFileOptions();
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, new DelimiterChangingValue(options));

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(QuoteDelimiterMessage);
                buffer.ToString().Should().BeEmpty();
            }
        }

        /// <summary>
        /// A field value that sets the field delimiter to a double quote when it is converted to text, as a converter could.
        /// </summary>
        private sealed class DelimiterChangingValue
        {
            private readonly CsvFileOptions _options;

            public DelimiterChangingValue(CsvFileOptions options)
            {
                _options = options;
            }

            public override string ToString()
            {
                _options.FieldDelimiterCharacter = '"';
                return TextValue;
            }
        }
#endif
    }
}
