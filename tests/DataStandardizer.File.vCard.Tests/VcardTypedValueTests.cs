using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardTypedValueTests
    {
        private const string TextValue = "a, b";
        private const string UriValue = "http://example.com/";

        // ref. RFC 6350 §4.3.1: a date, and §4.7: a UTC offset.
        private const string DateText = "19960415";
        private const string UtcOffsetText = "-0500";

        private static readonly VcardDateAndOrTime DateValue = VcardDateAndOrTime.Parse(DateText.AsSpan(), VcardValueFormat.Text);
        private static readonly VcardUtcOffset UtcOffsetValue = VcardUtcOffset.Parse(UtcOffsetText.AsSpan(), VcardValueFormat.Text);

        public static TheoryData<VcardTypedValue, VcardValueType> ValueTypeData()
        {
            return new TheoryData<VcardTypedValue, VcardValueType>
            {
                { new VcardTypedValue(TextValue), VcardValueType.Text },
                { new VcardTypedValue(new Uri(UriValue)), VcardValueType.Uri },
                { new VcardTypedValue(DateValue), VcardValueType.DateAndOrTime },
                { new VcardTypedValue(UtcOffsetValue), VcardValueType.UtcOffset }
            };
        }

        [Theory]
        [MemberData(nameof(ValueTypeData))]
        public void ValueType_Value_SaysWhatItHolds(VcardTypedValue value, VcardValueType expected)
        {
            // arrange
            var subject = value;

            // act
            var testResult = subject.ValueType;

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<VcardTypedValue, string> LexicalFormData()
        {
            return new TheoryData<VcardTypedValue, string>
            {
                { new VcardTypedValue(TextValue), "a\\, b" },
                { new VcardTypedValue(new Uri(UriValue)), UriValue },
                { new VcardTypedValue(DateValue), DateText },
                { new VcardTypedValue(UtcOffsetValue), UtcOffsetText }
            };
        }

        [Theory]
        [MemberData(nameof(LexicalFormData))]
        public void ToString_Value_GivesItAsInAContentLine(VcardTypedValue value, string expected)
        {
            // arrange
            var subject = value;

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Func<VcardTypedValue, object>, VcardTypedValue, object> HeldValueData()
        {
            return new TheoryData<Func<VcardTypedValue, object>, VcardTypedValue, object>
            {
                { value => value.Text, new VcardTypedValue(TextValue), TextValue },
                { value => value.Uri.OriginalString, new VcardTypedValue(new Uri(UriValue)), UriValue },
                { value => value.DateAndOrTime, new VcardTypedValue(DateValue), DateValue },
                { value => value.UtcOffset, new VcardTypedValue(UtcOffsetValue), UtcOffsetValue }
            };
        }

        [Theory]
        [MemberData(nameof(HeldValueData))]
        public void Member_OfTheTypeHeld_GivesTheValue(Func<VcardTypedValue, object> member, VcardTypedValue value, object expected)
        {
            // arrange
            var subject = value;

            // act
            var testResult = member(subject);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Func<VcardTypedValue, object>, VcardTypedValue> WrongMemberData()
        {
            return new TheoryData<Func<VcardTypedValue, object>, VcardTypedValue>
            {
                { value => value.Text, new VcardTypedValue(new Uri(UriValue)) },
                { value => value.Uri, new VcardTypedValue(TextValue) },
                { value => value.DateAndOrTime, new VcardTypedValue(TextValue) },
                { value => value.UtcOffset, new VcardTypedValue(TextValue) },
                { value => value.Text, default(VcardTypedValue) },
                { value => value.Uri, default(VcardTypedValue) },
                { value => value.DateAndOrTime, default(VcardTypedValue) },
                { value => value.UtcOffset, default(VcardTypedValue) },
                { value => value.ValueType, default(VcardTypedValue) }
            };
        }

        [Theory]
        [MemberData(nameof(WrongMemberData))]
        public void Member_NotOfTheTypeHeld_ThrowsInvalidOperationException(Func<VcardTypedValue, object> member, VcardTypedValue value)
        {
            // arrange
            var subject = value;

            // act
            Action testAction = () => member(subject);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<VcardTypedValue, VcardTypedValue, bool> EqualityData()
        {
            return new TheoryData<VcardTypedValue, VcardTypedValue, bool>
            {
                { new VcardTypedValue(TextValue), new VcardTypedValue(TextValue), true },
                { new VcardTypedValue(TextValue), new VcardTypedValue("b"), false },
                { new VcardTypedValue(new Uri(UriValue)), new VcardTypedValue(new Uri(UriValue)), true },
                { new VcardTypedValue(new Uri(UriValue)), new VcardTypedValue(new Uri("http://example.org/")), false },
                { new VcardTypedValue(new Uri("http://EXAMPLE.com/")), new VcardTypedValue(new Uri(UriValue)), false },
                { new VcardTypedValue(DateValue), new VcardTypedValue(DateValue), true },
                { new VcardTypedValue(UtcOffsetValue), new VcardTypedValue(UtcOffsetValue), true },
                { new VcardTypedValue(UriValue), new VcardTypedValue(new Uri(UriValue)), false },
                { default(VcardTypedValue), default(VcardTypedValue), true },
                { default(VcardTypedValue), new VcardTypedValue(TextValue), false }
            };
        }

        [Theory]
        [MemberData(nameof(EqualityData))]
        public void Equals_TwoValues_ComparesThem(VcardTypedValue left, VcardTypedValue right, bool expected)
        {
            // arrange
            var other = (object)right;

            // act
            var testResult = left.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityData))]
        public void EqualityOperator_TwoValues_ComparesThem(VcardTypedValue left, VcardTypedValue right, bool expected)
        {
            // arrange
            var subject = left;

            // act
            var testResult = subject == right;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityData))]
        public void InequalityOperator_TwoValues_ComparesThem(VcardTypedValue left, VcardTypedValue right, bool isEqual)
        {
            // arrange
            var expected = !isEqual;

            // act
            var testResult = left != right;

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<VcardTypedValue, VcardTypedValue> EqualValueData()
        {
            return new TheoryData<VcardTypedValue, VcardTypedValue>
            {
                { new VcardTypedValue(TextValue), new VcardTypedValue(TextValue) },
                { new VcardTypedValue(new Uri(UriValue)), new VcardTypedValue(new Uri(UriValue)) },
                { new VcardTypedValue(DateValue), new VcardTypedValue(DateValue) },
                { new VcardTypedValue(UtcOffsetValue), new VcardTypedValue(UtcOffsetValue) },
                { default(VcardTypedValue), default(VcardTypedValue) }
            };
        }

        [Theory]
        [MemberData(nameof(EqualValueData))]
        public void GetHashCode_EqualValues_AreTheSame(VcardTypedValue value, VcardTypedValue other)
        {
            // arrange
            var expected = other.GetHashCode();

            // act
            var testResult = value.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_DefaultValue_GivesTheTypeName()
        {
            // arrange
            var value = default(VcardTypedValue);

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be(typeof(VcardTypedValue).ToString());
        }

        [Fact]
        public void Constructor_NullText_ThrowsArgumentNullException()
        {
            // arrange
            const string Text = null;

            // act
            Action testAction = () => new VcardTypedValue(Text);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_TextWithAControlCharacter_ThrowsArgumentException()
        {
            // arrange
            const string TextWithControlCharacter = "a\x0001";

            // act
            Action testAction = () => new VcardTypedValue(TextWithControlCharacter);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullUri_ThrowsArgumentNullException()
        {
            // arrange
            const Uri NoUri = null;

            // act
            Action testAction = () => new VcardTypedValue(NoUri);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_RelativeUri_ThrowsArgumentException()
        {
            // arrange
            var uri = new Uri("a/b", UriKind.Relative);

            // act
            Action testAction = () => new VcardTypedValue(uri);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }
    }
}
