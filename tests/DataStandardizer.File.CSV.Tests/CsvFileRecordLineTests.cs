using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

public class CsvFileRecordLineTests
{
    // Field names given by [CsvField] attributes.
    private const string FirstCodeFieldName = "FirstCode";
    private const string SecondCodeFieldName = "SecondCode";
    private const string BaseFieldName = "BaseField";
    private const string DerivedFieldName = "DerivedField";

    private const string FirstCode = "A";
    private const string SecondCode = "B";

    [Fact]
    public void SetPropertyValue_SameNamedPropertyOnDifferentTypes_ResolvesFieldKeyPerType()
    {
        // arrange
        _ = new FirstCodeLine { Code = FirstCode };
        var testLine = new SecondCodeLine();

        // act
        testLine.Code = SecondCode;

        // assert
        FieldKeys(testLine).Should().BeEquivalentTo(new[] { SecondCodeFieldName }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void GetPropertyValue_SameNamedPropertySetOnDifferentTypes_ReturnsValueSetOnFirstType()
    {
        // arrange
        var testLine = new FirstCodeLine { Code = FirstCode };
        _ = new SecondCodeLine { Code = SecondCode };

        // act
        var testResult = testLine.Code;

        // assert
        testResult.Should().Be(FirstCode);
    }

    [Fact]
    public void GetPropertyValue_SameNamedPropertySetOnDifferentTypes_ReturnsValueSetOnSecondType()
    {
        // arrange
        _ = new FirstCodeLine { Code = FirstCode };
        var testLine = new SecondCodeLine { Code = SecondCode };

        // act
        var testResult = testLine.Code;

        // assert
        testResult.Should().Be(SecondCode);
    }

    [Fact]
    public void SetPropertyValue_DerivedTypeOwnPropertyWithCsvField_UsesCsvFieldName()
    {
        // arrange
        var testLine = new DerivedLine();

        // act
        testLine.Own = "own";

        // assert
        FieldKeys(testLine).Should().BeEquivalentTo(new[] { DerivedFieldName }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void SetPropertyValue_DerivedTypeInheritedPropertyWithCsvField_UsesPropertyName()
    {
        // arrange
        var testLine = new DerivedLine();

        // act
        testLine.Inherited = "inherited";

        // assert
        FieldKeys(testLine).Should().BeEquivalentTo(new[] { nameof(BaseLine.Inherited) }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void SetPropertyValue_BaseTypeOwnPropertyWithCsvFieldAfterDerivedType_UsesCsvFieldName()
    {
        // arrange
        // Resolve the field key of the inherited property on the derived type first.
        _ = new DerivedLine { Inherited = "derived" };
        var testLine = new BaseLine();

        // act
        testLine.Inherited = "base";

        // assert
        FieldKeys(testLine).Should().BeEquivalentTo(new[] { BaseFieldName }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void GetPropertyValue_RepeatedAccess_ReturnsLastValueSet()
    {
        // arrange
        const string lastCode = "C";
        var testLine = new FirstCodeLine { Code = FirstCode };
        _ = testLine.Code;
        testLine.Code = SecondCode;
        _ = testLine.Code;
        testLine.Code = lastCode;

        // act
        var testResult = testLine.Code;

        // assert
        testResult.Should().Be(lastCode);
    }

    [Fact]
    public void SetPropertyValue_RepeatedAccess_ResolvesSameFieldKey()
    {
        // arrange
        var testLine = new FirstCodeLine { Code = FirstCode };
        _ = testLine.Code;

        // act
        testLine.Code = SecondCode;

        // assert
        FieldKeys(testLine).Should().BeEquivalentTo(new[] { FirstCodeFieldName }, options => options.WithStrictOrdering());
    }

    private static System.Collections.ICollection FieldKeys(ICsvFileLine line) => line.Keys;

    private class FirstCodeLine : CsvFileRecordLine
    {
        [CsvField(FirstCodeFieldName)]
        public string? Code
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class SecondCodeLine : CsvFileRecordLine
    {
        [CsvField(SecondCodeFieldName)]
        public string? Code
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class BaseLine : CsvFileRecordLine
    {
        [CsvField(BaseFieldName)]
        public string? Inherited
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }

    private class DerivedLine : BaseLine
    {
        [CsvField(DerivedFieldName)]
        public string? Own
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }
    }
}
