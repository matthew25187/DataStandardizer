using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests of writing a header line that repeats a field name, which RFC 4180 §2 ¶3 allows, from the names that mappings give fields with
    /// <c>UseFieldName</c>.
    /// </summary>
    public class CsvFileWriterRepeatedFieldNameTests
    {
        private const string NameFieldName = "name";
        private const string LegalNameFieldName = "legal_name";
        private const string PreferredNameFieldName = "preferred_name";
        private const string IdFieldName = "id";
        private const string KeyFieldName = "key";

        // The id field and the key field, which is renamed from id, would both be written as id, which the mapper can't read back.
        private const string NotReadBackMessage =
            "The header line would be written as 'id,id', which the mapper doesn't read back with the same field names.";
        private const string FirstName = "Ann";
        private const string SecondName = "Bob";

        // A header line that repeats the name field.
        private const string RepeatedNameHeaderLine = "name,name\r\n";

        // A header line that repeats the name field, with one record.
        private const string RepeatedNameFile = "name,name\r\nAnn,Bob\r\n";

        // A header line with the name a mapping by index gives the second field.
        private const string IndexRenamedHeaderLine = "name,preferred_name\r\n";

        [Fact]
        public void WriteLine_HeaderLineWithMappingNames_WritesFileNames()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(LegalNameFieldName, LegalNameFieldName);
            headerLine.Add(PreferredNameFieldName, PreferredNameFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RepeatedNameLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RepeatedNameLineMapper>();

                // act
                csvWriter.WriteLine(headerLine);

                // assert
                buffer.ToString().Should().Be(RepeatedNameHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterHeaderLineWithMappingNames_WritesFieldsAtTheirPositions()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(LegalNameFieldName, LegalNameFieldName);
            headerLine.Add(PreferredNameFieldName, PreferredNameFieldName);
            var testLine = new RepeatedNameLine { Second = SecondName, First = FirstName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RepeatedNameLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RepeatedNameLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RepeatedNameFile);
            }
        }

        [Fact]
        public void WriteLine_LinesReadFromFileWithRepeatedHeaderName_WritesFileUnchanged()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            ICsvFileLine headerLine;
            ICsvFileLine recordLine;
            using (var csvReader = new CsvFileReader<RepeatedNameLine>(new StringReader(RepeatedNameFile), options))
            {
                csvReader.RegisterMapper<RepeatedNameLineMapper>();
                headerLine = csvReader.ReadLine();
                recordLine = csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RepeatedNameLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RepeatedNameLineMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().Be(RepeatedNameFile);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineWithNameOfIndexMapping_WritesMappingName()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(NameFieldName, NameFieldName);
            headerLine.Add(PreferredNameFieldName, PreferredNameFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RepeatedNameLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IndexRenamedRepeatedNameLineMapper>();

                // act
                csvWriter.WriteLine(headerLine);

                // assert
                buffer.ToString().Should().Be(IndexRenamedHeaderLine);
            }
        }

        [Fact]
        public void WriteLine_RenamedFieldWhoseFileNameAnotherMappedFieldHas_ThrowsCsvFileException()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(KeyFieldName, KeyFieldName);
            headerLine.Add(IdFieldName, IdFieldName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<KeyLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RenamedKeyLineMapper>();

                // act
                Action testAction = () => csvWriter.WriteLine(headerLine);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(NotReadBackMessage);
            }
        }

        private sealed class KeyLine : CsvFileRecordLine
        {
            public string Key
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Id
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps <c>Key</c> to the <c>id</c> field as <c>key</c>, and <c>Id</c> to the <c>id</c> field without renaming it, so they are different
        /// fields that a header line names alike.
        /// </summary>
        private sealed class RenamedKeyLineMapper : CsvFileMapperBase<KeyLine>
        {
            public RenamedKeyLineMapper()
            {
                this.Map().Property(x => x.Key).HasFieldName(IdFieldName).UseFieldName(KeyFieldName);
                this.Map().Property(x => x.Id).HasFieldName(IdFieldName);
            }
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
        /// Maps <c>Second</c> to the field at index 1 as <c>preferred_name</c>.  A mapping by index doesn't say what the field is called in the
        /// file, so the writer writes the mapping's name.
        /// </summary>
        private sealed class IndexRenamedRepeatedNameLineMapper : CsvFileMapperBase<RepeatedNameLine>
        {
            public IndexRenamedRepeatedNameLineMapper()
            {
                this.Map().Property(x => x.Second).HasFieldIndex(1).UseFieldName(PreferredNameFieldName);
            }
        }
    }
}
