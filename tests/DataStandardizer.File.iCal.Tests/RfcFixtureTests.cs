using System.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class RfcFixtureTests
    {
        private const string Begin = "BEGIN";
        private const string End = "END";
        private const string DtStart = "DTSTART";
        private const string CommonName = "CN";

        // ref. RFC 5545 §4, first example: the organizer, and the escaped and unescaped DESCRIPTION.
        private const string Rfc5545EventOrganizer = "mailto:jsmith@example.com";
        private const string Rfc5545EventDescription = "Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia";
        private const string Rfc5545EventDescriptionText = "Networld+Interop Conference and Exhibit\nAtlanta World Congress Center\nAtlanta, Georgia";

        // ref. RFC 5545 §4, first example: the index of the DESCRIPTION line, and its line numbers and those of the reader.
        private const int Rfc5545EventDescriptionIndex = 12;
        private const int Rfc5545EventDescriptionLineNumber = 13;
        private const int Rfc5545EventFirstEndIndex = 13;
        private const int Rfc5545EventFirstEndPhysicalLineNumber = 16;
        private const int Rfc5545EventLineCount = 15;
        private const int Rfc5545EventPhysicalLineCount = 17;

        // ref. RFC 5545 §4, second example: its number of lines, its attendee, and the start of its event.
        private const int Rfc5545TimeZoneLineCount = 33;
        private const int Rfc5545TimeZoneLastLineIndex = 32;
        private const int Rfc5545TimeZoneLastPhysicalLineNumber = 34;
        private const string Rfc5545TimeZoneAttendee = "mailto:employee-A@example.com";
        private const int Rfc5545TimeZoneAttendeePhysicalLineNumber = 23;
        private const string Rfc5545TimeZoneEventStart = "19980312T083000";

        // ref. RFC 5545 §4, third example: its number of lines, and the attachment of its alarm, whose value is folded.
        private const int Rfc5545TodoLineCount = 21;
        private const string Rfc5545TodoAttachment = "http://example.com/pub/audio-files/ssbanner.aud";

        // ref. RFC 5545 §3.2.4: the escaped ALTREP description, unescaped.
        private const int Rfc5545AltRepDescriptionIndex = 4;
        private const string Rfc5545AltRepDescriptionText = "The Fall'98 Wild Wizards Conference - - Las Vegas, NV, USA";

        // ref. RFC 6868 §4: the decoded parameter values of its examples.
        private const string Rfc6868Address = "Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212";
        private const string Rfc6868Geo = "40.446816;-80.00679";
        private const string Rfc6868CommonName = "George Herman \"Babe\" Ruth";
        private const int Rfc6868SecondLinePhysicalLineNumber = 3;

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLines_Rfc5545Event_ReadsEveryLine(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545Event, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Select(line => line.Name).Should().Equal(
                    Begin, "PRODID", "VERSION", Begin, "DTSTAMP", "UID", "ORGANIZER", DtStart, "DTEND", "STATUS", "CATEGORIES", "SUMMARY", "DESCRIPTION",
                    End, End);
                testResult[1].Value.Should().Be("-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN");
                testResult[6].Value.Should().Be(Rfc5545EventOrganizer);
                testResult[Rfc5545EventDescriptionIndex].Value.Should().Be(Rfc5545EventDescription);
                testResult[Rfc5545EventDescriptionIndex].LineNumber.Should().Be(Rfc5545EventDescriptionLineNumber);
                testResult[Rfc5545EventDescriptionIndex].PhysicalLineNumber.Should().Be(Rfc5545EventDescriptionLineNumber);
                testResult[Rfc5545EventFirstEndIndex].PhysicalLineNumber.Should().Be(Rfc5545EventFirstEndPhysicalLineNumber);
                reader.LinesRead.Should().Be(Rfc5545EventLineCount);
                reader.PhysicalLinesRead.Should().Be(Rfc5545EventPhysicalLineCount);
            }
        }

        [Fact]
        public void GetTextValue_Rfc5545EventDescription_IsUnescaped()
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545Event))
            {
                var description = ReadAll(reader)[Rfc5545EventDescriptionIndex];

                // act
                var testResult = description.GetTextValue();

                // assert
                testResult.Should().Be(Rfc5545EventDescriptionText);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLines_Rfc5545TimeZone_ReadsParameters(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545TimeZone, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(Rfc5545TimeZoneLineCount);
                var attendee = testResult.Should().ContainSingle(line => line.Name == "ATTENDEE").Subject;
                attendee.Parameters.Select(parameter => parameter.Name).Should().Equal("RSVP", "ROLE", "CUTYPE");
                attendee.Parameters[1].Values.Should().Equal("REQ-PARTICIPANT");
                attendee.Value.Should().Be(Rfc5545TimeZoneAttendee);
                attendee.PhysicalLineNumber.Should().Be(Rfc5545TimeZoneAttendeePhysicalLineNumber);
                var eventStart = testResult.Where(line => line.Name == DtStart).Should().HaveCount(3).And.Subject.Last();
                eventStart.Parameters.Should().ContainSingle(parameter => parameter.Name == "TZID").Which.Values.Should()
                    .Equal(Rfc5545Examples.NewYorkTimeZoneId);
                eventStart.Value.Should().Be(Rfc5545TimeZoneEventStart);
                testResult[Rfc5545TimeZoneLastLineIndex].PhysicalLineNumber.Should().Be(Rfc5545TimeZoneLastPhysicalLineNumber);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLines_Rfc5545Todo_UnfoldsAttachment(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545Todo, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Should().HaveCount(Rfc5545TodoLineCount);
                var attachment = testResult.Should().ContainSingle(line => line.Name == "ATTACH").Subject;
                attachment.Parameters.Should().ContainSingle(parameter => parameter.Name == "FMTTYPE").Which.Values.Should().Equal("audio/basic");
                attachment.Value.Should().Be(Rfc5545TodoAttachment);
            }
        }

        [Fact]
        public void ReadLines_Rfc5545FreeBusy_ReadsEveryLine()
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545FreeBusy))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult.Where(line => line.Name == "FREEBUSY").Select(line => line.Value)
                    .Should().Equal("19980314T233000Z/19980315T003000Z", "19980316T153000Z/19980316T163000Z", "19980318T030000Z/19980318T040000Z");
            }
        }

        [Fact]
        public void ReadLines_Rfc5545Parameters_ReadsParameters()
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545Parameters))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[0].Parameters.Should().ContainSingle(parameter => parameter.Name == "DELEGATED-FROM").Which.Values.Should()
                    .Equal(Rfc5545EventOrganizer);
                testResult[1].Parameters.Should().ContainSingle(parameter => parameter.Name == "MEMBER").Which.Values.Should()
                    .Equal("mailto:projectA@example.com", "mailto:projectB@example.com");
                testResult[2].Parameters.Should().ContainSingle(parameter => parameter.Name == CommonName).Which.Values.Should().Equal("John Smith");
                testResult[Rfc5545AltRepDescriptionIndex].Parameters.Should().ContainSingle(parameter => parameter.Name == "ALTREP").Which.Values
                    .Should().Equal("cid:part1.0001@example.org");
                testResult[9].Value.Should().Be("19970304,19970504,19970704,19970904");
                testResult[10].Parameters.Should().BeEmpty();
                testResult[10].Value.Should().Be("FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH");
                testResult[11].Parameters.Select(parameter => parameter.Values.Single()).Should().Equal("URI", "audio/basic");
                testResult[13].Value.Should().Be("3.1;Invalid property value;DTSTART:96-Apr-01");
            }
        }

        [Fact]
        public void GetTextValue_Rfc5545AltRepDescription_IsUnescaped()
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc5545Parameters))
            {
                var description = ReadAll(reader)[Rfc5545AltRepDescriptionIndex];

                // act
                var testResult = description.GetTextValue();

                // assert
                testResult.Should().Be(Rfc5545AltRepDescriptionText);
            }
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
        public void ReadLines_Rfc6868_DecodesParameterValues(int bufferSize)
        {
            // arrange
            using (var reader = CreateReader(RfcFixtures.Rfc6868, bufferSize: bufferSize))
            {
                // act
                var testResult = ReadAll(reader);

                // assert
                testResult[0].Parameters.Should().ContainSingle(parameter => parameter.Name == "X-ADDRESS").Which.Values.Should().Equal(Rfc6868Address);
                testResult[0].Value.Should().Be(Rfc6868Geo);
                testResult[1].Parameters.Should().ContainSingle(parameter => parameter.Name == CommonName).Which.Values.Should().Equal(Rfc6868CommonName);
                testResult[1].PhysicalLineNumber.Should().Be(Rfc6868SecondLinePhysicalLineNumber);
            }
        }
    }
}
