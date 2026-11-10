using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="VcardContentLine"/> and <see cref="VcardContentLineParameter"/> as a caller creates them.
    /// </summary>
    public class VcardContentLineTests
    {
        private const string Name = "NOTE";
        private const string Value = "v";
        private const string ParameterName = "P";

        private static readonly VcardContentLineParameter[] NoParameters = new VcardContentLineParameter[0];

        public static TheoryData<Func<VcardContentLine>> ConstructorsWithNullArgument()
        {
            return new TheoryData<Func<VcardContentLine>>
            {
                () => new VcardContentLine(null, Value),
                () => new VcardContentLine(Name, (string)null),
                () => new VcardContentLine(null, NoParameters, Value),
                () => new VcardContentLine(Name, null, Value),
                () => new VcardContentLine(Name, NoParameters, null),
                () => new VcardContentLine("g", null, NoParameters, Value),
                () => new VcardContentLine("g", Name, null, Value),
                () => new VcardContentLine("g", Name, NoParameters, null)
            };
        }

        public static TheoryData<Func<VcardContentLineParameter>> ParameterConstructorsWithNullArgument()
        {
            return new TheoryData<Func<VcardContentLineParameter>>
            {
                () => new VcardContentLineParameter(null, Value),
                () => new VcardContentLineParameter(ParameterName, (IEnumerable<string>)null)
            };
        }

        public static TheoryData<Func<VcardContentLineParameter>> ParameterConstructorsWithBadValues()
        {
            return new TheoryData<Func<VcardContentLineParameter>>
            {
                () => new VcardContentLineParameter(ParameterName),
                () => new VcardContentLineParameter(ParameterName, (string)null),
                () => new VcardContentLineParameter(ParameterName, new string[0])
            };
        }

        [Fact]
        public void Constructor_NameAndValue_HasNoGroupOrParameters()
        {
            // arrange
            var line = new VcardContentLine(Name, Value);

            // act
            var testResult = LineDescription.Of(line);

            // assert
            testResult.Should().Be("0/0 NOTE:v");
        }

        [Fact]
        public void Constructor_GroupAndParameters_HoldsThem()
        {
            // arrange
            var parameters = new[] { new VcardContentLineParameter(ParameterName, "a", "b") };

            // act
            var testResult = LineDescription.Of(new VcardContentLine("g", Name, parameters, Value));

            // assert
            testResult.Should().Be("0/0 g.NOTE;P=a|b:v");
        }

        [Theory]
        [MemberData(nameof(ConstructorsWithNullArgument))]
        public void Constructor_NullArgument_Throws(Func<VcardContentLine> constructor)
        {
            // act
            Action testAction = () => constructor();

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullParameter_Throws()
        {
            // arrange
            var parameters = new VcardContentLineParameter[] { null };

            // act
            Action testAction = () => new VcardContentLine(Name, parameters, Value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(ParameterConstructorsWithNullArgument))]
        public void ParameterConstructor_NullArgument_Throws(Func<VcardContentLineParameter> constructor)
        {
            // act
            Action testAction = () => constructor();

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(ParameterConstructorsWithBadValues))]
        public void ParameterConstructor_NoValuesOrNullValue_Throws(Func<VcardContentLineParameter> constructor)
        {
            // act
            Action testAction = () => constructor();

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        // ref. #267: neither list can be cast back to an array and changed.
        [Fact]
        public void Parameters_CastToList_IsReadOnly()
        {
            // arrange
            var line = new VcardContentLine(Name, new[] { new VcardContentLineParameter(ParameterName, Value) }, Value);

            // act
            var testResult = line.Parameters;

            // assert
            testResult.Should().NotBeAssignableTo<VcardContentLineParameter[]>();
            ((IList)testResult).IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void Values_CastToList_IsReadOnly()
        {
            // arrange
            var parameter = new VcardContentLineParameter(ParameterName, Value);

            // act
            var testResult = parameter.Values;

            // assert
            testResult.Should().NotBeAssignableTo<string[]>();
            ((IList)testResult).IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void Constructor_ArrayChangedAfterwards_KeepsOriginalValues()
        {
            // arrange
            const string original = "a";
            var values = new[] { original };

            // act
            var testResult = new VcardContentLineParameter(ParameterName, values);
            values[0] = "b";

            // assert
            testResult.Values.Should().Equal(original);
        }

        [Fact]
        public void GetParameter_NullName_Throws()
        {
            // arrange
            var line = new VcardContentLine(Name, Value);

            // act
            Action testAction = () => line.GetParameter(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetTextValue_LineNotReadFromData_ReportsLineZero()
        {
            // arrange
            var line = new VcardContentLine(Name, "a\\:b");

            // act
            Action testAction = () => line.GetTextValue();

            // assert
            var exception = testAction.Should().Throw<VcardFileException>().Which;
            exception.Data[VcardFileException.DataItemName.LineNumber].Should().Be(0L);
            exception.Data.Contains(VcardFileException.DataItemName.CardIndex).Should().BeFalse();
        }

        [Fact]
        public void ExceptionConstructor_MessageAndInnerException_KeepsThem()
        {
            // arrange
            const string message = "message";
            var innerException = new InvalidOperationException();

            // act
            var testResult = new VcardFileException(message, innerException);

            // assert
            testResult.Message.Should().Be(message);
            testResult.InnerException.Should().BeSameAs(innerException);
        }
    }
}
