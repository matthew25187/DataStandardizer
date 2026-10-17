using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests;

public class IcalContentLineTests
{
    #region Read-only lists

    [Fact]
    public void Parameters_LineBuiltInCode_CannotBeCastToArray()
    {
        // arrange
        var line = new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter("CN", "Ann") }, "mailto:a@example.com");

        // act
        var testResult = line.Parameters;

        // assert
        testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
        testResult.Should().BeAssignableTo<ICollection<IcalContentLineParameter>>().Which.IsReadOnly.Should().BeTrue();
        testResult[0].Values.Should().NotBeAssignableTo<string[]>();
        testResult[0].Values.Should().BeAssignableTo<ICollection<string>>().Which.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Parameters_LineWithNoParameters_CannotBeCastToArray()
    {
        // arrange
        var line = new IcalContentLine("SUMMARY", "Party");

        // act
        var testResult = line.Parameters;

        // assert
        testResult.Should().BeEmpty();
        testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
    }

    [Fact]
    public void Parameters_LineRead_CannotBeCastToArray()
    {
        // arrange
        using var reader = CreateReader(Crlf("ATTENDEE;CN=Ann;MEMBER=\"mailto:x@example.com\",\"mailto:y@example.com\":mailto:a@example.com"));
        var line = reader.ReadLine()!;

        // act
        var testResult = line.Parameters;

        // assert
        testResult.Should().HaveCount(2);
        testResult.Should().NotBeAssignableTo<IcalContentLineParameter[]>();
        testResult.Should().BeAssignableTo<ICollection<IcalContentLineParameter>>().Which.IsReadOnly.Should().BeTrue();
        testResult[1].Values.Should().Equal("mailto:x@example.com", "mailto:y@example.com");
        testResult[1].Values.Should().NotBeAssignableTo<string[]>();
        testResult[1].Values.Should().BeAssignableTo<ICollection<string>>().Which.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Values_ParameterBuiltInCode_DoesNotChangeWithSourceArray()
    {
        // arrange
        var values = new[] { "Ann" };
        var parameter = new IcalContentLineParameter("CN", values);

        // act
        values[0] = "Mallory";

        // assert
        parameter.Values.Should().Equal("Ann");
        parameter.Values.Should().NotBeAssignableTo<string[]>();
    }

    [Fact]
    public void Parameters_ReadTwice_ReturnsSameList()
    {
        // arrange
        var line = new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter("CN", "Ann") }, "mailto:a@example.com");

        // act
        var testResult = line.Parameters;

        // assert
        testResult.Should().BeSameAs(line.Parameters);
        testResult[0].Values.Should().BeSameAs(line.Parameters[0].Values);
    }

    #endregion
}
