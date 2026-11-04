using System.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests lines the check of how often a parameter occurs, and how many values it has, must leave alone.  The cases it rejects are in
    /// <see cref="ConformanceCases"/>.
    /// </summary>
    public class IcalParameterConformanceTests
    {
        public static TheoryData<string> AllowedLines()
        {
            return new TheoryData<string>
            {
                // ref. RFC 5545 §3.2.11: memberparam takes a list of values.
                "ATTENDEE;MEMBER=\"mailto:a@example.com\",\"mailto:b@example.com\":mailto:c@example.com",

                // ref. RFC 5545 §3.2.4: delfromparam takes a list of values.
                "ATTENDEE;DELEGATED-FROM=\"mailto:a@example.com\",\"mailto:b@example.com\":mailto:c@example.com",

                // ref. RFC 5545 §3.2.5: deltoparam takes a list of values.
                "ATTENDEE;DELEGATED-TO=\"mailto:a@example.com\",\"mailto:b@example.com\":mailto:c@example.com",

                // ref. RFC 7986 §6.1: displayparam takes a list of values.
                "IMAGE;VALUE=URI;DISPLAY=BADGE,THUMBNAIL:http://example.com/a.png",

                // ref. RFC 7986 §6.3: featureparam takes a list of values.
                "CONFERENCE;VALUE=URI;FEATURE=AUDIO,VIDEO:https://example.com/a",

                // ref. RFC 5545 §3.2: a comma in a quoted value does not separate values.
                "ATTENDEE;CN=\"Doe, John\":mailto:a@example.com",

                // ref. RFC 5545 §3.2: other-param sets no limit on an extension parameter.
                "SUMMARY;X-PARAM=a,b;X-PARAM=c:x",

                // ref. RFC 5545 §3.8.8.2: x-prop sets no limit on how often a parameter occurs.
                "X-ABC;LANGUAGE=en;LANGUAGE=de:x",

                // ref. RFC 5545 §3.8.8.2: this includes FMTTYPE.
                "X-ABC;FMTTYPE=audio/basic;FMTTYPE=image/png:x",
            };
        }

        public static TheoryData<string> LinesWithExcessValueParameters()
        {
            return new TheoryData<string>
            {
                "DTSTART;VALUE=DATE;VALUE=DATE-TIME:20260101",
                "DTSTART;VALUE=DATE,DATE-TIME:20260101",
            };
        }

        [Theory]
        [MemberData(nameof(AllowedLines))]
        public void Validate_AllowedParameters_ReportsNothing(string line)
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(line), new IcalOptions { AllowInvalidValues = true });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(LinesWithExcessValueParameters))]
        public void DtStart_ExcessValueParameterReadWithOption_UsesTheFirstValue(string line)
        {
            // arrange
            var calendarEvent = ReadCalendar(EventCalendar(line), new IcalOptions { AllowInvalidValues = true }).Events.Single();

            // act
            var testResult = calendarEvent.DtStart;

            // assert
            testResult.Value.IsDate.Should().BeTrue();
        }
    }
}
