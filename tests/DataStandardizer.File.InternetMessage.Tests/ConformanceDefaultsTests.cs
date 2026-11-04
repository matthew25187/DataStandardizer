using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests that the defaults follow the standards, and that each opt-in relaxes only the rule it names.
    /// </summary>
    public class ConformanceDefaultsTests
    {
        private const string Header = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n";
        private const string MimeHeader = Header + "MIME-Version: 1.0\r\n";

        // ref. RFC 2045 §4
        private const string NoMimeVersion = Header + "Content-Type: text/plain\r\n\r\nx\r\n";
        private const string WithMimeVersion = MimeHeader + "Content-Type: text/plain\r\n\r\nx\r\n";

        // ref. RFC 2045 §4: an embedded message needs the field only if it claims to follow MIME.
        private const string EmbeddedNoMimeVersion = MimeHeader + "Content-Type: message/rfc822\r\n\r\nSubject: y\r\nContent-Type: text/plain\r\n\r\nz\r\n";

        // ref. RFC 2045 §2.7, §2.8, §6.1 and §6.2
        private const string EightBitUnderDefault = MimeHeader + "\r\ncaf\xE9\r\n";
        private const string EightBitNotMime = Header + "\r\ncaf\xE9\r\n";
        private const string EightBitDeclared = MimeHeader + "Content-Transfer-Encoding: 8bit\r\n\r\ncaf\xE9\r\n";
        private const string BinaryDeclared = MimeHeader + "Content-Transfer-Encoding: binary\r\n\r\ncaf\xE9\0\n\r";
        private const string NulUnderSevenBit = MimeHeader + "Content-Transfer-Encoding: 7bit\r\n\r\na\0b\r\n";
        private const string NulUnderEightBit = MimeHeader + "Content-Transfer-Encoding: 8bit\r\n\r\na\0b\r\n";
        private const string BareLineFeedUnderMime = MimeHeader + "\r\na\nb\r\n";
        private const string BareCarriageReturnUnderMime = MimeHeader + "\r\na\rb\r\n";
        private const string CarriageReturnEndsMimeBody = MimeHeader + "\r\na\r";

        // ref. RFC 5322 §4.1: a receiver must accept these in the obsolete syntax.
        private const string ObsoleteBodyOctets = Header + "\r\na\0b\nc\rd\r\n";

        // ref. RFC 2045 §6.4: a part can't go beyond the encoding of what holds it.
        private const string EightBitPartIn7BitMultipart = MimeHeader + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Transfer-Encoding: 8bit\r\n\r\ncaf\xE9\r\n--b--\r\n";
        private const string EightBitPartIn8BitMultipart = MimeHeader + "Content-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: 8bit\r\n\r\n--b\r\nContent-Transfer-Encoding: 8bit\r\n\r\ncaf\xE9\r\n--b--\r\n";

        // ref. RFC 2046 §5.2.1
        private const string EmbeddedWithNoRequiredField = MimeHeader + "Content-Type: message/rfc822\r\n\r\nX-Other: y\r\n\r\nz\r\n";
        private const string EmbeddedWithSubject = MimeHeader + "Content-Type: message/rfc822\r\n\r\nSubject: y\r\n\r\nz\r\n";
        private const string EmbeddedWithFrom = MimeHeader + "Content-Type: message/rfc822\r\n\r\nFrom: c@d.test\r\n\r\nz\r\n";
        private const string EmbeddedWithDate = MimeHeader + "Content-Type: message/rfc822\r\n\r\nDate: Thu, 1 Jan 2026 00:00:00 +0000\r\n\r\nz\r\n";

        // A multipart header, for a body set afterwards that has no delimiters (RFC 2046 §5.1.1).
        private const string MultipartMessage = MimeHeader + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\n";
        private const string BodyWithNoDelimiters = "no delimiters\r\n";

        // The position of the octet above 127 in EightBitUnderDefault: 75 of the header and its empty line, and 3 of "caf", on line 5.
        private const long EightBitPosition = 78;
        private const long EightBitLine = 5;

        // The position of the octet that passes the limit in LineOverTheLimit: 56 of the header and its empty line, and 998 of the line, on line 4.
        private const long LongLinePosition = 1054;
        private const long LongLineLine = 4;

        // The end of the embedded header in EmbeddedWithNoRequiredField: 105 of the outer header and its empty line, and 14 of the embedded one, on line 8.
        private const long EmbeddedHeaderEndPosition = 119;
        private const long EmbeddedHeaderEndLine = 8;

        // ref. RFC 5322 §2.1.1: 998 octets and then a line break is the longest line.
        private const int LongestLine = 998;
        private const int LongestEncodedLine = 76;

        private static readonly string LineAtTheLimit = Header + "\r\n" + new string('x', LongestLine) + "\r\n";
        private static readonly string LineOverTheLimit = Header + "\r\n" + new string('x', LongestLine + 1) + "\r\n";
        private static readonly string LineOverTheLimitIn8Bit = MimeHeader + "Content-Transfer-Encoding: 8bit\r\n\r\n" + new string('x', LongestLine + 1) + "\r\n";
        private static readonly string LineOverTheLimitInBinary = MimeHeader + "Content-Transfer-Encoding: binary\r\n\r\n" + new string('x', LongestLine + 1) + "\r\n";
        private static readonly string Base64AtTheLimit = MimeHeader + "Content-Transfer-Encoding: base64\r\n\r\n" + new string('A', LongestEncodedLine) + "\r\n";
        private static readonly string Base64OverTheLimit = MimeHeader + "Content-Transfer-Encoding: base64\r\n\r\n" + new string('A', LongestEncodedLine + 4) + "\r\n";
        private static readonly string QuotedPrintableOverTheLimit = MimeHeader + "Content-Transfer-Encoding: quoted-printable\r\n\r\n" + new string('x', LongestEncodedLine + 1) + "\r\n";

        public static TheoryData<string> InvalidOctets => new TheoryData<string>
        {
            EightBitUnderDefault,
            EightBitNotMime,
            NulUnderSevenBit,
            NulUnderEightBit,
            BareLineFeedUnderMime,
            BareCarriageReturnUnderMime,
            CarriageReturnEndsMimeBody,
            EightBitPartIn7BitMultipart,
        };

        public static TheoryData<string> LongLines => new TheoryData<string>
        {
            LineOverTheLimit,
            LineOverTheLimitIn8Bit,
            Base64OverTheLimit,
            QuotedPrintableOverTheLimit,
        };

        public static TheoryData<string> ValidBodies => new TheoryData<string>
        {
            EightBitDeclared,
            BinaryDeclared,
            ObsoleteBodyOctets,
            EightBitPartIn8BitMultipart,
            LineAtTheLimit,
            LineOverTheLimitInBinary,
            Base64AtTheLimit,
        };

        [Theory]
        [MemberData(nameof(InvalidOctets))]
        public void ReadMessage_InvalidBodyOctets_ThrowsByDefault(string message)
        {
            // act
            Action testAction = () => Read(message, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidBodyOctets);
        }

        [Theory]
        [MemberData(nameof(InvalidOctets))]
        public void ReadEvent_InvalidBodyOctets_ThrowsByDefault(string message)
        {
            // arrange
            using (var subject = new ImfEntityReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(subject);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidBodyOctets);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOctets))]
        public void ReadMessage_InvalidBodyOctetsAllowed_RecordsADiagnostic(string message)
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(message)), new ImfOptions { AllowInvalidBodyOctets = true }))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidBodyOctets);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOctets))]
        public void WriteMessage_InvalidBodyOctetsReadUnderTheOption_ThrowsByDefaultAndWritesNothing(string message)
        {
            // arrange
            var read = Read(message, new ImfOptions { AllowInvalidBodyOctets = true });
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(read);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidBodyOctets);
                output.Length.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidOctets))]
        public void WriteMessage_InvalidBodyOctetsAllowed_WritesTheSameBytes(string message)
        {
            // arrange
            var options = new ImfOptions { AllowInvalidBodyOctets = true };
            var read = Read(message, options);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, options))
            {
                // act
                subject.WriteMessage(read);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(message);
            }
        }

        [Theory]
        [MemberData(nameof(LongLines))]
        public void ReadMessage_LongBodyLine_ThrowsByDefault(string message)
        {
            // act
            Action testAction = () => Read(message, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
        }

        [Theory]
        [MemberData(nameof(LongLines))]
        public void ReadMessage_LongBodyLineAllowed_RecordsADiagnostic(string message)
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(message)), new ImfOptions { AllowLongLines = true }))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Theory]
        [MemberData(nameof(LongLines))]
        public void WriteMessage_LongBodyLineReadUnderTheOption_ThrowsByDefault(string message)
        {
            // arrange
            var read = Read(message, new ImfOptions { AllowLongLines = true });
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(read);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Theory]
        [MemberData(nameof(ValidBodies))]
        public void WriteMessage_ValidBody_WritesTheSameBytesWithNoDiagnostic(string message)
        {
            // arrange
            var read = Read(message, new ImfOptions());
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                subject.WriteMessage(read);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(message);
                subject.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadEvent_InvalidBodyOctet_GivesItsPosition()
        {
            // arrange
            using (var subject = new ImfEntityReader(new MemoryStream(Octets.Of(EightBitUnderDefault))))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(subject);

                // assert
                var exception = testAction.Should().Throw<ImfFileException>().Which;
                exception.Data[ImfFileException.DataItemName.BytePosition].Should().Be(EightBitPosition);
                exception.Data[ImfFileException.DataItemName.LineNumber].Should().Be(EightBitLine);
            }
        }

        [Fact]
        public void ReadEvent_LongBodyLine_GivesItsPosition()
        {
            // arrange
            using (var subject = new ImfEntityReader(new MemoryStream(Octets.Of(LineOverTheLimit))))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(subject);

                // assert
                var exception = testAction.Should().Throw<ImfFileException>().Which;
                exception.Data[ImfFileException.DataItemName.BytePosition].Should().Be(LongLinePosition);
                exception.Data[ImfFileException.DataItemName.LineNumber].Should().Be(LongLineLine);
            }
        }

        [Fact]
        public void ReadMessage_InvalidBodyOctetAllowed_GivesItsPositionInTheDiagnostic()
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(EightBitUnderDefault)), new ImfOptions { AllowInvalidBodyOctets = true }))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.BytePosition.Should().Be(EightBitPosition);
            }
        }

        [Fact]
        public void ReadMessage_EmbeddedMessageWithNoRequiredField_GivesThePositionOfItsHeaderEnd()
        {
            // act
            Action testAction = () => Read(EmbeddedWithNoRequiredField, new ImfOptions());

            // assert
            var exception = testAction.Should().Throw<ImfFileException>().Which;
            exception.Data[ImfFileException.DataItemName.BytePosition].Should().Be(EmbeddedHeaderEndPosition);
            exception.Data[ImfFileException.DataItemName.LineNumber].Should().Be(EmbeddedHeaderEndLine);
        }

        [Fact]
        public void Validate_BodyWhoseStructureCannotBeRead_GivesTheProblemWithoutThrowing()
        {
            // arrange
            var subject = Read(MultipartMessage, new ImfOptions());
            subject.RawBody = Octets.Of(BodyWithNoDelimiters);

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidBodyStructure);
        }

        [Fact]
        public void ReadMessage_MimeFieldWithNoMimeVersion_ThrowsByDefault()
        {
            // act
            Action testAction = () => Read(NoMimeVersion, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMimeVersion);
        }

        [Fact]
        public void ReadEvent_MimeFieldWithNoMimeVersion_ThrowsByDefault()
        {
            // arrange
            using (var subject = new ImfEntityReader(new MemoryStream(Octets.Of(NoMimeVersion))))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(subject);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Fact]
        public void WriteMessage_MimeFieldWithNoMimeVersion_ThrowsByDefaultAndWritesNothing()
        {
            // arrange
            var read = Read(NoMimeVersion, new ImfOptions { AllowMissingMimeVersion = true });
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(read);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMimeVersion);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_MimeFieldWithNoMimeVersionAllowed_WritesTheSameBytesWithADiagnostic()
        {
            // arrange
            var options = new ImfOptions { AllowMissingMimeVersion = true };
            var read = Read(NoMimeVersion, options);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, options))
            {
                // act
                subject.WriteMessage(read);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(NoMimeVersion);
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Fact]
        public void ReadMessage_MimeVersionPresent_RecordsNoDiagnostic()
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(WithMimeVersion))))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadMessage_EmbeddedMessageWithMimeFieldAndNoMimeVersion_IsNotAnError()
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(EmbeddedNoMimeVersion))))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Fact]
        public void ReadMessage_EmbeddedMessageWithNoRequiredField_ThrowsByDefault()
        {
            // act
            Action testAction = () => Read(EmbeddedWithNoRequiredField, new ImfOptions());

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingRequiredField);
        }

        [Fact]
        public void ReadMessage_EmbeddedMessageWithNoRequiredFieldAllowed_RecordsADiagnostic()
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(EmbeddedWithNoRequiredField)), new ImfOptions { AllowMissingRequiredFields = true }))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingRequiredField);
            }
        }

        [Fact]
        public void WriteMessage_EmbeddedMessageWithNoRequiredField_ThrowsByDefault()
        {
            // arrange
            var read = Read(EmbeddedWithNoRequiredField, new ImfOptions { AllowMissingRequiredFields = true });
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(read);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingRequiredField);
            }
        }

        [Theory]
        [InlineData(EmbeddedWithSubject)]
        [InlineData(EmbeddedWithFrom)]
        [InlineData(EmbeddedWithDate)]
        public void ReadMessage_EmbeddedMessageWithOneRequiredField_RecordsNoDiagnostic(string message)
        {
            // arrange
            using (var subject = new ImfReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                subject.ReadMessage();

                // assert
                subject.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void Validate_EmbeddedMessageWithNoRequiredField_GivesTheProblem()
        {
            // arrange
            var subject = Read(EmbeddedWithNoRequiredField, new ImfOptions { AllowMissingRequiredFields = true });

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingRequiredField);
        }

        private static ImfMessage Read(string message, ImfOptions options)
        {
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), options))
            {
                return reader.ReadMessage();
            }
        }
    }
}
