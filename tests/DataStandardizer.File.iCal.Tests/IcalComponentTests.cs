using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string Trigger = "TRIGGER";
        private const string Summary = "SUMMARY";
        private const string Version = "VERSION";
        private const string Comment = "COMMENT";
        private const string Categories = "CATEGORIES";
        private const string DtStart = "DTSTART";
        private const string DtEndProperty = "DTEND";
        private const string CreatedProperty = "CREATED";
        private const string DtStampProperty = "DTSTAMP";
        private const string LastModifiedProperty = "LAST-MODIFIED";
        private const string CompletedProperty = "COMPLETED";
        private const string RecurrenceRule = "RRULE";
        private const string ValueParameter = "VALUE";
        private const string TimeZoneIdParameter = "TZID";
        private const string VendorParameter = "X-VENDOR";
        private const int SharedThreeLevels = 3;
        private const string SharedMessage = "A X-A component is nested in more than one place; each place needs a component of its own.";
        private const string KeepParameter = "X-KEEP";
        private const string TestComponentName = "X-TEST";
        private const string DisplayAction = "ACTION:DISPLAY";
        private const string ReminderDescription = "DESCRIPTION:Reminder";
        private const string AbsoluteTriggerWithVendorParameter = "TRIGGER;X-VENDOR=keep;VALUE=DATE-TIME:20260105T083000Z";
        private const string UndefinedTimeZoneId = "Nowhere";
        private const string AucklandTimeZoneId = "Pacific/Auckland";
        private const string CalendarSource = "https://example.com/cal.ics";

        // ref. RFC 5545 §3.8.4.6: the URL property.
        private const string UrlProperty = "URL";

        // Stamp, written as a DATE-TIME value.
        private const string StampValue = "20261015T090000Z";

        // Stamp's wall-clock time, written as a DATE-TIME value without the UTC designator.
        private const string StampLocalValue = "20261015T090000";

        // The text that the SUMMARY test sets, with each character that TEXT escapes, and the value it is written as.
        private const string SummaryText = "Line one\nTwo; three, four\\";
        private const string EscapedSummaryText = "Line one\\nTwo\\; three\\, four\\\\";

        // The line numbers in EventCalendar of the properties after the DTSTART that it adds.
        private const long FirstEventLineNumber = 8;

        private static readonly IcalDate StampDate = new IcalDate(2026, 10, 15);
        private static readonly IcalDateTime Stamp = new IcalDateTime(StampDate, new IcalTime(9, 0, 0, true));

        // Stamp's wall-clock time, without the UTC designator.
        private static readonly IcalTime StampLocalTime = new IcalTime(9, 0, 0);

        // Stamp's date and wall-clock time, as a floating DATE-TIME.
        private static readonly IcalDateTime StampLocal = new IcalDateTime(StampDate, StampLocalTime);

        public static TheoryData<Action> NullListItemCalls()
        {
            var component = new IcalEvent();
            component.Properties.Add(new IcalContentLine(Summary, "x"));
            component.Components.Add(new IcalAlarm());
            return new TheoryData<Action>
            {
                () => component.Properties.Add(null),
                () => component.Properties[0] = null,
                () => component.Components.Add(null),
                () => component.Components.Insert(0, null),
            };
        }

        public static TheoryData<Func<IcalComponent>, string> TypedComponents => new TheoryData<Func<IcalComponent>, string>
        {
            { () => new IcalCalendar(), "VCALENDAR" },
            { () => new IcalEvent(), "VEVENT" },
            { () => new IcalTodo(), "VTODO" },
            { () => new IcalJournal(), "VJOURNAL" },
            { () => new IcalFreeBusy(), "VFREEBUSY" },
            { () => new IcalTimeZone(), "VTIMEZONE" },
            { () => new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard), "STANDARD" },
            { () => new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Daylight), "DAYLIGHT" },
            { () => new IcalAlarm(), "VALARM" },
        };

        // Each accessor of an event whose property has a value that is not valid, with the line number of the property.
        public static TheoryData<Func<IcalEvent, object>, long> InvalidValueGetters => new TheoryData<Func<IcalEvent, object>, long>
        {
            { testEvent => testEvent.DtEnd, FirstEventLineNumber },
            { testEvent => testEvent.Priority, 9 },
            { testEvent => testEvent.Organizer, 10 },
            { testEvent => testEvent.RecurrenceRules, 11 },
            { testEvent => testEvent.Summary, 12 },
        };

        // Each accessor of a calendar component, with the number of components of its type in CalendarWithEveryKind.
        public static TheoryData<Func<IcalCalendar, IEnumerable<IcalComponent>>, int> ComponentAccessors =>
            new TheoryData<Func<IcalCalendar, IEnumerable<IcalComponent>>, int>
            {
                { calendar => calendar.Events, 2 },
                { calendar => calendar.TimeZones, 1 },
                { calendar => calendar.Todos, 1 },
                { calendar => calendar.Journals, 1 },
                { calendar => calendar.FreeBusy, 1 },
                { calendar => calendar.Components, 7 },
            };

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
            Action testAction = () => new IcalComponent(name);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
        }

        [Theory]
        [MemberData(nameof(NullListItemCalls), DisableDiscoveryEnumeration = true)]
        public void PropertiesAndComponents_NullItem_ThrowsArgumentNull(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(TypedComponents), DisableDiscoveryEnumeration = true)]
        public void Name_TypedComponent_IsItsComponentName(Func<IcalComponent> create, string expected)
        {
            // arrange
            var component = create();

            // act
            var testResult = component.Name;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Version_NewCalendar_Is20()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            var testResult = calendar.Version;

            // assert
            testResult.Should().Be("2.0");
        }

        // ref. RFC 5545 §3.7.4: vervalue = "2.0" / maxver / (minver ";" maxver).  The semicolon of a range separates its ends, so it is kept.
        [Theory]
        [InlineData("2.0", "2.0")]
        [InlineData("2.0;2.0", "2.0;2.0")]
        [InlineData("2\\,0;2.0", "2,0;2.0")]
        public void Version_RangeOrSingleVersion_IsUnescapedWithItsSemicolon(string value, string expected)
        {
            // arrange
            var calendar = new IcalCalendar();
            calendar.SetProperty(new IcalContentLine(Version, value));

            // act
            var testResult = calendar.Version;

            // assert
            testResult.Should().Be(expected);
        }

        // ref. RFC 5545 §3.7.4 and §3.3.11: a VERSION has at most one semicolon that is not escaped, and no such comma.
        [Theory]
        [InlineData("2.0;2.0;2.0")]
        [InlineData("2.0,2.0")]
        [InlineData("2.0;2,0")]
        [InlineData("2.0;\\x")]
        public void Version_InvalidText_Throws(string value)
        {
            // arrange
            var calendar = new IcalCalendar();
            calendar.SetProperty(new IcalContentLine(Version, value));

            // act
            Func<string> testAction = () => calendar.Version;

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        // ref. RFC 5545 §3.3.11: TSAFE-CHAR excludes "," and ";".  AllowInvalidValues lets the reader keep such a value, but the getter cannot
        // read it as TEXT.
        [Theory]
        [InlineData("SUMMARY:Lunch, then review")]
        [InlineData("SUMMARY:Lunch; then review")]
        public void Summary_UnescapedCommaOrSemicolonAllowedByReader_Throws(string line)
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var testEvent = ReadCalendar(EventCalendar(line), options).Events.Single();

            // act
            Func<string> testAction = () => testEvent.Summary;

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void Constructor_UndefinedObservanceKind_ThrowsArgumentOutOfRange()
        {
            // act
            Action testAction = () => new IcalTimeZoneObservance((IcalTimeZoneObservanceKind)0);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetProperties_NameInOtherCase_GetsEveryPropertyOfTheName()
        {
            // arrange
            var component = CreateComponentWithComments();

            // act
            var testResult = component.GetProperties("comment");

            // assert
            testResult.Select(property => property.Value).Should().Equal("a", "c");
        }

        [Fact]
        public void GetProperty_NameInOtherCase_GetsTheFirstPropertyOfTheName()
        {
            // arrange
            var component = CreateComponentWithComments();

            // act
            var testResult = component.GetProperty(Comment);

            // assert
            testResult.Value.Should().Be("a");
        }

        [Fact]
        public void RemoveProperties_NameInOtherCase_RemovesEveryPropertyOfTheName()
        {
            // arrange
            var component = CreateComponentWithComments();

            // act
            var testResult = component.RemoveProperties("comment");

            // assert
            testResult.Should().Be(2);
            component.Properties.Should().ContainSingle().Which.Name.Should().Be(Summary);
        }

        [Fact]
        public void SetProperty_NameInOtherCase_ReplacesEveryPropertyOfTheNameInPlaceOfTheFirst()
        {
            // arrange
            var component = new IcalComponent(TestComponentName);
            component.Properties.Add(new IcalContentLine("A", "1"));
            component.Properties.Add(new IcalContentLine("B", "1"));
            component.Properties.Add(new IcalContentLine("A", "2"));

            // act
            component.SetProperty(new IcalContentLine("a", "3"));

            // assert
            component.Properties.Select(property => property.Name).Should().Equal("a", "B");
            component.Properties.Select(property => property.Value).Should().Equal("3", "1");
        }

        [Fact]
        public void SetProperty_NewName_AddsThePropertyAtTheEnd()
        {
            // arrange
            var component = new IcalComponent(TestComponentName);
            component.Properties.Add(new IcalContentLine("A", "1"));

            // act
            component.SetProperty(new IcalContentLine("C", "1"));

            // assert
            component.Properties.Select(property => property.Name).Should().Equal("A", "C");
        }

        #endregion

        #region Typed accessors

        [Fact]
        public void Summary_SetOnEventWithTwoSummaries_ReplacesThemInPlaceKeepingTheFirstParameters()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("SUMMARY;LANGUAGE=en-NZ:Old", "SUMMARY;LANGUAGE=de:Alt"),
                new IcalOptions { AllowCardinalityViolations = true }).Events.Single();

            // act
            testEvent.Summary = SummaryText;

            // assert
            testEvent.Properties.Where(property => property.Name == Summary).Should().ContainSingle();
            testEvent.Properties[3].Name.Should().Be(Summary);
            testEvent.Properties[3].Value.Should().Be(EscapedSummaryText);
            testEvent.Properties[3].Parameters.Should().ContainSingle(parameter => parameter.Name == "LANGUAGE").Which.Values.Should().Equal("en-NZ");
        }

        [Fact]
        public void Summary_GetAfterSet_IsTheTextThatWasSet()
        {
            // arrange
            var testEvent = new IcalEvent { Summary = SummaryText };

            // act
            var testResult = testEvent.Summary;

            // assert
            testResult.Should().Be(SummaryText);
        }

        [Fact]
        public void Summary_SetNull_RemovesProperty()
        {
            // arrange
            var testEvent = new IcalEvent { Summary = "x", Location = "y" };

            // act
            testEvent.Summary = null;

            // assert
            testEvent.Properties.Should().ContainSingle().Which.Name.Should().Be("LOCATION");
        }

        [Fact]
        public void Categories_GetFromTwoProperties_ReadsEveryItem()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("CATEGORIES:A,B\\,C", "CATEGORIES:D")).Events.Single();

            // act
            var testResult = testEvent.Categories;

            // assert
            testResult.Should().Equal("A", "B,C", "D");
        }

        [Fact]
        public void Categories_SetOnEventWithTwoProperties_WritesOneProperty()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("CATEGORIES:A,B\\,C", "CATEGORIES:D")).Events.Single();

            // act
            testEvent.Categories = new[] { "X,Y", "Z" };

            // assert
            testEvent.Properties.Should().ContainSingle(property => property.Name == Categories).Which.Value.Should().Be("X\\,Y,Z");
        }

        [Fact]
        public void Categories_GetAfterSet_IsTheItemsThatWereSet()
        {
            // arrange
            var testEvent = new IcalEvent { Categories = new[] { "X,Y", "Z" } };

            // act
            var testResult = testEvent.Categories;

            // assert
            testResult.Should().Equal("X,Y", "Z");
        }

        [Fact]
        public void Categories_SetEmpty_RemovesProperty()
        {
            // arrange
            var testEvent = new IcalEvent { Categories = new[] { "Z" } };

            // act
            testEvent.Categories = new string[0];

            // assert
            testEvent.Properties.Should().NotContain(property => property.Name == Categories);
        }

        [Fact]
        public void DtStart_SetZonedDateTime_WritesValueAndTimeZoneParameter()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("DTSTART;X-KEEP=1;VALUE=DATE:20260101")).Events.Single();

            // act
            testEvent.DtStart = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 0), AucklandTimeZoneId);

            // assert
            var line = testEvent.Properties.Should().ContainSingle(property => property.Name == DtStart).Subject;
            line.Value.Should().Be("20260301T093000");
            line.Parameters.Select(parameter => parameter.Name).Should().Equal(KeepParameter, "TZID");
            line.Parameters.Select(parameter => parameter.Values[0]).Should().Equal("1", AucklandTimeZoneId);
        }

        [Fact]
        public void DtStart_SetDateOnZonedStart_WritesValueAndValueParameter()
        {
            // arrange
            var testEvent = ReadCalendar(EventCalendar("DTSTART;X-KEEP=1;VALUE=DATE:20260101")).Events.Single();
            testEvent.DtStart = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 0), AucklandTimeZoneId);

            // act
            testEvent.DtStart = new IcalDate(2026, 3, 2);

            // assert
            var line = testEvent.Properties.Should().ContainSingle(property => property.Name == DtStart).Subject;
            line.Value.Should().Be("20260302");
            line.Parameters.Select(parameter => parameter.Name).Should().Equal(KeepParameter, ValueParameter);
            line.Parameters.Select(parameter => parameter.Values[0]).Should().Equal("1", "DATE");
        }

        [Fact]
        public void DtStart_GetAfterSettingDate_IsTheDate()
        {
            // arrange
            var testEvent = new IcalEvent { DtStart = new IcalDate(2026, 3, 2) };

            // act
            var testResult = testEvent.DtStart;

            // assert
            testResult.Should().Be((IcalDateOrDateTime)new IcalDate(2026, 3, 2));
        }

        [Theory]
        [MemberData(nameof(InvalidValueGetters), DisableDiscoveryEnumeration = true)]
        public void Accessor_InvalidValue_ThrowsWithLineNumber(Func<IcalEvent, object> getter, long expectedLineNumber)
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var testEvent = ReadCalendar(EventCalendar("DTEND:2026", "PRIORITY:high", "ORGANIZER:not a uri", "RRULE:FREQ=SOMETIMES", "SUMMARY:\\x"),
                options).Events.Single();

            // act
            Action testAction = () => getter(testEvent);

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.LineNumber].Should().Be(expectedLineNumber);
        }

        [Fact]
        public void RefreshInterval_Set_WritesDurationWithValueParameter()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            calendar.RefreshInterval = new IcalDuration(1, TimeSpan.Zero);

            // assert
            var line = calendar.Properties.Should().ContainSingle(property => property.Name == "REFRESH-INTERVAL").Subject;
            line.Value.Should().Be("P1D");
            line.Parameters.Should().ContainSingle(parameter => parameter.Name == ValueParameter).Which.Values.Should().Equal("DURATION");
        }

        [Fact]
        public void RefreshInterval_GetAfterSet_IsTheDuration()
        {
            // arrange
            var calendar = new IcalCalendar { RefreshInterval = new IcalDuration(1, TimeSpan.Zero) };

            // act
            var testResult = calendar.RefreshInterval;

            // assert
            testResult.Should().Be(new IcalDuration(1, TimeSpan.Zero));
        }

        [Fact]
        public void Source_Set_WritesUriWithValueParameter()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            calendar.Source = new Uri(CalendarSource);

            // assert
            var line = calendar.Properties.Should().ContainSingle(property => property.Name == "SOURCE").Subject;
            line.Value.Should().Be(CalendarSource);
            line.Parameters.Should().ContainSingle(parameter => parameter.Name == ValueParameter).Which.Values.Should().Equal("URI");
        }

        [Fact]
        public void Source_GetAfterSet_IsTheUri()
        {
            // arrange
            var calendar = new IcalCalendar { Source = new Uri(CalendarSource) };

            // act
            var testResult = calendar.Source;

            // assert
            testResult.Should().Be(new Uri(CalendarSource));
        }

        [Fact]
        public void Url_Set_WritesUriWithoutParameters()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            calendar.Url = new Uri("https://example.com/");

            // assert
            calendar.Properties.Should().ContainSingle(property => property.Name == UrlProperty).Which.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void Url_SetRelativeUri_ThrowsArgumentException()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            Action testAction = () => calendar.Url = new Uri("relative", UriKind.Relative);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Url_SetNotNormalizedUri_WritesItAsGiven()
        {
            // arrange
            var calendar = new IcalCalendar();

            // act
            calendar.Url = new Uri(UriTestData.UpperCaseUri);

            // assert
            calendar.Properties.Should().ContainSingle(property => property.Name == UrlProperty).Which.Value.Should().Be(UriTestData.UpperCaseUri);
        }

        [Fact]
        public void Url_GetAfterSetNotNormalizedUri_HasTheTextAsGiven()
        {
            // arrange
            var calendar = new IcalCalendar { Url = new Uri(UriTestData.UpperCaseUri) };

            // act
            var testResult = calendar.Url;

            // assert
            testResult.OriginalString.Should().Be(UriTestData.UpperCaseUri);
        }

        [Theory]
        [MemberData(nameof(UriTestData.UriTextsNotAllowed), MemberType = typeof(UriTestData))]
        public void Url_SetUriTextNotAllowed_ThrowsArgumentException(string value)
        {
            // arrange
            var testEvent = new IcalEvent();
            var uri = new Uri(value);

            // act
            Action testAction = () => testEvent.Url = uri;

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("value");
        }

        [Fact]
        public void PercentComplete_Set_WritesInteger()
        {
            // arrange
            var todo = new IcalTodo();

            // act
            todo.PercentComplete = 40;

            // assert
            todo.Properties.Should().ContainSingle(property => property.Name == "PERCENT-COMPLETE").Which.Value.Should().Be("40");
        }

        [Fact]
        public void PercentComplete_GetAfterSet_IsTheNumber()
        {
            // arrange
            var todo = new IcalTodo { PercentComplete = 40 };

            // act
            var testResult = todo.PercentComplete;

            // assert
            testResult.Should().Be(40);
        }

        [Fact]
        public void Duration_Set_WritesDuration()
        {
            // arrange
            var todo = new IcalTodo();

            // act
            todo.Duration = new IcalDuration(0, TimeSpan.FromMinutes(90));

            // assert
            todo.Properties.Should().ContainSingle(property => property.Name == "DURATION").Which.Value.Should().Be("PT1H30M");
        }

        [Fact]
        public void RecurrenceRules_SetTwoRules_WritesOnePropertyPerRule()
        {
            // arrange
            var observance = CreateDaylightObservance();

            // act
            observance.RecurrenceRules = CreateTwoRules();

            // assert
            observance.Properties.Where(property => property.Name == RecurrenceRule).Select(property => property.Value).Should()
                .Equal("FREQ=YEARLY;BYDAY=-1SU;BYMONTH=9", "FREQ=YEARLY;COUNT=1");
        }

        [Fact]
        public void RecurrenceRules_GetAfterSettingTwoRules_HasBothRules()
        {
            // arrange
            var observance = CreateDaylightObservance();
            observance.RecurrenceRules = CreateTwoRules();

            // act
            var testResult = observance.RecurrenceRules;

            // assert
            testResult.Should().Equal(CreateTwoRules());
        }

        [Fact]
        public void Validate_ObservanceWithTwoRules_ReturnsNothing()
        {
            // arrange
            var observance = CreateDaylightObservance();
            observance.RecurrenceRules = CreateTwoRules();

            // act
            var testResult = observance.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void RecurrenceRules_SetEmpty_RemovesProperties()
        {
            // arrange
            var observance = CreateDaylightObservance();
            observance.RecurrenceRules = CreateTwoRules();

            // act
            observance.RecurrenceRules = new IcalRecurrenceRule[0];

            // assert
            observance.Properties.Should().NotContain(property => property.Name == RecurrenceRule);
        }

        [Fact]
        public void TriggerDuration_Set_WritesDuration()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO" };

            // act
            alarm.TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-5));

            // assert
            alarm.Properties.Should().ContainSingle(property => property.Name == Trigger).Which.Value.Should().Be("-PT5M");
        }

        [Fact]
        public void TriggerDateTime_SetOnRelativeTrigger_WritesDateTimeWithValueParameter()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-5)) };

            // act
            alarm.TriggerDateTime = Stamp;

            // assert
            var line = alarm.Properties.Should().ContainSingle(property => property.Name == Trigger).Subject;
            line.Value.Should().Be(StampValue);
            line.Parameters.Should().ContainSingle(parameter => parameter.Name == ValueParameter).Which.Values.Should().Equal("DATE-TIME");
        }

        [Fact]
        public void TriggerDuration_GetAfterSettingDateTime_IsNull()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-5)), TriggerDateTime = Stamp };

            // act
            var testResult = alarm.TriggerDuration;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void TriggerDateTime_GetAfterSet_IsTheDateTime()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDateTime = Stamp };

            // act
            var testResult = alarm.TriggerDateTime;

            // assert
            testResult.Should().Be(Stamp);
        }

        [Fact]
        public void TriggerDuration_SetOnAbsoluteTrigger_WritesDurationWithoutParameters()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDateTime = Stamp };

            // act
            alarm.TriggerDuration = IcalDuration.FromWeeks(1);

            // assert
            alarm.Properties.Should().ContainSingle(property => property.Name == Trigger).Which.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void TriggerDateTime_SetNull_RemovesTrigger()
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO", TriggerDateTime = Stamp };

            // act
            alarm.TriggerDateTime = null;

            // assert
            alarm.Properties.Should().NotContain(property => property.Name == Trigger);
        }

        // A floating date-time and a zoned one, neither of them in UTC, which RFC 5545 requires of an absolute TRIGGER (§3.8.6.3) and of
        // every property in UtcDateTimeAccessors.
        public static TheoryData<IcalDateTime> NonUtcDateTimes => new TheoryData<IcalDateTime>
        {
            new IcalDateTime(StampDate, StampLocalTime),
            new IcalDateTime(StampDate, StampLocalTime, AucklandTimeZoneId),
        };

        [Theory]
        [MemberData(nameof(NonUtcDateTimes))]
        public void TriggerDateTime_SetNotUtc_ThrowsArgumentException(IcalDateTime value)
        {
            // arrange
            var alarm = new IcalAlarm { Action = "AUDIO" };

            // act
            Action testAction = () => alarm.TriggerDateTime = value;

            // assert
            testAction.Should().ThrowExactly<ArgumentException>().WithParameterName("value");
        }

        [Theory]
        [MemberData(nameof(NonUtcDateTimes))]
        public void TriggerDateTime_SetNotUtc_LeavesExistingTriggerUnchanged(IcalDateTime value)
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;RELATED=END:-PT15M", DisplayAction, ReminderDescription);

            // act
            var exception = Record.Exception(() => alarm.TriggerDateTime = value);
            var testResult = alarm.Properties[0];

            // assert
            exception.Should().BeOfType<ArgumentException>();
            testResult.Name.Should().Be(Trigger);
            testResult.Parameters.Select(parameter => parameter.Name).Should().Equal(VendorParameter, "RELATED");
            testResult.Value.Should().Be("-PT15M");
        }

        /// <summary>
        /// A typed accessor for a property that RFC 5545 requires to be in UTC, with the component it belongs to.
        /// </summary>
        public sealed class UtcDateTimeAccessor
        {
            public UtcDateTimeAccessor(string name, string propertyName, Func<IcalComponent> createComponent,
                Action<IcalComponent, IcalDateTime?> setValue, Func<IcalComponent, IcalDateTime?> getValue)
            {
                Name = name;
                PropertyName = propertyName;
                CreateComponent = createComponent;
                SetValue = setValue;
                GetValue = getValue;
            }

            public string Name { get; }

            public string PropertyName { get; }

            public Func<IcalComponent> CreateComponent { get; }

            public Action<IcalComponent, IcalDateTime?> SetValue { get; }

            public Func<IcalComponent, IcalDateTime?> GetValue { get; }

            public override string ToString() => Name;
        }

        // RFC 5545 §3.8.2.1 (COMPLETED), §3.8.7.1 (CREATED), §3.8.7.2 (DTSTAMP) and §3.8.7.3 (LAST-MODIFIED): each value "MUST be specified
        // in the UTC time format".  RFC 7986 §5.4 gives VCALENDAR the same LAST-MODIFIED property.
        private static readonly UtcDateTimeAccessor[] UtcDateTimeAccessors =
        {
            new UtcDateTimeAccessor("IcalCalendar.LastModified", LastModifiedProperty, () => new IcalCalendar(),
                (component, value) => ((IcalCalendar)component).LastModified = value, component => ((IcalCalendar)component).LastModified),
            new UtcDateTimeAccessor("IcalEvent.Created", CreatedProperty, () => new IcalEvent(),
                (component, value) => ((IcalEvent)component).Created = value, component => ((IcalEvent)component).Created),
            new UtcDateTimeAccessor("IcalEvent.DtStamp", DtStampProperty, () => new IcalEvent(),
                (component, value) => ((IcalEvent)component).DtStamp = value, component => ((IcalEvent)component).DtStamp),
            new UtcDateTimeAccessor("IcalEvent.LastModified", LastModifiedProperty, () => new IcalEvent(),
                (component, value) => ((IcalEvent)component).LastModified = value, component => ((IcalEvent)component).LastModified),
            new UtcDateTimeAccessor("IcalFreeBusy.DtStamp", DtStampProperty, () => new IcalFreeBusy(),
                (component, value) => ((IcalFreeBusy)component).DtStamp = value, component => ((IcalFreeBusy)component).DtStamp),
            new UtcDateTimeAccessor("IcalJournal.Created", CreatedProperty, () => new IcalJournal(),
                (component, value) => ((IcalJournal)component).Created = value, component => ((IcalJournal)component).Created),
            new UtcDateTimeAccessor("IcalJournal.DtStamp", DtStampProperty, () => new IcalJournal(),
                (component, value) => ((IcalJournal)component).DtStamp = value, component => ((IcalJournal)component).DtStamp),
            new UtcDateTimeAccessor("IcalJournal.LastModified", LastModifiedProperty, () => new IcalJournal(),
                (component, value) => ((IcalJournal)component).LastModified = value, component => ((IcalJournal)component).LastModified),
            new UtcDateTimeAccessor("IcalTimeZone.LastModified", LastModifiedProperty, () => new IcalTimeZone(),
                (component, value) => ((IcalTimeZone)component).LastModified = value, component => ((IcalTimeZone)component).LastModified),
            new UtcDateTimeAccessor("IcalTodo.Completed", CompletedProperty, () => new IcalTodo(),
                (component, value) => ((IcalTodo)component).Completed = value, component => ((IcalTodo)component).Completed),
            new UtcDateTimeAccessor("IcalTodo.Created", CreatedProperty, () => new IcalTodo(),
                (component, value) => ((IcalTodo)component).Created = value, component => ((IcalTodo)component).Created),
            new UtcDateTimeAccessor("IcalTodo.DtStamp", DtStampProperty, () => new IcalTodo(),
                (component, value) => ((IcalTodo)component).DtStamp = value, component => ((IcalTodo)component).DtStamp),
            new UtcDateTimeAccessor("IcalTodo.LastModified", LastModifiedProperty, () => new IcalTodo(),
                (component, value) => ((IcalTodo)component).LastModified = value, component => ((IcalTodo)component).LastModified),
        };

        public static TheoryData<UtcDateTimeAccessor> UtcDateTimeProperties
        {
            get
            {
                var data = new TheoryData<UtcDateTimeAccessor>();
                foreach (var accessor in UtcDateTimeAccessors)
                    data.Add(accessor);
                return data;
            }
        }

        public static TheoryData<UtcDateTimeAccessor, IcalDateTime> UtcDateTimePropertiesWithNonUtcValues
        {
            get
            {
                var data = new TheoryData<UtcDateTimeAccessor, IcalDateTime>();
                foreach (var accessor in UtcDateTimeAccessors)
                {
                    foreach (var value in NonUtcDateTimes)
                        data.Add(accessor, value);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(UtcDateTimeProperties))]
        public void UtcDateTimeProperty_GetAfterSetUtc_IsTheDateTime(UtcDateTimeAccessor accessor)
        {
            // arrange
            var component = accessor.CreateComponent();
            accessor.SetValue(component, Stamp);

            // act
            var testResult = accessor.GetValue(component);

            // assert
            testResult.Should().Be(Stamp);
        }

        [Theory]
        [MemberData(nameof(UtcDateTimeProperties))]
        public void UtcDateTimeProperty_SetNull_RemovesTheProperty(UtcDateTimeAccessor accessor)
        {
            // arrange
            var component = accessor.CreateComponent();
            accessor.SetValue(component, Stamp);

            // act
            accessor.SetValue(component, null);
            var testResult = component.Properties;

            // assert
            testResult.Should().NotContain(property => property.Name == accessor.PropertyName);
        }

        [Theory]
        [MemberData(nameof(UtcDateTimePropertiesWithNonUtcValues))]
        public void UtcDateTimeProperty_SetNotUtc_ThrowsArgumentException(UtcDateTimeAccessor accessor, IcalDateTime value)
        {
            // arrange
            var component = accessor.CreateComponent();

            // act
            Action testAction = () => accessor.SetValue(component, value);

            // assert
            testAction.Should().ThrowExactly<ArgumentException>().WithParameterName("value");
        }

        [Theory]
        [MemberData(nameof(UtcDateTimePropertiesWithNonUtcValues))]
        public void UtcDateTimeProperty_SetNotUtc_LeavesExistingValueUnchanged(UtcDateTimeAccessor accessor, IcalDateTime value)
        {
            // arrange
            var component = accessor.CreateComponent();
            accessor.SetValue(component, Stamp);

            // act
            var exception = Record.Exception(() => accessor.SetValue(component, value));
            var testResult = accessor.GetValue(component);

            // assert
            exception.Should().BeOfType<ArgumentException>();
            testResult.Should().Be(Stamp);
        }

        /// <summary>
        /// A typed accessor of <see cref="IcalFreeBusy"/> for a DATE or DATE-TIME property that RFC 5545 requires, in a VFREEBUSY, to be a
        /// DATE-TIME in UTC.
        /// </summary>
        public sealed class FreeBusyUtcAccessor
        {
            public FreeBusyUtcAccessor(string name, string propertyName, Action<IcalFreeBusy, IcalDateOrDateTime?> setValue,
                Func<IcalFreeBusy, IcalDateOrDateTime?> getValue)
            {
                Name = name;
                PropertyName = propertyName;
                SetValue = setValue;
                GetValue = getValue;
            }

            public string Name { get; }

            public string PropertyName { get; }

            public Action<IcalFreeBusy, IcalDateOrDateTime?> SetValue { get; }

            public Func<IcalFreeBusy, IcalDateOrDateTime?> GetValue { get; }

            public override string ToString() => Name;
        }

        // RFC 5545 §3.8.2.4 (DTSTART) and §3.8.2.2 (DTEND): within VFREEBUSY, the value "MUST be specified as a date with UTC time".
        private static readonly FreeBusyUtcAccessor[] FreeBusyUtcAccessors =
        {
            new FreeBusyUtcAccessor("IcalFreeBusy.DtStart", DtStart, (freeBusy, value) => freeBusy.DtStart = value,
                freeBusy => freeBusy.DtStart),
            new FreeBusyUtcAccessor("IcalFreeBusy.DtEnd", DtEndProperty, (freeBusy, value) => freeBusy.DtEnd = value,
                freeBusy => freeBusy.DtEnd),
        };

        // A DATE, a floating DATE-TIME and a zoned one, none of which RFC 5545 allows for DTSTART or DTEND in a VFREEBUSY.
        private static readonly IcalDateOrDateTime[] NonUtcDateOrDateTimes =
        {
            new IcalDateOrDateTime(StampDate),
            new IcalDateOrDateTime(new IcalDateTime(StampDate, StampLocalTime)),
            new IcalDateOrDateTime(new IcalDateTime(StampDate, StampLocalTime, AucklandTimeZoneId)),
        };

        public static TheoryData<FreeBusyUtcAccessor> FreeBusyUtcProperties
        {
            get
            {
                var data = new TheoryData<FreeBusyUtcAccessor>();
                foreach (var accessor in FreeBusyUtcAccessors)
                    data.Add(accessor);
                return data;
            }
        }

        public static TheoryData<FreeBusyUtcAccessor, IcalDateOrDateTime> FreeBusyUtcPropertiesWithNonUtcValues
        {
            get
            {
                var data = new TheoryData<FreeBusyUtcAccessor, IcalDateOrDateTime>();
                foreach (var accessor in FreeBusyUtcAccessors)
                {
                    foreach (var value in NonUtcDateOrDateTimes)
                        data.Add(accessor, value);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcProperties))]
        public void FreeBusyUtcProperty_GetAfterSetUtc_IsTheDateTime(FreeBusyUtcAccessor accessor)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();
            accessor.SetValue(freeBusy, Stamp);

            // act
            var testResult = accessor.GetValue(freeBusy);

            // assert
            testResult.Should().Be(new IcalDateOrDateTime(Stamp));
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcProperties))]
        public void FreeBusyUtcProperty_SetUtc_WritesTheValueWithoutParameters(FreeBusyUtcAccessor accessor)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();

            // act
            accessor.SetValue(freeBusy, Stamp);
            var testResult = freeBusy.GetProperty(accessor.PropertyName);

            // assert
            testResult.Value.Should().Be(StampValue);
            testResult.Parameters.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcProperties))]
        public void FreeBusyUtcProperty_SetUtcOverZonedValue_DropsTheTimeZoneParameter(FreeBusyUtcAccessor accessor)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();
            freeBusy.Properties.Add(new IcalContentLine(accessor.PropertyName,
                new[] { new IcalContentLineParameter(TimeZoneIdParameter, AucklandTimeZoneId) }, StampLocalValue));

            // act
            accessor.SetValue(freeBusy, Stamp);
            var testResult = freeBusy.GetProperty(accessor.PropertyName);

            // assert
            testResult.Value.Should().Be(StampValue);
            testResult.Parameters.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcProperties))]
        public void FreeBusyUtcProperty_SetNull_RemovesTheProperty(FreeBusyUtcAccessor accessor)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();
            accessor.SetValue(freeBusy, Stamp);

            // act
            accessor.SetValue(freeBusy, null);
            var testResult = freeBusy.Properties;

            // assert
            testResult.Should().NotContain(property => property.Name == accessor.PropertyName);
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcPropertiesWithNonUtcValues))]
        public void FreeBusyUtcProperty_SetDateOrNotUtc_ThrowsArgumentException(FreeBusyUtcAccessor accessor, IcalDateOrDateTime value)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();

            // act
            Action testAction = () => accessor.SetValue(freeBusy, value);

            // assert
            testAction.Should().ThrowExactly<ArgumentException>().WithParameterName("value");
        }

        [Theory]
        [MemberData(nameof(FreeBusyUtcPropertiesWithNonUtcValues))]
        public void FreeBusyUtcProperty_SetDateOrNotUtc_LeavesExistingValueUnchanged(FreeBusyUtcAccessor accessor, IcalDateOrDateTime value)
        {
            // arrange
            var freeBusy = new IcalFreeBusy();
            accessor.SetValue(freeBusy, Stamp);

            // act
            var exception = Record.Exception(() => accessor.SetValue(freeBusy, value));
            var testResult = accessor.GetValue(freeBusy);

            // assert
            exception.Should().BeOfType<ArgumentException>();
            testResult.Should().Be(new IcalDateOrDateTime(Stamp));
        }

        // RFC 5545 §3.6.5: the DTSTART of an observance "MUST be specified as a date with local time value".  A UTC DATE-TIME and a zoned
        // one, neither of which it allows.
        public static TheoryData<IcalDateTime> NonLocalDateTimes => new TheoryData<IcalDateTime>
        {
            Stamp,
            new IcalDateTime(StampDate, StampLocalTime, AucklandTimeZoneId),
        };

        [Fact]
        public void TimeZoneObservanceDtStart_GetAfterSetLocal_IsTheDateTime()
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);
            observance.DtStart = StampLocal;

            // act
            var testResult = observance.DtStart;

            // assert
            testResult.Should().Be(StampLocal);
        }

        [Fact]
        public void TimeZoneObservanceDtStart_SetLocal_WritesTheValueWithoutParameters()
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);

            // act
            observance.DtStart = StampLocal;
            var testResult = observance.GetProperty(DtStart);

            // assert
            testResult.Value.Should().Be(StampLocalValue);
            testResult.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void TimeZoneObservanceDtStart_SetLocalOverZonedValue_DropsTheTimeZoneParameter()
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);
            observance.Properties.Add(new IcalContentLine(DtStart,
                new[] { new IcalContentLineParameter(TimeZoneIdParameter, AucklandTimeZoneId) }, StampLocalValue));

            // act
            observance.DtStart = StampLocal;
            var testResult = observance.GetProperty(DtStart);

            // assert
            testResult.Value.Should().Be(StampLocalValue);
            testResult.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void TimeZoneObservanceDtStart_SetNull_RemovesTheProperty()
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);
            observance.DtStart = StampLocal;

            // act
            observance.DtStart = null;
            var testResult = observance.Properties;

            // assert
            testResult.Should().NotContain(property => property.Name == DtStart);
        }

        [Theory]
        [MemberData(nameof(NonLocalDateTimes))]
        public void TimeZoneObservanceDtStart_SetUtcOrZoned_ThrowsArgumentException(IcalDateTime value)
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);

            // act
            Action testAction = () => observance.DtStart = value;

            // assert
            testAction.Should().ThrowExactly<ArgumentException>().WithParameterName("value");
        }

        [Theory]
        [MemberData(nameof(NonLocalDateTimes))]
        public void TimeZoneObservanceDtStart_SetUtcOrZoned_LeavesExistingValueUnchanged(IcalDateTime value)
        {
            // arrange
            var observance = new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard);
            observance.DtStart = StampLocal;

            // act
            var exception = Record.Exception(() => observance.DtStart = value);
            var testResult = observance.DtStart;

            // assert
            exception.Should().BeOfType<ArgumentException>();
            testResult.Should().Be(StampLocal);
        }

        [Fact]
        public void Alarm_TriggerDateTime_ReplacesInPlace_KeepingOtherParameters()
        {
            // arrange
            var alarm = ReadAlarm(AbsoluteTriggerWithVendorParameter, DisplayAction, ReminderDescription);

            // act
            alarm.TriggerDateTime = new IcalDateTime(new IcalDate(2026, 1, 5), new IcalTime(8, 45, 0, true));

            // assert
            alarm.Properties[0].Name.Should().Be(Trigger);
            alarm.Properties[0].Parameters.Select(p => p.Name).Should().Equal(VendorParameter, ValueParameter);
            alarm.Properties[0].Value.Should().Be("20260105T084500Z");
        }

        [Fact]
        public void Alarm_TriggerDateTime_OnRelativeTrigger_DropsRelatedInPlace()
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;RELATED=END:-PT15M", DisplayAction, ReminderDescription);

            // act
            alarm.TriggerDateTime = Stamp;

            // assert
            alarm.Properties.Select(property => property.Name).Should().Equal(Trigger, "ACTION", "DESCRIPTION");
            alarm.Properties[0].Parameters.Select(parameter => parameter.Name).Should().Equal(VendorParameter, ValueParameter);
            alarm.Properties[0].Parameters.Select(parameter => parameter.Values[0]).Should().Equal("keep", "DATE-TIME");
            alarm.Properties[0].Value.Should().Be(StampValue);
        }

        [Fact]
        public void Validate_RelativeTriggerReplacedByDateTime_ReturnsNothing()
        {
            // arrange
            var alarm = ReadAlarm("TRIGGER;X-VENDOR=keep;RELATED=END:-PT15M", DisplayAction, ReminderDescription);
            alarm.TriggerDateTime = Stamp;

            // act
            var testResult = alarm.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Alarm_TriggerDuration_OnAbsoluteTrigger_ReplacesInPlace_KeepingOtherParameters()
        {
            // arrange
            var alarm = ReadAlarm(AbsoluteTriggerWithVendorParameter, DisplayAction, ReminderDescription);

            // act
            alarm.TriggerDuration = new IcalDuration(0, TimeSpan.FromMinutes(-30));

            // assert
            alarm.Properties.Select(property => property.Name).Should().Equal(Trigger, "ACTION", "DESCRIPTION");
            alarm.Properties[0].Parameters.Select(parameter => parameter.Name).Should().Equal(VendorParameter);
            alarm.Properties[0].Value.Should().Be("-PT30M");
        }

        [Theory]
        [MemberData(nameof(ComponentAccessors), DisableDiscoveryEnumeration = true)]
        public void ComponentAccessor_CalendarWithEveryKind_GetsTheComponentsOfItsType(Func<IcalCalendar, IEnumerable<IcalComponent>> accessor,
            int expectedCount)
        {
            // arrange
            var calendar = CreateCalendarWithEveryKind();

            // act
            var testResult = accessor(calendar);

            // assert
            testResult.Should().HaveCount(expectedCount);
        }

        [Fact]
        public void Observances_GoogleExportTimeZone_GetsBothObservances()
        {
            // arrange
            var timeZone = ReadCalendar(CalendarFixtures.GoogleExport).TimeZones.Single();

            // act
            var testResult = timeZone.Observances;

            // assert
            testResult.Should().HaveCount(2);
        }

        #endregion

        #region IcalDateOrDateTime

        [Fact]
        public void IsDate_Date_IsTrue()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDate(2026, 1, 1);

            // act
            var testResult = value.IsDate;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IsDate_DateTime_IsFalse()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = value.IsDate;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsDate_Default_IsTrue()
        {
            // arrange
            var value = default(IcalDateOrDateTime);

            // act
            var testResult = value.IsDate;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void ValueType_Date_IsDate()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDate(2026, 1, 1);

            // act
            var testResult = value.ValueType;

            // assert
            testResult.Should().Be(IcalValueType.Date);
        }

        [Fact]
        public void ValueType_DateTime_IsDateTime()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = value.ValueType;

            // assert
            testResult.Should().Be(IcalValueType.DateTime);
        }

        [Fact]
        public void ToString_Date_WritesDate()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDate(2026, 1, 1);

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be("20260101");
        }

        [Fact]
        public void ToString_DateTime_WritesDateTime()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be("20260101T000000");
        }

        [Fact]
        public void Date_DateTime_IsItsDate()
        {
            // arrange
            IcalDateOrDateTime value = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = value.Date;

            // assert
            testResult.Should().Be(new IcalDate(2026, 1, 1));
        }

        [Fact]
        public void Equality_DateAndMidnight_IsFalse()
        {
            // arrange
            IcalDateOrDateTime date = new IcalDate(2026, 1, 1);
            IcalDateOrDateTime midnight = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = date == midnight;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Inequality_DateAndMidnight_IsTrue()
        {
            // arrange
            IcalDateOrDateTime date = new IcalDate(2026, 1, 1);
            IcalDateOrDateTime midnight = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0));

            // act
            var testResult = date != midnight;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameDate_IsTrue()
        {
            // arrange
            IcalDateOrDateTime date = new IcalDate(2026, 1, 1);
            var other = new IcalDateOrDateTime(new IcalDate(2026, 1, 1));

            // act
            var testResult = date.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameDate_IsTheSame()
        {
            // arrange
            IcalDateOrDateTime date = new IcalDate(2026, 1, 1);
            var expected = new IcalDateOrDateTime(new IcalDate(2026, 1, 1)).GetHashCode();

            // act
            var testResult = date.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void DateTime_Date_ThrowsInvalidOperation()
        {
            // arrange
            IcalDateOrDateTime date = new IcalDate(2026, 1, 1);

            // act
            Action testAction = () => _ = date.DateTime;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
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

        // ref. RFC 5545 §3.3.11: the TZID escapes its first comma but not its second, so it is not valid TEXT.  The time zone it defines is
        // still the one the event's TZID parameter names, so the problems are the value and the quotes the parameter needs for its comma
        // (RFC 5545 §3.2.19), and the time zone is defined.
        [Fact]
        public void Validate_TimeZoneIdWithUnescapedComma_IsStillDefined()
        {
            // arrange
            var lines = IcalTestData.Lines(
                new[]
                {
                    IcalLines.BeginTimeZone, "TZID:Foo\\, Bar, Baz", IcalLines.BeginStandard, "DTSTART:19700101T000000", "TZOFFSETFROM:+0100",
                    "TZOFFSETTO:+0100", IcalLines.EndStandard, IcalLines.EndTimeZone
                },
                new[] { IcalLines.BeginEvent, IcalLines.EventUid, IcalLines.NewYearStamp, "DTSTART;TZID=\"Foo, Bar, Baz\":20260101T090000", IcalLines.EndEvent });
            var calendar = ReadCalendar(Calendar(lines), new IcalOptions { AllowInvalidValues = true, AllowQuotedParameterValues = true });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.Kind).Should().Equal(IcalDiagnosticKind.InvalidValue, IcalDiagnosticKind.QuotedParameterValue);
        }

        [Fact]
        public void Validate_ManyProblems_ReturnsEachKind()
        {
            // arrange
            var calendar = CreateCalendarWithManyProblems();

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.Kind).Should().Equal(IcalDiagnosticKind.MissingRequiredProperty,
                IcalDiagnosticKind.MissingRequiredProperty, IcalDiagnosticKind.UndefinedTimeZone, IcalDiagnosticKind.MissingRequiredProperty);
        }

        [Theory]
        [InlineData(0, "VCALENDAR: The VCALENDAR component has no PRODID property, which it requires.")]
        [InlineData(1, "VCALENDAR/VEVENT[2]: The VEVENT component has no UID property, which it requires.")]
        [InlineData(2, "VCALENDAR/VEVENT[2]: The TZID parameter of the DTSTART property names the time zone 'Nowhere', which has no VTIMEZONE in the calendar.")]
        [InlineData(3, "VCALENDAR/VEVENT[2]/VALARM[1]: A VALARM component with ACTION:DISPLAY requires a DESCRIPTION property.")]
        public void ToString_DiagnosticOfManyProblems_GivesPathAndMessage(int index, string expected)
        {
            // arrange
            var diagnostic = CreateCalendarWithManyProblems().Validate()[index];

            // act
            var testResult = diagnostic.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Validate_EventInItself_ReportsTheNesting()
        {
            // arrange
            var testEvent = new IcalEvent { Uid = "1", DtStamp = Stamp };
            testEvent.Components.Add(testEvent);

            // act
            var testResult = testEvent.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.ToString()).Should().Equal(
                "VEVENT/VEVENT[1]: A VEVENT component cannot be nested in a VEVENT component.",
                "VEVENT/VEVENT[1]: A VEVENT component is nested in itself.");
        }

        [Fact]
        public void Validate_ComponentInItselfTwice_ReportsEachNesting()
        {
            // arrange
            var component = new IcalComponent(NestedComponentName);
            component.Components.Add(component);
            component.Components.Add(component);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.ToString()).Should().Equal(
                "X-A/X-A[1]: A X-A component is nested in itself.",
                "X-A/X-A[2]: A X-A component is nested in itself.");
        }

        [Fact]
        public void Validate_ComponentInItsChild_ReportsTheNesting()
        {
            // arrange
            var component = new IcalComponent(NestedComponentName);
            var child = new IcalComponent("X-B");
            component.Components.Add(child);
            child.Components.Add(component);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.ToString()).Should().Equal(
                "X-A/X-B[1]/X-A[1]: A X-A component is nested in itself.");
        }

        [Fact]
        public void Validate_ComponentInTwoParents_ReportsTheSecondPlace()
        {
            // arrange
            var component = new IcalComponent(NestedComponentName);
            var first = new IcalComponent(NestedComponentName);
            var second = new IcalComponent(NestedComponentName);
            var shared = new IcalComponent(NestedComponentName);
            component.Components.Add(first);
            component.Components.Add(second);
            first.Components.Add(shared);
            second.Components.Add(shared);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.Kind, diagnostic.ComponentPath, diagnostic.Message)).Should().Equal(
                (IcalDiagnosticKind.InvalidComponentNesting, "X-A/X-A[2]/X-A[1]", SharedMessage));
        }

        [Fact]
        public void Validate_DistinctComponentsThatAreEqual_ReturnsNothing()
        {
            // arrange
            var component = new EqualToEveryComponent();
            var first = new EqualToEveryComponent();
            var second = new EqualToEveryComponent();
            component.Components.Add(first);
            component.Components.Add(second);
            first.Components.Add(new EqualToEveryComponent());

            // act
            var testResult = component.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ComponentTwiceInOneParent_ReportsTheSecondPlace()
        {
            // arrange
            var component = SharedComponents(1);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.ComponentPath, diagnostic.Message)).Should().Equal(("X-A/X-A[2]", SharedMessage));
        }

        [Fact]
        public void Validate_ComponentsSharedAtEachLevel_ReportsEachSecondPlace()
        {
            // arrange
            var component = SharedComponents(SharedThreeLevels);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.ComponentPath, diagnostic.Message)).Should().Equal(
                ("X-A/X-A[1]/X-A[1]/X-A[2]", SharedMessage),
                ("X-A/X-A[1]/X-A[2]", SharedMessage),
                ("X-A/X-A[2]", SharedMessage));
        }

        [Fact]
        public void Validate_ComponentsSharedAtSixtyLevels_ReportsEachLevelOnce()
        {
            // arrange
            var component = SharedComponents(SharedLevels);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Should().HaveCount(SharedLevels).And.OnlyContain(diagnostic => diagnostic.Message == SharedMessage);
        }

        [Fact]
        public void Validate_ComponentsNestedToTheLimit_ReturnsNothing()
        {
            // arrange
            var component = NestedComponents(MaxComponentDepth);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ComponentsNestedPastTheLimit_ReportsTheNesting()
        {
            // arrange
            var component = NestedComponents(PastMaxComponentDepth);

            // act
            var testResult = component.Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.Kind, diagnostic.Message)).Should().Equal(
                (IcalDiagnosticKind.InvalidComponentNesting, "Components are nested more than 64 levels deep; RFC 5545§3.6 allows three."));
        }

        [Fact]
        public void Validate_StandaloneEvent_SkipsCalendarChecks()
        {
            // arrange
            var testEvent = new IcalEvent { Uid = "1", DtStamp = Stamp };
            testEvent.Properties.Add(new IcalContentLine("DTEND", new[] { new IcalContentLineParameter("TZID", UndefinedTimeZoneId) }, "20260101T090000"));

            // act
            var testResult = testEvent.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ReadCalendar_OrdersByLine()
        {
            // arrange
            var calendar = ReadCalendarWithProblemsOnSeveralLines();

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Select(diagnostic => (diagnostic.LineNumber, diagnostic.PropertyName)).Should()
                .Equal((4L, "UID"), (7L, "PRIORITY"), (10L, "COLOR"));
        }

        [Fact]
        public void ToString_DiagnosticOfReadCalendar_GivesItsLineNumber()
        {
            // arrange
            var diagnostic = ReadCalendarWithProblemsOnSeveralLines().Validate()[0];

            // act
            var testResult = diagnostic.ToString();

            // assert
            testResult.Should().Be("VCALENDAR/VEVENT[1] (line 4): The VEVENT component has no UID property, which it requires.");
        }

        #endregion

        private static IcalComponent CreateComponentWithComments()
        {
            var component = new IcalComponent(TestComponentName);
            component.Properties.Add(new IcalContentLine("Comment", "a"));
            component.Properties.Add(new IcalContentLine(Summary, "b"));
            component.Properties.Add(new IcalContentLine(Comment, "c"));
            return component;
        }

        private static IcalTimeZoneObservance CreateDaylightObservance()
        {
            return new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Daylight)
            {
                DtStart = new IcalDateTime(new IcalDate(1970, 9, 27), new IcalTime(2, 0, 0)),
                OffsetFrom = new IcalUtcOffset(TimeSpan.FromHours(12)),
                OffsetTo = new IcalUtcOffset(TimeSpan.FromHours(13))
            };
        }

        private static IcalRecurrenceRule[] CreateTwoRules()
        {
            return new[]
            {
                IcalRecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU".AsSpan(), IcalValueFormat.Text),
                IcalRecurrenceRule.Parse("FREQ=YEARLY;COUNT=1".AsSpan(), IcalValueFormat.Text)
            };
        }

        private static IcalCalendar CreateCalendarWithEveryKind()
        {
            var calendar = ReadCalendar(CalendarFixtures.GoogleExport);
            calendar.Components.Add(new IcalTodo());
            calendar.Components.Add(new IcalJournal());
            calendar.Components.Add(new IcalFreeBusy());
            calendar.Components.Add(new IcalComponent("X-OTHER"));
            return calendar;
        }

        private static IcalCalendar CreateCalendarWithManyProblems()
        {
            var calendar = new IcalCalendar();
            calendar.Components.Add(new IcalEvent { Uid = "1", DtStamp = Stamp, DtStart = Stamp });
            var second = new IcalEvent
            {
                DtStamp = Stamp, DtStart = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(9, 0, 0), UndefinedTimeZoneId)
            };
            second.Components.Add(new IcalAlarm { Action = "DISPLAY", TriggerDuration = IcalDuration.FromWeeks(-1) });
            calendar.Components.Add(second);
            return calendar;
        }

        private static IcalCalendar ReadCalendarWithProblemsOnSeveralLines()
        {
            var text = Calendar(IcalLines.BeginEvent, IcalLines.NewYearStamp, "DTSTART:20260101T000000Z", "PRIORITY:11", IcalLines.EndEvent, "COLOR:a",
                "COLOR:b");
            var options = new IcalOptions { AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredProperties = true };
            return ReadCalendar(text, options);
        }

        /// <summary>
        /// A <see cref="NestedComponentName"/> component that is equal to every other, so that a check comparing components with
        /// <see cref="object.Equals(object)"/> instead of by reference would find each one nested in itself or in more than one place.
        /// </summary>
        private sealed class EqualToEveryComponent : IcalComponent
        {
            public EqualToEveryComponent() : base(NestedComponentName)
            {
            }

            public override bool Equals(object obj)
            {
                return obj is IcalComponent;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }
    }
}
