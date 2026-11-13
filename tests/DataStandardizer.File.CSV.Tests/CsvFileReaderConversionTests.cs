using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests of how a reader converts a mapped field value to its property type: the built-in parse used when no converter is configured, the
    /// precedence of configured converters over it, and how a value that fails to convert is reported.
    /// </summary>
    public class CsvFileReaderConversionTests
    {
        private const string MalformedInteger = "abc";
        private const string ValuePropertyName = "Value";
        private const string PropertyDataItem = "Property";
        private const string FieldValueDataItem = "FieldValue";
        private const string InvalidMalformedIntegerMessage = "Encountered invalid value 'abc' in CSV field .";

        /// <summary>
        /// The value <see cref="FixedInt32Converter"/> converts every field value to.
        /// </summary>
        private const int TypeConvertedValue = 7;

        /// <summary>
        /// A field value with spaces around it, which a string property read without a converter keeps as it is.
        /// </summary>
        private const string PaddedText = "\"  bob  \"";

        /// <summary>
        /// The text of <see cref="PaddedText"/> as it is read, without the quotes.
        /// </summary>
        private const string PaddedFieldText = "  bob  ";

        /// <summary>
        /// The value the converters of <see cref="DelegateConvertedTextLineMapper"/> and <see cref="FixedStringConverter"/> convert every field value to.
        /// </summary>
        private const string ConvertedText = "CONVERTED";

        /// <summary>
        /// The constant value of <see cref="ConstantTextLine.Value"/>.
        /// </summary>
        private const string ConstantText = "constant";

        /// <summary>
        /// The value the converter delegate of <see cref="DelegateConvertedLineMapper"/> converts every field value to.
        /// </summary>
        private const int DelegateConvertedValue = 9;

        /// <summary>
        /// One and a half, written with a comma as the decimal separator, in a quoted field so the comma is not a field delimiter.
        /// </summary>
        private const string CommaDecimalValue = "\"1,5\"";

        /// <summary>
        /// A line with one field, which is empty.  An empty line has no fields, so the field is quoted.
        /// </summary>
        private const string EmptyField = "\"\"";

        /// <summary>
        /// One and a half, written with a full stop, the decimal separator of the invariant culture.
        /// </summary>
        private const string InvariantDecimalValue = "1.5";

        private const decimal OneAndAHalf = 1.5m;

        /// <summary>
        /// A file whose header puts the <c>Amount</c> field second, after a field that no mapping names.
        /// </summary>
        private const string AmountSecondFile = "Other,Amount\r\nx,abc\r\n";

        /// <summary>
        /// The position of the <c>Amount</c> field in the lines of <see cref="AmountSecondFile"/>.
        /// </summary>
        private const int AmountFieldPosition = 1;

        /// <summary>
        /// The name of the second field in the header of <see cref="AmountSecondFile"/>.
        /// </summary>
        private const string AmountFieldName = "Amount";

        /// <summary>
        /// The text of the <c>Amount</c> field in the record line of <see cref="AmountSecondFile"/>.
        /// </summary>
        private const string AmountFieldText = "abc";

        /// <summary>
        /// A file whose header names the field <c>Id</c>, with one record whose <c>Id</c> is an integer.
        /// </summary>
        private const string IdFile = "Id\r\n5\r\n";

        /// <summary>
        /// The value a bad-value handler sets on the line in place of a value that failed to convert.
        /// </summary>
        private const int HandlerFallbackValue = 3;

        /// <summary>
        /// An integer field, which the built-in parse reads as <see cref="Int32Value"/>.
        /// </summary>
        private const string Int32Text = "5";

        /// <summary>
        /// The value of <see cref="Int32Text"/>.
        /// </summary>
        private const int Int32Value = 5;

        public static TheoryData<string, Func<string, object>, string, object> ParsedValueCases => new TheoryData<string, Func<string, object>, string, object>
        {
            { "int", ReadValue<int>, "-42", -42 },
            { "long", ReadValue<long>, "9000000000", 9000000000L },
            { "short", ReadValue<short>, "-300", (short)-300 },
            { "sbyte", ReadValue<sbyte>, "-8", (sbyte)-8 },
            { "uint", ReadValue<uint>, "4000000000", 4000000000U },
            { "ulong", ReadValue<ulong>, "18000000000000000000", 18000000000000000000UL },
            { "ushort", ReadValue<ushort>, "60000", (ushort)60000 },
            { "byte", ReadValue<byte>, "200", (byte)200 },
            { "decimal", ReadValue<decimal>, "1234.5", 1234.5m },
            { "decimal with thousands separator", ReadValue<decimal>, "\"1,234.5\"", 1234.5m },
            { "double", ReadValue<double>, "0.25", 0.25d },
            { "float", ReadValue<float>, "0.5", 0.5f },
            { "double, positive infinity symbol", ReadValue<double>, "Infinity", double.PositiveInfinity },
            { "double, negative infinity symbol", ReadValue<double>, "-Infinity", double.NegativeInfinity },
            { "double, NaN symbol", ReadValue<double>, "NaN", double.NaN },
            { "float, positive infinity symbol", ReadValue<float>, "Infinity", float.PositiveInfinity },
            { "bool", ReadValue<bool>, "True", true },
            { "char", ReadValue<char>, "x", 'x' },
            { "DateTime", ReadValue<DateTime>, "2026-11-06T13:45:00", new DateTime(2026, 11, 6, 13, 45, 0) },
            { "DateTimeOffset", ReadValue<DateTimeOffset>, "2026-11-06T13:45:00+13:00", new DateTimeOffset(2026, 11, 6, 13, 45, 0, new TimeSpan(13, 0, 0)) },
            { "TimeSpan", ReadValue<TimeSpan>, "01:02:03", new TimeSpan(1, 2, 3) },
            { "Guid", ReadValue<Guid>, "00112233-4455-6677-8899-aabbccddeeff", new Guid(0x00112233, 0x4455, 0x6677, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff) },
            { "enum", ReadValue<Colour>, "Green", Colour.Green },
            { "nullable int", ReadValue<int?>, "-42", -42 },
            { "nullable long", ReadValue<long?>, "9000000000", 9000000000L },
            { "nullable decimal", ReadValue<decimal?>, "1234.5", 1234.5m },
            { "nullable double", ReadValue<double?>, "0.25", 0.25d },
            { "nullable bool", ReadValue<bool?>, "False", false },
            { "nullable DateTime", ReadValue<DateTime?>, "2026-11-06T13:45:00", new DateTime(2026, 11, 6, 13, 45, 0) },
            { "nullable DateTimeOffset", ReadValue<DateTimeOffset?>, "2026-11-06T13:45:00+13:00", new DateTimeOffset(2026, 11, 6, 13, 45, 0, new TimeSpan(13, 0, 0)) },
            { "nullable Guid", ReadValue<Guid?>, "00112233-4455-6677-8899-aabbccddeeff", new Guid(0x00112233, 0x4455, 0x6677, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff) },
            { "nullable enum", ReadValue<Colour?>, "Green", Colour.Green },
            { "type with a TypeConverter attribute", ReadValue<Code>, "abc", new Code("abc") },
#if NET7_0_OR_GREATER
            { "ISpanParsable type", ReadValue<SpanParsableCode>, "abc", new SpanParsableCode("abc") },
            { "IParsable type", ReadValue<ParsableCode>, "abc", new ParsableCode("abc") },
#endif
        };

        public static TheoryData<string, Func<string, object>, object> EmptyValueCases => new TheoryData<string, Func<string, object>, object>
        {
            { "int", ReadValue<int>, 0 },
            { "decimal", ReadValue<decimal>, 0m },
            { "DateTime", ReadValue<DateTime>, default(DateTime) },
            { "Guid", ReadValue<Guid>, Guid.Empty },
            { "enum", ReadValue<Colour>, default(Colour) },
            { "nullable int", ReadValue<int?>, null },
            { "nullable decimal", ReadValue<decimal?>, null },
            { "nullable DateTime", ReadValue<DateTime?>, null },
            { "nullable enum", ReadValue<Colour?>, null },
            { "type with a TypeConverter attribute", ReadValue<Code>, null },
        };

        public static TheoryData<string, Func<string, object>, string> MalformedValueCases => new TheoryData<string, Func<string, object>, string>
        {
            { "int, not a number", ReadValue<int>, MalformedInteger },
            { "int, hexadecimal", ReadValue<int>, "0x10" },
            { "int, fraction", ReadValue<int>, "1.5" },
            { "int, out of range", ReadValue<int>, "2147483648" },
            { "int, white space only", ReadValue<int>, " " },
            { "nullable int, not a number", ReadValue<int?>, MalformedInteger },
            { "decimal, not a number", ReadValue<decimal>, "1.2.3" },
            { "double, too large", ReadValue<double>, "1e999" },
            { "double, too large and negative", ReadValue<double>, "-1e999" },
            { "double, infinity symbol in other case", ReadValue<double>, "infinity" },
            { "double, NaN symbol in other case", ReadValue<double>, "nan" },
            { "float, too large", ReadValue<float>, "1e39" },
            { "bool, not a Boolean", ReadValue<bool>, "yes" },
            { "char, two characters", ReadValue<char>, "xy" },
            { "DateTime, not a date", ReadValue<DateTime>, "2026-13-45" },
            { "Guid, too short", ReadValue<Guid>, "00112233" },
            { "enum, other case", ReadValue<Colour>, "green" },
            { "enum, unknown name", ReadValue<Colour>, "Purple" },
        };

        [Theory]
        [MemberData(nameof(ParsedValueCases))]
        public void ReadLine_MappedPropertyWithoutConverter_ParsesFieldValue(string propertyType, Func<string, object> readValue, string fieldValue, object expected)
        {
            // arrange
            _ = propertyType;

            // act
            var testResult = readValue(fieldValue);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadLine_DateTimeFieldEndingInZWithoutConverter_ReadsUtcTimeAsWritten()
        {
            // arrange

            // act
            var testResult = (DateTime)ReadValue<DateTime>("2024-01-02T03:04:05.6780000Z");

            // assert
            testResult.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5, 678));
            testResult.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [MemberData(nameof(EmptyValueCases))]
        public void ReadLine_EmptyFieldWithoutConverter_ReadsDefaultValue(string propertyType, Func<string, object> readValue, object expected)
        {
            // arrange
            _ = propertyType;

            // act
            var testResult = readValue(EmptyField);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MalformedValueCases))]
        public void ReadLine_MalformedValueWithoutBadValueHandler_ThrowsCsvFileException(string propertyType, Func<string, object> readValue, string fieldValue)
        {
            // arrange
            _ = propertyType;

            // act
            Action testAction = () => readValue(fieldValue);

            // assert
            testAction.Should().Throw<CsvFileException>();
        }

        [Fact]
        public void ReadLine_MalformedValueWithoutBadValueHandler_ExceptionNamesPropertyAndValue()
        {
            // act
            Action testAction = () => ReadValue<int>(MalformedInteger);

            // assert
            var exception = testAction.Should().Throw<CsvFileException>().WithMessage(InvalidMalformedIntegerMessage).Which;
            exception.Data[PropertyDataItem].Should().Be(ValuePropertyName);
            exception.Data[FieldValueDataItem].Should().Be(MalformedInteger);
        }

        [Fact]
        public void ReadLine_MalformedValueWithBadValueHandler_CallsHandlerWithRawFieldValue()
        {
            // arrange
            var rawFieldValues = new List<string>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<ValueLine<int>>(context => rawFieldValues.Add(context.RawFieldValue))
            };

            // act
            _ = ReadValue<int>(MalformedInteger, options);

            // assert
            rawFieldValues.Should().Equal(MalformedInteger);
        }

        [Fact]
        public void ReadLine_MalformedValueWithBadValueHandler_ReadsDefaultValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<ValueLine<int>>(_ => { })
            };

            // act
            var testResult = ReadValue<int>(MalformedInteger, options);

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void ReadLine_MalformedValueWithBadValueHandler_LineKeepsFieldText()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<ValueLine<int>>(_ => { })
            };
            using (var csvReader = new CsvFileReader<ValueLine<int>>(new StringReader(MalformedInteger), options))
            {
                // act
                var testResult = ((ICsvFileLine)csvReader.ReadLine())[ValuePropertyName];

                // assert
                testResult.Should().Be(MalformedInteger);
            }
        }

        [Fact]
        public void ReadLine_BadValueHandlerSetsValueOfPropertyType_ReadsHandlerValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<ValueLine<int>>(context => context.Model.Value = HandlerFallbackValue)
            };

            // act
            var testResult = ReadValue<int>(MalformedInteger, options);

            // assert
            testResult.Should().Be(HandlerFallbackValue);
        }

        [Fact]
        public void ReadLine_ValidatorRejectsValueWithBadValueHandler_ReadsDefaultValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<RejectedAmountLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<RejectedAmountLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<RejectedAmountLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = ((RejectedAmountLine)csvReader.ReadLine()).Amount;

                // assert
                testResult.Should().Be(0);
            }
        }

        [Fact]
        public void GetPropertyValue_UnmappedNonStringPropertyOfReadLine_ThrowsInvalidCastException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<UnmappedIdLine>(new StringReader(IdFile), options))
            {
                _ = csvReader.ReadLine();
                var testLine = (UnmappedIdLine)csvReader.ReadLine();

                // act
                Action testAction = () => _ = testLine.Id;

                // assert
                testAction.Should().Throw<InvalidCastException>();
            }
        }

        [Fact]
        public void ReadLine_CultureSet_ParsesWithCulture()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, Culture = CreateCommaDecimalCulture() };

            // act
            var testResult = ReadValue<decimal>(CommaDecimalValue, options);

            // assert
            testResult.Should().Be(OneAndAHalf);
        }

        [Fact]
        public void ReadLine_CultureNotSet_ParsesWithInvariantCulture()
        {
            // arrange
            using (new CurrentCultureScope(CreateCommaDecimalCulture()))
            {

                // act
                var testResult = ReadValue<decimal>(InvariantDecimalValue);

                // assert
                testResult.Should().Be(OneAndAHalf);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterOfTypeReturnsNull_ReadsNull()
        {
            // act
            var testResult = ReadValue<NullConvertedCode>("abc");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ReadLine_FromStringConverterConfiguredForTypeWhoseConverterCantBeCreated_ReadsConvertedValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<UncreatableConverterLine>(new StringReader("abc"), options))
            {
                csvReader.RegisterMapper<UncreatableConverterLineMapper>();

                // act
                var testResult = (UncreatableConverterLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().BeSameAs(UncreatableConverterLineMapper.ConvertedValue);
            }
        }

        [Fact]
        public void ReadLine_MalformedValueAfterUnmappedField_CallsHandlerWithFieldPosition()
        {
            // arrange
            var fieldIndexes = new List<int?>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<AmountLine>(context => fieldIndexes.Add(context.FieldIndex))
            };
            using (var csvReader = new CsvFileReader<AmountLine>(new StringReader(AmountSecondFile), options))
            {
                _ = csvReader.ReadLine();

                // act
                _ = csvReader.ReadLine();

                // assert
                fieldIndexes.Should().Equal(AmountFieldPosition);
            }
        }

        [Fact]
        public void ReadLine_ValidatorRejectsValueAfterUnmappedField_CallsHandlerWithFieldPosition()
        {
            // arrange
            var fieldIndexes = new List<int?>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<ValidatedAmountLine>(context => fieldIndexes.Add(context.FieldIndex))
            };
            using (var csvReader = new CsvFileReader<ValidatedAmountLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<ValidatedAmountLineMapper>();
                _ = csvReader.ReadLine();

                // act
                _ = csvReader.ReadLine();

                // assert
                fieldIndexes.Should().Equal(AmountFieldPosition);
            }
        }

        [Fact]
        public void ReadLine_FromStringConverterOnNamedFieldAfterUnmappedField_GivesConverterRawFieldValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ContextCapturingLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<NamedContextCapturingLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = (ContextCapturingLine)csvReader.ReadLine();

                // assert
                testResult.Context.RawFieldValue.Should().Be(AmountFieldText);
            }
        }

        [Fact]
        public void ReadLine_FromStringConverterOnNamedFieldAfterUnmappedField_GivesConverterFieldPosition()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ContextCapturingLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<NamedContextCapturingLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = (ContextCapturingLine)csvReader.ReadLine();

                // assert
                testResult.Context.FieldIndex.Should().Be(AmountFieldPosition);
            }
        }

        [Fact]
        public void ReadLine_FromStringConverterOnIndexedField_GivesConverterHeaderFieldName()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ContextCapturingLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<IndexedContextCapturingLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = (ContextCapturingLine)csvReader.ReadLine();

                // assert
                testResult.Context.FieldName.Should().Be(AmountFieldName);
            }
        }

        [Fact]
        public void ReadLine_ValidatorRejectsValueAfterUnmappedField_CallsHandlerWithRawFieldValue()
        {
            // arrange
            var rawFieldValues = new List<string>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<ValidatedAmountLine>(context => rawFieldValues.Add(context.RawFieldValue))
            };
            using (var csvReader = new CsvFileReader<ValidatedAmountLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<ValidatedAmountLineMapper>();
                _ = csvReader.ReadLine();

                // act
                _ = csvReader.ReadLine();

                // assert
                rawFieldValues.Should().Equal(AmountFieldText);
            }
        }

        [Fact]
        public void ReadLine_ValidatorOnFieldAnEarlierMappingConverted_CallsHandlerWithFieldAsRead()
        {
            // arrange
            var badValues = new List<KeyValuePair<int?, string>>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<SharedAmountLine>(context => badValues.Add(new KeyValuePair<int?, string>(context.FieldIndex, context.RawFieldValue)))
            };
            using (var csvReader = new CsvFileReader<SharedAmountLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<SharedAmountLineMapper>();
                _ = csvReader.ReadLine();

                // act
                _ = csvReader.ReadLine();

                // assert
                badValues.Should().Equal(new[] { new KeyValuePair<int?, string>(AmountFieldPosition, AmountFieldText) });
            }
        }

        [Fact]
        public void ReadLine_ValidatorOnFieldAnEarlierConstantAdded_CallsHandlerWithNoField()
        {
            // arrange
            var badValues = new List<KeyValuePair<int?, string>>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<AddedFieldLine>(context => badValues.Add(new KeyValuePair<int?, string>(context.FieldIndex, context.RawFieldValue)))
            };
            using (var csvReader = new CsvFileReader<AddedFieldLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<AddedFieldLineMapper>();
                _ = csvReader.ReadLine();

                // act
                _ = csvReader.ReadLine();

                // assert
                badValues.Should().Equal(new[] { new KeyValuePair<int?, string>(null, null) });
            }
        }

        [Fact]
        public void ReadLine_TypeWithoutParser_ReadsDefaultValue()
        {
            // act
            var testResult = ReadValue<Unparsable>("abc");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ReadLine_FromStringConverterConfigured_TakesPrecedenceOverBuiltInParse()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<DelegateConvertedLine>(new StringReader("1"), options))
            {
                csvReader.RegisterMapper<DelegateConvertedLineMapper>();

                // act
                var testResult = (DelegateConvertedLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(DelegateConvertedValue);
            }
        }

        [Fact]
        public void ReadLine_StringPropertyWithoutConverter_ReadsFieldText()
        {
            // act
            var testResult = ReadValue<string>(PaddedText);

            // assert
            testResult.Should().Be(PaddedFieldText);
        }

        [Fact]
        public void ReadLine_FromStringConverterOnStringProperty_ReadsConvertedValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<DelegateConvertedTextLine>(new StringReader(PaddedText), options))
            {
                csvReader.RegisterMapper<DelegateConvertedTextLineMapper>();

                // act
                var testResult = (DelegateConvertedTextLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(ConvertedText);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterOnStringProperty_ReadsConvertedValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<TypeConvertedTextLine>(new StringReader(PaddedText), options))
            {
                // act
                var testResult = (TypeConvertedTextLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(ConvertedText);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterNotFromStringOnStringProperty_ReadsFieldText()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<ToStringOnlyConvertedTextLine>(new StringReader(PaddedText), options))
            {
                // act
                var testResult = (ToStringOnlyConvertedTextLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(PaddedFieldText);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterNotFromStringOnInt32Property_ParsesFieldValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<ToStringOnlyConvertedInt32Line>(new StringReader(Int32Text), options))
            {
                // act
                var testResult = (ToStringOnlyConvertedInt32Line)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(Int32Value);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterNotFromStringOnInt32PropertyMalformedValue_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<ToStringOnlyConvertedInt32Line>(new StringReader(MalformedInteger), options))
            {
                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(InvalidMalformedIntegerMessage);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterNotFromStringOnInt32PropertyEmptyField_ReadsDefaultValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<ToStringOnlyConvertedInt32Line>(new StringReader(EmptyField), options))
            {
                // act
                var testResult = (ToStringOnlyConvertedInt32Line)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(0);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterAttributeNamingNonConverterType_ParsesFieldValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<NonConverterInt32Line>(new StringReader(Int32Text), options))
            {
                // act
                var testResult = (NonConverterInt32Line)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(Int32Value);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterNotFromStringOnFieldAnEarlierMappingConverted_ReadsLineValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedTextLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<SharedToStringOnlyTextLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = (SharedTextLine)csvReader.ReadLine();

                // assert
                testResult.Second.Should().Be(ConvertedText);
            }
        }

        [Fact]
        public void ReadLine_FromStringConverterOnFieldAnEarlierMappingConverted_GivesConverterFieldAsRead()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedTextLine>(new StringReader(AmountSecondFile), options))
            {
                csvReader.RegisterMapper<SharedTextLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = (SharedTextLine)csvReader.ReadLine();

                // assert
                testResult.Second.Should().Be(AmountFieldText);
            }
        }

        [Fact]
        public void ReadLine_ConstantValueOfPropertyTypeWithTypeConverter_ReadsConstantValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<ConstantTextLine>(new StringReader(PaddedText), options))
            {
                // act
                var testResult = (ConstantTextLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(ConstantText);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterConfigured_TakesPrecedenceOverBuiltInParse()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<TypeConvertedLine>(new StringReader("1"), options))
            {

                // act
                var testResult = (TypeConvertedLine)csvReader.ReadLine();

                // assert
                testResult.Value.Should().Be(TypeConvertedValue);
            }
        }

        [Fact]
        public void ReadLine_TypeConverterThrowsWithoutBadValueHandler_ThrowsCsvFileExceptionWithConverterException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<Int32ConvertedLine>(new StringReader(MalformedInteger), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(InvalidMalformedIntegerMessage).WithInnerException<Exception>();
            }
        }

        [Fact]
        public void ReadLine_TypeConverterThrowsWithBadValueHandler_CallsHandlerWithRawFieldValue()
        {
            // arrange
            var rawFieldValues = new List<string>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<Int32ConvertedLine>(context => rawFieldValues.Add(context.RawFieldValue))
            };
            using (var csvReader = new CsvFileReader<Int32ConvertedLine>(new StringReader(MalformedInteger), options))
            {

                // act
                _ = csvReader.ReadLine();

                // assert
                rawFieldValues.Should().Equal(MalformedInteger);
            }
        }

        /// <summary>
        /// Reads a one-field line, with no header line, into a <see cref="ValueLine{T}"/> and returns its property value.
        /// </summary>
        private static object ReadValue<T>(string line)
        {
            return ReadValue<T>(line, new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None });
        }

        private static object ReadValue<T>(string line, CsvFileOptions options)
        {
            using (var csvReader = new CsvFileReader<ValueLine<T>>(new StringReader(line), options))
            {
                return ((ValueLine<T>)csvReader.ReadLine()).Value;
            }
        }

        /// <summary>
        /// Creates a culture whose decimal separator is a comma and whose group separator is a full stop, without depending on the culture data of
        /// the operating system.
        /// </summary>
        private static CultureInfo CreateCommaDecimalCulture()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            return culture;
        }

        public enum Colour
        {
            Red = 1,
            Green = 2
        }

        private sealed class ValueLine<T> : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            public T Value
            {
                get => GetPropertyValue<T>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class TypeConvertedLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(FixedInt32Converter))]
            public int Value
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class Int32ConvertedLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(Int32Converter))]
            public int Value
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class AmountLine : CsvFileRecordLine
        {
            [CsvFieldMapping("Amount")]
            public int Amount
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class ValidatedAmountLine : CsvFileRecordLine
        {
            public string Amount
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line with an <see cref="int"/> property that no mapping names, so the reader stores its field's text.
        /// </summary>
        private sealed class UnmappedIdLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class RejectedAmountLine : CsvFileRecordLine
        {
            public int Amount
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class RejectedAmountLineMapper : CsvFileMapperBase<RejectedAmountLine>
        {
            public RejectedAmountLineMapper()
            {
                this.Map()
                    .Property(x => x.Amount)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(Int32Converter))
                    .ValidateUsing(_ => false);
            }
        }

        private sealed class ValidatedAmountLineMapper : CsvFileMapperBase<ValidatedAmountLine>
        {
            public ValidatedAmountLineMapper()
            {
                this.Map()
                    .Property(x => x.Amount)
                    .HasFieldName("Amount")
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
            }
        }

        /// <summary>
        /// A line whose one property holds the context its converter was given.
        /// </summary>
        private sealed class ContextCapturingLine : CsvFileRecordLine
        {
            public CsvFieldContext<ContextCapturingLine> Context
            {
                get => GetPropertyValue<CsvFieldContext<ContextCapturingLine>>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class NamedContextCapturingLineMapper : CsvFileMapperBase<ContextCapturingLine>
        {
            public NamedContextCapturingLineMapper()
            {
                this.Map()
                    .Property(x => x.Context)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertFromString<ContextCapturingLine, CsvFieldContext<ContextCapturingLine>>(context => context));
            }
        }

        private sealed class IndexedContextCapturingLineMapper : CsvFileMapperBase<ContextCapturingLine>
        {
            public IndexedContextCapturingLineMapper()
            {
                this.Map()
                    .Property(x => x.Context)
                    .HasFieldIndex(AmountFieldPosition)
                    .ConvertUsing(new CsvFieldConvertFromString<ContextCapturingLine, CsvFieldContext<ContextCapturingLine>>(context => context));
            }
        }

        /// <summary>
        /// A line with two properties mapped to the <c>Amount</c> field.
        /// </summary>
        private sealed class SharedAmountLine : CsvFileRecordLine
        {
            public int Converted
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Validated
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Converts the <c>Amount</c> field to a number first, so the line holds the number under that name, and then rejects the field.
        /// </summary>
        private sealed class SharedAmountLineMapper : CsvFileMapperBase<SharedAmountLine>
        {
            public SharedAmountLineMapper()
            {
                this.Map()
                    .Property(x => x.Converted)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertFromString<SharedAmountLine, int>(_ => DelegateConvertedValue));
                this.Map()
                    .Property(x => x.Validated)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
            }
        }

        /// <summary>
        /// A line with a constant and a validated property mapped to a field the file doesn't have.
        /// </summary>
        private sealed class AddedFieldLine : CsvFileRecordLine
        {
            public string Constant
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Validated
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Adds the <c>Extra</c> field to the line with a constant value first, and then rejects that field.
        /// </summary>
        private sealed class AddedFieldLineMapper : CsvFileMapperBase<AddedFieldLine>
        {
            public const string AddedFieldName = "Extra";

            public AddedFieldLineMapper()
            {
                this.Map()
                    .Property(x => x.Constant)
                    .HasFieldName(AddedFieldName)
                    .HasConstantValue("x");
                this.Map()
                    .Property(x => x.Validated)
                    .HasFieldName(AddedFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
            }
        }

        private sealed class UncreatableConverterLine : CsvFileRecordLine
        {
            public UncreatableConverterCode Value
            {
                get => GetPropertyValue<UncreatableConverterCode>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class UncreatableConverterLineMapper : CsvFileMapperBase<UncreatableConverterLine>
        {
            public static readonly UncreatableConverterCode ConvertedValue = new UncreatableConverterCode();

            public UncreatableConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Value)
                    .HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertFromString<UncreatableConverterLine, UncreatableConverterCode>(_ => ConvertedValue));
            }
        }

        private sealed class DelegateConvertedLine : CsvFileRecordLine
        {
            public int Value
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class DelegateConvertedLineMapper : CsvFileMapperBase<DelegateConvertedLine>
        {
            public DelegateConvertedLineMapper()
            {
                this.Map()
                    .Property(x => x.Value)
                    .HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertFromString<DelegateConvertedLine, int>(_ => DelegateConvertedValue));
            }
        }

        private sealed class DelegateConvertedTextLine : CsvFileRecordLine
        {
            public string Value
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class DelegateConvertedTextLineMapper : CsvFileMapperBase<DelegateConvertedTextLine>
        {
            public DelegateConvertedTextLineMapper()
            {
                this.Map()
                    .Property(x => x.Value)
                    .HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertFromString<DelegateConvertedTextLine, string>(_ => ConvertedText));
            }
        }

        private sealed class TypeConvertedTextLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(FixedStringConverter))]
            public string Value
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class ConstantTextLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = ConstantText)]
            [TypeConverter(typeof(FixedStringConverter))]
            public string Value
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class ToStringOnlyConvertedTextLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(ToStringOnlyConverter))]
            public string Value
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class ToStringOnlyConvertedInt32Line : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(ToStringOnlyConverter))]
            public int Value
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line with two string properties mapped to the <c>Amount</c> field.
        /// </summary>
        private sealed class SharedTextLine : CsvFileRecordLine
        {
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Converts the <c>Amount</c> field to <see cref="ConvertedText"/> first, so the line holds that under the field's name, and then reads the
        /// field again with a converter that gives the text it is given.
        /// </summary>
        private sealed class SharedTextLineMapper : CsvFileMapperBase<SharedTextLine>
        {
            public SharedTextLineMapper()
            {
                this.Map()
                    .Property(x => x.First)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertFromString<SharedTextLine, string>(_ => ConvertedText));
                this.Map()
                    .Property(x => x.Second)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertFromString<SharedTextLine, string>(context => context.RawFieldValue));
            }
        }

        /// <summary>
        /// Converts the <c>Amount</c> field to <see cref="ConvertedText"/> first, so the line holds that under the field's name, and then reads the
        /// field again with a type converter that doesn't convert from a string.
        /// </summary>
        private sealed class SharedToStringOnlyTextLineMapper : CsvFileMapperBase<SharedTextLine>
        {
            public SharedToStringOnlyTextLineMapper()
            {
                this.Map()
                    .Property(x => x.First)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertFromString<SharedTextLine, string>(_ => ConvertedText));
                this.Map()
                    .Property(x => x.Second)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(ToStringOnlyConverter));
            }
        }

        /// <summary>
        /// A line whose <see cref="TypeConverterAttribute"/> names a type that isn't a <see cref="TypeConverter"/>.
        /// </summary>
        private sealed class NonConverterInt32Line : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(Unparsable))]
            public int Value
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Converts every value to <see cref="ConvertedText"/>, and doesn't convert from a string.
        /// </summary>
        private sealed class ToStringOnlyConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => ConvertedText;
        }

        /// <summary>
        /// Converts every string to <see cref="ConvertedText"/>.
        /// </summary>
        private sealed class FixedStringConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => ConvertedText;
        }

        /// <summary>
        /// Converts every string to <see cref="TypeConvertedValue"/>.
        /// </summary>
        private sealed class FixedInt32Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => TypeConvertedValue;
        }

        /// <summary>
        /// A type that is not parsable and has no type converter.
        /// </summary>
        public sealed class Unparsable
        {
        }

        /// <summary>
        /// A type that is only parsable through the <see cref="TypeConverter"/> its attribute names.
        /// </summary>
        [TypeConverter(typeof(CodeConverter))]
        public sealed class Code : IEquatable<Code>
        {
            public Code(string text)
            {
                Text = text;
            }

            public string Text { get; }

            public bool Equals(Code other) => other != null && other.Text == Text;

            public override bool Equals(object obj) => Equals(obj as Code);

            public override int GetHashCode() => Text.GetHashCode();
        }

        /// <summary>
        /// A type whose <see cref="TypeConverter"/> converts every string to <c>null</c>.
        /// </summary>
        [TypeConverter(typeof(NullConverter))]
        public sealed class NullConvertedCode
        {
        }

        public sealed class NullConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => null;
        }

        /// <summary>
        /// A type whose <see cref="TypeConverter"/> throws when it is created.
        /// </summary>
        [TypeConverter(typeof(UncreatableConverter))]
        public sealed class UncreatableConverterCode
        {
        }

        public sealed class UncreatableConverter : TypeConverter
        {
            public UncreatableConverter()
            {
                throw new NotSupportedException();
            }
        }

        public sealed class CodeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new Code((string)value);
        }

