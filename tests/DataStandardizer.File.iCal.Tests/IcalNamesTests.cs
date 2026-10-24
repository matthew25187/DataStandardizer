using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        public static TheoryData<string> PropertyNames() => new TheoryData<string>(Constants(typeof(IcalPropertyNames)));

        public static TheoryData<string> ComponentNames() => new TheoryData<string>(Constants(typeof(IcalComponentNames)));

        [Theory]
        [MemberData(nameof(PropertyNames))]
        public void PropertyName_IsKnownToRegistry(string name)
        {
            // act
            var testResult = IcalValueTypeRegistry.IsKnownProperty(name.AsSpan());

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Names_AreDistinctUpperCaseTokens()
        {
            // arrange
            Type[] types = new[] { typeof(IcalPropertyNames), typeof(IcalParameterNames), typeof(IcalComponentNames) };

            // act
            var testResult = types.Select(type => Constants(type)).ToList();

            // assert
            testResult.Select(names => names.Length).Should().Equal(52, 24, 9);
            foreach (var names in testResult)
            {
                names.Should().OnlyHaveUniqueItems();
                names.Should().OnlyContain(name => name.Length > 0 && name.All(character => (character >= 'A' && character <= 'Z') || character == '-'));
            }
        }

        [Theory]
        [MemberData(nameof(ComponentNames))]
        public void ComponentName_ReadsAsItsTypedComponent(string name)
        {
            // arrange
            var parent = name == IcalComponentNames.Standard || name == IcalComponentNames.Daylight ? IcalComponentNames.TimeZone
                : name == IcalComponentNames.Alarm ? IcalComponentNames.Event
                : IcalComponentNames.Calendar;
            var text = name == IcalComponentNames.Calendar
                ? Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:x", "BEGIN:X-ANY", "END:X-ANY", "END:VCALENDAR")
                : parent == IcalComponentNames.Calendar
                    ? Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:x", $"BEGIN:{name}", $"END:{name}", "END:VCALENDAR")
                    : Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:x", $"BEGIN:{parent}", $"BEGIN:{name}", $"END:{name}", $"END:{parent}",
                        "END:VCALENDAR");
            var options = new IcalOptions { AllowMissingRequiredProperties = true };

            // act
            var calendar = ModelTestData.ReadCalendar(text, options);
            var testResult = name == IcalComponentNames.Calendar
                ? calendar
                : parent == IcalComponentNames.Calendar ? calendar.Components[0] : calendar.Components[0].Components[0];

            // assert
            testResult.Name.Should().Be(name);
            testResult.GetType().Should().NotBe(typeof(IcalComponent));
        }

        private static string[] Constants(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral)
                .Select(field => (string)field.GetRawConstantValue()).ToArray();
        }
    }
}
