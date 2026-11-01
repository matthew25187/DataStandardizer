using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Reads and writes calendars for the tests of the component object model.
    /// </summary>
    internal static class ModelTestData
    {
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
