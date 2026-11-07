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
        private const string NullValueReason = "a null value represents no member";
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
        private const string RejectedValue = "bad";
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

        // Values of the members of the enumerations whose nested types aren't all members.
        private const string AValue = "a";
        private const string BValue = "b";
        private const string CValue = "c";
        private const string CrimsonValue = "crimson";

        // Names declared by DuplicateNameEnum and NonPublicNestedFieldEnum.
        private const string RedName = "Red";
        private const string NonPublicNestedFieldName = "Default";

        // Exception message thrown for DuplicateNameEnum.
        private const string RepeatedMemberNameMessage = "The string enumeration DataStandardizer.Core.Tests.StringEnumTests+DuplicateNameEnum has more than one member named Red.";

        // Names and values of CaseVariantNameEnum, and inputs that match them only when case is ignored, or not at all.
        private const string MixedCaseName = "Abc";
        private const string UpperCaseName = "ABC";
        private const string LowerCaseName = "abc";
        private const string LowerCaseValue = "xyz";
        private const string UpperCaseValue = "XYZ";
        private const string MixedCaseValue = "Xyz";
        private const string GhiValue = "jkl";
        private const string UvwValue = "ghi";
        private const string UnmatchedCaseVariantInput = "abd";

        // Names and values of WhiteSpaceValueEnum, and inputs with white space around them.
        private const string PlainValue = "pad";
        private const string LeadingWhiteSpaceValue = " pad";
        private const string NameWithSurroundingSpaces = " Plain ";
        private const string NameWithSurroundingTabAndLineBreak = "\tPlain\r\n";
        private const string PaddedNameWithSurroundingSpaces = " Padded ";
        private const string ValueWithSurroundingSpaces = " pad ";
        private const string ValueWithTrailingSpace = "pad ";
        private const string UpperCaseLeadingWhiteSpaceValue = " PAD";
        private const string UpperCaseValueWithSurroundingSpaces = " PAD ";
        private const string MixedCaseLeadingWhiteSpaceValue = " Pad";
        private const string ShoutedValue = "PAD";
        // Also the name Plain with a space before it.
        private const string ShadowingValue = " Plain";

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

        [Fact]
        public void IsDefinedGeneric_WithNullValue_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => StringEnum.IsDefined<SimpleEnum>(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(ValueParameterName);
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
        [MemberData(nameof(ToObjectOverloadsWithValueTheOperatorRejects))]
        public void ToObject_WithValueTheOperatorRejects_ThrowsInvalidCastException(Action toObject)
        {
            // act
            var testAction = toObject;

            // assert
            testAction.Should().ThrowExactly<InvalidCastException>();
        }

        [Theory]
        [MemberData(nameof(ToObjectOverloadsOnEnumWithoutOperator))]
        public void ToObject_OnEnumWithoutOperator_ThrowsInvalidOperationException(Action toObject)
        {
            // act
            var testAction = toObject;

            // assert
            testAction.Should().ThrowExactly<InvalidOperationException>();
        }

        [Theory]
        [InlineData(typeof(DayOfWeek), nameof(DayOfWeek.Sunday)), InlineData(typeof(UriBuilder), nameof(UriBuilder.Scheme))]
        public void TryParseNonGeneric_WithNonStringEnumType_ThrowsArgumentException(Type enumType, string value)
        {
            // act
            Action testAction = () => StringEnum.TryParse(enumType, value, out _);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
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
        public void TryParseNonGenericIgnoreCaseOverload_WithNonStringEnumType_ThrowsArgumentException(Type enumType, string value)
        {
            // act
            Action testAction = () => StringEnum.TryParse(enumType, value, false, out _);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
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

        [Fact]
        public void GetNamesGeneric_WithInstanceFieldOfEnumTypeInNestedClass_ReturnsStaticMemberNames()
        {
            // arrange
            var expectedResult = new[] { nameof(InstanceFieldEnum.A), nameof(InstanceFieldEnum.B) };

            // act
            var testResult = StringEnum.GetNames<InstanceFieldEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetNamesGeneric_WithLambdaCapturingEnumValue_ReturnsMemberNames()
        {
            // arrange
            var expectedResult = new[] { nameof(ClosureEnum.A), nameof(ClosureEnum.B) };

            // act
            var testResult = StringEnum.GetNames<ClosureEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void TryParseGeneric_WithLambdaCapturingEnumValue_ReturnsMember()
        {
            // act
            var testResult = StringEnum.TryParse(nameof(ClosureEnum.B), out ClosureEnum parsedValue);

            // assert
            testResult.Should().BeTrue();
            parsedValue.Should().Be(ClosureEnum.B);
        }

        [Fact]
        public void GetNamesGeneric_WithNonPublicStaticFieldInNestedClass_OmitsNonPublicField()
        {
            // arrange
            var expectedResult = new[] { nameof(NonPublicNestedFieldEnum.A) };

            // act
            var testResult = StringEnum.GetNames<NonPublicNestedFieldEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void IsDefinedGeneric_WithNameOfNonPublicStaticFieldInNestedClass_ReturnsFalse()
        {
            // act
            var testResult = StringEnum.IsDefined<NonPublicNestedFieldEnum>(NonPublicNestedFieldName);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetNamesGeneric_WithMemberTwoLevelsDown_ReturnsMemberNamesAtEveryDepth()
        {
            // arrange
            var expectedResult = new[] { nameof(DeeplyNestedEnum.A), nameof(DeeplyNestedEnum.Outer.B), nameof(DeeplyNestedEnum.Outer.Inner.C) };

            // act
            var testResult = StringEnum.GetNames<DeeplyNestedEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void ParseGeneric_WithNameOfMemberTwoLevelsDown_ReturnsMember()
        {
            // act
            var testResult = StringEnum.Parse<DeeplyNestedEnum>(nameof(DeeplyNestedEnum.Outer.Inner.C));

            // assert
            testResult.Should().Be(DeeplyNestedEnum.Outer.Inner.C);
        }

        [Fact]
        public void GetNamesGeneric_WithFieldInGenericNestedClass_OmitsGenericClassField()
        {
            // arrange
            var expectedResult = new[] { nameof(GenericNestedClassEnum.A) };

            // act
            var testResult = StringEnum.GetNames<GenericNestedClassEnum>();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetNamesGeneric_AfterAnotherTypeFailsToCache_ReturnsNamesOfRequestedType()
        {
            // arrange
            var expectedResult = new[] { nameof(SimpleEnum.One), nameof(SimpleEnum.Two), nameof(SimpleEnum.Three) };

            // act
            var firstResult = StringEnum.GetNames<SimpleEnum>();
            var failure = Record.Exception(() => StringEnum.GetNames<HashThrowingEnum>());
            var testResult = StringEnum.GetNames<SimpleEnum>();

            // assert
            firstResult.Should().BeEquivalentTo(expectedResult);
            failure.Should().BeOfType<NotSupportedException>();
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [MemberData(nameof(MembersOfEnumWithRepeatedMemberName))]
        public void Members_OfEnumWithRepeatedMemberName_ThrowInvalidOperationException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<InvalidOperationException>().Which.Message.Should().Be(RepeatedMemberNameMessage);
        }

        [Theory]
        [MemberData(nameof(TryParseOverloadsWithNullEnumType))]
        public void TryParseNonGeneric_WithNullEnumType_ThrowsArgumentNullException(Action tryParse)
        {
            // act
            var testAction = tryParse;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [MemberData(nameof(NonGenericMembersOnTypesThatAreNotStringEnumerations))]
        public void NonGenericMembers_OnTypeThatIsNotStringEnumeration_ThrowArgumentException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage(NotStringEnumerationMessage).WithParameterName(EnumTypeParameterName);
        }

        [Theory]
        [MemberData(nameof(NonGenericMembersWithNullEnumTypeAndNullValue))]
        public void NonGenericMembers_WithNullEnumTypeAndNullValue_ThrowArgumentNullExceptionNamingSameParameterAsSystemEnum(Action member, string expectedParameterName)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(expectedParameterName);
        }

        [Theory]
        [MemberData(nameof(TryParseOverloadsWithNullValueAndInvalidEnumType))]
        public void TryParseNonGeneric_WithNullValueAndInvalidEnumType_ReturnsFalse(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeFalse(NullValueReason);
        }

        [Theory]
        [MemberData(nameof(ParseIgnoreCaseOverloadsOnCaseVariantNameEnum))]
        public void ParseIgnoreCaseOverloads_OnEnumWithNamesDifferingOnlyByCase_ReturnsMatchingMember(Func<string, object> parse, string value, CaseVariantNameEnum expectedResult)
        {
            // act
            var testResult = parse(value);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseCaseSensitiveOverloadsOnCaseVariantNameEnum))]
        public void ParseCaseSensitiveOverloads_OnEnumWithNamesDifferingOnlyByCase_ReturnsExactMatch(Func<string, object> parse, string value, CaseVariantNameEnum expectedResult)
        {
            // act
            var testResult = parse(value);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseOverloadsOnCaseVariantNameEnumWithUnmatchedInput))]
        public void ParseOverloads_OnEnumWithNamesDifferingOnlyByCaseWithUnmatchedInput_ThrowsArgumentException(Action parse)
        {
            // act
            var testAction = parse;

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName(ValueParameterName);
        }

        [Theory]
        [MemberData(nameof(TryParseIgnoreCaseOverloadsOnCaseVariantNameEnum))]
        public void TryParseIgnoreCaseOverloads_OnEnumWithNamesDifferingOnlyByCase_ReturnsTrueAndMatchingMember(Func<string, (bool Success, object? Result)> tryParse, string value, CaseVariantNameEnum expectedResult)
        {
            // act
            var testResult = tryParse(value);

            // assert
            testResult.Success.Should().BeTrue();
            testResult.Result.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(TryParseCaseSensitiveOverloadsOnCaseVariantNameEnum))]
        public void TryParseCaseSensitiveOverloads_OnEnumWithNamesDifferingOnlyByCase_ReturnsTrueAndExactMatch(Func<string, (bool Success, object? Result)> tryParse, string value, CaseVariantNameEnum expectedResult)
        {
            // act
            var testResult = tryParse(value);

            // assert
            testResult.Success.Should().BeTrue();
            testResult.Result.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(TryParseOverloadsOnCaseVariantNameEnumWithUnmatchedInput))]
        public void TryParseOverloads_OnEnumWithNamesDifferingOnlyByCaseWithUnmatchedInput_ReturnsFalse(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ParseOverloadsOnWhiteSpaceValueEnum))]
        public void ParseOverloads_WithWhiteSpaceAroundNameOrValue_ReturnsMatchingMember(Func<string, object> parse, string value, WhiteSpaceValueEnum expectedResult)
        {
            // act
            var testResult = parse(value);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(TryParseOverloadsOnWhiteSpaceValueEnum))]
        public void TryParseOverloads_WithWhiteSpaceAroundNameOrValue_ReturnsTrueAndMatchingMember(Func<string, (bool Success, object? Result)> tryParse, string value, WhiteSpaceValueEnum expectedResult)
        {
            // act
            var testResult = tryParse(value);

            // assert
            testResult.Success.Should().BeTrue();
            testResult.Result.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(TryParseOverloadsOnWhiteSpaceValueEnumWithOnlyWhiteSpace))]
        public void TryParseOverloads_WithOnlyWhiteSpaceOnEnumWithEmptyValue_ReturnsFalse(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ParseIgnoreCaseOverloadsOnWhiteSpaceValueEnum))]
        public void ParseIgnoreCaseOverloads_WithValueMatchingAsGivenIgnoringCase_ReturnsThatMember(Func<string, object> parse, string value, WhiteSpaceValueEnum expectedResult)
        {
            // act
            var testResult = parse(value);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [MemberData(nameof(TryParseIgnoreCaseOverloadsOnWhiteSpaceValueEnum))]
        public void TryParseIgnoreCaseOverloads_WithValueMatchingAsGivenIgnoringCase_ReturnsTrueAndThatMember(Func<string, (bool Success, object? Result)> tryParse, string value, WhiteSpaceValueEnum expectedResult)
        {
            // act
            var testResult = tryParse(value);

            // assert
            testResult.Success.Should().BeTrue();
            testResult.Result.Should().Be(expectedResult);
        }

        #region Test Data

        // Each input with the member it resolves to when ignoring case: names are compared before values, an exact (ordinal) match wins, otherwise the first member that matches ignoring case.
        private static readonly (string Value, CaseVariantNameEnum ExpectedResult)[] CaseVariantNameEnumIgnoreCaseCases =
        {
            (MixedCaseName, CaseVariantNameEnum.Abc),
            (UpperCaseName, CaseVariantNameEnum.ABC),
            (LowerCaseName, CaseVariantNameEnum.Abc),
            (LowerCaseValue, CaseVariantNameEnum.Abc),
            (UpperCaseValue, CaseVariantNameEnum.ABC),
            (MixedCaseValue, CaseVariantNameEnum.Abc),
            (UvwValue, CaseVariantNameEnum.Ghi),
        };

        // Each input with the member it resolves to when case is regarded: only an exact (ordinal) name or value matches.
        private static readonly (string Value, CaseVariantNameEnum ExpectedResult)[] CaseVariantNameEnumCaseSensitiveCases =
        {
            (MixedCaseName, CaseVariantNameEnum.Abc),
            (UpperCaseName, CaseVariantNameEnum.ABC),
            (LowerCaseValue, CaseVariantNameEnum.Abc),
            (UpperCaseValue, CaseVariantNameEnum.ABC),
            (UvwValue, CaseVariantNameEnum.Uvw),
        };

        public static TheoryData<Func<string, object>, string, CaseVariantNameEnum> ParseIgnoreCaseOverloadsOnCaseVariantNameEnum =>
            CombineOverloadsWithCases(new Func<string, object>[]
            {
                value => StringEnum.Parse(typeof(CaseVariantNameEnum), value, true),
                value => StringEnum.Parse<CaseVariantNameEnum>(value, true),
            }, CaseVariantNameEnumIgnoreCaseCases);

        public static TheoryData<Func<string, object>, string, CaseVariantNameEnum> ParseCaseSensitiveOverloadsOnCaseVariantNameEnum =>
            CombineOverloadsWithCases(new Func<string, object>[]
            {
                value => StringEnum.Parse(typeof(CaseVariantNameEnum), value),
                value => StringEnum.Parse(typeof(CaseVariantNameEnum), value, false),
                value => StringEnum.Parse<CaseVariantNameEnum>(value),
                value => StringEnum.Parse<CaseVariantNameEnum>(value, false),
            }, CaseVariantNameEnumCaseSensitiveCases);

        public static TheoryData<Action> ParseOverloadsOnCaseVariantNameEnumWithUnmatchedInput => new TheoryData<Action>
        {
            () => StringEnum.Parse(typeof(CaseVariantNameEnum), UnmatchedCaseVariantInput, true),
            () => StringEnum.Parse<CaseVariantNameEnum>(UnmatchedCaseVariantInput, true),
            () => StringEnum.Parse(typeof(CaseVariantNameEnum), LowerCaseName),
            () => StringEnum.Parse(typeof(CaseVariantNameEnum), LowerCaseName, false),
            () => StringEnum.Parse<CaseVariantNameEnum>(LowerCaseName),
            () => StringEnum.Parse<CaseVariantNameEnum>(LowerCaseName, false),
        };

        public static TheoryData<Func<string, (bool Success, object? Result)>, string, CaseVariantNameEnum> TryParseIgnoreCaseOverloadsOnCaseVariantNameEnum =>
            CombineOverloadsWithCases(new Func<string, (bool Success, object? Result)>[]
            {
                value => (StringEnum.TryParse(typeof(CaseVariantNameEnum), value, true, out var result), result),
                value => (StringEnum.TryParse<CaseVariantNameEnum>(value, true, out var result), result),
            }, CaseVariantNameEnumIgnoreCaseCases);

        public static TheoryData<Func<string, (bool Success, object? Result)>, string, CaseVariantNameEnum> TryParseCaseSensitiveOverloadsOnCaseVariantNameEnum =>
            CombineOverloadsWithCases(new Func<string, (bool Success, object? Result)>[]
            {
                value => (StringEnum.TryParse(typeof(CaseVariantNameEnum), value, out var result), result),
                value => (StringEnum.TryParse(typeof(CaseVariantNameEnum), value, false, out var result), result),
                value => (StringEnum.TryParse<CaseVariantNameEnum>(value, out var result), result),
                value => (StringEnum.TryParse<CaseVariantNameEnum>(value, false, out var result), result),
            }, CaseVariantNameEnumCaseSensitiveCases);

        public static TheoryData<Func<bool>> TryParseOverloadsOnCaseVariantNameEnumWithUnmatchedInput => new TheoryData<Func<bool>>
        {
            () => StringEnum.TryParse(typeof(CaseVariantNameEnum), UnmatchedCaseVariantInput, true, out _),
            () => StringEnum.TryParse<CaseVariantNameEnum>(UnmatchedCaseVariantInput, true, out _),
            () => StringEnum.TryParse(typeof(CaseVariantNameEnum), LowerCaseName, out _),
            () => StringEnum.TryParse(typeof(CaseVariantNameEnum), LowerCaseName, false, out _),
            () => StringEnum.TryParse<CaseVariantNameEnum>(LowerCaseName, out _),
            () => StringEnum.TryParse<CaseVariantNameEnum>(LowerCaseName, false, out _),
            () => StringEnum.TryParse(typeof(CaseVariantNameEnum), null, out _),
            () => StringEnum.TryParse(typeof(CaseVariantNameEnum), null, true, out _),
            () => StringEnum.TryParse<CaseVariantNameEnum>(null, out _),
            () => StringEnum.TryParse<CaseVariantNameEnum>(null, true, out _),
        };

        // Each input with the member it resolves to, whether or not case is ignored: names are compared before values, and white space around a name is ignored; a value is
        // compared as given first, then without the white space around it, and an exact match wins over a match ignoring case.
        private static readonly (string Value, WhiteSpaceValueEnum ExpectedResult)[] WhiteSpaceValueEnumCases =
        {
            (NameWithSurroundingSpaces, WhiteSpaceValueEnum.Plain),
            (NameWithSurroundingTabAndLineBreak, WhiteSpaceValueEnum.Plain),
            (PaddedNameWithSurroundingSpaces, WhiteSpaceValueEnum.Padded),
            (ShadowingValue, WhiteSpaceValueEnum.Plain),
            (PlainValue, WhiteSpaceValueEnum.Plain),
            (LeadingWhiteSpaceValue, WhiteSpaceValueEnum.Padded),
            (ValueWithSurroundingSpaces, WhiteSpaceValueEnum.Plain),
            (ValueWithTrailingSpace, WhiteSpaceValueEnum.Plain),
            (UpperCaseLeadingWhiteSpaceValue, WhiteSpaceValueEnum.Shouted),
            (UpperCaseValueWithSurroundingSpaces, WhiteSpaceValueEnum.Shouted),
        };

        // When no value matches exactly, as given or without white space, a match ignoring case on the value as given wins over one on the value without white space.
        private static readonly (string Value, WhiteSpaceValueEnum ExpectedResult)[] WhiteSpaceValueEnumIgnoreCaseCases =
        {
            (MixedCaseLeadingWhiteSpaceValue, WhiteSpaceValueEnum.Padded),
        };

        public static TheoryData<Func<bool>> TryParseOverloadsOnWhiteSpaceValueEnumWithOnlyWhiteSpace => new TheoryData<Func<bool>>
        {
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), SpaceValue, out _),
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), SpaceValue, false, out _),
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), SpaceValue, true, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(SpaceValue, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(SpaceValue, false, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(SpaceValue, true, out _),
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), TabValue, out _),
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), TabValue, false, out _),
            () => StringEnum.TryParse(typeof(WhiteSpaceValueEnum), TabValue, true, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(TabValue, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(TabValue, false, out _),
            () => StringEnum.TryParse<WhiteSpaceValueEnum>(TabValue, true, out _),
        };

        public static TheoryData<Func<string, object>, string, WhiteSpaceValueEnum> ParseOverloadsOnWhiteSpaceValueEnum =>
            CombineOverloadsWithCases(new Func<string, object>[]
            {
                value => StringEnum.Parse(typeof(WhiteSpaceValueEnum), value),
                value => StringEnum.Parse(typeof(WhiteSpaceValueEnum), value, false),
                value => StringEnum.Parse(typeof(WhiteSpaceValueEnum), value, true),
                value => StringEnum.Parse<WhiteSpaceValueEnum>(value),
                value => StringEnum.Parse<WhiteSpaceValueEnum>(value, false),
                value => StringEnum.Parse<WhiteSpaceValueEnum>(value, true),
            }, WhiteSpaceValueEnumCases);

        public static TheoryData<Func<string, (bool Success, object? Result)>, string, WhiteSpaceValueEnum> TryParseOverloadsOnWhiteSpaceValueEnum =>
            CombineOverloadsWithCases(new Func<string, (bool Success, object? Result)>[]
            {
                value => (StringEnum.TryParse(typeof(WhiteSpaceValueEnum), value, out var result), result),
                value => (StringEnum.TryParse(typeof(WhiteSpaceValueEnum), value, false, out var result), result),
                value => (StringEnum.TryParse(typeof(WhiteSpaceValueEnum), value, true, out var result), result),
                value => (StringEnum.TryParse<WhiteSpaceValueEnum>(value, out var result), result),
                value => (StringEnum.TryParse<WhiteSpaceValueEnum>(value, false, out var result), result),
                value => (StringEnum.TryParse<WhiteSpaceValueEnum>(value, true, out var result), result),
            }, WhiteSpaceValueEnumCases);

        public static TheoryData<Func<string, object>, string, WhiteSpaceValueEnum> ParseIgnoreCaseOverloadsOnWhiteSpaceValueEnum =>
            CombineOverloadsWithCases(new Func<string, object>[]
            {
                value => StringEnum.Parse(typeof(WhiteSpaceValueEnum), value, true),
                value => StringEnum.Parse<WhiteSpaceValueEnum>(value, true),
            }, WhiteSpaceValueEnumIgnoreCaseCases);

        public static TheoryData<Func<string, (bool Success, object? Result)>, string, WhiteSpaceValueEnum> TryParseIgnoreCaseOverloadsOnWhiteSpaceValueEnum =>
            CombineOverloadsWithCases(new Func<string, (bool Success, object? Result)>[]
            {
                value => (StringEnum.TryParse(typeof(WhiteSpaceValueEnum), value, true, out var result), result),
                value => (StringEnum.TryParse<WhiteSpaceValueEnum>(value, true, out var result), result),
            }, WhiteSpaceValueEnumIgnoreCaseCases);

        private static TheoryData<TOverload, string, TExpected> CombineOverloadsWithCases<TOverload, TExpected>(IEnumerable<TOverload> overloads, IEnumerable<(string Value, TExpected ExpectedResult)> testCases)
        {
            var theoryData = new TheoryData<TOverload, string, TExpected>();
            foreach (var overload in overloads)
            {
                foreach (var testCase in testCases)
                {
                    theoryData.Add(overload, testCase.Value, testCase.ExpectedResult);
                }
            }

            return theoryData;
        }

        public static TheoryData<Action> ToObjectOverloadsWithValueTheOperatorRejects => new TheoryData<Action>
        {
            () => StringEnum.ToObject(typeof(RejectingEnum), RejectedValue),
            () => StringEnum.ToObject<RejectingEnum>(RejectedValue),
        };

        public static TheoryData<Action> ToObjectOverloadsOnEnumWithoutOperator => new TheoryData<Action>
        {
            () => StringEnum.ToObject(typeof(NoOperatorEnum), AValue),
            () => StringEnum.ToObject<NoOperatorEnum>(AValue),
        };

        public static TheoryData<Action> MembersOfEnumWithRepeatedMemberName => new TheoryData<Action>
        {
            () => StringEnum.GetName(typeof(DuplicateNameEnum), DuplicateNameEnum.Red),
            () => StringEnum.GetName(DuplicateNameEnum.Red),
            () => StringEnum.GetNames(typeof(DuplicateNameEnum)),
            () => StringEnum.GetNames<DuplicateNameEnum>(),
            () => StringEnum.GetValues(typeof(DuplicateNameEnum)),
            () => StringEnum.GetValues<DuplicateNameEnum>(),
            () => StringEnum.IsDefined(typeof(DuplicateNameEnum), RedName),
            () => StringEnum.IsDefined<DuplicateNameEnum>(RedName),
            () => StringEnum.Parse(typeof(DuplicateNameEnum), RedName),
            () => StringEnum.Parse(typeof(DuplicateNameEnum), RedName, true),
            () => StringEnum.Parse<DuplicateNameEnum>(RedName),
            () => StringEnum.Parse<DuplicateNameEnum>(RedName, true),
            () => StringEnum.TryParse(typeof(DuplicateNameEnum), RedName, out _),
            () => StringEnum.TryParse(typeof(DuplicateNameEnum), RedName, true, out _),
            () => StringEnum.TryParse<DuplicateNameEnum>(RedName, out _),
            () => StringEnum.TryParse<DuplicateNameEnum>(RedName, true, out _),
        };

        public static TheoryData<Action> TryParseOverloadsWithNullEnumType => new TheoryData<Action>
        {
            () => StringEnum.TryParse(null!, AValue, out _),
            () => StringEnum.TryParse(null!, AValue, true, out _),
        };

        // Each non-generic member, given a type that isn't a string enumeration and a value that isn't null.
        public static TheoryData<Action> NonGenericMembersOnTypesThatAreNotStringEnumerations
        {
            get
            {
                var theoryData = new TheoryData<Action>();
                var cases = new (Type EnumType, object Value)[]
                {
                    (typeof(ForeignInterfaceEnum), ForeignInterfaceEnum.A),
                    (typeof(OpenGenericEnum<>), AValue),
                    (typeof(ClassEnum), ClassEnum.A),
                };
                foreach (var (enumType, value) in cases)
                {
                    theoryData.Add(() => StringEnum.GetName(enumType, value));
                    theoryData.Add(() => StringEnum.GetNames(enumType));
                    theoryData.Add(() => StringEnum.GetValues(enumType));
                    theoryData.Add(() => StringEnum.IsDefined(enumType, AValue));
                    theoryData.Add(() => StringEnum.Parse(enumType, AValue));
                    theoryData.Add(() => StringEnum.Parse(enumType, AValue, true));
                    theoryData.Add(() => StringEnum.ToObject(enumType, AValue));
                    theoryData.Add(() => StringEnum.TryParse(enumType, AValue, out _));
                    theoryData.Add(() => StringEnum.TryParse(enumType, AValue, true, out _));
                }

                return theoryData;
            }
        }

        // The parameter System.Enum names when both are null: GetName and IsDefined check enumType first, Parse and ToObject check value first.
        public static TheoryData<Action, string> NonGenericMembersWithNullEnumTypeAndNullValue => new TheoryData<Action, string>
        {
            { () => StringEnum.GetName(null!, null!), EnumTypeParameterName },
            { () => StringEnum.IsDefined(null!, null!), EnumTypeParameterName },
            { () => StringEnum.Parse(null!, null!), ValueParameterName },
            { () => StringEnum.Parse(null!, null!, true), ValueParameterName },
            { () => StringEnum.ToObject(null!, null!), ValueParameterName },
        };

        // Like Enum.TryParse, a null value fails before enumType is checked.
        public static TheoryData<Func<bool>> TryParseOverloadsWithNullValueAndInvalidEnumType => new TheoryData<Func<bool>>
        {
            () => StringEnum.TryParse(null!, null, out _),
            () => StringEnum.TryParse(null!, null, true, out _),
            () => StringEnum.TryParse(typeof(DayOfWeek), null, out _),
            () => StringEnum.TryParse(typeof(DayOfWeek), null, true, out _),
        };

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

        // A public nested class holds an instance field of the enumeration's type, which isn't a member.
        public readonly struct InstanceFieldEnum : IStringEnum
        {
            private readonly string _value;

            private InstanceFieldEnum(string value)
            {
                _value = value;
            }

            public static explicit operator InstanceFieldEnum(string value) => new InstanceFieldEnum(value);

            public static implicit operator string(InstanceFieldEnum value) => value._value;

            public static readonly InstanceFieldEnum A = new InstanceFieldEnum(AValue);
            public static readonly InstanceFieldEnum B = new InstanceFieldEnum(BValue);

            public class Holder
            {
                public InstanceFieldEnum Value = A;
            }

            public override bool Equals(object? obj) => obj is InstanceFieldEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // A lambda captures a value of the enumeration, so the compiler generates a nested closure class with an instance field of its type.
        public readonly struct ClosureEnum : IStringEnum
        {
            private readonly string _value;

            private ClosureEnum(string value)
            {
                _value = value;
            }

            public static explicit operator ClosureEnum(string value) => new ClosureEnum(value);

            public static implicit operator string(ClosureEnum value) => value._value;

            public static readonly ClosureEnum A = new ClosureEnum(AValue);
            public static readonly ClosureEnum B = new ClosureEnum(BValue);

            private static readonly ClosureEnum[] AllMembers = { A, B };

            public bool IsMember()
            {
                var self = this;
                return AllMembers.Any(member => member._value == self._value);
            }

            public override bool Equals(object? obj) => obj is ClosureEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // A non-public nested class holds a non-public static field of the enumeration's type, which isn't a member.
        public readonly struct NonPublicNestedFieldEnum : IStringEnum
        {
            private readonly string _value;

            private NonPublicNestedFieldEnum(string value)
            {
                _value = value;
            }

            public static explicit operator NonPublicNestedFieldEnum(string value) => new NonPublicNestedFieldEnum(value);

            public static implicit operator string(NonPublicNestedFieldEnum value) => value._value;

            public static readonly NonPublicNestedFieldEnum A = new NonPublicNestedFieldEnum(AValue);

            public static NonPublicNestedFieldEnum Fallback => Cache.Default;

            private static class Cache
            {
                internal static readonly NonPublicNestedFieldEnum Default = A;
            }

            public override bool Equals(object? obj) => obj is NonPublicNestedFieldEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // Members are declared on the struct, one level down and two levels down.
        public readonly struct DeeplyNestedEnum : IStringEnum
        {
            private readonly string _value;

            private DeeplyNestedEnum(string value)
            {
                _value = value;
            }

            public static explicit operator DeeplyNestedEnum(string value) => new DeeplyNestedEnum(value);

            public static implicit operator string(DeeplyNestedEnum value) => value._value;

            public static readonly DeeplyNestedEnum A = new DeeplyNestedEnum(AValue);

            public static class Outer
            {
                public static readonly DeeplyNestedEnum B = new DeeplyNestedEnum(BValue);

                public static class Inner
                {
                    public static readonly DeeplyNestedEnum C = new DeeplyNestedEnum(CValue);
                }
            }

            public override bool Equals(object? obj) => obj is DeeplyNestedEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // Two members share the name Red, which makes the enumeration malformed.
        public readonly struct DuplicateNameEnum : IStringEnum
        {
            private readonly string _value;

            private DuplicateNameEnum(string value)
            {
                _value = value;
            }

            public static explicit operator DuplicateNameEnum(string value) => new DuplicateNameEnum(value);

            public static implicit operator string(DuplicateNameEnum value) => value._value;

            public static readonly DuplicateNameEnum Red = new DuplicateNameEnum(RedValue);

            public static class Color
            {
                public static readonly DuplicateNameEnum Red = new DuplicateNameEnum(CrimsonValue);
            }

            public override bool Equals(object? obj) => obj is DuplicateNameEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // Two member names, and two member values, differ only by case, and one member's value matches another member's name ignoring case.
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public readonly struct CaseVariantNameEnum : IStringEnum
        {
            private readonly string _value;

            private CaseVariantNameEnum(string value)
            {
                _value = value;
            }

            public static explicit operator CaseVariantNameEnum(string value) => new CaseVariantNameEnum(value);

            public static implicit operator string(CaseVariantNameEnum value) => value._value;

            public static readonly CaseVariantNameEnum Abc = new CaseVariantNameEnum(LowerCaseValue);

            public static readonly CaseVariantNameEnum ABC = new CaseVariantNameEnum(UpperCaseValue);

            public static readonly CaseVariantNameEnum Ghi = new CaseVariantNameEnum(GhiValue);

            // Its value matches the name Ghi ignoring case.
            public static readonly CaseVariantNameEnum Uvw = new CaseVariantNameEnum(UvwValue);

            public override bool Equals(object? obj) => obj is CaseVariantNameEnum other && string.Equals(_value, other._value, StringComparison.Ordinal);

            public override int GetHashCode() => _value.GetHashCode();

            public override string ToString() => _value;

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // One member's value is another member's value with white space before it, one differs from that by case, one is another member's name with white space before it,
        // and one is the empty string.
        public readonly struct WhiteSpaceValueEnum : IStringEnum
        {
            private readonly string _value;

            private WhiteSpaceValueEnum(string value)
            {
                _value = value;
            }

            public static explicit operator WhiteSpaceValueEnum(string value) => new WhiteSpaceValueEnum(value);

            public static implicit operator string(WhiteSpaceValueEnum value) => value._value;

            public static readonly WhiteSpaceValueEnum Plain = new WhiteSpaceValueEnum(PlainValue);

            public static readonly WhiteSpaceValueEnum Padded = new WhiteSpaceValueEnum(LeadingWhiteSpaceValue);

            public static readonly WhiteSpaceValueEnum Shouted = new WhiteSpaceValueEnum(ShoutedValue);

            public static readonly WhiteSpaceValueEnum Shadowed = new WhiteSpaceValueEnum(ShadowingValue);

            public static readonly WhiteSpaceValueEnum Blank = new WhiteSpaceValueEnum(EmptyValue);

            public override bool Equals(object? obj) => obj is WhiteSpaceValueEnum other && string.Equals(_value, other._value, StringComparison.Ordinal);

            public override int GetHashCode() => _value.GetHashCode();

            public override string ToString() => _value;

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // A public nested generic class declares a field of the enumeration's type, which has no single value, so it isn't a member.
        public readonly struct GenericNestedClassEnum : IStringEnum
        {
            private readonly string _value;

            private GenericNestedClassEnum(string value)
            {
                _value = value;
            }

            public static explicit operator GenericNestedClassEnum(string value) => new GenericNestedClassEnum(value);

            public static implicit operator string(GenericNestedClassEnum value) => value._value;

            public static readonly GenericNestedClassEnum A = new GenericNestedClassEnum(AValue);

            public static class Of<T>
            {
                public static readonly GenericNestedClassEnum B = new GenericNestedClassEnum(BValue);
            }

            public override bool Equals(object? obj) => obj is GenericNestedClassEnum other && _value == other._value;

            public override int GetHashCode() => _value.GetHashCode();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // Its members can't be hashed, so StringEnum fails while it caches them.
        public readonly struct HashThrowingEnum : IStringEnum
        {
            private readonly string _value;

            private HashThrowingEnum(string value)
            {
                _value = value;
            }

            public static explicit operator HashThrowingEnum(string value) => new HashThrowingEnum(value);

            public static implicit operator string(HashThrowingEnum value) => value._value;

            public static readonly HashThrowingEnum A = new HashThrowingEnum(AValue);

            public override bool Equals(object? obj) => obj is HashThrowingEnum other && _value == other._value;

            public override int GetHashCode() => throw new NotSupportedException();

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // Its conversion from string rejects every value, as a conversion operator does for a value the enumeration doesn't accept.
        public readonly struct RejectingEnum : IStringEnum
        {
            private readonly string _value;

            private RejectingEnum(string value)
            {
                _value = value;
            }

            public static explicit operator RejectingEnum(string value) => throw new InvalidCastException();

            public static implicit operator string(RejectingEnum value) => value._value;

            public static readonly RejectingEnum A = new RejectingEnum(AValue);

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // It has a public constructor that takes a string, but no conversion from string.
        public readonly struct NoOperatorEnum : IStringEnum
        {
            private readonly string _value;

            public NoOperatorEnum(string value)
            {
                _value = value;
            }

            public static implicit operator string(NoOperatorEnum value) => value._value;

            public static readonly NoOperatorEnum A = new NoOperatorEnum(AValue);

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // A generic definition has open type parameters, so its static fields have no values to read.
        public readonly struct OpenGenericEnum<T> : IStringEnum
        {
            private readonly string _value;

            private OpenGenericEnum(string value)
            {
                _value = value;
            }

            public static explicit operator OpenGenericEnum<T>(string value) => new OpenGenericEnum<T>(value);

            public static implicit operator string(OpenGenericEnum<T> value) => value._value;

            public static readonly OpenGenericEnum<T> A = new OpenGenericEnum<T>(AValue);

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // A class that implements IStringEnum isn't a string enumeration, which has to be a struct.
        public sealed class ClassEnum : IStringEnum
        {
            private readonly string _value;

            private ClassEnum(string value)
            {
                _value = value;
            }

            public static explicit operator ClassEnum(string value) => new ClassEnum(value);

            public static implicit operator string(ClassEnum value) => value._value;

            public static readonly ClassEnum A = new ClassEnum(AValue);

            public int CompareTo(object? obj) => _value.CompareTo(obj);

            public TypeCode GetTypeCode() => _value.GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
            byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
            char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
            DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
            decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
            double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
            short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
            int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
            long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
            sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
            float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
            string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
            object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
            ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
            uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
            ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);
        }

        // An unrelated interface that shares its name with DataStandardizer.Core.IStringEnum.
        public static class Foreign
        {
            public interface IStringEnum
            {
            }
        }

        // It implements Foreign.IStringEnum, not DataStandardizer.Core.IStringEnum, so it isn't a string enumeration.
        public readonly struct ForeignInterfaceEnum : Foreign.IStringEnum
        {
            private readonly string _value;

            private ForeignInterfaceEnum(string value)
            {
                _value = value;
            }

            public static explicit operator ForeignInterfaceEnum(string value) => new ForeignInterfaceEnum(value);

            public static implicit operator string(ForeignInterfaceEnum value) => value._value;

            public static readonly ForeignInterfaceEnum A = new ForeignInterfaceEnum(AValue);
        }

        #endregion
    }
}