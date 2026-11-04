using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests the check of the <c>FMTTYPE</c> parameter (RFC 5545 §3.2.8) that readers and writers make.  The cases in
    /// <see cref="ConformanceCases"/> test that each reader rejects a calendar, and accepts it with its option.
    /// </summary>
    public class IcalFormatTypeConformanceTests
    {
        private const string Attachment = "ATTACH";
        private const string InvalidFormatTypeLine = "ATTACH;FMTTYPE=audio:http://example.com/a";
        private const string InvalidFormatTypeValue = "audio";
        private const string TwoFormatTypesLine = "ATTACH;FMTTYPE=audio/basic;FMTTYPE=image/png:http://example.com/a";

        private static readonly IcalOptions AllowingInvalidValues = new IcalOptions { AllowInvalidValues = true };

        public static TheoryData<string, string, string> ValidFormatTypes()
        {
            return new TheoryData<string, string, string>
            {
                // ref. RFC 5545 §3.8.1.1: the example of the ATTACH property.
                { "ATTACH;FMTTYPE=application/msword:ftp://example.com/pub/docs/agenda.doc", Attachment, "application/msword" },

                // ref. RFC 7986 §5.10: the example of the IMAGE property.
                { "IMAGE;VALUE=URI;DISPLAY=BADGE;FMTTYPE=image/png:http://example.com/images/party.png", "IMAGE", "image/png" },

                // ref. RFC 5545 §3.8.8.2: the example of a non-standard property.
                { "X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au", "X-ABC-MMSUBJ", "audio/basic" },

                // ref. RFC 4288 §4.2: a reg-name may start with a symbol.
                { "ATTACH;FMTTYPE=!a/.b:http://example.com/a", Attachment, "!a/.b" },
            };
        }

        public static TheoryData<Func<IcalCalendar, string>> Writers()
        {
            return new TheoryData<Func<IcalCalendar, string>>
            {
                calendar => WriteCalendars(new[] { calendar }),
                calendar => WriteXcal(new[] { calendar }),
                calendar => WriteJcal(new[] { calendar }),
            };
        }

        [Theory]
        [MemberData(nameof(ValidFormatTypes))]
        public void ReadCalendar_ValidFormatTypeWithDefaults_GivesTheMediaType(string line, string propertyName, string expected)
        {
            // arrange
            var text = EventCalendar(line);

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Components.Single().GetProperty(propertyName).GetFormatType().ToString().Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ValidFormatTypes))]
        public void WriteCalendars_ValidFormatTypeWithDefaults_WritesTheLine(string line, string propertyName, string expected)
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(line));

            // act
            var testResult = ReadCalendar(WriteCalendars(new[] { calendar }));

            // assert
            testResult.Components.Single().GetProperty(propertyName).GetFormatType().ToString().Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Writers))]
        public void Write_InvalidFormatTypeWithDefaults_ThrowsWithTheDiagnostic(Func<IcalCalendar, string> write)
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(InvalidFormatTypeLine), AllowingInvalidValues);

            // act
            Action testAction = () => write(calendar);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Attachment);
        }

        [Fact]
        public void ReadCalendar_InvalidFormatTypeWithOption_KeepsTheValue()
        {
            // arrange
            var text = EventCalendar(InvalidFormatTypeLine);

            // act
            var testResult = ReadCalendar(text, AllowingInvalidValues);

            // assert
            testResult.Components.Single().GetProperty(Attachment).GetParameter(IcalParameterNames.FormatType).Values.Should().Equal(InvalidFormatTypeValue);
        }

        [Fact]
        public void GetFormatType_ValueTheOptionAccepted_ThrowsIcalFileException()
        {
            // arrange
            var property = ReadCalendar(EventCalendar(InvalidFormatTypeLine), AllowingInvalidValues).Components.Single().GetProperty(Attachment);

            // act
            Action testAction = () => property.GetFormatType();

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void WriteCalendars_InvalidFormatTypeWithOption_WritesTheValueAsRead()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(InvalidFormatTypeLine), AllowingInvalidValues);

            // act
            var testResult = WriteCalendars(new[] { calendar }, AllowingInvalidValues);

            // assert
            testResult.Should().Contain(InvalidFormatTypeLine);
        }

        [Fact]
        public void WriteXcal_TwoFormatTypesWithOption_ReadsBackAsTwoParameters()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(TwoFormatTypesLine), AllowingInvalidValues);

            // act
            var testResult = ReadXcal(WriteXcal(new[] { calendar }, AllowingInvalidValues), AllowingInvalidValues);

            // assert
            testResult.Single().Components.Single().GetProperty(Attachment).Parameters.Should().HaveCount(2);
        }

        [Fact]
        public void WriteJcal_TwoFormatTypesWithOption_ThrowsBecauseJcalCannotHoldThem()
        {
            // arrange: RFC 7265 §3.5 makes each parameter a key of a JSON object.
            var calendar = ReadCalendar(EventCalendar(TwoFormatTypesLine), AllowingInvalidValues);

            // act
            Action testAction = () => WriteJcal(new[] { calendar }, AllowingInvalidValues);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void Validate_InvalidFormatTypeOnLineBuiltInCode_ReportsInvalidValue()
        {
            // arrange
            var component = new IcalComponent("X-COMPONENT");
            component.Properties.Add(new IcalContentLine(Attachment, new[] { new IcalContentLineParameter(IcalParameterNames.FormatType, InvalidFormatTypeValue) },
                "http://example.com/a"));

            // act
            var testResult = component.Validate();

            // assert
            var diagnostic = testResult.Should().ContainSingle().Subject;
            diagnostic.Kind.Should().Be(IcalDiagnosticKind.InvalidValue);
            diagnostic.PropertyName.Should().Be(Attachment);
        }
    }
}
