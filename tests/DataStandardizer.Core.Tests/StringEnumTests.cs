using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Core.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class StringEnumTests
    {
        // Parameter names of the StringEnum methods.
        private const string EnumTypeParameterName = "enumType";
        private const string ValueParameterName = "value";

        // Exception messages thrown by StringEnum.
        private const string NotStringEnumerationMessage = "enumType is not a string enumeration.*";
        private const string EmptyOrWhiteSpaceValueMessage = "value is either an empty string or only contains white space.*";
        private const string NotNamedConstantMessage = "value is a name, but not one of the named constants defined for the enumeration.*";
        private const string NoEnumerationInformationMessage = "value does not contain enumeration information.*";

        // Reasons given in assertions.
        private const string IsNameOfEnumValueReason = "{0} is the name of enum value {1}";
        private const string IsMemberReason = "{0} is a member of {1}";
        private const string IsNotMemberReason = "{0} is not a member of {1}";
        private const string NotStringEnumReason = "{0} does not represent a string enum";
        private const string DoesNotRepresentMemberReason = "{0} does not represent a member of {1}";
        private const string IsNameOfMemberReason = "{0} is the name of a member of {1}";
        private const string IsValueOfMemberReason = "{0} is the value of a member of {1}";
        private const string IsMemberHavingNameReason = "{0} is the member having the name {1}";
        private const string IsMemberHavingValueReason = "{0} is the member having the value {1}";

        // Values that are empty or contain only white space.
        private const string EmptyValue = "";
        private const string SpaceValue = " ";
        private const string TabValue = "\t";

        // Values of the members of SimpleEnum and ComplexEnum.
        private const string SimpleEnumOneValue = "1";
        private const string SimpleEnumTwoValue = "2";
        private const string SimpleEnumThreeValue = "3";
        private const string ComplexEnumFirstValue = "1";
        private const string BirdValue = "bird";
        private const string CatValue = "cat";
        private const string DogValue = "dog";
        private const string BlackValue = "black";
        private const string RedValue = "red";
        private const string GreenValue = "green";
        private const string BlueValue = "blue";

        // Names and values that the test enumerations don't define.
        private const string ZeroName = "Zero";
        private const string FourName = "Four";
        private const string FiveName = "Five";
        private const string SecondName = "Second";
        private const string FishName = "Fish";
        private const string LizardName = "Lizard";
        private const string YellowName = "Yellow";
        private const string SimpleEnumUndefinedValue = "0";
        private const string ComplexEnumUndefinedValue = "2";

        [Fact]
        public void GetNameNonGeneric_WithNullEnumType_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.GetName(null!, SimpleEnum.One);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetNameNonGeneric_WithNullValue_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.GetName(typeof(SimpleEnum), null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Fact]
        public void GetNameNonGeneric_OnNonStringEnumType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => StringEnum.GetName(typeof(DayOfWeek), DayOfWeek.Sunday);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetNameNonGeneric_WithValueNotMatchingEnumType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => StringEnum.GetName(typeof(ComplexEnum), SimpleEnum.One);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage("value is not of type enumType.*").WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(GetNameNonGeneric_WithStringEnumValue_TestCaseFactory))]
        public void GetNameNonGeneric_WithStringEnumValue_ReturnsStringEnumMemberName(Type enumType, object enumValue, string expectedResult)
        {
            // act
            var testResult = StringEnum.GetName(enumType, enumValue);

            // assert
            testResult.Should().Be(expectedResult, IsNameOfEnumValueReason, expectedResult, enumValue);
        }

        [Theory]
        [ClassData(typeof(GetNameGeneric_WithStringEnumValue_TestCaseFactory))]
        public void GetNameGeneric_WithStringEnumValue_ReturnsStringEnumMemberName<T>(T enumValue, string expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.GetName(enumValue);

            // assert
            testResult.Should().Be(expectedResult, IsNameOfEnumValueReason, expectedResult, enumValue);
        }

        [Fact]
        public void GetNamesNonGeneric_WithNullEnumType_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.GetNames(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetNamesNonGeneric_WithNonStringEnumType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => StringEnum.GetNames(typeof(DayOfWeek));

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetNamesNonGeneric_WithFlatStringEnumType_ReturnsStringEnumMemberNames()
        {
            // arrange
            var expectedResult = new[] { nameof(SimpleEnum.One), nameof(SimpleEnum.Two), nameof(SimpleEnum.Three) };

            // act
            var testResult = StringEnum.GetNames(typeof(SimpleEnum));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetNamesNonGeneric_WithHierarchicalStringEnumType_ReturnsStringEnumMemberNames()
        {
            // arrange
            var expectedResult = new[]
            {
                nameof(ComplexEnum.First), nameof(ComplexEnum.Animal.Bird), nameof(ComplexEnum.Animal.Cat), nameof(ComplexEnum.Animal.Dog), nameof(ComplexEnum.Color.Black), nameof(ComplexEnum.Color.Blue), nameof(ComplexEnum.Color.Green),
                nameof(ComplexEnum.Color.Red)
            };

            // act
            var testResult = StringEnum.GetNames(typeof(ComplexEnum));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetNamesGeneric_WithFlatStringEnumType_ReturnsStringEnumMemberNames()
        {
            // arrange
            var expectedResult = new[] { nameof(SimpleEnum.One), nameof(SimpleEnum.Two), nameof(SimpleEnum.Three) };

            // act
            var testResult = StringEnum.GetNames<SimpleEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetNamesGeneric_WithHierarchicalStringEnumType_ReturnsStringEnumMemberNames()
        {
            // arrange
            var expectedResult = new[]
            {
                nameof(ComplexEnum.First), nameof(ComplexEnum.Animal.Bird), nameof(ComplexEnum.Animal.Cat), nameof(ComplexEnum.Animal.Dog), nameof(ComplexEnum.Color.Black), nameof(ComplexEnum.Color.Blue), nameof(ComplexEnum.Color.Green),
                nameof(ComplexEnum.Color.Red)
            };

            // act
            var testResult = StringEnum.GetNames<ComplexEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetValuesNonGeneric_WithNullEnumType_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.GetValues(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetValuesNonGeneric_WithNonStringEnumType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => StringEnum.GetValues(typeof(DayOfWeek));

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void GetValuesNonGeneric_WithFlatStringEnumType_ReturnsStringEnumMemberValues()
        {
            // arrange
            var expectedResult = new[] { SimpleEnum.One, SimpleEnum.Two, SimpleEnum.Three };

            // act
            var testResult = StringEnum.GetValues(typeof(SimpleEnum));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetValuesNonGeneric_WithHierarchicalStringEnumType_ReturnsStringEnumMemberValues()
        {
            // arrange
            var expectedResult = new[] { ComplexEnum.First, ComplexEnum.Animal.Bird, ComplexEnum.Animal.Cat, ComplexEnum.Animal.Dog, ComplexEnum.Color.Black, ComplexEnum.Color.Blue, ComplexEnum.Color.Green, ComplexEnum.Color.Red };

            // act
            var testResult = StringEnum.GetValues(typeof(ComplexEnum));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetValuesGeneric_WithFlatStringEnumType_ReturnsStringEnumMemberValues()
        {
            // arrange
            var expectedResult = new[] { SimpleEnum.One, SimpleEnum.Two, SimpleEnum.Three };

            // act
            var testResult = StringEnum.GetValues<SimpleEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetValuesGeneric_WithHierarchicalStringEnumType_ReturnsStringEnumMemberValues()
        {
            // arrange
            var expectedResult = new[] { ComplexEnum.First, ComplexEnum.Animal.Bird, ComplexEnum.Animal.Cat, ComplexEnum.Animal.Dog, ComplexEnum.Color.Black, ComplexEnum.Color.Blue, ComplexEnum.Color.Green, ComplexEnum.Color.Red };

            // act
            var testResult = StringEnum.GetValues<ComplexEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void IsDefinedNonGeneric_WithNullEnumType_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.IsDefined(null!, "Hi");

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Fact]
        public void IsDefinedNonGeneric_WithNullValue_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.IsDefined(typeof(SimpleEnum), null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Fact]
        public void IsDefinedNonGeneric_WithNonStringEnumType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => StringEnum.IsDefined(typeof(DayOfWeek), "Sunday");

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(ZeroName), InlineData(FourName), InlineData("Ten")]
        public void IsDefinedNonGeneric_WithFlatStringEnumNonMember_ReturnsFalse(string enumValue)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(SimpleEnum), enumValue);

            // assert
            testResult.Should().BeFalse(IsNotMemberReason, enumValue, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(nameof(SimpleEnum.One)), InlineData(nameof(SimpleEnum.Two)), InlineData(nameof(SimpleEnum.Three))]
        public void IsDefinedNonGeneric_WithFlatStringEnumMemberName_ReturnsTrue(string enumMemberName)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(SimpleEnum), enumMemberName);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberName, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(SimpleEnumOneValue), InlineData(SimpleEnumTwoValue), InlineData(SimpleEnumThreeValue)]
        public void IsDefinedNonGeneric_WithFlatStringEnumMemberValue_ReturnsTrue(string enumMemberValue)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(SimpleEnum), enumMemberValue);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberValue, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(ZeroName), InlineData(SecondName), InlineData("Snake"), InlineData(YellowName)]
        public void IsDefinedNonGeneric_WithHierarchicalStringEnumNonMember_ReturnsFalse(string enumValue)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(ComplexEnum), enumValue);

            // assert
            testResult.Should().BeFalse(IsNotMemberReason, enumValue, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(nameof(ComplexEnum.First)), InlineData(nameof(ComplexEnum.Animal.Cat)), InlineData(nameof(ComplexEnum.Color.Black))]
        public void IsDefinedNonGeneric_WithHierarchicalStringEnumMemberName_ReturnsTrue(string enumMemberName)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(ComplexEnum), enumMemberName);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberName, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(ComplexEnumFirstValue), InlineData(BirdValue), InlineData(BlackValue)]
        public void IsDefinedNonGeneric_WithHierarchicalStringEnumMemberValue_ReturnsTrue(string enumMemberValue)
        {
            // act
            var testResult = StringEnum.IsDefined(typeof(ComplexEnum), enumMemberValue);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberValue, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(ZeroName), InlineData(FourName), InlineData("Twelve")]
        public void IsDefinedGeneric_WithFlatStringEnumNonMember_ReturnsFalse(string enumValue)
        {
            // act
            var testResult = StringEnum.IsDefined<SimpleEnum>(enumValue);

            // assert
            testResult.Should().BeFalse(IsNotMemberReason, enumValue, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(nameof(SimpleEnum.One)), InlineData(nameof(SimpleEnum.Two)), InlineData(nameof(SimpleEnum.Three))]
        public void IsDefinedGeneric_WithFlatStringEnumMemberName_ReturnsTrue(string enumMemberName)
        {
            // act
            var testResult = StringEnum.IsDefined<SimpleEnum>(enumMemberName);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberName, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(SimpleEnumOneValue), InlineData(SimpleEnumTwoValue), InlineData(SimpleEnumThreeValue)]
        public void IsDefinedGeneric_WithFlatStringEnumMemberValue_ReturnsTrue(string enumMemberValue)
        {
            // act
            var testResult = StringEnum.IsDefined<SimpleEnum>(enumMemberValue);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberValue, nameof(SimpleEnum));
        }

        [Theory]
        [InlineData(ZeroName), InlineData("Two"), InlineData(LizardName), InlineData("Purple")]
        public void IsDefinedGeneric_WithHierarchicalStringEnumNonMember_ReturnsFalse(string enumValue)
        {
            // act
            var testResult = StringEnum.IsDefined<ComplexEnum>(enumValue);

            // assert
            testResult.Should().BeFalse(IsNotMemberReason, enumValue, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(nameof(ComplexEnum.First)), InlineData(nameof(ComplexEnum.Animal.Cat)), InlineData(nameof(ComplexEnum.Color.Red))]
        public void IsDefinedGeneric_WithHierarchicalStringEnumMemberName_ReturnsTrue(string enumMemberName)
        {
            // act
            var testResult = StringEnum.IsDefined<ComplexEnum>(enumMemberName);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberName, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(ComplexEnumFirstValue), InlineData(DogValue), InlineData(RedValue)]
        public void IsDefinedGeneric_WithHierarchicalStringEnumMemberValue_ReturnsTrue(string enumMemberValue)
        {
            // act
            var testResult = StringEnum.IsDefined<ComplexEnum>(enumMemberValue);

            // assert
            testResult.Should().BeTrue(IsMemberReason, enumMemberValue, nameof(ComplexEnum));
        }

        [Theory]
        [InlineData(nameof(SimpleEnum.One)), InlineData(nameof(ComplexEnum.First))]
        public void ParseNonGeneric_WithNullEnumType_ThrowsArgumentNullException(string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse(null!, enumValue);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum)), InlineData(typeof(ComplexEnum))]
        public void ParseNonGeneric_WithNullValue_ThrowsArgumentNullException(Type enumType)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Sunday))]
        public void ParseNonGeneric_WithNonStringEnumType_ThrowsArgumentException(Type enumType, string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), EmptyValue), InlineData(typeof(ComplexEnum), SpaceValue), InlineData(typeof(SimpleEnum), TabValue)]
        public void ParseNonGeneric_WithWhiteSpaceValue_ThrowsArgumentException(Type enumType, string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(EmptyOrWhiteSpaceValueMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), ZeroName), InlineData(typeof(SimpleEnum), FourName), InlineData(typeof(ComplexEnum), SecondName), InlineData(typeof(ComplexEnum), FishName), InlineData(typeof(ComplexEnum), YellowName)]
        public void ParseNonGeneric_WithNonStringEnumValue_ThrowsArgumentException(Type enumType, string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotNamedConstantMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(ParseNonGeneric_WithStringEnumMemberName_TestCaseFactory))]
        public void ParseNonGeneric_WithStringEnumMemberName_ReturnsEnumMember(Type enumType, string enumMemberName, object expectedResult)
        {
            // act
            var testResult = StringEnum.Parse(enumType, enumMemberName);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [ClassData(typeof(ParseNonGeneric_WithStringEnumMemberValue_TestCaseFactory))]
        public void ParseNonGeneric_WithStringEnumMemberValue_ReturnsEnumMember(Type enumType, string enumMemberValue, object expectedResult)
        {
            // act
            var testResult = StringEnum.Parse(enumType, enumMemberValue);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(nameof(SimpleEnum.One), false), InlineData(nameof(SimpleEnum.One), true)]
        public void ParseNonGenericIgnoreCaseOverload_WithNullEnumType_ThrowsArgumentNullException(string enumValue, bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse(null!, enumValue, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), false), InlineData(typeof(SimpleEnum), true), InlineData(typeof(ComplexEnum), false), InlineData(typeof(ComplexEnum), true)]
        public void ParseNonGenericIgnoreCaseOverload_WithNullValue_ThrowsArgumentNullException(Type enumType, bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, null!, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Tuesday), false), InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Monday), true)]
        public void ParseNonGenericIgnoreCaseOverload_WithNonStringEnumType_ThrowsArgumentException(Type enumType, string enumValue, bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), EmptyValue), InlineData(typeof(SimpleEnum), SpaceValue), InlineData(typeof(SimpleEnum), TabValue)]
        public void ParseNonGenericIgnoreCaseOverload_WithWhiteSpaceValue_ThrowsArgumentException(Type enumType, string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue, false);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage("value is either an empty string (\"\") or only contains white space.*").WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), ZeroName, false), InlineData(typeof(SimpleEnum), FourName, true), InlineData(typeof(ComplexEnum), SecondName, false)]
        public void ParseNonGenericIgnoreCaseOverload_WithNonStringEnumValue_ThrowsArgumentException(Type enumType, string enumValue, bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse(enumType, enumValue, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotNamedConstantMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory))]
        public void ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_ReturnsEnumMember(Type enumType, string enumMemberName, bool ignoreCase, object expectedResult)
        {
            // act
            var testResult = StringEnum.Parse(enumType, enumMemberName, ignoreCase);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [ClassData(typeof(ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory))]
        public void ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_ReturnsEnumMember(Type enumType, string enumMemberValue, bool ignoreCase, object expectedResult)
        {
            // act
            var testResult = StringEnum.Parse(enumType, enumMemberValue, ignoreCase);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Fact]
        public void ParseGeneric_WithNullValue_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(EmptyValue), InlineData(SpaceValue), InlineData(TabValue)]
        public void ParseGeneric_WithWhiteSpaceValue_ThrowsArgumentException(string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(enumValue);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(EmptyOrWhiteSpaceValueMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(SimpleEnumUndefinedValue), InlineData("4"), InlineData(ZeroName), InlineData(FourName)]
        public void ParseGeneric_WithNonStringEnumValue_ThrowsArgumentException(string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(enumValue);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NoEnumerationInformationMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(ParseGeneric_WithStringEnumMemberName_TestCaseFactory))]
        public void ParseGeneric_WithStringEnumMemberName_ReturnsEnumMember<T>(string enumMemberName, T expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.Parse<T>(enumMemberName);

            // assert
            testResult.Should().Be(expectedResult, IsMemberReason, enumMemberName, typeof(T).Name);
        }

        [Theory]
        [ClassData(typeof(ParseGeneric_WithStringEnumMemberValue_TestCaseFactory))]
        public void ParseGeneric_WithStringEnumMemberValue_ReturnsEnumMember<T>(string enumMemberValue, T expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.Parse<T>(enumMemberValue);

            // assert
            testResult.Should().Be(expectedResult, IsMemberReason, enumMemberValue, typeof(T).Name);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void ParseGenericIgnoreCaseOverload_WithNullValue_ThrowsArgumentNullException(bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(null!, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(EmptyValue), InlineData(SpaceValue), InlineData(TabValue)]
        public void ParseGenericIgnoreCaseOverload_WithWhiteSpaceValue_ThrowsArgumentException(string enumValue)
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(enumValue, false);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(EmptyOrWhiteSpaceValueMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(ZeroName, false), InlineData(ZeroName, true), InlineData(FourName, false), InlineData(FourName, true)]
        public void ParseGenericIgnoreCaseOverload_WithNonStringEnumValue_ThrowsArgumentException(string enumValue, bool ignoreCase)
        {
            // act
            Action testAction = () => StringEnum.Parse<SimpleEnum>(enumValue, ignoreCase);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NoEnumerationInformationMessage).WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(ParseGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory))]
        public void ParseGenericIgnoreCaseOverload_WithStringEnumMemberName_ReturnsEnumMember<T>(string enumMemberName, bool ignoreCase, T expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.Parse<T>(enumMemberName, ignoreCase);

            // assert
            testResult.Should().Be(expectedResult, IsMemberReason, enumMemberName, typeof(T).Name);
        }

        [Theory]
        [ClassData(typeof(ParseGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory))]
        public void ParseGenericIgnoreCaseOverload_WithStringEnumMemberValue_ReturnsEnumMember<T>(string enumMemberValue, bool ignoreCase, T expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.Parse<T>(enumMemberValue, ignoreCase);

            // assert
            testResult.Should().Be(expectedResult, IsMemberReason, enumMemberValue, typeof(T).Name);
        }

        [Fact]
        public void ToObjectNonGeneric_WithNullEnumType_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.ToObject(null!, String.Empty);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [InlineData(typeof(SimpleEnum)), InlineData(typeof(ComplexEnum))]
        public void ToObjectNonGeneric_WithNullValue_ThrowsArgumentNullException(Type enumType)
        {
            // act
            Action testAction = () => StringEnum.ToObject(enumType, null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [InlineData(typeof(DayOfWeek)), InlineData(typeof(DateTime))]
        public void ToObjectNonGeneric_WithNonStringEnumType_ThrowsArgumentException(Type enumType)
        {
            // act
            Action testAction = () => StringEnum.ToObject(enumType, String.Empty);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [ClassData(typeof(ToObjectNonGeneric_WithNonNullValue_TestCaseFactory))]
        public void ToObjectNonGeneric_WithNonNullValue_ReturnsStringEnum(Type enumType, string enumValue, object expectedResult)
        {
            // act
            var testResult = StringEnum.ToObject(enumType, enumValue);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Fact]
        public void ToObjectGeneric_WithNullValue_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.ToObject<SimpleEnum>(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [ClassData(typeof(ToObjectGeneric_WithNonNullValue_TestCaseFactory))]
        public void ToObjectGeneric_WithNonNullValue_ReturnsStringEnum<T>(string enumValue, T expectedResult) where T : struct, IStringEnum
        {
            // act
            var testResult = StringEnum.ToObject<T>(enumValue);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Sunday)), InlineData(typeof(UriBuilder), nameof(UriBuilder.Scheme))]
        public void TryParseNonGeneric_WithNonStringEnumType_ReturnsFalse(Type enumType, string value)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, value, out _);

            // assert
            testResult.Should().BeFalse(NotStringEnumReason, nameof(enumType));
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), ZeroName), InlineData(typeof(SimpleEnum), SimpleEnumUndefinedValue), InlineData(typeof(ComplexEnum), SecondName), InlineData(typeof(ComplexEnum), ComplexEnumUndefinedValue)]
        public void TryParseNonGeneric_WithNonStringEnumValue_ReturnsFalse(Type enumType, string value)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, value, out _);

            // assert
            testResult.Should().BeFalse(DoesNotRepresentMemberReason, value, enumType.Name);
        }

        [Theory]
        [ClassData(typeof(TryParseNonGeneric_WithStringEnumMemberName_TestCaseFactory))]
        public void TryParseNonGeneric_WithStringEnumMemberName_ReturnsStringEnumMember(Type enumType, string enumMemberName, object expectedResult)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, enumMemberName, out var actualResult);

            // assert
            testResult.Should().BeTrue(IsNameOfMemberReason, enumMemberName, enumType.Name);
            actualResult.Should().Be(expectedResult, IsMemberHavingNameReason, expectedResult, enumMemberName);
        }

        [Theory]
        [ClassData(typeof(TryParseNonGeneric_WithStringEnumMemberValue_TestCaseFactory))]
        public void TryParseNonGeneric_WithStringEnumMemberValue_ReturnsStringEnumMember(Type enumType, string enumMemberValue, object expectedResult)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, enumMemberValue, out var actualResult);

            // assert
            testResult.Should().BeTrue(IsValueOfMemberReason, enumMemberValue, enumType.Name);
            actualResult.Should().Be(expectedResult, IsMemberHavingValueReason, expectedResult, enumMemberValue);
        }

        [Theory]
        [InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Sunday)), InlineData(typeof(UriBuilder), nameof(UriBuilder.Scheme))]
        public void TryParseNonGenericIgnoreCaseOverload_WithNonStringEnumType_ReturnsFalse(Type enumType, string value)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, value, false, out _);

            // assert
            testResult.Should().BeFalse(NotStringEnumReason, nameof(enumType));
        }

        [Theory]
        [InlineData(typeof(SimpleEnum), ZeroName), InlineData(typeof(SimpleEnum), SimpleEnumUndefinedValue), InlineData(typeof(ComplexEnum), SecondName), InlineData(typeof(ComplexEnum), ComplexEnumUndefinedValue)]
        public void TryParseNonGenericIgnoreCaseOverload_WithNonStringEnumValue_ReturnsFalse(Type enumType, string value)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, value, false, out _);

            // assert
            testResult.Should().BeFalse(DoesNotRepresentMemberReason, value, enumType.Name);
        }

        [Theory]
        [ClassData(typeof(TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory))]
        public void TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_ReturnsStringEnumMember(Type enumType, string enumMemberName, bool ignoreCase, object expectedResult)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, enumMemberName, ignoreCase, out var actualResult);

            // assert
            testResult.Should().BeTrue(IsNameOfMemberReason, enumMemberName, enumType.Name);
            actualResult.Should().Be(expectedResult, IsMemberHavingNameReason, expectedResult, enumMemberName);
        }

        [Theory]
        [ClassData(typeof(TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory))]
        public void TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_ReturnsStringEnumMember(Type enumType, string enumMemberValue, bool ignoreCase, object expectedResult)
        {
            // act
            var testResult = StringEnum.TryParse(enumType, enumMemberValue, ignoreCase, out var actualResult);

            // assert
            testResult.Should().BeTrue(IsValueOfMemberReason, enumMemberValue, enumType.Name);
            actualResult.Should().Be(expectedResult, IsMemberHavingValueReason, expectedResult, enumMemberValue);
        }

        [Theory]
        [ClassData(typeof(WithExistingMemberNameOnFlatEnum_TestCaseFactory))]
        public void TryParse_WithExistingMemberNameOnFlatEnum_ReturnsTrue(string testValue, SimpleEnum enumValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, out SimpleEnum actualValue);

            // assert
            result.Should().BeTrue();
            actualValue.Should().Be(enumValue);
        }

        [Theory]
        [ClassData(typeof(WithExistingMemberNameOnHierarchicalEnum_TestCaseFactory))]
        public void TryParse_WithExistingMemberNameOnHierarchicalEnum_ReturnsTrue(string testValue, ComplexEnum enumValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, out ComplexEnum actualValue);

            // assert
            result.Should().BeTrue();
            actualValue.Should().Be(enumValue);
        }

        [Theory]
        [ClassData(typeof(WithMissingMemberNameOnFlatEnum_TestCaseFactory))]
        public void TryParse_WithMissingMemberNameOnFlatEnum_ReturnsFalse(string testValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, out SimpleEnum _);

            // assert
            result.Should().BeFalse();
        }

        [Theory]
        [ClassData(typeof(WithMissingMemberNameOnHierarchicalEnum_TestCaseFactory))]
        public void TryParse_WithMissingMemberNameOnHierarchicalEnum_ReturnsFalse(string testValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, out ComplexEnum _);

            // assert
            result.Should().BeFalse();
        }

        [Theory]
        [ClassData(typeof(WithExistingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory))]
        public void TryParse_WithExistingMemberNameAndIgnoreCaseFlagOnFlatEnum_ReturnsTrue(string testValue, bool ignoreCase, SimpleEnum enumValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, ignoreCase, out SimpleEnum actualValue);

            // assert
            result.Should().BeTrue();
            actualValue.Should().Be(enumValue);
        }

        [Theory]
        [ClassData(typeof(WithExistingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory))]
        public void TryParse_WithExistingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_ReturnsTrue(string testValue, bool ignoreCase, ComplexEnum enumValue)
        {
            // act
            var result = StringEnum.TryParse(testValue, ignoreCase, out ComplexEnum actualValue);

            // assert
            result.Should().BeTrue();
            actualValue.Should().Be(enumValue);
        }

        [Theory]
        [ClassData(typeof(WithMissingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory))]
        public void TryParse_WithMissingMemberNameAndIgnoreCaseFlagOnFlatEnum_ReturnsFalse(string testValue, bool ignoreCase)
        {
            // act
            var result = StringEnum.TryParse(testValue, ignoreCase, out SimpleEnum _);

            // assert
            result.Should().BeFalse();
        }

        [Theory]
        [ClassData(typeof(WithMissingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory))]
        public void TryParse_WithMissingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_ReturnsFalse(string testValue, bool ignoreCase)
        {
            // act
            var result = StringEnum.TryParse(testValue, ignoreCase, out ComplexEnum _);

            // assert
            result.Should().BeFalse();
        }

        #region Test Data

        public class GetNameNonGeneric_WithStringEnumValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnum.One, nameof(SimpleEnum.One) };
                yield return new object[] { typeof(SimpleEnum), SimpleEnum.Two, nameof(SimpleEnum.Two) };
                yield return new object[] { typeof(SimpleEnum), SimpleEnum.Three, nameof(SimpleEnum.Three) };
                yield return new object[] { typeof(ComplexEnum), ComplexEnum.First, nameof(ComplexEnum.First) };
                yield return new object[] { typeof(ComplexEnum), ComplexEnum.Animal.Cat, nameof(ComplexEnum.Animal.Cat) };
                yield return new object[] { typeof(ComplexEnum), ComplexEnum.Color.Black, nameof(ComplexEnum.Color.Black) };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class GetNameGeneric_WithStringEnumValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { SimpleEnum.One, nameof(SimpleEnum.One) };
                yield return new object[] { SimpleEnum.Two, nameof(SimpleEnum.Two) };
                yield return new object[] { SimpleEnum.Three, nameof(SimpleEnum.Three) };
                yield return new object[] { ComplexEnum.First, nameof(ComplexEnum.First) };
                yield return new object[] { ComplexEnum.Animal.Bird, nameof(ComplexEnum.Animal.Bird) };
                yield return new object[] { ComplexEnum.Color.Blue, nameof(ComplexEnum.Color.Blue) };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ToObjectNonGeneric_WithNonNullValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), DogValue, ComplexEnum.Animal.Dog };
                yield return new object[] { typeof(ComplexEnum), GreenValue, ComplexEnum.Color.Green };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ToObjectGeneric_WithNonNullValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { SimpleEnumOneValue, SimpleEnum.One };
                yield return new object[] { ComplexEnumFirstValue, ComplexEnum.First };
                yield return new object[] { BirdValue, ComplexEnum.Animal.Bird };
                yield return new object[] { BlueValue, ComplexEnum.Color.Blue };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseNonGeneric_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.Animal.Cat), ComplexEnum.Animal.Cat };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.Color.Black), ComplexEnum.Color.Black };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseNonGeneric_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), BirdValue, ComplexEnum.Animal.Bird };
                yield return new object[] { typeof(ComplexEnum), RedValue, ComplexEnum.Color.Red };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), false, SimpleEnum.One };
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), true, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), false, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), true, ComplexEnum.First };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, false, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, false, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, true, ComplexEnum.First };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseGeneric_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { nameof(SimpleEnum.One), SimpleEnum.One };
                yield return new object[] { nameof(ComplexEnum.First), ComplexEnum.First };
                yield return new object[] { nameof(ComplexEnum.Animal.Bird), ComplexEnum.Animal.Bird };
                yield return new object[] { nameof(ComplexEnum.Color.Blue), ComplexEnum.Color.Blue };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseGeneric_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { SimpleEnumOneValue, SimpleEnum.One };
                yield return new object[] { ComplexEnumFirstValue, ComplexEnum.First };
                yield return new object[] { CatValue, ComplexEnum.Animal.Cat };
                yield return new object[] { GreenValue, ComplexEnum.Color.Green };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { nameof(SimpleEnum.One), false, SimpleEnum.One };
                yield return new object[] { nameof(SimpleEnum.One), true, SimpleEnum.One };
                yield return new object[] { nameof(ComplexEnum.First), false, ComplexEnum.First };
                yield return new object[] { nameof(ComplexEnum.First), true, ComplexEnum.First };
                yield return new object[] { nameof(ComplexEnum.Animal.Cat), false, ComplexEnum.Animal.Cat };
                yield return new object[] { nameof(ComplexEnum.Animal.Cat), true, ComplexEnum.Animal.Cat };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ParseGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { SimpleEnumOneValue, false, SimpleEnum.One };
                yield return new object[] { SimpleEnumOneValue, true, SimpleEnum.One };
                yield return new object[] { ComplexEnumFirstValue, false, ComplexEnum.First };
                yield return new object[] { ComplexEnumFirstValue, true, ComplexEnum.First };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class TryParseNonGeneric_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.Animal.Cat), ComplexEnum.Animal.Cat };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.Color.Black), ComplexEnum.Color.Black };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class TryParseNonGeneric_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), BirdValue, ComplexEnum.Animal.Bird };
                yield return new object[] { typeof(ComplexEnum), BlueValue, ComplexEnum.Color.Blue };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberName_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), false, SimpleEnum.One };
                yield return new object[] { typeof(SimpleEnum), nameof(SimpleEnum.One), true, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), false, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), nameof(ComplexEnum.First), true, ComplexEnum.First };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class TryParseNonGenericIgnoreCaseOverload_WithStringEnumMemberValue_TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, false, SimpleEnum.One };
                yield return new object[] { typeof(SimpleEnum), SimpleEnumOneValue, true, SimpleEnum.One };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, false, ComplexEnum.First };
                yield return new object[] { typeof(ComplexEnum), ComplexEnumFirstValue, true, ComplexEnum.First };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class WithExistingMemberNameOnFlatEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithExistingMemberNameOnFlatEnum_TestCaseFactory()
            {
                TestData = new (string Name, SimpleEnum EnumObject)[]
                    {
                        (nameof(SimpleEnum.One), EnumObject: SimpleEnum.One),
                        (nameof(SimpleEnum.Two), EnumObject: SimpleEnum.Two),
                        (nameof(SimpleEnum.Three), EnumObject: SimpleEnum.Three)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.EnumObject });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithExistingMemberNameOnHierarchicalEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithExistingMemberNameOnHierarchicalEnum_TestCaseFactory()
            {
                TestData = new (string Name, ComplexEnum EnumObject)[]
                    {
                        (nameof(ComplexEnum.First), ComplexEnum.First),
                        (nameof(ComplexEnum.Animal.Bird), ComplexEnum.Animal.Bird),
                        (nameof(ComplexEnum.Color.Red), ComplexEnum.Color.Red)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.EnumObject });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithExistingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithExistingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory()
            {
                TestData = new (string Name, bool IgnoreCase, SimpleEnum EnumObject)[]
                    {
                        ("one", true, SimpleEnum.One),
                        (nameof(SimpleEnum.Two), false, SimpleEnum.Two),
                        ("THREE", true, SimpleEnum.Three)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.IgnoreCase, testCase.EnumObject });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithExistingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithExistingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory()
            {
                TestData = new (string Name, bool IgnoreCase, ComplexEnum EnumObject)[]
                    {
                        ("first", true, ComplexEnum.First),
                        (nameof(ComplexEnum.Animal.Cat), false, ComplexEnum.Animal.Cat),
                        ("BLUE", true, ComplexEnum.Color.Blue)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.IgnoreCase, testCase.EnumObject });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithMissingMemberNameOnFlatEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithMissingMemberNameOnFlatEnum_TestCaseFactory()
            {
                TestData = new[] { FourName, FiveName, "Six" }.Select(testCase => new object[] { testCase });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithMissingMemberNameOnHierarchicalEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithMissingMemberNameOnHierarchicalEnum_TestCaseFactory()
            {
                TestData = new[] { SecondName, FishName, YellowName }.Select(testCase => new object[] { testCase });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithMissingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithMissingMemberNameAndIgnoreCaseFlagOnFlatEnum_TestCaseFactory()
            {
                TestData = new (string Name, bool IgnoreCase)[]
                    {
                        ("four", true),
                        (FiveName, false),
                        ("SIX", true)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.IgnoreCase });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        public class WithMissingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory : IEnumerable<object[]>
        {
            private static readonly IEnumerable<object[]> TestData;

            static WithMissingMemberNameAndIgnoreCaseFlagOnHierarchicalEnum_TestCaseFactory()
            {
                TestData = new (string Name, bool IgnoreCase)[]
                    {
                        ("second", true),
                        (LizardName, false),
                        ("BROWN", true)
                    }
                    .Select(testCase => new object[] { testCase.Name, testCase.IgnoreCase });
            }

            #region Implementation of IEnumerable

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)TestData).GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region Test Types

        public readonly struct SimpleEnum : IStringEnum, IEquatable<SimpleEnum>
        {
            private readonly string _value;

            private SimpleEnum(string value)
            {
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }

            #region Operators

            public static explicit operator SimpleEnum(string value)
            {
                return new SimpleEnum(value);
            }

            public static implicit operator string(SimpleEnum value)
            {
                return value._value;
            }

            public static bool operator ==(SimpleEnum lhs, SimpleEnum rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(SimpleEnum lhs, SimpleEnum rhs)
            {
                return !(lhs == rhs);
            }

            #endregion

            public static readonly SimpleEnum One = new SimpleEnum(SimpleEnumOneValue);
            public static readonly SimpleEnum Two = new SimpleEnum(SimpleEnumTwoValue);
            public static readonly SimpleEnum Three = new SimpleEnum(SimpleEnumThreeValue);

            #region Equality members

            public bool Equals(SimpleEnum other)
            {
                return _value == other._value;
            }

            public override bool Equals(object? obj)
            {
                switch (obj)
                {
                    case SimpleEnum otherValue:
                        return Equals(otherValue);
                    case string otherValue:
                        return string.Equals(_value, otherValue, StringComparison.Ordinal);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            #endregion

            public override string ToString()
            {
                var thisValue = _value;
                var memberField = this.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(field => field.GetValue(null) is SimpleEnum memberValue && memberValue._value == thisValue);
                return memberField?.Name ?? thisValue ?? this.GetType().Name;
            }

            public string ToString(string? format, IFormatProvider? formatProvider)
            {
                return this.ToString();
            }

            public int CompareTo(object? obj)
            {
                return _value.CompareTo(obj);
            }

            #region Implementation of IConvertible

            public TypeCode GetTypeCode()
            {
                return _value.GetTypeCode();
            }

            bool IConvertible.ToBoolean(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToBoolean(provider);
            }

            byte IConvertible.ToByte(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToByte(provider);
            }

            char IConvertible.ToChar(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToChar(provider);
            }

            DateTime IConvertible.ToDateTime(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDateTime(provider);
            }

            decimal IConvertible.ToDecimal(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDecimal(provider);
            }

            double IConvertible.ToDouble(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDouble(provider);
            }

            short IConvertible.ToInt16(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt16(provider);
            }

            int IConvertible.ToInt32(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt32(provider);
            }

            long IConvertible.ToInt64(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt64(provider);
            }

            sbyte IConvertible.ToSByte(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToSByte(provider);
            }

            float IConvertible.ToSingle(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToSingle(provider);
            }

            string IConvertible.ToString(IFormatProvider? provider)
            {
                return _value.ToString(provider);
            }

            object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToType(conversionType, provider);
            }

            ushort IConvertible.ToUInt16(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt16(provider);
            }

            uint IConvertible.ToUInt32(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt32(provider);
            }

            ulong IConvertible.ToUInt64(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt64(provider);
            }

            #endregion
        }

        public readonly struct ComplexEnum : IStringEnum, IEquatable<ComplexEnum>
        {
            private readonly string _value;

            private ComplexEnum(string value)
            {
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }

            public static explicit operator ComplexEnum(string value)
            {
                return new ComplexEnum(value);
            }

            public static implicit operator string(ComplexEnum value)
            {
                return value._value;
            }

            public static readonly ComplexEnum First = new ComplexEnum(ComplexEnumFirstValue);

            public static class Animal
            {
                public static readonly ComplexEnum Bird = new ComplexEnum(BirdValue);
                public static readonly ComplexEnum Cat = new ComplexEnum(CatValue);
                public static readonly ComplexEnum Dog = new ComplexEnum(DogValue);
            }

            public static class Color
            {
                public static readonly ComplexEnum Black = new ComplexEnum(BlackValue);
                public static readonly ComplexEnum Red = new ComplexEnum(RedValue);
                public static readonly ComplexEnum Green = new ComplexEnum(GreenValue);
                public static readonly ComplexEnum Blue = new ComplexEnum(BlueValue);
            }

            #region Equality members

            public bool Equals(ComplexEnum other)
            {
                return _value == other._value;
            }

            public override bool Equals(object? obj)
            {
                switch (obj)
                {
                    case ComplexEnum otherValue:
                        return Equals(otherValue);
                    case string otherValue:
                        return string.Equals(_value, otherValue, StringComparison.Ordinal);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            public static bool operator ==(ComplexEnum left, ComplexEnum right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ComplexEnum left, ComplexEnum right)
            {
                return !left.Equals(right);
            }

            #endregion

            public override string ToString()
            {
                var thisValue = _value;
                var memberField = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Union(this.GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.Static).Where(type => type.IsClass).SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static)))
                    .FirstOrDefault(field => field.GetValue(null) is ComplexEnum memberValue && memberValue._value == thisValue);
                return memberField?.Name ?? thisValue ?? this.GetType().Name;
            }

            public string ToString(string? format, IFormatProvider? formatProvider)
            {
                return this.ToString();
            }

            public int CompareTo(object? obj)
            {
                return _value.CompareTo(obj);
            }

            #region Implementation of IConvertible

            public TypeCode GetTypeCode()
            {
                return _value.GetTypeCode();
            }

            bool IConvertible.ToBoolean(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToBoolean(provider);
            }

            byte IConvertible.ToByte(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToByte(provider);
            }

            char IConvertible.ToChar(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToChar(provider);
            }

            DateTime IConvertible.ToDateTime(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDateTime(provider);
            }

            decimal IConvertible.ToDecimal(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDecimal(provider);
            }

            double IConvertible.ToDouble(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToDouble(provider);
            }

            short IConvertible.ToInt16(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt16(provider);
            }

            int IConvertible.ToInt32(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt32(provider);
            }

            long IConvertible.ToInt64(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToInt64(provider);
            }

            sbyte IConvertible.ToSByte(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToSByte(provider);
            }

            float IConvertible.ToSingle(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToSingle(provider);
            }

            string IConvertible.ToString(IFormatProvider? provider)
            {
                return _value.ToString(provider);
            }

            object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToType(conversionType, provider);
            }

            ushort IConvertible.ToUInt16(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt16(provider);
            }

            uint IConvertible.ToUInt32(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt32(provider);
            }

            ulong IConvertible.ToUInt64(IFormatProvider? provider)
            {
                return ((IConvertible)_value).ToUInt64(provider);
            }

            #endregion
        }

        #endregion
    }
}