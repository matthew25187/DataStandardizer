using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of <see cref="IcalPropertyNames"/>, <see cref="IcalParameterNames"/> and <see cref="IcalComponentNames"/>.
    /// </summary>
    public class IcalNamesTests
    {
        private const string BeginCalendar = "BEGIN:VCALENDAR";
        private const string Version20 = "VERSION:2.0";
        private const string ProductId = "PRODID:x";
        private const string EndCalendar = "END:VCALENDAR";

        // The number of names that each class declares.
        private const int PropertyNameCount = 52;
        private const int ParameterNameCount = 24;
        private const int ComponentNameCount = 9;

        public static TheoryData<string> PropertyNames() => new TheoryData<string>(Constants(typeof(IcalPropertyNames)));

        public static TheoryData<Type> NameClasses() => new TheoryData<Type>
        {
            typeof(IcalPropertyNames),
            typeof(IcalParameterNames),
            typeof(IcalComponentNames),
        };

        public static TheoryData<Type, int> NameClassCounts() => new TheoryData<Type, int>
        {
            { typeof(IcalPropertyNames), PropertyNameCount },
            { typeof(IcalParameterNames), ParameterNameCount },
            { typeof(IcalComponentNames), ComponentNameCount },
        };

        /// <summary>
        /// Gets each component that is a child of the calendar, with a calendar that holds it.
        /// </summary>
        public static TheoryData<string, string> CalendarChildComponents()
        {
            var data = new TheoryData<string, string>();
            foreach (var name in Constants(typeof(IcalComponentNames)))
            {
                if (name != IcalComponentNames.Calendar && name != IcalComponentNames.Standard && name != IcalComponentNames.Daylight
                    && name != IcalComponentNames.Alarm)
                {
                    data.Add(name, Crlf(BeginCalendar, Version20, ProductId, "BEGIN:" + name, "END:" + name, EndCalendar));
                }
            }

            return data;
        }

        /// <summary>
        /// Gets each component that is a child of another component, with a calendar that holds it.
        /// </summary>
        public static TheoryData<string, string> NestedComponents() => new TheoryData<string, string>
        {
            {
                IcalComponentNames.Standard,
                Crlf(BeginCalendar, Version20, ProductId, "BEGIN:VTIMEZONE", "BEGIN:STANDARD", "END:STANDARD", "END:VTIMEZONE", EndCalendar)
            },
            {
                IcalComponentNames.Daylight,
                Crlf(BeginCalendar, Version20, ProductId, "BEGIN:VTIMEZONE", "BEGIN:DAYLIGHT", "END:DAYLIGHT", "END:VTIMEZONE", EndCalendar)
            },
            {
                IcalComponentNames.Alarm,
                Crlf(BeginCalendar, Version20, ProductId, "BEGIN:VEVENT", "BEGIN:VALARM", "END:VALARM", "END:VEVENT", EndCalendar)
            },
        };

        [Theory]
        [MemberData(nameof(PropertyNames))]
        public void IsKnownProperty_DeclaredPropertyName_IsTrue(string name)
        {
            // act
            var testResult = IcalValueTypeRegistry.IsKnownProperty(name.AsSpan());

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NameClassCounts))]
        public void Constants_NameClass_HasExpectedNumberOfNames(Type type, int expectedCount)
        {
            // act
            var testResult = Constants(type);

            // assert
            testResult.Should().HaveCount(expectedCount);
        }

        [Theory]
        [MemberData(nameof(NameClasses))]
        public void Constants_NameClass_AreDistinct(Type type)
        {
            // act
            var testResult = Constants(type);

            // assert
            testResult.Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [MemberData(nameof(NameClasses))]
        public void Constants_NameClass_AreUpperCaseTokens(Type type)
        {
            // act
            var testResult = Constants(type);

            // assert
            testResult.Should().OnlyContain(name => name.Length > 0 && name.All(character => (character >= 'A' && character <= 'Z') || character == '-'));
        }

        [Fact]
        public void ReadCalendar_CalendarComponent_IsTypedComponent()
        {
            // arrange
            var text = Crlf(BeginCalendar, Version20, ProductId, "BEGIN:X-ANY", "END:X-ANY", EndCalendar);
            var options = new IcalOptions { AllowMissingRequiredProperties = true };

            // act
            var testResult = ModelTestData.ReadCalendar(text, options);

            // assert
            testResult.Name.Should().Be(IcalComponentNames.Calendar);
            testResult.GetType().Should().NotBe(typeof(IcalComponent));
        }

        [Theory]
        [MemberData(nameof(CalendarChildComponents))]
        public void ReadCalendar_CalendarChildComponent_IsTypedComponent(string name, string text)
        {
            // arrange
            var options = new IcalOptions { AllowMissingRequiredProperties = true };

            // act
            var testResult = ModelTestData.ReadCalendar(text, options);

            // assert
            testResult.Components[0].Name.Should().Be(name);
            testResult.Components[0].GetType().Should().NotBe(typeof(IcalComponent));
        }

        [Theory]
        [MemberData(nameof(NestedComponents))]
        public void ReadCalendar_NestedComponent_IsTypedComponent(string name, string text)
        {
            // arrange
            var options = new IcalOptions { AllowMissingRequiredProperties = true };

            // act
            var testResult = ModelTestData.ReadCalendar(text, options);

            // assert
            testResult.Components[0].Components[0].Name.Should().Be(name);
            testResult.Components[0].Components[0].GetType().Should().NotBe(typeof(IcalComponent));
        }

        private static string[] Constants(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral)
                .Select(field => (string)field.GetRawConstantValue()).ToArray();
        }
    }
}
