using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    public class CsvFileHeaderLineTests
    {
        private const string FirstFieldName = "One";
        private const string SecondFieldName = "Two";
        private const string RenamedFieldName = "Second";

        [Fact]
        public void FieldNames_ValueReplacedAfterRead_ReturnsReplacedName()
        {
            // arrange
            var testLine = CreateHeaderLine();
            _ = testLine.FieldNames;
            ((ICsvFileLine)testLine)[SecondFieldName] = RenamedFieldName;

            // act
            var testResult = testLine.FieldNames;

            // assert
            testResult.Should().Equal(FirstFieldName, RenamedFieldName);
        }

        [Fact]
        public void FieldNames_FieldsMovedAfterRead_ReturnsNamesInNewOrder()
        {
            // arrange
            ICsvFileLine csvLine = CreateHeaderLine();
            var testLine = (CsvFileHeaderLine)csvLine;
            _ = testLine.FieldNames;
            csvLine.RemoveAt(0);
            csvLine.Add(FirstFieldName, FirstFieldName);

            // act
            var testResult = testLine.FieldNames;

            // assert
            testResult.Should().Equal(SecondFieldName, FirstFieldName);
        }

        [Fact]
        public void FieldNames_ReadAgainWithoutChange_ReturnsSameList()
        {
            // arrange
            var testLine = CreateHeaderLine();
            var firstRead = testLine.FieldNames;

            // act
            var testResult = testLine.FieldNames;

            // assert
            testResult.Should().BeSameAs(firstRead);
        }

        [Fact]
        public void FieldNames_NonStringValue_ReturnsEmptyName()
        {
            // arrange
            var testLine = new CsvFileHeaderLine();
            testLine.Add(FirstFieldName, 1);

            // act
            IReadOnlyList<string> testResult = testLine.FieldNames;

            // assert
            testResult.Should().Equal(string.Empty);
        }

        private static CsvFileHeaderLine CreateHeaderLine()
        {
            var line = new CsvFileHeaderLine();
            line.Add(FirstFieldName, FirstFieldName);
            line.Add(SecondFieldName, SecondFieldName);

            return line;
        }
    }
}
