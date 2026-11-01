using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class CsvFileRecordLineTests
{
    private const int LineCount = 10_000;
    private const int NumberValue = 1;
    private const string TextValue = "One";
    private const string NumberFieldName = "NumberField";

    [Fact]
    public void GetPropertyValue_ConcurrentAccessFromSeveralThreads_ReturnsValuesAndFieldKeys()
    {
        // arrange
        // No other test uses ConcurrentLine, so the threads race to resolve the field keys of its properties.
        string[] expectedFieldKeys = [NumberFieldName, nameof(ConcurrentLine.Text)];

        // act
        var testResult = ParallelRunner.Run(LineCount, _ => ReadBack(new ConcurrentLine { Number = NumberValue, Text = TextValue }));

        // assert
        testResult.Should().HaveCount(LineCount).And.OnlyContain(line =>
            line.Number == NumberValue && line.Text == TextValue && line.FieldKeys.SequenceEqual(expectedFieldKeys));
    }

    /// <summary>
    /// Reads back the property values of a line and the keys of its fields.
    /// </summary>
    private static (int Number, string? Text, string[] FieldKeys) ReadBack(ConcurrentLine testLine)
    {
        ICsvFileLine csvLine = testLine;
        var fieldKeys = new string[csvLine.Keys.Count];
        csvLine.Keys.CopyTo(fieldKeys, 0);

        return (testLine.Number, testLine.Text, fieldKeys);
    }

    private sealed class ConcurrentLine : CsvFileRecordLine
    {
        [CsvField(NumberFieldName)]
        public int Number
        {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }

        public string? Text
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }
}
