using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class MimeFieldReaderTests
    {
        private const string DescriptionText = "a complete map of the human genome";

        // ref. RFC 2231 §3: section numbers start at 0 with no gaps.
        private const string MissingSectionBoundary = "Content-Type: multipart/mixed; boundary*1=b\r\n\r\n--b\r\n\r\n--b--";

        public static TheoryData<string> MimeFields => new TheoryData<string>
        {
            "Content-Type: text/plain",
            "Content-Transfer-Encoding: 7bit",
            "Content-ID: <a@b>",
            "Content-Description: a",
            "Content-Disposition: inline",
            "Content-Language: en",
            "CONTENT-TYPE: text/plain",
        };

        [Theory]
        [MemberData(nameof(MimeFields))]
        public void ReadEvent_MimeFieldAndNoMimeVersion_ReportsADiagnostic(string field)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(field + "\r\n\r\na"))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Theory]
        [InlineData("MIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\na")]
        [InlineData("Content-Type: text/plain\r\nMIME-Version: 1.0\r\n\r\na")]
        [InlineData("Subject: a\r\n\r\nb")]
        [InlineData("Content-Length: 1\r\n\r\na")]
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
            const string message = "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: text/plain\r\n\r\na\r\n--b--";
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
            const string message = "MIME-Version: 1.0\r\nContent-Type: message/rfc822\r\n\r\nContent-Type: text/plain\r\n\r\na";
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
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
            using (var reader = EntityEvents.CreateReader(MissingSectionBoundary, new ImfOptions { AllowMalformedParameters = true }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().NotThrow();
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