#if NET7_0_OR_GREATER
        public readonly record struct SpanParsableCode(string Text) : ISpanParsable<SpanParsableCode>
        {
            public const string StringOverloadText = "string overload";

            public static SpanParsableCode Parse(string s, IFormatProvider provider) => new SpanParsableCode(StringOverloadText);

            /// <summary>
            /// Gives a value that a test expecting the <see cref="ReadOnlySpan{T}"/> overload's result fails on, so that it shows the reader uses that overload.
            /// </summary>
            public static bool TryParse(string s, IFormatProvider provider, out SpanParsableCode result)
            {
                result = new SpanParsableCode(StringOverloadText);
                return true;
            }

            public static SpanParsableCode Parse(ReadOnlySpan<char> s, IFormatProvider provider) => new SpanParsableCode(s.ToString());

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out SpanParsableCode result)
            {
                result = new SpanParsableCode(s.ToString());
                return true;
            }
        }

        public sealed record ParsableCode(string Text) : IParsable<ParsableCode>
        {
            public static ParsableCode Parse(string s, IFormatProvider provider) => new ParsableCode(s);

            public static bool TryParse(string s, IFormatProvider provider, out ParsableCode result)
            {
                result = new ParsableCode(s);
                return true;
            }
        }
#endif
    }
}
