using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class MimeFieldReaderTests
    {
        private const string DescriptionText = "a complete map of the human genome";

        // ref. RFC 5322 §3.6: the fields a message needs, so that these messages have only the problem the test is about.
        private const string Origination = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n";

        // ref. RFC 2231 §3: section numbers start at 0 with no gaps.
        private const string MissingSectionBoundary = "Content-Type: multipart/mixed; boundary*1=b\r\n\r\n--b\r\n\r\na\r\n--b--";

        public static TheoryData<string> MimeFields => new TheoryData<string>
        {
            Origination + "Content-Type: text/plain\r\n\r\na",
            Origination + "Content-Transfer-Encoding: 7bit\r\n\r\na",
            Origination + "Content-ID: <a@b>\r\n\r\na",
            Origination + "Content-Description: a\r\n\r\na",
            Origination + "Content-Disposition: inline\r\n\r\na",
            Origination + "Content-Language: en\r\n\r\na",
            Origination + "CONTENT-TYPE: text/plain\r\n\r\na",
        };

        [Theory]
        [MemberData(nameof(MimeFields))]
        public void ReadEvent_MimeFieldAndNoMimeVersion_ReportsADiagnostic(string message)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Theory]
        [InlineData(Origination + "MIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\na")]
        [InlineData(Origination + "Content-Type: text/plain\r\nMIME-Version: 1.0\r\n\r\na")]
        [InlineData(Origination + "Subject: a\r\n\r\nb")]
        [InlineData(Origination + "Content-Length: 1\r\n\r\na")]
        public void ReadEvent_NoMimeVersionNeeded_ReportsNothing(string message)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadEvent_PartWithMimeFields_ReportsNothing()
        {
            // arrange
            const string message = Origination + "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: text/plain\r\n\r\na\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadEvent_EmbeddedMessageWithNoMimeVersion_ReportsItsPath()
        {
            // arrange
            const string message = Origination + "MIME-Version: 1.0\r\nContent-Type: message/rfc822\r\n\r\nSubject: a\r\nContent-Type: text/plain\r\n\r\na";
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                var diagnostic = reader.Diagnostics.Should().ContainSingle().Which;
                diagnostic.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
                diagnostic.EntityPath.Should().Be("1");
            }
        }

        [Fact]
        public void ReadEvent_MalformedParameter_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(MissingSectionBoundary))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_MalformedParameterWithAllowMalformedParameters_ReadsTheParts()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(MissingSectionBoundary, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMalformedParameters = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Contain("body 1 [a]");
            }
        }

        [Fact]
        public void DecodeUnstructured_ContentDescriptionWrittenByAWriter_GivesTheSameText()
        {
            // arrange
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Description", ImfHeaderText.FormatUnstructured(DescriptionText, ImfHeaderEncoding.EncodedWords));

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(written);

            // assert
            testResult.Should().Be(DescriptionText);
        }
    }
}
