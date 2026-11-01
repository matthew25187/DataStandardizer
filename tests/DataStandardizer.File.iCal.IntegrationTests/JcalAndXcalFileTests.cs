using System;
using System.Linq;
using System.Text;
using DataStandardizer.File.iCal.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="JcalReader"/>, <see cref="JcalWriter"/>, <see cref="XcalReader"/> and
    /// <see cref="XcalWriter"/>.
    /// </summary>
    public class JcalAndXcalFileTests
    {
        // ref. RFC 6321 Appendix B.1 and RFC 7265 Appendix B.1: the SUMMARY of the event of the first example.
        private const string Example1Summary = "Planning meeting";

        private const string ProductId = "-//Example//Test//EN";

        private const string JournalUid = "j@example.com";

        // ref. RFC 7265 §3: a calendar with that PRODID and one VJOURNAL with that UID and JournalStamp, in jCal form, with the VERSION that
        // the writer adds first.
        private const string MinimalJcal =
            "[\"vcalendar\",[[\"version\",{},\"text\",\"2.0\"],[\"prodid\",{},\"text\",\"-//Example//Test//EN\"]]," +
            "[[\"vjournal\",[[\"uid\",{},\"text\",\"j@example.com\"],[\"dtstamp\",{},\"date-time\",\"2026-01-01T00:00:00Z\"]],[]]]]";

        // ref. RFC 6321 §3: the same calendar in xCal form, indented by two spaces with CRLF line breaks as the writer writes it by default.
        private const string MinimalXcal =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">\r\n" +
            "  <vcalendar>\r\n" +
            "    <properties>\r\n" +
            "      <version>\r\n" +
            "        <text>2.0</text>\r\n" +
            "      </version>\r\n" +
            "      <prodid>\r\n" +
            "        <text>-//Example//Test//EN</text>\r\n" +
            "      </prodid>\r\n" +
            "    </properties>\r\n" +
            "    <components>\r\n" +
            "      <vjournal>\r\n" +
            "        <properties>\r\n" +
            "          <uid>\r\n" +
            "            <text>j@example.com</text>\r\n" +
            "          </uid>\r\n" +
            "          <dtstamp>\r\n" +
            "            <date-time>2026-01-01T00:00:00Z</date-time>\r\n" +
            "          </dtstamp>\r\n" +
            "        </properties>\r\n" +
            "      </vjournal>\r\n" +
            "    </components>\r\n" +
            "  </vcalendar>\r\n" +
            "</icalendar>";

        // Midnight UTC on New Year's Day 2026.
        private static readonly IcalDateTime JournalStamp = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0, true));

        [Fact]
        public void ReadCalendars_JcalFilePath_ReadsTheCalendar()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(JcalTestData.Example1Jcal)))
            using (var reader = new JcalReader(file.Path))
            {
                // act
                var testResult = reader.ReadCalendars().ToList();

                // assert
                testResult.Should().ContainSingle().Which.Events.Should().ContainSingle().Which.Summary.Should().Be(Example1Summary);
            }
        }

        [Fact]
        public void Dispose_JcalReaderOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(JcalTestData.Example1Jcal)))
            {
                var reader = new JcalReader(file.Path);
                reader.ReadCalendars().ToList();

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteCalendar_JcalFilePath_WritesTheCalendar()
        {
            // arrange
            var calendar = CreateCalendar();
            using (var file = TemporaryFile.Create())
            {
                // act
                using (var writer = new JcalWriter(file.Path))
                {
                    writer.WriteCalendar(calendar);
                }

                // assert
                file.ReadAllBytes().Should().Equal(Encoding.UTF8.GetBytes(MinimalJcal));
            }
        }

        [Fact]
        public void Dispose_JcalWriterOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new JcalWriter(file.Path);
                writer.WriteCalendar(CreateCalendar());

                // act
                writer.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void ReadCalendars_XcalFilePath_ReadsTheCalendar()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(XcalTestData.Example1Xcal)))
            using (var reader = new XcalReader(file.Path))
            {
                // act
                var testResult = reader.ReadCalendars().ToList();

                // assert
                testResult.Should().ContainSingle().Which.Events.Should().ContainSingle().Which.Summary.Should().Be(Example1Summary);
            }
        }

        [Fact]
        public void Dispose_XcalReaderOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(XcalTestData.Example1Xcal)))
            {
                var reader = new XcalReader(file.Path);
                reader.ReadCalendars().ToList();

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Dispose_XcalReaderOverFilePathNotRead_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(XcalTestData.Example1Xcal)))
            {
                var reader = new XcalReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Dispose_XcalReaderOverFilePathWithEncodingNotRead_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(XcalTestData.Example1Xcal)))
            {
                var reader = new XcalReader(file.Path, new IcalOptions { Encoding = Encoding.UTF8 });

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteCalendar_XcalFilePath_WritesTheCalendar()
        {
            // arrange
            var calendar = CreateCalendar();
            using (var file = TemporaryFile.Create())
            {
                // act
                using (var writer = new XcalWriter(file.Path))
                {
                    writer.WriteCalendar(calendar);
                }

                // assert
                Encoding.UTF8.GetString(file.ReadAllBytes()).Should().Be(MinimalXcal);
            }
        }

        [Fact]
        public void Dispose_XcalWriterOverFilePathNotWritten_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new XcalWriter(file.Path);

                // act
                writer.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        /// <summary>
        /// Builds a calendar with the PRODID <see cref="ProductId"/> and one VJOURNAL.
        /// </summary>
        private static IcalCalendar CreateCalendar()
        {
            var calendar = new IcalCalendar { ProductId = ProductId };
            calendar.Components.Add(new IcalJournal { Uid = JournalUid, DtStamp = JournalStamp });
            return calendar;
        }
    }
}
