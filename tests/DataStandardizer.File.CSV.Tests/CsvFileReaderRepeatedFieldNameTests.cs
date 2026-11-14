using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests of reading a header line that repeats a field name, which RFC 4180 §2 ¶3 allows, and of the names that mappings give fields with
    /// <c>UseFieldName</c>.
    /// </summary>
    public class CsvFileReaderRepeatedFieldNameTests
    {
        private const string NameFieldName = "name";
        private const string IdFieldName = "id";
        private const string LegalNameFieldName = "legal_name";
        private const string PreferredNameFieldName = "preferred_name";
        private const string RenamedIdFieldName = "person_id";
        private const string CodeFieldName = "code";
        private const string FirstName = "Ann";
        private const string SecondName = "Bob";
        private const string Id = "7";

        // A header line that repeats the name field, with one record.
        private const string RepeatedNameFile = "name,name\r\nAnn,Bob";

        // A header line that repeats the name field after an id field, with one record.
        private const string IdAndRepeatedNameFile = "id,name,name\r\n7,Ann,Bob";

        // A header line without a repeated name, with one record.
        private const string IdFile = "id\r\n7";

        // A file without a header line, of one record.
        private const string NoHeaderFile = "Ann,Bob";

        private const string DuplicateFieldNameMessage = "Found 1 duplicate field names.";
        private const string FieldNamesDataItem = "FieldNames";
        private const string NotRenamedMessage =
            "The header line has 2 fields named 'name', so the mapping of property 'Second' must give its field a name of its own with UseFieldName.";
        private const string SharedFieldNotRenamedMessage =
            "The mappings of properties 'Id' and 'Code' share the field at index 0, but give it different names, 'person_id' and 'id'.";

        // A header line with one count field, whose value isn't a number.
        private const string InvalidCountFile = "count\r\nx";
        private const string CountFieldName = "count";
        private const string RenamedCountFieldName = "item_count";
        private const string InvalidRenamedCountMessage = "Encountered invalid value 'x' in CSV field item_count.";

        private const string SharedFieldRenamedMessage =
            "The mappings of properties 'Id' and 'Code' share the field at index 0, but give it different names, 'person_id' and 'code'.";

        [Fact]
        public void ReadLine_TwoNameMappingsWithRepeatedHeaderName_PropertiesReturnFieldsInOrder()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RepeatedNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_TwoNameMappingsWithRepeatedHeaderName_ReturnsHeaderLineWithMappingNames()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as CsvFileHeaderLine;

                // assert
                testResult.FieldNames.Should().Equal(LegalNameFieldName, PreferredNameFieldName);
            }
        }

        [Fact]
        public void ReadLine_TwoNameMappingsWithRepeatedHeaderName_LineHoldsFieldsUnderMappingNames()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(LegalNameFieldName, PreferredNameFieldName);
            }
        }

        [Fact]
        public void ReadLine_TwoNameMappingsWithRepeatedHeaderName_FieldsReachableByPosition()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Values.Should().Equal(FirstName, SecondName);
            }
        }

        [Fact]
        public void ReadLine_DeclarativeMappingsWithRepeatedHeaderName_PropertiesReturnFieldsInDeclarationOrder()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<DeclarativeRepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as DeclarativeRepeatedNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_RepeatedHeaderNameThatNoMappingCovers_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<IdLine>(new StringReader(IdAndRepeatedNameFile), options))
            {
                csvReader.RegisterMapper<IdLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(DuplicateFieldNameMessage)
                    .Which.Data[FieldNamesDataItem].Should().BeEquivalentTo(new[] { NameFieldName });
            }
        }

        [Fact]
        public void ReadLine_RepeatedHeaderNameMappedWithoutUseFieldName_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<PartlyRenamedRepeatedNameLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(NotRenamedMessage);
            }
        }

        [Fact]
        public void ReadLine_UseFieldNameOnFieldWhoseNameIsNotRepeated_LineHoldsFieldUnderMappingName()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<IdLine>(new StringReader(IdFile), options))
            {
                csvReader.RegisterMapper<RenamedIdLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(RenamedIdFieldName);
            }
        }

        [Fact]
        public void ReadLine_MappingSharesFieldThatAnotherMappingRenames_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedIdLine>(new StringReader(IdFile), options))
            {
                csvReader.RegisterMapper<RenamedSharedIdLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(SharedFieldNotRenamedMessage);
            }
        }

        [Fact]
        public void ReadLine_MappingsShareFieldAndRenameItAlike_PropertiesReturnField()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedIdLine>(new StringReader(IdFile), options))
            {
                csvReader.RegisterMapper<AlikeRenamedSharedIdLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as SharedIdLine;

                // assert
                testResult.Id.Should().Be(Id);
                testResult.Code.Should().Be(Id);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingRenamesOneOfRepeatedNameAndNameMappingDoesNot_PropertiesReturnFields()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<IndexAndNameRepeatedNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RepeatedNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingTakesFirstOfRepeatedName_NameMappingTakesNextField()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<IndexFirstRepeatedNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RepeatedNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_NameMappingsGiveSameNameToRepeatedName_ShareOneField()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ThreeNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<SharedFirstThreeNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as ThreeNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(FirstName);
                testResult.Third.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_InvalidValueInRenamedField_ThrowsCsvFileExceptionNamingRenamedField()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CountLine>(new StringReader(InvalidCountFile), options))
            {
                csvReader.RegisterMapper<RenamedCountLineMapper>();
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(InvalidRenamedCountMessage);
            }
        }

        [Fact]
        public void ReadLine_MappingsShareFieldAndRenameItDifferently_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedIdLine>(new StringReader(IdFile), options))
            {
                csvReader.RegisterMapper<DifferentlyRenamedSharedIdLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(SharedFieldRenamedMessage);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingRenamesRepeatedName_ReturnsHeaderLineWithMappingName()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<IndexRenamedRepeatedNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as CsvFileHeaderLine;

                // assert
                testResult.FieldNames.Should().Equal(NameFieldName, PreferredNameFieldName);
            }
        }

        [Fact]
        public void ReadLine_MoreNameMappingsThanRepeatedFields_LastMappingSharesLastField()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ThreeNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<ThreeNameLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as ThreeNameLine;

                // assert
                testResult.Third.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineUsedWithRepeatedNameAndHeaderHandler_PassesMappingNamesToHandler()
        {
            // arrange
            var headerHandlerCalls = new List<IReadOnlyList<string>>();
            var headerHandler = new CsvFileHeader(headerLine =>
            {
                headerHandlerCalls.Add(headerLine.FieldNames);
                return headerLine.FieldNames;
            });
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();

                // act
                csvReader.ReadLine();

                // assert
                headerHandlerCalls.Should().ContainSingle().Which.Should().Equal(LegalNameFieldName, PreferredNameFieldName);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineIgnoredWithRepeatedNameAndHeaderHandler_PassesMappingNamesToHandler()
        {
            // arrange
            var headerHandlerCalls = new List<IReadOnlyList<string>>();
            var headerHandler = new CsvFileHeader(headerLine =>
            {
                headerHandlerCalls.Add(headerLine.FieldNames);
                return headerLine.FieldNames;
            });
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore, HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();

                // act
                csvReader.ReadLine();

                // assert
                headerHandlerCalls.Should().ContainSingle().Which.Should().Equal(LegalNameFieldName, PreferredNameFieldName);
            }
        }

        [Fact]
        public void ReadLine_NoHeaderLineAndNameMappingsRenamingSameName_PropertiesReturnFieldsInOrder()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(NoHeaderFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as RepeatedNameLine;

                // assert
                testResult.First.Should().Be(FirstName);
                testResult.Second.Should().Be(SecondName);
            }
        }

        [Fact]
        public void UseFieldName_Null_ThrowsArgumentNullException()
        {
            // arrange

            // act
            Action testAction = () => new NullUseFieldNameLineMapper();

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void UseFieldName_WhiteSpace_ThrowsArgumentException()
        {
            // arrange

            // act
            Action testAction = () => new WhiteSpaceUseFieldNameLineMapper();

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UseFieldNameAttributeProperty_Null_ThrowsArgumentNullException()
        {
            // arrange
            var attribute = new CsvFieldMappingAttribute(NameFieldName);

            // act
            Action testAction = () => attribute.UseFieldName = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void UseFieldNameAttributeProperty_WhiteSpace_ThrowsArgumentException()
        {
            // arrange
            var attribute = new CsvFieldMappingAttribute(NameFieldName);

            // act
            Action testAction = () => attribute.UseFieldName = " ";

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        private sealed class RepeatedNameLine : CsvFileRecordLine
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
        /// Maps <c>First</c> to the first <c>name</c> field as <c>legal_name</c>, and <c>Second</c> to the second as <c>preferred_name</c>.
        /// </summary>
        private sealed class RepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public RepeatedNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Second).HasFieldName(NameFieldName).UseFieldName(PreferredNameFieldName);
            }
        }

        /// <summary>
        /// Maps both properties to the <c>name</c> field, renaming only the first.
        /// </summary>
        private sealed class PartlyRenamedRepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public PartlyRenamedRepeatedNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Second).HasFieldName(NameFieldName);
            }
        }

        /// <summary>
        /// Maps <c>Second</c> to the field at index 1 as <c>preferred_name</c>.
        /// </summary>
        private sealed class IndexRenamedRepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public IndexRenamedRepeatedNameLineMapper()
            {
                this.Map().Property(x => x.Second).HasFieldIndex(1).UseFieldName(PreferredNameFieldName);
            }
        }

        /// <summary>
        /// Maps a property to an invalid name with <c>UseFieldName</c>.
        /// </summary>
        private sealed class NullUseFieldNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public NullUseFieldNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(null);
            }
        }

        /// <summary>
        /// Maps a property to a name of white space with <c>UseFieldName</c>.
        /// </summary>
        private sealed class WhiteSpaceUseFieldNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public WhiteSpaceUseFieldNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(" ");
            }
        }

        private sealed class DeclarativeRepeatedNameLine : CsvFileRecordLine
        {
            [CsvFieldMapping(NameFieldName, UseFieldName = LegalNameFieldName)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(NameFieldName, UseFieldName = PreferredNameFieldName)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class ThreeNameLine : CsvFileRecordLine
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

            public string Third
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps three properties to the <c>name</c> field, the last two giving it the same name.
        /// </summary>
        private sealed class ThreeNameLineMapper : CsvFileMapperBase<ThreeNameLine>
        {
            public ThreeNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Second).HasFieldName(NameFieldName).UseFieldName(PreferredNameFieldName);
                this.Map().Property(x => x.Third).HasFieldName(NameFieldName).UseFieldName(PreferredNameFieldName);
            }
        }

        private sealed class IdLine : CsvFileRecordLine
        {
            public string Id
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps <c>Id</c> to the <c>id</c> field.
        /// </summary>
        private sealed class IdLineMapper : CsvFileMapperBase<IdLine>
        {
            public IdLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName);
            }
        }

        /// <summary>
        /// Maps <c>Id</c> to the <c>id</c> field as <c>person_id</c>.
        /// </summary>
        private sealed class RenamedIdLineMapper : CsvFileMapperBase<IdLine>
        {
            public RenamedIdLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName).UseFieldName(RenamedIdFieldName);
            }
        }

        private sealed class SharedIdLine : CsvFileRecordLine
        {
            public string Id
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Code
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps both properties to the <c>id</c> field, and <c>Id</c> renames it <c>person_id</c>.
        /// </summary>
        private sealed class RenamedSharedIdLineMapper : CsvFileMapperBase<SharedIdLine>
        {
            public RenamedSharedIdLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName).UseFieldName(RenamedIdFieldName);
                this.Map().Property(x => x.Code).HasFieldName(IdFieldName);
            }
        }

        /// <summary>
        /// Maps both properties to the <c>id</c> field, both renaming it <c>person_id</c>.
        /// </summary>
        private sealed class AlikeRenamedSharedIdLineMapper : CsvFileMapperBase<SharedIdLine>
        {
            public AlikeRenamedSharedIdLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName).UseFieldName(RenamedIdFieldName);
                this.Map().Property(x => x.Code).HasFieldName(IdFieldName).UseFieldName(RenamedIdFieldName);
            }
        }

        /// <summary>
        /// Maps <c>Second</c> to the field at index 1 as <c>preferred_name</c>, and <c>First</c> to the <c>name</c> field without renaming it.
        /// </summary>
        private sealed class IndexAndNameRepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public IndexAndNameRepeatedNameLineMapper()
            {
                this.Map().Property(x => x.Second).HasFieldIndex(1).UseFieldName(PreferredNameFieldName);
                this.Map().Property(x => x.First).HasFieldName(NameFieldName);
            }
        }

        /// <summary>
        /// Maps <c>First</c> to the field at index 0 as <c>legal_name</c>, and <c>Second</c> to the <c>name</c> field as <c>preferred_name</c>.
        /// </summary>
        private sealed class IndexFirstRepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public IndexFirstRepeatedNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldIndex(0).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Second).HasFieldName(NameFieldName).UseFieldName(PreferredNameFieldName);
            }
        }

        /// <summary>
        /// Maps three properties to the <c>name</c> field: <c>First</c> and <c>Second</c> as <c>legal_name</c>, then <c>Third</c> as
        /// <c>preferred_name</c>.
        /// </summary>
        private sealed class SharedFirstThreeNameLineMapper : CsvFileMapperBase<ThreeNameLine>
        {
            public SharedFirstThreeNameLineMapper()
            {
                this.Map().Property(x => x.First).HasFieldName(NameFieldName).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Second).HasFieldName(NameFieldName).UseFieldName(LegalNameFieldName);
                this.Map().Property(x => x.Third).HasFieldName(NameFieldName).UseFieldName(PreferredNameFieldName);
            }
        }

        private sealed class CountLine : CsvFileRecordLine
        {
            public int Count
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps <c>Count</c> to the <c>count</c> field as <c>item_count</c>.
        /// </summary>
        private sealed class RenamedCountLineMapper : CsvFileMapperBase<CountLine>
        {
            public RenamedCountLineMapper()
            {
                this.Map().Property(x => x.Count).HasFieldName(CountFieldName).UseFieldName(RenamedCountFieldName);
            }
        }

        /// <summary>
        /// Maps both properties to the <c>id</c> field, giving it different names.
        /// </summary>
        private sealed class DifferentlyRenamedSharedIdLineMapper : CsvFileMapperBase<SharedIdLine>
        {
            public DifferentlyRenamedSharedIdLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName).UseFieldName(RenamedIdFieldName);
                this.Map().Property(x => x.Code).HasFieldName(IdFieldName).UseFieldName(CodeFieldName);
            }
        }
    }
}
