using System.Collections;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

public class CsvFileRecordLineTests
{
    [Fact]
    public void SetPropertyValue_SameNamedPropertyOnDifferentTypes_ResolvesFieldKeyPerType()
    {
        // arrange
        var firstLine = new FirstCodeLine();
        var secondLine = new SecondCodeLine();

        // act
        firstLine.Code = "A";
        secondLine.Code = "B";

        // assert
        FieldKeys(firstLine).Should().Equal("FirstCode");
        FieldKeys(secondLine).Should().Equal("SecondCode");
        firstLine.Code.Should().Be("A");
        secondLine.Code.Should().Be("B");
    }

    [Fact]
    public void SetPropertyValue_DerivedTypeOwnPropertyWithCsvField_UsesCsvFieldName()
    {
        // arrange
        var testLine = new DerivedLine();

        // act
        testLine.Own = "own";

        // assert
        FieldKeys(testLine).Should().Equal("DerivedField");
    }

    [Fact]
    public void SetPropertyValue_DerivedTypeInheritedPropertyWithCsvField_UsesPropertyName()
    {
        // arrange
        var testLine = new DerivedLine();

        // act
        testLine.Inherited = "inherited";

        // assert
        FieldKeys(testLine).Should().Equal(nameof(BaseLine.Inherited));
    }

    [Fact]
    public void SetPropertyValue_BaseTypeOwnPropertyWithCsvField_UsesCsvFieldName()
    {
        // arrange
        var derivedLine = new DerivedLine { Inherited = "derived" };
        var testLine = new BaseLine();

        // act
        testLine.Inherited = "base";

        // assert
        FieldKeys(derivedLine).Should().Equal(nameof(BaseLine.Inherited));
        FieldKeys(testLine).Should().Equal("BaseField");
    }

    [Fact]
    public void GetPropertyValue_RepeatedAccess_ResolvesSameFieldKey()
    {
        // arrange
        var testLine = new FirstCodeLine();

        // act
        for (var i = 0; i < 10; i++)
        {
            testLine.Code = i.ToString();
            _ = testLine.Code;
        }

        // assert
        FieldKeys(testLine).Should().Equal("FirstCode");
        testLine.Code.Should().Be("9");
    }

    [Fact]
    public void GetPropertyValue_ConcurrentAccessFromSeveralThreads_DoesNotThrow()
    {
        // arrange
        const int iterationCount = 10_000;
        var failures = 0;

        // act
        Action testAction = () => Parallel.For(0, iterationCount, i =>
        {
            var testLine = new ConcurrentLine { Number = i, Text = i.ToString() };
            var keys = FieldKeys(testLine);
            if (testLine.Number != i
                || testLine.Text != i.ToString()
                || !keys.SequenceEqual(new[] { "NumberField", nameof(ConcurrentLine.Text) }))
            {
                Interlocked.Increment(ref failures);
            }
        });

        // assert
        testAction.Should().NotThrow();
        failures.Should().Be(0);
    }

    private static string[] FieldKeys(IDictionary line)
    {
        return line.Keys.Cast<string>().ToArray();
    }

    private class FirstCodeLine : CsvFileRecordLine
    {
        [CsvField("FirstCode")]
        public string? Code
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class SecondCodeLine : CsvFileRecordLine
    {
        [CsvField("SecondCode")]
        public string? Code
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class BaseLine : CsvFileRecordLine
    {
        [CsvField("BaseField")]
        public string? Inherited
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class DerivedLine : BaseLine
    {
        [CsvField("DerivedField")]
        public string? Own
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class ConcurrentLine : CsvFileRecordLine
    {
        [CsvField("NumberField")]
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
