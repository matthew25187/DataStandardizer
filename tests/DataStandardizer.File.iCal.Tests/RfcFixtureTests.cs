using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests;

public class RfcFixtureTests
{
    [Theory]
    [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
    public void ReadLines_Rfc5545Event_ReadsEveryLine(int bufferSize)
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc5545Event, bufferSize: bufferSize);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult.Select(line => line.Name).Should().Equal(
            "BEGIN", "PRODID", "VERSION", "BEGIN", "DTSTAMP", "UID", "ORGANIZER", "DTSTART", "DTEND", "STATUS", "CATEGORIES", "SUMMARY", "DESCRIPTION",
            "END", "END");
        testResult[1].Value.Should().Be("-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN");
        testResult[6].Value.Should().Be("mailto:jsmith@example.com");

        var description = testResult[12];
        description.Value.Should().Be("Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia");
        description.GetTextValue().Should().Be("Networld+Interop Conference and Exhibit\nAtlanta World Congress Center\nAtlanta, Georgia");
        description.LineNumber.Should().Be(13);
        description.PhysicalLineNumber.Should().Be(13);
        testResult[13].PhysicalLineNumber.Should().Be(16);
        reader.LinesRead.Should().Be(15);
        reader.PhysicalLinesRead.Should().Be(17);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
    public void ReadLines_Rfc5545TimeZone_ReadsParameters(int bufferSize)
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc5545TimeZone, bufferSize: bufferSize);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult.Should().HaveCount(33);
        var attendee = testResult.Single(line => line.Name == "ATTENDEE");
        attendee.Parameters.Select(parameter => parameter.Name).Should().Equal("RSVP", "ROLE", "CUTYPE");
        attendee.GetParameter("role")!.Values.Should().Equal("REQ-PARTICIPANT");
        attendee.Value.Should().Be("mailto:employee-A@example.com");
        attendee.PhysicalLineNumber.Should().Be(23);

        var starts = testResult.Where(line => line.Name == "DTSTART").ToList();
        starts.Should().HaveCount(3);
        starts[2].GetParameter("TZID")!.Values.Should().Equal("America/New_York");
        starts[2].Value.Should().Be("19980312T083000");
        testResult[^1].PhysicalLineNumber.Should().Be(34);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
    public void ReadLines_Rfc5545Todo_UnfoldsAttachment(int bufferSize)
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc5545Todo, bufferSize: bufferSize);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult.Should().HaveCount(21);
        var attachment = testResult.Single(line => line.Name == "ATTACH");
        attachment.GetParameter("FMTTYPE")!.Values.Should().Equal("audio/basic");
        attachment.Value.Should().Be("http://example.com/pub/audio-files/ssbanner.aud");
    }

    [Fact]
    public void ReadLines_Rfc5545FreeBusy_ReadsEveryLine()
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc5545FreeBusy);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult.Where(line => line.Name == "FREEBUSY").Select(line => line.Value)
            .Should().Equal("19980314T233000Z/19980315T003000Z", "19980316T153000Z/19980316T163000Z", "19980318T030000Z/19980318T040000Z");
    }

    [Fact]
    public void ReadLines_Rfc5545Parameters_ReadsParameters()
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc5545Parameters);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult[0].GetParameter("DELEGATED-FROM")!.Values.Should().Equal("mailto:jsmith@example.com");
        testResult[1].GetParameter("MEMBER")!.Values.Should().Equal("mailto:projectA@example.com", "mailto:projectB@example.com");
        testResult[2].GetParameter("CN")!.Values.Should().Equal("John Smith");
        testResult[4].GetParameter("ALTREP")!.Values.Should().Equal("cid:part1.0001@example.org");
        testResult[4].GetTextValue().Should().Be("The Fall'98 Wild Wizards Conference - - Las Vegas, NV, USA");
        testResult[9].Value.Should().Be("19970304,19970504,19970704,19970904");
        testResult[10].Parameters.Should().BeEmpty();
        testResult[10].Value.Should().Be("FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH");
        testResult[11].Parameters.Select(parameter => parameter.Values.Single()).Should().Equal("URI", "audio/basic");
        testResult[13].Value.Should().Be("3.1;Invalid property value;DTSTART:96-Apr-01");
    }

    [Theory]
    [MemberData(nameof(BufferSizeData), MemberType = typeof(IcalTestData))]
    public void ReadLines_Rfc6868_DecodesParameterValues(int bufferSize)
    {
        // arrange
        using var reader = CreateReader(RfcFixtures.Rfc6868, bufferSize: bufferSize);

        // act
        var testResult = ReadAll(reader);

        // assert
        testResult[0].GetParameter("X-ADDRESS")!.Values.Should().Equal("Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212");
        testResult[0].Value.Should().Be("40.446816;-80.00679");
        testResult[1].GetParameter("CN")!.Values.Should().Equal("George Herman \"Babe\" Ruth");
        testResult[1].PhysicalLineNumber.Should().Be(3);
    }
}
