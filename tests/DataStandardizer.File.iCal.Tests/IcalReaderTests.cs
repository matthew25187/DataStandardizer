using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of <see cref="IcalReader"/>: the typed model it builds, the nesting of components, VERSION, and the conformance checks.
    /// </summary>
    public class IcalReaderTests
    {
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
            testEvent.Uid.Should().Be("uid1@example.com");
            testEvent.Organizer.Should().Be(new Uri("mailto:jsmith@example.com"));
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
        public void ReadCalendars_Rfc5545TimeZone_LoadsTimeZoneAndZonedEvent()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545TimeZone);

            // assert
            var timeZone = testResult.TimeZones.Should().ContainSingle().Subject;
            timeZone.TimeZoneId.Should().Be("America/New_York");
            var observances = timeZone.Observances.ToList();
            observances.Select(observance => observance.Kind).Should().Equal(IcalTimeZoneObservanceKind.Standard, IcalTimeZoneObservanceKind.Daylight);
            observances[0].Name.Should().Be("STANDARD");
            observances[0].DtStart.Should().Be(new IcalDateTime(new IcalDate(1998, 10, 25), new IcalTime(2, 0, 0)));
            observances[0].OffsetFrom.Should().Be(new IcalUtcOffset(TimeSpan.FromHours(-4)));
            observances[0].OffsetTo.Should().Be(new IcalUtcOffset(TimeSpan.FromHours(-5)));
            observances[0].TimeZoneName.Should().Be("EST");
            observances[1].TimeZoneName.Should().Be("EDT");

            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            var start = testEvent.DtStart.Value;
            start.IsDate.Should().BeFalse();
            start.DateTime.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 12), new IcalTime(8, 30, 0), "America/New_York"));
            testEvent.Classification.Should().Be("PUBLIC");
            testEvent.Created.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 9), new IcalTime(13, 0, 0, true)));
            testEvent.Location.Should().Be("1CP Conference Room 4350");
            var attendee = testEvent.Attendees.Should().ContainSingle().Subject;
            attendee.Value.Should().Be("mailto:employee-A@example.com");
            attendee.GetParameter("RSVP").Values.Should().Equal("TRUE");
            attendee.GetParameter("CUTYPE").Values.Should().Equal("GROUP");
            testEvent.GetProperty("ORGANIZER").GetParameter("ROLE").Values.Should().Equal("CHAIR");
        }

        [Fact]
        public void ReadCalendars_Rfc5545Todo_RejectsAbsoluteTriggerWithoutValueParameter()
        {
            // act
            Action act = () => ReadCalendars(RfcFixtures.Rfc5545Todo);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("TRIGGER");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VTODO[1]/VALARM[1]");
        }

        [Fact]
        public void ReadCalendars_Rfc5545TodoWithInvalidValuesAllowed_KeepsTriggerAsWritten()
        {
            // act
            var testResult = ReadCalendar(RfcFixtures.Rfc5545Todo, new IcalOptions { AllowInvalidValues = true });

            // assert
            var todo = testResult.Todos.Should().ContainSingle().Subject;
            var alarm = todo.Alarms.Should().ContainSingle().Subject;
            alarm.GetProperty("TRIGGER").Value.Should().Be("19980403T120000Z");
            alarm.TriggerDateTime.Should().BeNull();
            Action act = () => _ = alarm.TriggerDuration;
            act.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.PropertyName].Should().Be("TRIGGER");
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
            todo.Attendees.Should().ContainSingle().Which.GetParameter("PARTSTAT").Values.Should().Equal("ACCEPTED");

            var alarm = todo.Alarms.Should().ContainSingle().Subject;
            alarm.Action.Should().Be("AUDIO");
            alarm.TriggerDateTime.Should().Be(new IcalDateTime(new IcalDate(1998, 4, 3), new IcalTime(12, 0, 0, true)));
            alarm.TriggerDuration.Should().BeNull();
            alarm.Repeat.Should().Be(4);
            alarm.Duration.Should().Be(new IcalDuration(0, TimeSpan.FromHours(1)));
            var attachment = alarm.Attachments.Should().ContainSingle().Subject;
            attachment.Value.Should().Be("http://example.com/pub/audio-files/ssbanner.aud");
            attachment.GetParameter("FMTTYPE").Values.Should().Equal("audio/basic");
        }

        [Fact]
        public void ReadCalendars_Rfc5545Journal_LoadsJournal()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.Rfc5545Journal);

            // assert
            var journal = testResult.Journals.Should().ContainSingle().Subject;
            journal.Status.Should().Be("DRAFT");
            journal.Classification.Should().Be("PUBLIC");
            journal.Categories.Should().Equal("Project Report", "XYZ", "Weekly Meeting");
            journal.Descriptions.Should().ContainSingle().Which.GetTextValue().Should()
                .StartWith("Project xyz Review Meeting Minutes\nAgenda\n1. Review").And.Contain("John Smith, Jane Doe, Jim Dandy");
        }

        [Fact]
        public void ReadCalendars_Rfc5545FreeBusy_RejectsMissingDtStampAndUid()
        {
            // act
            Action act = () => ReadCalendars(RfcFixtures.Rfc5545FreeBusy);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
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
            freeBusy.Organizer.Should().Be(new Uri("mailto:jsmith@example.com"));
            freeBusy.Url.Should().Be(new Uri("http://www.example.com/calendar/busytime/jsmith.ifb"));
            freeBusy.Periods.Should().HaveCount(3);
            freeBusy.Periods[0].Start.Should().Be(new IcalDateTime(new IcalDate(1998, 3, 14), new IcalTime(23, 30, 0, true)));
            freeBusy.Uid.Should().BeNull();
            testResult.Validate().Select(diagnostic => diagnostic.PropertyName).Should().Equal("DTSTAMP", "UID");
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
            testResult.Images.Should().ContainSingle().Which.GetParameter("DISPLAY").Values.Should().Equal("BADGE");

            var testEvent = testResult.Events.Should().ContainSingle().Subject;
            testEvent.Color.Should().Be("red");
            testEvent.Images.Should().ContainSingle();
            testEvent.Conferences.Should().HaveCount(5);
            testEvent.Conferences[0].GetParameter("FEATURE").Values.Should().Equal("PHONE", "MODERATOR");
            testEvent.Conferences[0].GetParameter("LABEL").Values.Should().Equal("Moderator dial-in");
        }

        #endregion

        #region Vendor exports

        [Fact]
        public void ReadCalendars_GoogleExport_LoadsRecurringAndAllDayEvents()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.GoogleExport);

            // assert
            testResult.Method.Should().Be("PUBLISH");
            testResult.CalendarScale.Should().Be("GREGORIAN");
            testResult.GetProperty("X-WR-CALNAME").Value.Should().Be("Work");
            testResult.TimeZones.Single().GetProperty("X-LIC-LOCATION").Value.Should().Be("Pacific/Auckland");

            var events = testResult.Events.ToList();
            events.Should().HaveCount(2);
            events[0].RecurrenceRules.Should().ContainSingle().Which.Frequency.Should().Be(IcalRecurrenceFrequency.Weekly);
            events[0].DtStart.Value.DateTime.TimeZoneId.Should().Be("Pacific/Auckland");
            events[0].Description.Should().Be("Daily notes: https://example.com/notes\nBring, as always, coffee.");
            events[0].Location.Should().BeEmpty();
            events[0].Attendees.Single().GetParameter("X-NUM-GUESTS").Values.Should().Equal("0");
            events[0].Alarms.Single().TriggerDuration.Should().Be(new IcalDuration(0, TimeSpan.FromMinutes(10)).Negate());
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
            testResult.TimeZones.Single().TimeZoneId.Should().Be("New Zealand Standard Time");
            var testEvent = testResult.Events.Single();
            testEvent.DtStart.Value.DateTime.TimeZoneId.Should().Be("New Zealand Standard Time");
            testEvent.Uid.Should().HaveLength(112);
            testEvent.Description.Should().Be("\n");
            testEvent.Priority.Should().Be(5);
            testEvent.GetProperty("X-MICROSOFT-CDO-BUSYSTATUS").Value.Should().Be("BUSY");
            testEvent.Alarms.Single().GetProperty("TRIGGER").GetParameter("RELATED").Values.Should().Equal("START");
        }

        [Fact]
        public void ReadCalendars_AppleExport_RejectsRelativeAttachmentByDefault()
        {
            // act
            Action act = () => ReadCalendars(CalendarFixtures.AppleExport);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("ATTACH");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[1]/VALARM[1]");
        }

        [Fact]
        public void ReadCalendars_AppleExportWithInvalidValuesAllowed_KeepsExtensions()
        {
            // act
            var testResult = ReadCalendar(CalendarFixtures.AppleExport, new IcalOptions { AllowInvalidValues = true });

            // assert
            var testEvent = testResult.Events.Single();
            testEvent.Summary.Should().Be("Lunch");
            testEvent.Sequence.Should().Be(1);
            testEvent.GetProperty("X-APPLE-STRUCTURED-LOCATION").GetParameter("X-TITLE").Values.Should().Equal("Cafe");
            var alarm = testEvent.Alarms.Single();
            alarm.GetProperty("ACKNOWLEDGED").Value.Should().Be("20261103T113000Z");
            alarm.GetProperty("UID").Should().NotBeNull();
            testResult.Validate().Should().ContainSingle().Which.PropertyName.Should().Be("ATTACH");
        }

        #endregion

        #region Components

        [Fact]
        public void ReadCalendars_UnknownComponentAndProperties_AreKept()
        {
            // arrange
            var text = Calendar("X-CALENDAR-PROP;X-PARAM=1:value", "BEGIN:X-ABC-PROJECT", "X-NAME:Apollo", "BEGIN:X-ABC-TASK", "SUMMARY:Nested",
                "END:X-ABC-TASK", "END:X-ABC-PROJECT", "BEGIN:VJOURNAL", "UID:j@example.com", "DTSTAMP:20260101T000000Z", "IANA-PROP:x",
                "END:VJOURNAL");

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.GetProperty("X-CALENDAR-PROP").GetParameter("X-PARAM").Values.Should().Equal("1");
            var project = testResult.Components[0];
            project.Should().BeOfType<IcalComponent>();
            project.Name.Should().Be("X-ABC-PROJECT");
            project.GetProperty("x-name").Value.Should().Be("Apollo");
            project.Components.Should().ContainSingle().Which.Name.Should().Be("X-ABC-TASK");
            testResult.Journals.Single().GetProperty("IANA-PROP").Value.Should().Be("x");
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
            calendar.Name.Should().Be("VCALENDAR");
            calendar.Events.Should().ContainSingle().Which.Name.Should().Be("VEVENT");
        }

        [Theory]
        [InlineData("VTODO", "VEVENT", "VCALENDAR/VTODO[1]/VEVENT[1]")]
        [InlineData("VEVENT", "VCALENDAR", "VCALENDAR/VEVENT[1]/VCALENDAR[1]")]
        [InlineData("VJOURNAL", "VALARM", "VCALENDAR/VJOURNAL[1]/VALARM[1]")]
        public void ReadCalendars_KnownComponentInWrongParent_Throws(string parent, string child, string expectedPath)
        {
            // arrange
            var text = Calendar($"BEGIN:{parent}", "UID:p@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260101T000000Z", $"BEGIN:{child}",
                $"END:{child}", $"END:{parent}");
            var options = new IcalOptions
            {
                AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true, AllowUndefinedTimeZones = true
            };

            // act
            Action act = () => ReadCalendars(text, options);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidComponentNesting);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(expectedPath);
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(8L);
        }

        [Fact]
        public void ReadCalendars_ObservanceOutsideTimeZone_ThrowsWhateverTheOptions()
        {
            // arrange
            var text = EventCalendar("BEGIN:STANDARD", "END:STANDARD");
            var options = new IcalOptions
            {
                AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true, AllowUndefinedTimeZones = true
            };

            // act
            Action act = () => ReadCalendars(text, options);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidComponentNesting);
            exception.Message.Should().Be("A STANDARD component cannot be nested in a VEVENT component.");
        }

        [Fact]
        public void ReadCalendars_ManyCalendars_ReadsEachAndCounts()
        {
            // arrange
            var text = CalendarFixtures.GoogleExport + RfcFixtures.Rfc5545Event + CalendarFixtures.OutlookExport;
            using (var reader = CreateCalendarReader(text))
            {
                // act
                var testResult = reader.ReadCalendars().ToList();

                // assert
                testResult.Should().HaveCount(3);
                testResult[1].Events.Single().Uid.Should().Be("uid1@example.com");
                reader.CalendarsRead.Should().Be(3);
                reader.ComponentsRead.Should().Be(6 + 1 + 5);
                reader.LinesRead.Should().Be(ContentLines(text).Count);
                reader.PhysicalLinesRead.Should().Be(text.Split(new[] { "\r\n" }, StringSplitOptions.None).Length - 1);
                reader.ReadCalendar().Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendars_ComponentPaths_IndexSiblingsOfTheSameName()
        {
            // arrange
            var events = Enumerable.Range(1, 3).SelectMany(index => new[]
            {
                "BEGIN:VEVENT", $"UID:{index}@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260101T000000Z",
                index == 3 ? "SUMMARY:Third" : "SUMMARY:Other", index == 3 ? "SUMMARY:Again" : "COMMENT:-", "END:VEVENT", "BEGIN:VTODO", $"UID:t{index}",
                "DTSTAMP:20260101T000000Z", "END:VTODO"
            }).ToArray();

            // act
            Action act = () => ReadCalendars(Calendar(events));

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be("The VEVENT component has 2 SUMMARY properties; it allows at most one.");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[3]");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(4L + 2 * 11 + 5);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("SUMMARY");
        }

        #endregion

        #region BEGIN and END

        [Fact]
        public void ReadCalendar_MismatchedEnd_ThrowsWithLineAndFaults()
        {
            // arrange
            var text = Calendar("BEGIN:VEVENT", "UID:e", "END:VTODO");
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action act = () => reader.ReadCalendar();

                // assert
                var exception = act.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be("Found END:VTODO, but the BEGIN:VEVENT on line 4 is not closed; expected END:VEVENT.");
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(6L);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[1]");
                act.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCalendar_UnclosedComponent_ThrowsWithBeginLine()
        {
            // arrange
            var text = Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:x", "BEGIN:VEVENT", "UID:e");

            // act
            Action act = () => ReadCalendars(text);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be("The data ended before END:VEVENT; the BEGIN:VEVENT on line 4 is not closed.");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(4L);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[1]");
        }

        [Theory]
        [InlineData("END:VCALENDAR", "Found END:VCALENDAR with no BEGIN:VCALENDAR before it.")]
        [InlineData("BEGIN:VEVENT", "Found BEGIN:VEVENT outside a VCALENDAR; expected BEGIN:VCALENDAR.")]
        [InlineData("PRODID:x", "Found a PRODID property outside a VCALENDAR; expected BEGIN:VCALENDAR.")]
        [InlineData("BEGIN;X=1:VCALENDAR", "A BEGIN line cannot have parameters.")]
        [InlineData("BEGIN:V_CAL", "A BEGIN line must name a component: one or more letters, digits and hyphens.")]
        public void ReadCalendar_LineOutsideCalendar_Throws(string line, string expectedMessage)
        {
            // act
            Action act = () => ReadCalendars(Crlf(line));

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be(expectedMessage);
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\r\n")]
        public void ReadCalendar_NoCalendar_Throws(string text)
        {
            // act
            Action act = () => ReadCalendars(text);

            // assert
            act.Should().Throw<IcalFileException>();
        }

        #endregion

        #region VERSION

        [Theory]
        [InlineData("2.0")]
        [InlineData("1.0;2.0")]
        [InlineData("2.0;3.0")]
        public void ReadCalendar_VersionIncluding2_IsAccepted(string version)
        {
            // arrange
            var text = RfcFixtures.Rfc5545Event.Replace("VERSION:2.0", "VERSION:" + version);

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Version.Should().Be(version);
            testResult.Validate().Should().BeEmpty();
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("3.0")]
        [InlineData("1.0;1.5")]
        [InlineData("2.1;3.0")]
        [InlineData("2.0;2.0;2.0")]
        [InlineData("two")]
        public void ReadCalendar_VersionExcluding2_ThrowsAtVersionLine(string version)
        {
            // arrange
            var text = RfcFixtures.Rfc5545Event.Replace("VERSION:2.0", "VERSION:" + version);

            // act
            Action act = () => ReadCalendars(text, new IcalOptions { AllowInvalidValues = true });

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Contain("VERSION:" + version).And.Contain("vCalendar 1.0 (VERSION:1.0) is not supported");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(3L);
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
        }

        [Fact]
        public void ReadCalendar_VCalendar10_ThrowsBeforeReadingItsBody()
        {
            // arrange
            var text = Crlf("BEGIN:VCALENDAR", "VERSION:1.0", "BEGIN:VEVENT", "DTSTART:19960918T143000Z",
                "DALARM:19960918T133000Z;PT5M;2;Your Taxes Are Due !!!", "DESCRIPTION;ENCODING=QUOTED-PRINTABLE:Line one=0D=0A=", "Line two",
                "END:VEVENT", "END:VCALENDAR");
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action act = () => reader.ReadCalendar();

                // assert
                act.Should().Throw<IcalFileException>().WithMessage("*VERSION:1.0*");
                reader.LinesRead.Should().Be(2);
                act.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCalendar_VCalendar10_ContentLinesStillReadable()
        {
            // arrange
            var text = Crlf("BEGIN:VCALENDAR", "VERSION:1.0", "END:VCALENDAR");
            using (var reader = CreateReader(text))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Select(line => line.Value).Should().Equal("VCALENDAR", "1.0", "VCALENDAR");
            }
        }

        #endregion

        #region Conformance

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public void ReadCalendar_NonConformantWithDefaults_Throws(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];

            // act
            Action act = () => ReadCalendars(testCase.Text);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
            if (testCase.PropertyName is null)
            {
                exception.Data.Contains(IcalFileException.DataItemName.PropertyName).Should().BeFalse();
            }
            else
            {
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(testCase.PropertyName);
            }
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
            var calendar = testResult.Should().ContainSingle().Subject;
            var diagnostic = calendar.Validate().Should().ContainSingle().Subject;
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
            Action act = () => ReadCalendars(testCase.Text, testCase.AcceptingAllButOptions);

            // assert
            act.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
        }

        [Fact]
        public void ReadCalendar_BareLineFeed_RejectedUnlessAccepted()
        {
            // arrange
            var text = CalendarFixtures.GoogleExport.Replace("\r\n", "\n");

            // act
            Action act = () => ReadCalendars(text);
            var testResult = ReadCalendars(text, new IcalOptions { AcceptBareLineFeed = true });

            // assert
            act.Should().Throw<IcalFileException>();
            testResult.Should().ContainSingle().Which.Events.Should().HaveCount(2);
        }

        [Fact]
        public void ReadCalendar_EventWithoutStartInCalendarWithMethod_IsRead()
        {
            // arrange
            var text = Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:x", "METHOD:CANCEL", "BEGIN:VEVENT", "UID:e@example.com",
                "DTSTAMP:20260101T000000Z", "END:VEVENT", "END:VCALENDAR");

            // act
            var testResult = ReadCalendar(text);

            // assert
            testResult.Events.Single().DtStart.Should().BeNull();
        }

        [Fact]
        public void ReadCalendar_ConformanceFailure_ReaderContinuesWithNextCalendar()
        {
            // arrange
            var text = ConformanceCases.All["MissingUid"].Text + RfcFixtures.Rfc5545Event;
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action act = () => reader.ReadCalendar();
                act.Should().Throw<IcalFileException>();
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Events.Single().Uid.Should().Be("uid1@example.com");
                reader.CalendarsRead.Should().Be(2);
                reader.ReadCalendar().Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendar_ContentLineSyntaxError_Faults()
        {
            // arrange
            var text = Calendar("BEGIN:VEVENT", "NO COLON", "END:VEVENT");
            using (var reader = CreateCalendarReader(text))
            {
                // act
                Action act = () => reader.ReadCalendar();

                // assert
                act.Should().Throw<IcalFileException>();
                act.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion

        #region Disposal and concurrency

        [Fact]
        public void Dispose_ThenMembers_ThrowObjectDisposedException()
        {
            // arrange
            var reader = CreateCalendarReader(RfcFixtures.Rfc5545Event);
            reader.Dispose();

            // act
            Action[] members =
            {
                () => reader.ReadCalendar(), () => reader.ReadCalendars(), () => reader.ReadCalendarAsync(), () => reader.ReadCalendarsAsync(),
                () => _ = reader.CalendarsRead, () => _ = reader.ComponentsRead, () => _ = reader.LinesRead, () => _ = reader.PhysicalLinesRead
            };

            // assert
            foreach (var member in members)
            {
                member.Should().Throw<ObjectDisposedException>();
            }

            reader.Dispose();
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
                Action act = () => reader.ReadCalendar();

                // assert
                act.Should().Throw<InvalidOperationException>().WithMessage("Another read is in progress.*");
                source.OpenGate();
                (await pending).Events.Should().ContainSingle();
            }
        }

        #endregion
    }
}
