using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Reads and writes calendars for the tests of the component object model.
    /// </summary>
    internal static class ModelTestData
    {
        /// <summary>
        /// A <c>LANGUAGE</c> value whose ',' a writer must quote, though RFC 5545 §3.2.10 has no quoted form.
        /// </summary>
        public const string LanguageThatNeedsQuotes = "en,de";

        /// <summary>
        /// The <c>X-ABC</c> line <see cref="CalendarWithCodeBuiltLanguage"/> adds, as <see cref="IcalWriter"/> writes it with the value
        /// <see cref="LanguageThatNeedsQuotes"/>.
        /// </summary>
        public const string QuotedCodeBuiltLanguageLine = "X-ABC;LANGUAGE=\"en,de\":x";

        /// <summary>
        /// Reads a calendar of one event, and adds to the event an <c>X-ABC</c> property built in code with a <c>LANGUAGE</c> parameter, which a
        /// writer formats afresh.
        /// </summary>
        public static IcalCalendar CalendarWithCodeBuiltLanguage(string language)
        {
            var calendar = ReadCalendar(EventCalendar());
            calendar.Events.Single().Properties.Add(new IcalContentLine("X-ABC",
                new[] { new IcalContentLineParameter(IcalParameterNames.Language, language) }, "x"));
            return calendar;
        }

        /// <summary>
        /// The most levels of components that validation and the writers accept, counting the outermost, as the readers do.  RFC 5545 §3.6
        /// allows three, so a limit this high refuses nothing the RFC allows.
        /// </summary>
        public const int MaxComponentDepth = 64;

        /// <summary>
        /// One level more than <see cref="MaxComponentDepth"/>.
        /// </summary>
        public const int PastMaxComponentDepth = 65;

        /// <summary>
        /// The name of the components that <see cref="NestedComponents"/> nests: an x-comp, which the check of where each known component may be nested passes over.
        /// </summary>
        public const string NestedComponentName = "X-A";

        /// <summary>
        /// Builds a chain of <see cref="NestedComponentName"/> components, each holding the next, and gets the outermost.
        /// </summary>
        /// <param name="levels">The number of components in the chain.</param>
        public static IcalComponent NestedComponents(int levels)
        {
            var outermost = new IcalComponent(NestedComponentName);
            var current = outermost;
            for (var level = 1; level < levels; level++)
            {
                var child = new IcalComponent(NestedComponentName);
                current.Components.Add(child);
                current = child;
            }

            return outermost;
        }

        /// <summary>
        /// Reads a calendar of one event, and adds the event to its own components.
        /// </summary>
        public static IcalCalendar CalendarWithEventInItself()
        {
            var calendar = ReadCalendar(EventCalendar());
            var nested = calendar.Events.Single();
            nested.Components.Add(nested);
            return calendar;
        }

        /// <summary>
        /// Reads a calendar of one event, and adds to it a chain of <see cref="MaxComponentDepth"/> components, so that with the VCALENDAR the
        /// components are nested one level more than the limit.
        /// </summary>
        public static IcalCalendar CalendarNestedPastLimit()
        {
            var calendar = ReadCalendar(EventCalendar());
            calendar.Components.Add(NestedComponents(MaxComponentDepth));
            return calendar;
        }

        /// <summary>
        /// Gets the factories of the calendars whose components are nested more deeply than a writer accepts: one with a component nested in
        /// itself, and one nested past <see cref="MaxComponentDepth"/>.
        /// </summary>
        public static TheoryData<Func<IcalCalendar>> CalendarsNestedTooDeeply => new TheoryData<Func<IcalCalendar>>
        {
            CalendarWithEventInItself,
            CalendarNestedPastLimit
        };

        public static IcalReader CreateCalendarReader(string text, IcalOptions options = null, int bufferSize = 16 * 1024)
        {
            return new IcalReader(new StringReader(text), options ?? new IcalOptions(), bufferSize);
        }

        public static List<IcalCalendar> ReadCalendars(string text, IcalOptions options = null, int bufferSize = 16 * 1024)
        {
            using (var reader = CreateCalendarReader(text, options, bufferSize))
            {
                return reader.ReadCalendars().ToList();
            }
        }

        public static IcalCalendar ReadCalendar(string text, IcalOptions options = null)
        {
            return ReadCalendars(text, options).Should().ContainSingle().Subject;
        }

        public static string WriteCalendars(IEnumerable<IcalCalendar> calendars, IcalOptions options = null)
        {
            var text = new StringWriter();
            using (var writer = new IcalWriter(text, options ?? new IcalOptions()))
            {
                writer.WriteCalendars(calendars);
            }

            return text.ToString();
        }

        /// <summary>
        /// Gets the content lines of iCalendar text, without their line numbers, so that texts that differ only in folding compare equal.
        /// </summary>
        public static List<LineSnapshot> ContentLines(string text)
        {
            using (var reader = new IcalContentLineReader(new StringReader(text)))
            {
                return reader.ReadLines().Select(line => LineSnapshot.FromLine(line).WithoutLineNumbers()).ToList();
            }
        }

        /// <summary>
        /// Builds a calendar around the lines of one component, or other calendar content.
        /// </summary>
        public static string Calendar(params string[] lines)
        {
            return IcalTestData.Crlf(IcalTestData.Lines(new[] { IcalLines.BeginCalendar, IcalLines.Version20, IcalLines.TestProductId }, lines,
                new[] { IcalLines.EndCalendar }));
        }

        /// <summary>
        /// Builds a calendar holding one VEVENT with the required properties and more.  A DTSTART is added unless the lines have one.
        /// </summary>
        public static string EventCalendar(params string[] eventLines)
        {
            string[] start = eventLines.Any(line => line.StartsWith("DTSTART", StringComparison.Ordinal)) ? new string[0] : new[] { IcalLines.NewYearStart };
            return Calendar(IcalTestData.Lines(new[] { IcalLines.BeginEvent, IcalLines.EventUid, IcalLines.NewYearStamp }, start, eventLines,
                new[] { IcalLines.EndEvent }));
        }

        /// <summary>
        /// Reads a VEVENT holding one VALARM with these lines, and gets the alarm.
        /// </summary>
        public static IcalAlarm ReadAlarm(params string[] alarmLines)
        {
            var testEvent = ReadCalendar(EventCalendar(IcalTestData.Lines(new[] { IcalLines.BeginAlarm }, alarmLines,
                new[] { IcalLines.EndAlarm }))).Events.Should().ContainSingle().Subject;
            return testEvent.Alarms.Should().ContainSingle().Subject;
        }
    }
}
