using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the component object model: the property list, the typed accessors and <see cref="IcalComponent.Validate"/>.
    /// </summary>
    public class IcalComponentTests
    {
        private static readonly IcalDateTime Stamp = new IcalDateTime(new IcalDate(2026, 10, 15), new IcalTime(9, 0, 0, true));

        #region Construction and the property list

        [Theory]
        [InlineData("X-ABC-PROJECT")]
        [InlineData("vevent")]
        [InlineData("A1-")]
        public void Constructor_ValidName_KeepsName(string name)
        {
            // act
            var testResult = new IcalComponent(name);

            // assert
            testResult.Name.Should().Be(name);
            testResult.Properties.Should().BeEmpty();
            testResult.Components.Should().BeEmpty();
            testResult.LineNumber.Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("X_ABC")]
        [InlineData("V EVENT")]
        [InlineData("VEVENT:")]
        public void Constructor_InvalidName_Throws(string name)
        {
            // act
            Action act = () => new IcalComponent(name);

            // assert
            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
        }

        [Fact]
        public void Lists_Null_Throws()
        {
            // arrange
            var component = new IcalEvent();
            component.Properties.Add(new IcalContentLine("SUMMARY", "x"));
            component.Components.Add(new IcalAlarm());

            // act
            Action[] acts =
            {
                () => component.Properties.Add(null), () => component.Properties[0] = null, () => component.Components.Add(null),
                () => component.Components.Insert(0, null)
            };

            // assert
            foreach (var act in acts)
            {
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void TypedComponents_HaveTheirNames()
        {
            // act
            IcalComponent[] testResult =
            {
                new IcalCalendar(), new IcalEvent(), new IcalTodo(), new IcalJournal(), new IcalFreeBusy(), new IcalTimeZone(),
                new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard), new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Daylight),
                new IcalAlarm()
            };

            // assert
            testResult.Select(component => component.Name).Should()
                .Equal("VCALENDAR", "VEVENT", "VTODO", "VJOURNAL", "VFREEBUSY", "VTIMEZONE", "STANDARD", "DAYLIGHT", "VALARM");
            ((IcalCalendar)testResult[0]).Version.Should().Be("2.0");
            Action act = () => new IcalTimeZoneObservance((IcalTimeZoneObservanceKind)0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetPropertiesAndRemoveProperties_IgnoreCase()
        {
            // arrange
            var component = new IcalComponent("X-TEST");
            component.Properties.Add(new IcalContentLine("Comment", "a"));
            component.Properties.Add(new IcalContentLine("SUMMARY", "b"));
            component.Properties.Add(new IcalContentLine("COMMENT", "c"));

            // act
            var found = component.GetProperties("comment");
            var first = component.GetProperty("COMMENT");
            var removed = component.RemoveProperties("comment");

            // assert
            found.Select(property => property.Value).Should().Equal("a", "c");
            first.Value.Should().Be("a");
            removed.Should().Be(2);
            component.Properties.Should().ContainSingle().Which.Name.Should().Be("SUMMARY");
            component.GetProperty("COMMENT").Should().BeNull();
        }

        [Fact]
        public void SetProperty_ReplacesEveryPropertyOfTheNameInPlaceOfTheFirst()
        {
            // arrange
            var component = new IcalComponent("X-TEST");
            component.Properties.Add(new IcalContentLine("A", "1"));
            component.Properties.Add(new IcalContentLine("B", "1"));
            component.Properties.Add(new IcalContentLine("A", "2"));

            // act
            component.SetProperty(new IcalContentLine("a", "3"));
            component.SetProperty(new IcalContentLine("C", "1"));

            // assert
            component.Properties.Select(property => property.Name + "=" + property.Value).Should().Equal("a=3", "B=1", "C=1");
        }

        #endregion

        #region Typed accessors

        [Fact]
        public void TextAccessor_SetKeepsParametersAndEscapes()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("SUMMARY;LANGUAGE=en-NZ:Old", "SUMMARY;LANGUAGE=de:Alt"),
                new IcalOptions { AllowCardinalityViolations = true }).Events.Single();

            // act
            testEvent.Summary = "Line one\nTwo; three, four\\";

            // assert
            var summary = testEvent.GetProperties("SUMMARY").Should().ContainSingle().Subject;
            summary.Value.Should().Be("Line one\\nTwo\\; three\\, four\\\\");
            summary.GetParameter("LANGUAGE").Values.Should().Equal("en-NZ");
            testEvent.Summary.Should().Be("Line one\nTwo; three, four\\");
            testEvent.Properties.IndexOf(summary).Should().Be(3);
        }

        [Fact]
        public void TextAccessor_SetNull_RemovesProperty()
        {
            // arrange
            var testEvent = new IcalEvent { Summary = "x", Location = "y" };

            // act
            testEvent.Summary = null;

            // assert
            testEvent.Summary.Should().BeNull();
            testEvent.Properties.Should().ContainSingle().Which.Name.Should().Be("LOCATION");
        }

        [Fact]
        public void TextListAccessor_ReadsEveryPropertyAndWritesOne()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("CATEGORIES:A,B\\,C", "CATEGORIES:D")).Events.Single();

            // act
            var before = testEvent.Categories;
            testEvent.Categories = new[] { "X,Y", "Z" };

            // assert
            before.Should().Equal("A", "B,C", "D");
            testEvent.GetProperties("CATEGORIES").Should().ContainSingle().Which.Value.Should().Be("X\\,Y,Z");
            testEvent.Categories.Should().Equal("X,Y", "Z");
            testEvent.Categories = new string[0];
            testEvent.GetProperty("CATEGORIES").Should().BeNull();
        }

        [Fact]
        public void DateOrDateTimeAccessor_SetWritesValueAndTimeZoneParameters()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("DTSTART;X-KEEP=1;VALUE=DATE:20260101")).Events.Single();
            var zoned = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 0), "Pacific/Auckland");

            // act
            testEvent.DtStart = zoned;
            var zonedLine = testEvent.GetProperty("DTSTART");
            testEvent.DtStart = new IcalDate(2026, 3, 2);
            var dateLine = testEvent.GetProperty("DTSTART");

            // assert
            zonedLine.Value.Should().Be("20260301T093000");
            zonedLine.Parameters.Select(parameter => parameter.Name + "=" + parameter.Values[0]).Should().Equal("X-KEEP=1", "TZID=Pacific/Auckland");
            dateLine.Value.Should().Be("20260302");
            dateLine.Parameters.Select(parameter => parameter.Name + "=" + parameter.Values[0]).Should().Equal("X-KEEP=1", "VALUE=DATE");
            testEvent.DtStart.Should().Be((IcalDateOrDateTime)new IcalDate(2026, 3, 2));
        }

        [Fact]
        public void Accessors_InvalidValue_ThrowWithLocation()
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var testEvent = ReadCalendar(EventCalendar("DTEND:2026", "PRIORITY:high", "ORGANIZER:not a uri", "RRULE:FREQ=SOMETIMES", "SUMMARY:\\x"),
                options).Events.Single();

            // act
            Func<object>[] getters =
            {
                () => testEvent.DtEnd, () => testEvent.Priority, () => testEvent.Organizer, () => testEvent.RecurrenceRules, () => testEvent.Summary
            };

            // assert
            foreach (var getter in getters)
            {
                var exception = getter.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().BeOfType<long>().Which.Should().BeGreaterThan(5);
            }
        }

        [Fact]
        public void NumberDurationAndUriAccessors_RoundTrip()
        {
            // arrange
            var calendar = new IcalCalendar
            {
                RefreshInterval = new IcalDuration(1, TimeSpan.Zero),
                Source = new Uri("https://example.com/cal.ics"),
                Url = new Uri("https://example.com/")
            };
            var todo = new IcalTodo { PercentComplete = 40, Priority = 1, Duration = new IcalDuration(0, TimeSpan.FromMinutes(90)) };

            // assert
            calendar.GetProperty("REFRESH-INTERVAL").GetParameter("VALUE").Values.Should().Equal("DURATION");
            calendar.GetProperty("SOURCE").GetParameter("VALUE").Values.Should().Equal("URI");
            calendar.GetProperty("URL").Parameters.Should().BeEmpty();
            calendar.RefreshInterval.Should().Be(new IcalDuration(1, TimeSpan.Zero));
            calendar.Source.Should().Be(new Uri("https://example.com/cal.ics"));
            todo.GetProperty("PERCENT-COMPLETE").Value.Should().Be("40");
            todo.GetProperty("DURATION").Value.Should().Be("PT1H30M");
            todo.PercentComplete.Should().Be(40);
            Action act = () => calendar.Url = new Uri("relative", UriKind.Relative);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RecurrenceRulesAccessor_WritesOnePropertyPerRule()
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Daylight)
            {
                DtStart = new IcalDateTime(new IcalDate(1970, 9, 27), new IcalTime(2, 0, 0)),
                OffsetFrom = new IcalUtcOffset(TimeSpan.FromHours(12)),
                OffsetTo = new IcalUtcOffset(TimeSpan.FromHours(13))
            };

            // act
            observance.RecurrenceRules =
            new[]
            {
                IcalRecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU".AsSpan(), IcalValueFormat.Text),
                IcalRecurrenceRule.Parse("FREQ=YEARLY;COUNT=1".AsSpan(), IcalValueFormat.Text)
            };

            // assert
            observance.GetProperties("RRULE").Select(property => property.Value).Should().Equal("FREQ=YEARLY;BYDAY=-1SU;BYMONTH=9", "FREQ=YEARLY;COUNT=1");
            observance.RecurrenceRules.Should().HaveCount(2);
            observance.Validate().Should().BeEmpty();
            observance.RecurrenceRules = new IcalRecurrenceRule[0];
            observance.GetProperty("RRULE").Should().BeNull();
        }

        [Fact]
        public void AlarmTriggerAccessors_SwitchBetweenKinds()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-5)) };

            // act
            var relative = alarm.GetProperty("TRIGGER");
            alarm.TriggerDateTime = Stamp;
            var absolute = alarm.GetProperty("TRIGGER");

            // assert
            relative.Value.Should().Be("-PT5M");
            absolute.Value.Should().Be("20261015T090000Z");
            absolute.GetParameter("VALUE").Values.Should().Equal("DATE-TIME");
            alarm.TriggerDuration.Should().BeNull();
            alarm.TriggerDateTime.Should().Be(Stamp);
            alarm.GetProperties("TRIGGER").Should().ContainSingle();
            alarm.TriggerDuration = IcalDuration.FromWeeks(1);
            alarm.GetProperties("TRIGGER").Should().ContainSingle().Which.Parameters.Should().BeEmpty();
            alarm.TriggerDateTime = null;
            alarm.GetProperty("TRIGGER").Should().BeNull();
        }

        [Fact]
        public void Alarm_TriggerDateTime_ReplacesInPlace_KeepingOtherParameters()
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;VALUE=DATE-TIME:20260105T083000Z", "ACTION:DISPLAY", "DESCRIPTION:Reminder");

            // act
            alarm.TriggerDateTime = new IcalDateTime(new IcalDate(2026, 1, 5), new IcalTime(8, 45, 0, true));

            // assert
            alarm.Properties[0].Name.Should().Be("TRIGGER");
            alarm.Properties[0].Parameters.Select(p => p.Name).Should().Equal("X-VENDOR", "VALUE");
            alarm.Properties[0].Value.Should().Be("20260105T084500Z");
        }

        [Fact]
        public void Alarm_TriggerDateTime_OnRelativeTrigger_DropsRelatedInPlace()
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;RELATED=END:-PT15M", "ACTION:DISPLAY", "DESCRIPTION:Reminder");

            // act
            alarm.TriggerDateTime = Stamp;

            // assert
            alarm.Properties.Select(property => property.Name).Should().Equal("TRIGGER", "ACTION", "DESCRIPTION");
            alarm.Properties[0].Parameters.Select(parameter => parameter.Name + "=" + parameter.Values[0]).Should()
                .Equal("X-VENDOR=keep", "VALUE=DATE-TIME");
            alarm.Properties[0].Value.Should().Be("20261015T090000Z");
            alarm.Validate().Should().BeEmpty();
        }

        [Fact]
        public void Alarm_TriggerDuration_OnAbsoluteTrigger_ReplacesInPlace_KeepingOtherParameters()
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;VALUE=DATE-TIME:20260105T083000Z", "ACTION:DISPLAY", "DESCRIPTION:Reminder");

            // act
            alarm.TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-30));

            // assert
            alarm.Properties.Select(property => property.Name).Should().Equal("TRIGGER", "ACTION", "DESCRIPTION");
            alarm.Properties[0].Parameters.Select(parameter => parameter.Name).Should().Equal("X-VENDOR");
            alarm.Properties[0].Value.Should().Be("-PT30M");
        }

        [Fact]
        public void CalendarComponentAccessors_FilterByType()
        {
            // arrange
            var calendar = ReadCalendar(CalendarFixtures.GoogleExport);
            calendar.Components.Add(new IcalTodo());
            calendar.Components.Add(new IcalJournal());
            calendar.Components.Add(new IcalFreeBusy());
            calendar.Components.Add(new IcalComponent("X-OTHER"));

            // assert
            calendar.Events.Should().HaveCount(2);
            calendar.TimeZones.Should().ContainSingle().Which.Observances.Should().HaveCount(2);
            calendar.Todos.Should().ContainSingle();
            calendar.Journals.Should().ContainSingle();
            calendar.FreeBusy.Should().ContainSingle();
            calendar.Components.Should().HaveCount(7);
        }

        #endregion

        #region IcalDateOrDateTime

        [Fact]
        public void DateOrDateTime_DateAndDateTime_AreDistinct()
        {
            // arrange
            var date = new IcalDate(2026, 1, 1);
            IcalDateOrDateTime dateValue = date;
            IcalDateOrDateTime midnight = new IcalDateTime(date, new IcalTime(0, 0, 0));

            // assert
            dateValue.IsDate.Should().BeTrue();
            dateValue.ValueType.Should().Be(IcalValueType.Date);
            dateValue.ToString().Should().Be("20260101");
            midnight.IsDate.Should().BeFalse();
            midnight.ValueType.Should().Be(IcalValueType.DateTime);
            midnight.ToString().Should().Be("20260101T000000");
            midnight.Date.Should().Be(date);
            (dateValue == midnight).Should().BeFalse();
            (dateValue != midnight).Should().BeTrue();
            dateValue.Should().Be(new IcalDateOrDateTime(date));
            dateValue.GetHashCode().Should().Be(new IcalDateOrDateTime(date).GetHashCode());
            Action act = () => _ = dateValue.DateTime;
            act.Should().Throw<InvalidOperationException>();
            default(IcalDateOrDateTime).IsDate.Should().BeTrue();
        }

        #endregion

        #region Validate

        [Fact]
        public void Validate_CompliantCalendar_ReturnsNothing()
        {
            // arrange
            var calendar = new IcalCalendar { ProductId = "-//Test//Test//EN" };
            calendar.Components.Add(new IcalEvent { Uid = "1", DtStamp = Stamp, DtStart = Stamp });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ManyProblems_ReturnsEachWithPath()
        {
            // arrange
            var calendar = new IcalCalendar();
            calendar.Components.Add(new IcalEvent { Uid = "1", DtStamp = Stamp, DtStart = Stamp });
            var second = new IcalEvent { DtStamp = Stamp, DtStart = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(9, 0, 0), "Nowhere") };
            second.Components.Add(new IcalAlarm { Action = "DISPLAY", TriggerDuration = IcalDuration.FromWeeks(-1) });
            calendar.Components.Add(second);

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.ToString()).Should().Equal(
                "VCALENDAR: The VCALENDAR component has no PRODID property, which it requires.",
                "VCALENDAR/VEVENT[2]: The VEVENT component has no UID property, which it requires.",
                "VCALENDAR/VEVENT[2]: The TZID parameter of the DTSTART property names the time zone 'Nowhere', which has no VTIMEZONE in the calendar.",
                "VCALENDAR/VEVENT[2]/VALARM[1]: A VALARM component with ACTION:DISPLAY requires a DESCRIPTION property.");
            testResult.Select(diagnostic => diagnostic.Kind).Should().Equal(IcalDiagnosticKind.MissingRequiredProperty,
                IcalDiagnosticKind.MissingRequiredProperty, IcalDiagnosticKind.UndefinedTimeZone, IcalDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void Validate_StandaloneEvent_SkipsCalendarChecks()
        {
            // arrange
            var testEvent = new IcalEvent { Uid = "1", DtStamp = Stamp };
            testEvent.Properties.Add(new IcalContentLine("DTEND", new[] { new IcalContentLineParameter("TZID", "Nowhere") }, "20260101T090000"));

            // act
            var testResult = testEvent.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ReadCalendar_OrdersByLine()
        {
            // arrange
            var text = Calendar("BEGIN:VEVENT", "DTSTAMP:20260101T000000Z", "DTSTART:20260101T000000Z", "PRIORITY:11", "END:VEVENT", "COLOR:a",
                "COLOR:b");
            var options = new IcalOptions { AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true };

            // act
            var testResult = ReadCalendar(text, options).Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.LineNumber, diagnostic.PropertyName)).Should()
                .Equal((4L, "UID"), (7L, "PRIORITY"), (10L, "COLOR"));
            testResult[0].ToString().Should().Be("VCALENDAR/VEVENT[1] (line 4): The VEVENT component has no UID property, which it requires.");
        }

        #endregion
    }
}
