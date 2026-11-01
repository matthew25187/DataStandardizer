using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;
using static DataStandardizer.File.iCal.Tests.IcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of <see cref="IcalReader"/>: the typed model it builds, the nesting of components, VERSION, and the conformance checks.
    /// </summary>
    public class IcalReaderTests
    {
        // ref. RFC 5545 §4, first example: the UID of its event, and its organizer.
        private const string Rfc5545EventUid = "uid1@example.com";
        private const string Rfc5545Organizer = "mailto:jsmith@example.com";

        private const string Trigger = "TRIGGER";
        private const string ShortProductId = "PRODID:x";
        private const string ShortEventUid = "UID:e";
        private const string NewYearStartUtc = "DTSTART:20260101T000000Z";
        private const string AlarmPath = "VCALENDAR/VEVENT[1]/VALARM[1]";
        private const string NewZealandStandardTime = "New Zealand Standard Time";
        private const string Auckland = "Pacific/Auckland";
        private const string PublicClassification = "PUBLIC";
        private const string Version10 = "VERSION:1.0";

        // The calendars of CalendarFixtures.GoogleExport, RfcFixtures.Rfc5545Event and CalendarFixtures.OutlookExport, one after the other, and
        // the numbers of components, content lines and physical lines in them.
        private const int ThreeCalendarsComponentCount = 12;
        private const long ThreeCalendarsLineCount = 123;
        private const long ThreeCalendarsPhysicalLineCount = 128;

        // The line of the second SUMMARY of the third VEVENT in SiblingComponents.
        private const long ThirdEventSecondSummaryLineNumber = 31;

        private static readonly string ThreeCalendars = CalendarFixtures.GoogleExport + RfcFixtures.Rfc5545Event + CalendarFixtures.OutlookExport;

        private static readonly string GoogleExportWithBareLineFeeds = CalendarFixtures.GoogleExport.Replace("\r\n", "\n");

        // Three VEVENTs, each followed by a VTODO; the third VEVENT has two SUMMARY properties.
        private static readonly string SiblingComponents = Calendar(
            BeginEvent, "UID:1@example.com", NewYearStamp, NewYearStartUtc, "SUMMARY:Other", "COMMENT:-", EndEvent,
            BeginTodo, "UID:t1", NewYearStamp, EndTodo,
            BeginEvent, "UID:2@example.com", NewYearStamp, NewYearStartUtc, "SUMMARY:Other", "COMMENT:-", EndEvent,
            BeginTodo, "UID:t2", NewYearStamp, EndTodo,
            BeginEvent, "UID:3@example.com", NewYearStamp, NewYearStartUtc, "SUMMARY:Third", "SUMMARY:Again", EndEvent,
            BeginTodo, "UID:t3", NewYearStamp, EndTodo);

        private static readonly IcalOptions EveryAccommodation = new IcalOptions
        {
            AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true, AllowUndefinedTimeZones = true
        };

        // RfcFixtures.Rfc5545Event with a VERSION that includes 2.0.
        public static TheoryData<string, string> VersionsIncluding2()
        {
            var data = new TheoryData<string, string>();
            foreach (var version in new[] { "2.0", "1.0;2.0", "2.0;3.0" })
            {
                data.Add(version, RfcFixtures.Rfc5545Event.Replace(Version20, "VERSION:" + version));
            }

            return data;
        }

        // RfcFixtures.Rfc5545Event with a VERSION that excludes 2.0, and its VERSION line.
        public static TheoryData<string, string> VersionsExcluding2()
        {
            var data = new TheoryData<string, string>();
            foreach (var version in new[] { "1.0", "3.0", "1.0;1.5", "2.1;3.0", "2.0;2.0;2.0", "two" })
            {
                data.Add("VERSION:" + version, RfcFixtures.Rfc5545Event.Replace(Version20, "VERSION:" + version));
            }

            return data;
        }

        public static TheoryData<Func<IcalReader, object>> DisposedReaderMembers => new TheoryData<Func<IcalReader, object>>
        {
            reader => reader.ReadCalendar(),
            reader => reader.ReadCalendars(),
            reader => reader.ReadCalendarAsync(),
            reader => reader.ReadCalendarsAsync(),
            reader => reader.CalendarsRead,
            reader => reader.ComponentsRead,
            reader => reader.LinesRead,
            reader => reader.PhysicalLinesRead,
        };

        #region RFC examples

        [Fact]
        public void ReadCalendars_Rfc5545Event_LoadsTypedModel()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545Event);

            // assert
            testResult.ProductId.Should().Be("-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN");
            testResult.Version.Should().Be("2.0");
            testResult.LineNumber.Should().Be(1);
            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.Name.Should().Be("VEVENT");
            testEvent.LineNumber.Should().Be(4);
            testEvent.DtStamp.Should().Be(new IcalDateTime(new IcalDate(1996, 7, 4), new IcalTime(12, 0, 0, true)));
            testEvent.Uid.Should().Be(Rfc5545EventUid);
            testEvent.Organizer.Should().Be(new Uri(Rfc5545Organizer));
            testEvent.DtStart.Should().Be((IcalDateOrDateTime)new IcalDateTime(new IcalDate(1996, 9, 18), new IcalTime(14, 30, 0, true)));
            testEvent.DtEnd.Should().Be((IcalDateOrDateTime)new IcalDateTime(new IcalDate(1996, 9, 20), new IcalTime(22, 0, 0, true)));
            testEvent.Status.Should().Be("CONFIRMED");
            testEvent.Categories.Should().Equal("CONFERENCE");
            testEvent.Summary.Should().Be("Networld+Interop Conference");
            testEvent.Description.Should().Be("Networld+Interop Conference and Exhibit\nAtlanta World Congress Center\nAtlanta, Georgia");
            testEvent.Duration.Should().BeNull();
            testEvent.Alarms.Should().BeEmpty();
        }

        [Fact]
        public void ReadCalendars_Rfc5545TimeZone_LoadsTimeZone()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545TimeZone);

            // assert
            var timeZone = testResult.TimeZones.Should().ContainSingle().Subject;
            timeZone.TimeZoneId.Should().Be(Rfc5545Examples.NewYorkTimeZoneId);
            var observances = timeZone.Observances.ToList();
            observances.Select(observance => observance.Kind).Should().Equal(IcalTimeZoneObservanceKind.Standard, IcalTimeZoneObservanceKind.Daylight);
            observances[0].Name.Should().Be("STANDARD");
            observances[0].DtStart.Should().Be(new IcalDateTime(new IcalDate(1998, 10, 25), new IcalTime(2, 0, 0)));
            observances[0].OffsetFrom.Should().Be(new IcalUtcOffset(TimeSpan.FromHours(-4)));
            observances[0].OffsetTo.Should().Be(new IcalUtcOffset(TimeSpan.FromHours(-5)));
            observances[0].TimeZoneName.Should().Be("EST");
            observances[1].TimeZoneName.Should().Be("EDT");
        }

        [Fact]
        public void ReadCalendars_Rfc5545TimeZone_LoadsZonedEvent()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545TimeZone);

            // assert
            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.DtStart.Value.IsDate.Should().BeFalse();
            testEvent.DtStart.Value.DateTime.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 12), new IcalTime(8, 30, 0),
                Rfc5545Examples.NewYorkTimeZoneId));
            testEvent.Classification.Should().Be(PublicClassification);
            testEvent.Created.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 9), new IcalTime(13, 0, 0, true)));
            testEvent.Location.Should().Be("1CP Conference Room 4350");
            var attendee = testEvent.Attendees.Should().ContainSingle().Subject;
            attendee.Value.Should().Be("mailto:employee-A@example.com");
            attendee.Parameters.Should().ContainSingle(parameter => parameter.Name == "RSVP").Which.Values.Should().Equal("TRUE");
            attendee.Parameters.Should().ContainSingle(parameter => parameter.Name == "CUTYPE").Which.Values.Should().Equal("GROUP");
            testEvent.Properties.Should().ContainSingle(property => property.Name == "ORGANIZER").Which.Parameters.Should()
                .ContainSingle(parameter => parameter.Name == "ROLE").Which.Values.Should().Equal("CHAIR");
        }

        [Fact]
        public void ReadCalendars_Rfc5545Todo_RejectsAbsoluteTriggerWithoutValueParameter()
        {
            // act
            Action testAction = () => ReadCalendars(RfcFixtures.Rfc5545Todo);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Trigger);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VTODO[1]/VALARM[1]");
        }

        [Fact]
        public void ReadCalendars_Rfc5545TodoWithInvalidValuesAllowed_KeepsTriggerAsWritten()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545Todo, new IcalOptions { AllowInvalidValues = true });

            // assert
            var alarm = testResult.Todos.Should().ContainSingle().Which.Alarms.Should().ContainSingle().Subject;
            alarm.Properties.Should().ContainSingle(property => property.Name == Trigger).Which.Value.Should().Be("19980403T120000Z");
            alarm.TriggerDateTime.Should().BeNull();
        }

        [Fact]
        public void TriggerDuration_Rfc5545TodoAbsoluteTriggerWithoutValueParameter_ThrowsIcalFileException()
        {
            // arrange
            var alarm = ReadCalendar(RfcFixtures.Rfc5545Todo, new IcalOptions { AllowInvalidValues = true }).Todos.Single().Alarms.Single();

            // act
            Action testAction = () => _ = alarm.TriggerDuration;

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Trigger);
        }

        [Fact]
        public void ReadCalendars_Rfc5545TodoCorrected_LoadsTodoAndAlarm()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.Rfc5545TodoCorrected);

            // assert
            var todo = testResult.Todos.Should().ContainSingle().Subject;
            todo.Sequence.Should().Be(2);
            todo.Uid.Should().Be("uid4@example.com");
            todo.Due.Should().Be((IcalDateOrDateTime)new IcalDateTime(new IcalDate(1998, 4, 15), new IcalTime(0, 0, 0)));
            todo.Status.Should().Be("NEEDS-ACTION");
            todo.Summary.Should().Be("Submit Income Taxes");
            todo.Attendees.Should().ContainSingle().Which.Parameters.Should().ContainSingle(parameter => parameter.Name == "PARTSTAT").Which.Values
                .Should().Equal("ACCEPTED");

            var alarm = todo.Alarms.Should().ContainSingle().Subject;
            alarm.Action.Should().Be("AUDIO");
            alarm.TriggerDateTime.Should().Be(new IcalDateTime(new IcalDate(1998, 4, 3), new IcalTime(12, 0, 0, true)));
            alarm.TriggerDuration.Should().BeNull();
            alarm.Repeat.Should().Be(4);
            alarm.Duration.Should().Be(new IcalDuration(0, TimeSpan.FromHours(1)));
            var attachment = alarm.Attachments.Should().ContainSingle().Subject;
            attachment.Value.Should().Be("http://example.com/pub/audio-files/ssbanner.aud");
            attachment.Parameters.Should().ContainSingle(parameter => parameter.Name == "FMTTYPE").Which.Values.Should().Equal("audio/basic");
        }

        [Fact]
        public void ReadCalendars_Rfc5545Journal_LoadsJournal()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.Rfc5545Journal);

            // assert
            var journal = testResult.Journals.Should().ContainSingle().Subject;
            journal.Status.Should().Be("DRAFT");
            journal.Classification.Should().Be(PublicClassification);
            journal.Categories.Should().Equal("Project Report", "XYZ", "Weekly Meeting");
            journal.Descriptions.Should().ContainSingle();
        }

        [Fact]
        public void GetTextValue_Rfc5545JournalDescription_IsUnescaped()
        {
            // arrange
            var description = ReadCalendar(CalendarFixtures.Rfc5545Journal).Journals.Single().Descriptions.Single();

            // act
            var testResult = description.GetTextValue();

            // assert
            testResult.Should().StartWith("Project xyz Review Meeting Minutes\nAgenda\n1. Review").And.Contain("John Smith, Jane Doe, Jim Dandy");
        }

        [Fact]
        public void ReadCalendars_Rfc5545FreeBusy_RejectsMissingDtStampAndUid()
        {
            // act
            Action testAction = () => ReadCalendars(RfcFixtures.Rfc5545FreeBusy);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DTSTAMP");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(4L);
        }

        [Fact]
        public void ReadCalendars_Rfc5545FreeBusyWithMissingPropertiesAllowed_LoadsPeriods()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545FreeBusy, new IcalOptions { AllowMissingRequiredProperties = true });

            // assert
            var freeBusy = testResult.FreeBusy.Should().ContainSingle().Subject;
            freeBusy.Organizer.Should().Be(new Uri(Rfc5545Organizer));
            freeBusy.Url.Should().Be(new Uri("http://www.example.com/calendar/busytime/jsmith.ifb"));
            freeBusy.Periods.Should().HaveCount(3);
            freeBusy.Periods[0].Start.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 14), new IcalTime(23, 30, 0, true)));
            freeBusy.Uid.Should().BeNull();
        }

        [Fact]
        public void Validate_Rfc5545FreeBusyReadWithMissingPropertiesAllowed_ReportsDtStampAndUid()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545FreeBusy, new IcalOptions { AllowMissingRequiredProperties = true });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.PropertyName).Should().Equal("DTSTAMP", "UID");
        }

        [Fact]
        public void ReadCalendars_Rfc7986_LoadsNewProperties()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.Rfc7986);

            // assert
            testResult.CalendarName.Should().Be("Company Vacation Days");
            testResult.Description.Should().Be("The description of the calendar.");
            testResult.Uid.Should().Be("5FC53010-1267-4F8E-BC28-1D7AE55A7C99");
            testResult.Url.Should().Be(new Uri("http://example.com/calendar/vacation"));
            testResult.LastModified.Should().Be(new IcalDateTime(new IcalDate(2011, 5, 15), new IcalTime(0, 0, 0, true)));
            testResult.Categories.Should().Equal("MEETING");
            testResult.RefreshInterval.Should().Be(IcalDuration.FromWeeks(1));
            testResult.Source.Should().Be(new Uri("https://example.com/holidays.ics"));
            testResult.Color.Should().Be("turquoise");
            testResult.Images.Should().ContainSingle().Which.Parameters.Should().ContainSingle(parameter => parameter.Name == "DISPLAY").Which.Values
                .Should().Equal("BADGE");

            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.Color.Should().Be("red");
            testEvent.Images.Should().ContainSingle();
            testEvent.Conferences.Should().HaveCount(5);
            testEvent.Conferences[0].Parameters.Should().ContainSingle(parameter => parameter.Name == "FEATURE").Which.Values.Should()
                .Equal("PHONE", "MODERATOR");
            testEvent.Conferences[0].Parameters.Should().ContainSingle(parameter => parameter.Name == "LABEL").Which.Values.Should()
                .Equal("Moderator dial-in");
        }

        #endregion

        #region Vendor exports

        [Fact]
        public void ReadCalendars_GoogleExport_LoadsCalendarProperties()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.GoogleExport);

            // assert
            testResult.Method.Should().Be("PUBLISH");
            testResult.CalendarScale.Should().Be("GREGORIAN");
            testResult.Properties.Should().ContainSingle(property => property.Name == "X-WR-CALNAME").Which.Value.Should().Be("Work");
            testResult.TimeZones.Should().ContainSingle().Which.Properties.Should().ContainSingle(property => property.Name == "X-LIC-LOCATION")
                .Which.Value.Should().Be(Auckland);
        }

        [Fact]
        public void ReadCalendars_GoogleExport_LoadsRecurringAndAllDayEvents()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.GoogleExport);

            // assert
            var events = testResult.Events.ToList();
            events.Should().HaveCount(2);
            events[0].RecurrenceRules.Should().ContainSingle().Which.Frequency.Should().Be(IcalRecurrenceFrequency.Weekly);
            events[0].DtStart.Value.DateTime.TimeZoneId.Should().Be(Auckland);
            events[0].Description.Should().Be("Daily notes: https://example.com/notes\nBring, as always, coffee.");
            events[0].Location.Should().BeEmpty();
            events[0].Attendees.Should().ContainSingle().Which.Parameters.Should().ContainSingle(parameter => parameter.Name == "X-NUM-GUESTS")
                .Which.Values.Should().Equal("0");
            events[0].Alarms.Should().ContainSingle().Which.TriggerDuration.Should().Be(new IcalDuration(0, TimeSpan.FromMinutes(-10)));
            events[1].DtStart.Should().Be((IcalDateOrDateTime)new IcalDate(2026, 12, 25));
            events[1].DtEnd.Value.IsDate.Should().BeTrue();
            events[1].Transparency.Should().Be("TRANSPARENT");
        }

        [Fact]
        public void ReadCalendars_OutlookExport_LoadsWindowsTimeZoneAndExtensions()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.OutlookExport);

            // assert
            testResult.TimeZones.Should().ContainSingle().Which.TimeZoneId.Should().Be(NewZealandStandardTime);
            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.DtStart.Value.DateTime.TimeZoneId.Should().Be(NewZealandStandardTime);
            testEvent.Uid.Should().HaveLength(112);
            testEvent.Description.Should().Be("\n");
            testEvent.Priority.Should().Be(5);
            testEvent.Properties.Should().ContainSingle(property => property.Name == "X-MICROSOFT-CDO-BUSYSTATUS").Which.Value.Should().Be("BUSY");
            testEvent.Alarms.Should().ContainSingle().Which.Properties.Should().ContainSingle(property => property.Name == Trigger).Which.Parameters
                .Should().ContainSingle(parameter => parameter.Name == "RELATED").Which.Values.Should().Equal("START");
        }

        [Fact]
        public void ReadCalendars_AppleExport_RejectsRelativeAttachmentByDefault()
        {
            // act
            Action testAction = () => ReadCalendars(CalendarFixtures.AppleExport);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("ATTACH");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(AlarmPath);
        }

        [Fact]
        public void ReadCalendars_AppleExportWithInvalidValuesAllowed_KeepsExtensions()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.AppleExport, new IcalOptions { AllowInvalidValues = true });

            // assert
            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.Summary.Should().Be("Lunch");
            testEvent.Sequence.Should().Be(1);
            testEvent.Properties.Should().ContainSingle(property => property.Name == "X-APPLE-STRUCTURED-LOCATION").Which.Parameters.Should()
                .ContainSingle(parameter => parameter.Name == "X-TITLE").Which.Values.Should().Equal("Cafe");
            var alarm = testEvent.Alarms.Should().ContainSingle().Subject;
            alarm.Properties.Should().ContainSingle(property => property.Name == "ACKNOWLEDGED").Which.Value.Should().Be("20261103T113000Z");
            alarm.Properties.Should().ContainSingle(property => property.Name == "UID");
        }

        [Fact]
        public void Validate_AppleExportReadWithInvalidValuesAllowed_ReportsTheRelativeAttachment()
        {
            // arrange
            var calendar = ReadCalendar(CalendarFixtures.AppleExport, new IcalOptions { AllowInvalidValues = true });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().ContainSingle().Which.PropertyName.Should().Be("ATTACH");
        }

        #endregion

        #region Components

        [Fact]
        public void ReadCalendars_UnknownComponentAndProperties_AreKept()
        {
            // arrange
            var text = Calendar("X-CALENDAR-PROP;X-PARAM=1:value", "BEGIN:X-ABC-PROJECT", "X-NAME:Apollo", "BEGIN:X-ABC-TASK", "SUMMARY:Nested",
                "END:X-ABC-TASK", "END:X-ABC-PROJECT", BeginJournal, "UID:j@example.com", NewYearStamp, "IANA-PROP:x", EndJournal);

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Properties.Should().ContainSingle(property => property.Name == "X-CALENDAR-PROP").Which.Parameters.Should()
                .ContainSingle(parameter => parameter.Name == "X-PARAM").Which.Values.Should().Equal("1");
            var project = testResult.Components[0];
            project.Should().BeOfType<IcalComponent>();
            project.Name.Should().Be("X-ABC-PROJECT");
            project.Properties.Should().ContainSingle(property => property.Name == "X-NAME").Which.Value.Should().Be("Apollo");
            project.Components.Should().ContainSingle().Which.Name.Should().Be("X-ABC-TASK");
            testResult.Journals.Should().ContainSingle().Which.Properties.Should().ContainSingle(property => property.Name == "IANA-PROP").Which.Value
                .Should().Be("x");
        }

        [Fact]
        public void ReadCalendars_LowerCaseNames_CreateTypedComponents()
        {
            // arrange
            var text = Crlf("begin:vcalendar", "version:2.0", "prodid:x", "begin:vevent", "uid:e", "dtstamp:20260101T000000Z", "dtstart:20260101",
                "end:VEVENT", "end:vcalendar");

            // act
            var testResult = ReadCalendars(text, new IcalOptions { AllowInvalidValues = true });

            // assert
            var calendar = testResult.Should().ContainSingle().Subject;
            calendar.Name.Should().Be(CalendarPath);
            calendar.Events.Should().ContainSingle().Which.Name.Should().Be("VEVENT");
        }

        [Theory]
        [InlineData(BeginTodo, BeginEvent, EndEvent, EndTodo, "VCALENDAR/VTODO[1]/VEVENT[1]")]
        [InlineData(BeginEvent, BeginCalendar, EndCalendar, EndEvent, "VCALENDAR/VEVENT[1]/VCALENDAR[1]")]
        [InlineData(BeginJournal, BeginAlarm, EndAlarm, EndJournal, "VCALENDAR/VJOURNAL[1]/VALARM[1]")]
        public void ReadCalendars_KnownComponentInWrongParent_Throws(string beginParent, string beginChild, string endChild, string endParent,
            string expectedPath)
        {
            // arrange
            var text = Calendar(beginParent, "UID:p@example.com", NewYearStamp, NewYearStartUtc, beginChild, endChild, endParent);

            // act
            Action testAction = () => ReadCalendars(text, EveryAccommodation);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidComponentNesting);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(expectedPath);
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(8L);
        }

        [Fact]
        public void ReadCalendars_ObservanceOutsideTimeZone_ThrowsWhateverTheOptions()
        {
            // arrange
            var text = EventCalendar(BeginStandard, EndStandard);

            // act
            Action testAction = () => ReadCalendars(text, EveryAccommodation);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidComponentNesting);
            exception.Message.Should().Be("A STANDARD component cannot be nested in a VEVENT component.");
        }

        [Fact]
        public void ReadCalendars_ManyCalendars_ReadsEachAndCounts()
        {
            // arrange
            using (var reader = CreateCalendarReader(ThreeCalendars))
            {
                // act
                var testResult = reader.ReadCalendars().ToList();

                // assert
                testResult.Should().HaveCount(3);
                testResult[1].Events.Should().ContainSingle().Which.Uid.Should().Be(Rfc5545EventUid);
                reader.CalendarsRead.Should().Be(3);
                reader.ComponentsRead.Should().Be(ThreeCalendarsComponentCount);
                reader.LinesRead.Should().Be(ThreeCalendarsLineCount);
                reader.PhysicalLinesRead.Should().Be(ThreeCalendarsPhysicalLineCount);
            }
        }

        [Fact]
        public void ReadCalendar_AfterTheLastCalendar_ReturnsNull()
        {
            // arrange
            using (var reader = CreateCalendarReader(ThreeCalendars))
            {
                reader.ReadCalendars().ToList();

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendars_ComponentPaths_IndexSiblingsOfTheSameName()
        {
            // act
            Action testAction = () => ReadCalendars(SiblingComponents);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be("The VEVENT component has 2 SUMMARY properties; it allows at most one.");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[3]");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(ThirdEventSecondSummaryLineNumber);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("SUMMARY");
        }

        #endregion

        #region BEGIN and END

        [Fact]
        public void ReadCalendar_MismatchedEnd_ThrowsWithLine()
        {
            // arrange
            var text = Calendar(BeginEvent, ShortEventUid, EndTodo);
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be("Found END:VTODO, but the BEGIN:VEVENT on line 4 is not closed; expected END:VEVENT.");
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(6L);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(EventPath);
            }
        }

        [Fact]
        public void ReadCalendar_AfterMismatchedEnd_ThrowsInvalidOperation()
        {
            // arrange
            var text = Calendar(BeginEvent, ShortEventUid, EndTodo);
            using (var reader = CreateCalendarReader(text))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCalendar_UnclosedComponent_ThrowsWithBeginLine()
        {
            // arrange
            var text = Crlf(BeginCalendar, Version20, ShortProductId, BeginEvent, ShortEventUid);

            // act
            Action testAction = () => ReadCalendars(text);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be("The data ended before END:VEVENT; the BEGIN:VEVENT on line 4 is not closed.");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(4L);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(EventPath);
        }

        [Theory]
        [InlineData(EndCalendar, "Found END:VCALENDAR with no BEGIN:VCALENDAR before it.")]
        [InlineData(BeginEvent, "Found BEGIN:VEVENT outside a VCALENDAR; expected BEGIN:VCALENDAR.")]
        [InlineData(ShortProductId, "Found a PRODID property outside a VCALENDAR; expected BEGIN:VCALENDAR.")]
        [InlineData("BEGIN;X=1:VCALENDAR", "A BEGIN line cannot have parameters.")]
        [InlineData("BEGIN:V_CAL", "A BEGIN line must name a component: one or more letters, digits and hyphens.")]
        public void ReadCalendar_LineOutsideCalendar_Throws(string line, string expectedMessage)
        {
            // arrange
            var text = Crlf(line);

            // act
            Action testAction = () => ReadCalendars(text);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be(expectedMessage);
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\r\n")]
        public void ReadCalendar_NoCalendar_Throws(string text)
        {
            // act
            Action testAction = () => ReadCalendars(text);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        #endregion

        #region VERSION

        [Theory]
        [MemberData(nameof(VersionsIncluding2))]
        public void ReadCalendar_VersionIncluding2_IsAccepted(string version, string text)
        {
            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Version.Should().Be(version);
        }

        [Theory]
        [MemberData(nameof(VersionsIncluding2))]
        public void Validate_CalendarWithVersionIncluding2_ReturnsNothing(string version, string text)
        {
            // arrange
            _ = version;
            var calendar = ReadCalendar(text);

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(VersionsExcluding2))]
        public void ReadCalendar_VersionExcluding2_ThrowsAtVersionLine(string versionLine, string text)
        {
            // act
            Action testAction = () => ReadCalendars(text, new IcalOptions { AllowInvalidValues = true });

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Contain(versionLine).And.Contain("vCalendar 1.0 (VERSION:1.0) is not supported");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(3L);
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
        }

        [Fact]
        public void ReadCalendar_VCalendar10_ThrowsAtItsVersion()
        {
            // arrange
            using (var reader = CreateCalendarReader(CreateVCalendar10()))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<IcalFileException>().WithMessage("*VERSION:1.0*");
            }
        }

        [Fact]
        public void LinesRead_AfterVCalendar10_StopsAtItsVersion()
        {
            // arrange
            using (var reader = CreateCalendarReader(CreateVCalendar10()))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                var testResult = reader.LinesRead;

                // assert
                testResult.Should().Be(2);
            }
        }

        [Fact]
        public void ReadCalendar_AfterVCalendar10_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = CreateCalendarReader(CreateVCalendar10()))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCalendar_VCalendar10_ContentLinesStillReadable()
        {
            // arrange
            var text = Crlf(BeginCalendar, Version10, EndCalendar);
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Select(line => line.Value).Should().Equal(CalendarPath, "1.0", CalendarPath);
            }
        }

        #endregion

        #region Conformance

        [Theory]
        [MemberData(nameof(ConformanceCases.AccommodatedWithPropertyName), MemberType = typeof(ConformanceCases))]
        public void ReadCalendar_NonConformantPropertyWithDefaults_ThrowsForTheProperty(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];

            // act
            Action testAction = () => ReadCalendars(testCase.Text);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(testCase.PropertyName);
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.AccommodatedWithoutPropertyName), MemberType = typeof(ConformanceCases))]
        public void ReadCalendar_NonConformantComponentWithDefaults_ThrowsWithoutAPropertyName(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];

            // act
            Action testAction = () => ReadCalendars(testCase.Text);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
            exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public void ReadCalendar_NonConformantWithItsOption_IsRead(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];

            // act
            var testResult = ReadCalendars(testCase.Text, testCase.AcceptingOptions);

            // assert
            testResult.Should().ContainSingle();
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public void Validate_NonConformantCalendarReadWithItsOption_ReportsTheProblem(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var calendar = ReadCalendars(testCase.Text, testCase.AcceptingOptions).Single();

            // act
            var testResult = calendar.Validate();

            // assert
            var diagnostic = testResult.Should().ContainSingle().Subject;
            diagnostic.Kind.Should().Be(testCase.Kind);
            diagnostic.ComponentPath.Should().Be(testCase.ComponentPath);
            diagnostic.PropertyName.Should().Be(testCase.PropertyName);
            diagnostic.LineNumber.Should().BePositive();
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public void ReadCalendar_NonConformantWithOtherOptions_Throws(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];

            // act
            Action testAction = () => ReadCalendars(testCase.Text, testCase.AcceptingAllButOptions);

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
        }

        [Fact]
        public void ReadCalendar_BareLineFeedByDefault_Throws()
        {
            // act
            Action testAction = () => ReadCalendars(GoogleExportWithBareLineFeeds);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ReadCalendar_BareLineFeedAccepted_ReadsTheCalendar()
        {
            // act
            var testResult = ReadCalendars(GoogleExportWithBareLineFeeds, new IcalOptions { AcceptBareLineFeed = true });

            // assert
            testResult.Should().ContainSingle().Which.Events.Should().HaveCount(2);
        }

        [Fact]
        public void ReadCalendar_EventWithoutStartInCalendarWithMethod_IsRead()
        {
            // arrange
            var text = Crlf(BeginCalendar, Version20, ShortProductId, "METHOD:CANCEL", BeginEvent, "UID:e@example.com", NewYearStamp, EndEvent,
                EndCalendar);

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Events.Should().ContainSingle().Which.DtStart.Should().BeNull();
        }

        [Fact]
        public void ReadCalendar_AfterConformanceFailure_ReadsTheNextCalendar()
        {
            // arrange
            var text = ConformanceCases.All[ConformanceCases.MissingUidCase].Text + RfcFixtures.Rfc5545Event;
            using (var reader = CreateCalendarReader(text))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Events.Should().ContainSingle().Which.Uid.Should().Be(Rfc5545EventUid);
                reader.CalendarsRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadCalendar_ContentLineSyntaxError_Throws()
        {
            // arrange
            var text = Calendar(BeginEvent, "NO COLON", EndEvent);
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<IcalFileException>();
            }
        }

        [Fact]
        public void ReadCalendar_AfterContentLineSyntaxError_ThrowsInvalidOperation()
        {
            // arrange
            var text = Calendar(BeginEvent, "NO COLON", EndEvent);
            using (var reader = CreateCalendarReader(text))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region Disposal and concurrency

        [Theory]
        [MemberData(nameof(DisposedReaderMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposedException(Func<IcalReader, object> member)
        {
            // arrange
            var reader = CreateCalendarReader(RfcFixtures.Rfc5545Event);
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var reader = CreateCalendarReader(RfcFixtures.Rfc5545Event);
            reader.Dispose();

            // act
            Action testAction = () => reader.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public async Task ReadCalendar_WhileAsyncReadInProgress_Throws()
        {
            // arrange
            var source = new GatedTextReader(RfcFixtures.Rfc5545Event);
            using (var reader = new IcalReader(source))
            {
                var pending = reader.ReadCalendarAsync();
                await source.ReadStarted;

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("Another read is in progress.*");
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_SyncReadRejectedWhileInProgress_ReadsTheCalendar()
        {
            // arrange
            var source = new GatedTextReader(RfcFixtures.Rfc5545Event);
            using (var reader = new IcalReader(source))
            {
                var pending = reader.ReadCalendarAsync();
                await source.ReadStarted;
                Record.Exception(() => reader.ReadCalendar());
                source.OpenGate();

                // act
                var testResult = await pending;

                // assert
                testResult.Events.Should().ContainSingle();
            }
        }

        #endregion

        private static string CreateVCalendar10()
        {
            return Crlf(BeginCalendar, Version10, BeginEvent, "DTSTART:19960918T143000Z", "DALARM:19960918T133000Z;PT5M;2;Your Taxes Are Due !!!",
                "DESCRIPTION;ENCODING=QUOTED-PRINTABLE:Line one=0D=0A=", "Line two", EndEvent, EndCalendar);
        }
    }
}
