using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class ReaderActionsTests
{
    private const string InvalidLine = "\"a\"b";
    private const string ValidLine = "a";

    [Fact]
    public void ReadLineIgnoringFailure_InvalidData_DoesNotThrow()
    {
        // arrange
        using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(InvalidLine));

        // act
        Action testAction = () => ReaderActions.ReadLineIgnoringFailure(csvReader);

        // assert
        testAction.Should().NotThrow();
    }

    [Fact]
    public void ReadLineIgnoringFailure_ValidData_DoesNotThrow()
    {
        // arrange
        using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ValidLine));

        // act
        Action testAction = () => ReaderActions.ReadLineIgnoringFailure(csvReader);

        // assert
        testAction.Should().NotThrow();
    }
}
