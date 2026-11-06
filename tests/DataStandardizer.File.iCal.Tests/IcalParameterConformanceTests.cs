using System;
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

                // ref. RFC 5545 §3.2.1: altrepparam = "ALTREP" "=" DQUOTE uri DQUOTE
                "DESCRIPTION;ALTREP=\"http://example.com/a\":x",

                // ref. RFC 5545 §3.2.2: cnparam = "CN" "=" param-value, which may be a quoted-string with nothing that needs quoting.
                "ATTENDEE;CN=\"Doe\":mailto:a@example.com",

                // ref. RFC 5545 §3.2.18: sentbyparam = "SENT-BY" "=" DQUOTE cal-address DQUOTE
                "ORGANIZER;SENT-BY=\"mailto:a@example.com\":mailto:b@example.com",

                // ref. RFC 7986 §6.2: emailparam = "EMAIL" "=" param-value
                "ATTENDEE;EMAIL=\"a@example.com\":mailto:b@example.com",

                // ref. RFC 7986 §6.4: labelparam = "LABEL" "=" param-value
                "CONFERENCE;VALUE=URI;LABEL=\"Room\":https://example.com/a",

                // ref. RFC 5545 §3.2: other-param = (iana-param / x-param), whose values are param-value.
                "SUMMARY;X-PARAM=\"a\":x",
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

        [Fact]
        public void Validate_QuotedFormatTypeWithMediaTypeParameter_ReportsBothProblems()
        {
            // arrange: the media type parameter needs the quotes for its ';', and RFC 5545 §3.2.8 allows neither.
            var options = new IcalOptions { AllowInvalidValues = true, AllowQuotedParameterValues = true };
            var calendar = ReadCalendar(EventCalendar("ATTACH;FMTTYPE=\"text/html; charset=utf-8\":http://example.com/a"), options);

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.Kind).Should().Equal(IcalDiagnosticKind.InvalidValue, IcalDiagnosticKind.QuotedParameterValue);
        }

        [Fact]
        public void ReadCalendar_QuotedFormatTypeWithMediaTypeParameterReadWithDefaults_ThrowsForTheInvalidValue()
        {
            // act
            Action testAction = () => ReadCalendar(EventCalendar("ATTACH;FMTTYPE=\"text/html; charset=utf-8\":http://example.com/a"), new IcalOptions());

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should()
                .Be(IcalDiagnosticKind.InvalidValue);
        }

        // A typed setter keeps the parameter on a new line, which a writer formats afresh, quoting only what needs it.
        [Fact]
        public void Validate_QuotedParameterKeptByTypedSetter_ReportsNothing()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar("SUMMARY;LANGUAGE=\"en\":x"), new IcalOptions { AllowQuotedParameterValues = true });
            calendar.Events.Single().Summary = "y";

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void DtStart_QuotedTimeZoneIdReadWithOption_UsesTheValueWithoutQuotes()
        {
            // arrange
            var options = new IcalOptions { AllowQuotedParameterValues = true, AllowUndefinedTimeZones = true };
            var calendarEvent = ReadCalendar(EventCalendar("DTSTART;TZID=\"Zone\":20260101T090000"), options).Events.Single();

            // act
            var testResult = calendarEvent.DtStart;

            // assert
            testResult.Value.DateTime.TimeZoneId.Should().Be("Zone");
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
