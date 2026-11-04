using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the rules of RFC 5322 §3.6, each with and without its option, on read, on write and in <see cref="ImfMessage.Validate"/>.
    /// </summary>
    public class ImfConformanceTests
    {
        private const string SampleDate = "Fri, 21 Nov 1997 09:55:06 -0600";
        private const string DateField = "Date: " + SampleDate + "\r\n";
        private const string FromField = "From: a@b.test\r\n";
        private const string Minimal = DateField + FromField;
        private const string EmptyLine = "\r\n";
        private const string OnlyFrom = FromField + EmptyLine;
        private const string RepeatedFrom = FromField + FromField + EmptyLine;
        private const string RepeatedToMessage = Minimal + "To: a@b.test\r\nTo: c@d.test, e@f.test\r\n" + EmptyLine;
        private const int RepeatedToCount = 3;
        private const string FirstSubject = "first";
        private const string RepeatedSubjectMessage = Minimal + "Subject: first\r\nSubject: second\r\n" + EmptyLine;
        private const string OnlyFromWithBody = OnlyFrom + "Body";
        private const string InvalidDateMessage = "Date: yesterday\r\n\r\n";
        private const string InvalidDateWithBody = InvalidDateMessage + "Body";
        private const string OnlyInvalidFrom = "From: nobody\r\n\r\n";
        private const string SeveralProblems = "Subject: 1\r\nSubject: 2\r\nCc: nobody\r\n\r\n";
        private const string InvalidFromAtPosition = "Subject: 1\r\n" + DateField + OnlyInvalidFrom;
        private const string WrongDayMessage = "Date: " + WrongDayOfWeek + "\r\n" + OnlyFrom;
        private const string FixedMessage = FromField + DateField + EmptyLine;

        // The length of OnlyFrom: 16 for the field, and 2 for the empty line.
        private const int OnlyFromLength = 18;

        // The length of InvalidDateMessage: 17 for the field, and 2 for the empty line.
        private const int InvalidDateMessageLength = 19;

        // Where the From field of InvalidFromAtPosition starts: 12 for the Subject field, and 39 for the Date field.
        private const int InvalidFromPosition = 51;

        private const string Date = "Date";
        private const string From = "From";
        private const string Sender = "Sender";
        private const string ResentDate = "Resent-Date";
        private const string ResentFrom = "Resent-From";
        private const string ResentSender = "Resent-Sender";

        private const string ResentDateField = "Resent-Date: " + SampleDate + "\r\n";
        private const string ResentFromField = "Resent-From: c@d.test\r\n";

        // ref. RFC 5322 §3.3: 21 November 1997 is a Friday.
        private const string WrongDayOfWeek = "Mon, 21 Nov 1997 09:55:06 -0600";

        /// <summary>
        /// Each header that repeats a field that the table of RFC 5322 §3.6 allows once.  §4 and §4.5 require a receiver to accept it, and a writer must not generate it.
        /// </summary>
        public static TheoryData<string, ImfDiagnosticKind, string> RepeatedFields => new TheoryData<string, ImfDiagnosticKind, string>
        {
            { DateField + DateField + FromField + EmptyLine, ImfDiagnosticKind.CardinalityViolation, Date },
            { DateField + FromField + FromField + EmptyLine, ImfDiagnosticKind.CardinalityViolation, From },
            { Minimal + "Sender: a@b.test\r\nSender: a@b.test\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, Sender },
            { Minimal + "Reply-To: a@b.test\r\nReply-To: a@b.test\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "Reply-To" },
            { Minimal + "To: a@b.test\r\nto: a@b.test\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "to" },
            { Minimal + "Cc: a@b.test\r\nCc: a@b.test\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "Cc" },
            { Minimal + "Bcc:\r\nBcc:\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "Bcc" },
            { Minimal + "Message-ID: <1@b.test>\r\nMessage-ID: <2@b.test>\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "Message-ID" },
            { Minimal + "In-Reply-To: <1@b.test>\r\nIn-Reply-To: <2@b.test>\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "In-Reply-To" },
            { Minimal + "References: <1@b.test>\r\nReferences: <2@b.test>\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "References" },
            { Minimal + "Subject: 1\r\nSubject: 2\r\n" + EmptyLine, ImfDiagnosticKind.CardinalityViolation, "Subject" },
        };

        /// <summary>
        /// Each header with no field of a kind it needs.  §3.5, §4 and §4.5 of RFC 5322 require a receiver to accept it, as the <c>obs-fields</c> grammar has no minimum for any field, and a
        /// writer must not generate it.
        /// </summary>
        public static TheoryData<string, ImfDiagnosticKind, string> MissingFields => new TheoryData<string, ImfDiagnosticKind, string>
        {
            // ref. RFC 5322 §3.6: the fields with a minimum number of 1.
            { FromField + EmptyLine, ImfDiagnosticKind.MissingRequiredField, Date },
            { DateField + EmptyLine, ImfDiagnosticKind.MissingRequiredField, From },

            // ref. RFC 5322 §3.6.2, RFC 6854 §2.1: Sender is required with more than one mailbox, counting those in groups.
            { DateField + "From: a@b.test, c@d.test\r\n" + EmptyLine, ImfDiagnosticKind.MissingRequiredField, Sender },
            { DateField + "From: G: a@b.test, c@d.test;\r\n" + EmptyLine, ImfDiagnosticKind.MissingRequiredField, Sender },
            { DateField + "From: G: a@b.test;, c@d.test\r\n" + EmptyLine, ImfDiagnosticKind.MissingRequiredField, Sender },

            // ref. RFC 5322 §3.6.6
            { ResentFromField + Minimal + EmptyLine, ImfDiagnosticKind.MissingRequiredField, ResentDate },
            { ResentDateField + Minimal + EmptyLine, ImfDiagnosticKind.MissingRequiredField, ResentFrom },
            { ResentDateField + "Resent-From: a@b.test, c@d.test\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.MissingRequiredField, ResentSender },
        };

        /// <summary>
        /// Each header with one problem that a reader refuses by default, the kind of that problem, and the field it is with.
        /// </summary>
        public static TheoryData<string, ImfDiagnosticKind, string> Violations => new TheoryData<string, ImfDiagnosticKind, string>
        {
            // ref. RFC 5322 §3.6: the grammar of each field body.
            { "Date: yesterday\r\n" + FromField + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, Date },
            { "Date: " + WrongDayOfWeek + "\r\n" + FromField + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, Date },
            { DateField + "From: nobody\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, From },
            { Minimal + "Sender: a@b.test, c@d.test\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, Sender },

            // ref. RFC 6854 §2.1: "If the sender field uses group syntax, the group MUST NOT contain more than one mailbox."
            { Minimal + "Sender: G: a@b.test, c@d.test;\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, Sender },
            { Minimal + "Reply-To: nobody\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Reply-To" },
            { Minimal + "To:\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "To" },
            { Minimal + "Cc: nobody\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Cc" },
            { Minimal + "Bcc: nobody\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Bcc" },
            { Minimal + "Message-ID: 1@b.test\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Message-ID" },
            { Minimal + "In-Reply-To: <1>\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "In-Reply-To" },
            { Minimal + "References: <1>\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "References" },
            { Minimal + "Keywords: a@b\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Keywords" },
            { Minimal + "Return-Path: a@b.test\r\n" + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Return-Path" },
            { "Received: by 2002:a05::1; " + SampleDate + "\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Received" },
            { "Received: by a.test; yesterday\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Received" },

            // ref. RFC 5322 §3.6.7: received-token = word / angle-addr / addr-spec / domain, and these are none of them.
            { "Received: from \"a\".b; " + SampleDate + "\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Received" },
            { "Received: from a.\"b\"; " + SampleDate + "\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Received" },
            { "Resent-Date: yesterday\r\n" + ResentFromField + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, ResentDate },
            { ResentDateField + "Resent-From: nobody\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, ResentFrom },
            { ResentDateField + ResentFromField + "Resent-Message-ID: 1\r\n" + Minimal + EmptyLine, ImfDiagnosticKind.InvalidFieldValue, "Resent-Message-ID" },
        };

        /// <summary>
        /// Headers that follow the rules, some only just.
        /// </summary>
        public static TheoryData<string> Conforming => new TheoryData<string>
        {
            Minimal + EmptyLine,

            // ref. RFC 6854 §2.1: a group in From or Sender, with no more than one mailbox in all, needs no Sender.
            DateField + "From: G: a@b.test;\r\n" + EmptyLine,
            DateField + "From: G:;\r\n" + EmptyLine,
            DateField + "From: a@b.test, c@d.test\r\nSender: a@b.test\r\n" + EmptyLine,
            DateField + "From: a@b.test, c@d.test\r\nSender: G: a@b.test;\r\n" + EmptyLine,

            // ref. RFC 5322 §3.6: these may be repeated.
            Minimal + "Comments: 1\r\nComments: 2\r\nKeywords: a\r\nKeywords: b\r\nX-A: 1\r\nX-A: 2\r\n" + EmptyLine,
            "Received: ; " + SampleDate + "\r\nReceived: ; " + SampleDate + "\r\nReturn-Path: <>\r\n" + Minimal + EmptyLine,
            ResentDateField + ResentFromField + ResentDateField + ResentFromField + Minimal + EmptyLine,
            ResentDateField + "Resent-From: a@b.test, c@d.test\r\nResent-Sender: a@b.test\r\nResent-Bcc:\r\n" + Minimal + EmptyLine,

            // ref. RFC 5322 §3.6.8: an optional field holds anything.
            Minimal + "X-Date: yesterday\r\n" + EmptyLine,
        };

        [Theory]
        [MemberData(nameof(Conforming))]
        public void ReadMessage_ConformingHeader_GivesNoDiagnostics(string message)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                reader.ReadMessage();

                // assert
                reader.Diagnostics.Should().BeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(Conforming))]
        public void Validate_ConformingHeader_GivesNothing(string message)
        {
            // arrange
            var subject = ImfMessages.Read(message);

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(Violations))]
        public void ReadMessage_ViolationWithoutOption_Throws(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(kind);
                data[ImfFileException.DataItemName.FieldName].Should().Be(fieldName);
                data[ImfFileException.DataItemName.EntityPath].Should().Be(string.Empty);
                data.Contains(ImfFileException.DataItemName.BytePosition).Should().BeTrue();
                data.Contains(ImfFileException.DataItemName.LineNumber).Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(Violations))]
        public async Task ReadMessageAsync_ViolationWithoutOption_FaultsTheTask(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            var reader = new ImfReader(new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadMessageAsync();

                // assert
                var data = (await testAction.Should().ThrowAsync<ImfFileException>()).Which.Data;
                data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(kind);
                data[ImfFileException.DataItemName.FieldName].Should().Be(fieldName);
            });
        }

        [Theory]
        [MemberData(nameof(Violations))]
        [MemberData(nameof(MissingFields))]
        [MemberData(nameof(RepeatedFields))]
        public void ReadMessage_ViolationWithOption_RecordsADiagnostic(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), ImfMessages.AcceptAll()))
            {
                // act
                reader.ReadMessage();

                // assert
                reader.Diagnostics.Should().ContainSingle();
                reader.Diagnostics[0].Kind.Should().Be(kind);
                reader.Diagnostics[0].FieldName.Should().Be(fieldName);
                reader.Diagnostics[0].EntityPath.Should().Be(string.Empty);
            }
        }

        [Theory]
        [MemberData(nameof(Violations))]
        [MemberData(nameof(MissingFields))]
        [MemberData(nameof(RepeatedFields))]
        public void Validate_Violation_GivesTheDiagnosticWithoutThrowing(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            var subject = ImfMessages.Read(message, ImfMessages.AcceptAll());

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().ContainSingle();
            testResult[0].Kind.Should().Be(kind);
            testResult[0].FieldName.Should().Be(fieldName);
        }

        [Theory]
        [MemberData(nameof(Violations))]
        [MemberData(nameof(MissingFields))]
        [MemberData(nameof(RepeatedFields))]
        public void WriteMessage_ViolationWithoutOption_ThrowsAndWritesNothing(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            var subject = ImfMessages.Read(message, ImfMessages.AcceptAll());
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                var data = testAction.Should().Throw<ImfFileException>().Which.Data;
                data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(kind);
                data[ImfFileException.DataItemName.FieldName].Should().Be(fieldName);
                data.Contains(ImfFileException.DataItemName.BytePosition).Should().BeFalse();
                data.Contains(ImfFileException.DataItemName.LineNumber).Should().BeFalse();
                stream.Length.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(Violations))]
        [MemberData(nameof(MissingFields))]
        [MemberData(nameof(RepeatedFields))]
        public async Task WriteMessageAsync_ViolationWithoutOption_FaultsTheTaskAndWritesNothing(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            var subject = ImfMessages.Read(message, ImfMessages.AcceptAll());
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = () => writer.WriteMessageAsync(subject);

                // assert
                var data = (await testAction.Should().ThrowAsync<ImfFileException>()).Which.Data;
                data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(kind);
                data[ImfFileException.DataItemName.FieldName].Should().Be(fieldName);
                stream.ToArray().Should().BeEmpty();
            });
        }

        [Theory]
        [MemberData(nameof(Violations))]
        [MemberData(nameof(MissingFields))]
        public void WriteMessage_ViolationWithOption_RecordsADiagnosticAndWritesByteForByte(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            var subject = ImfMessages.Read(message, ImfMessages.AcceptAll());
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream, ImfMessages.AcceptAll()))
            {
                // act
                writer.WriteMessage(subject);

                // assert
                Octets.ToText(stream.ToArray()).Should().Be(message);
                writer.Diagnostics.Should().ContainSingle();
                writer.Diagnostics[0].Kind.Should().Be(kind);
                writer.Diagnostics[0].FieldName.Should().Be(fieldName);
            }
        }

        [Fact]
        public void ReadMessage_OnlyTheCardinalityOption_StillRejectsAnInvalidValue()
        {
            // arrange
            var options = new ImfOptions { AllowCardinalityViolations = true };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(InvalidDateMessage)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(MissingFields))]
        [MemberData(nameof(RepeatedFields))]
        public void ReadMessage_ObsoleteSyntaxByDefault_RecordsADiagnostic(string message, ImfDiagnosticKind kind, string fieldName)
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message))))
            {
                // act
                reader.ReadMessage();

                // assert
                reader.Diagnostics.Should().ContainSingle();
                reader.Diagnostics[0].Kind.Should().Be(kind);
                reader.Diagnostics[0].FieldName.Should().Be(fieldName);
            }
        }

        [Fact]
        public void To_RepeatedField_CombinesTheAddresses()
        {
            // arrange
            var subject = ImfMessages.Read(RepeatedToMessage);

            // act
            var testResult = subject.To;

            // assert
            testResult.Should().HaveCount(RepeatedToCount);
        }

        [Fact]
        public void Subject_RepeatedField_GivesTheFirst()
        {
            // arrange
            var subject = ImfMessages.Read(RepeatedSubjectMessage);

            // act
            var testResult = subject.Subject;

            // assert
            testResult.Should().Be(FirstSubject);
        }

        [Fact]
        public void ReadMessage_OnlyTheMissingFieldOption_StillRejectsAnInvalidValue()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(OnlyInvalidFrom)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void Validate_SeveralProblems_GivesEvery()
        {
            // arrange
            var subject = ImfMessages.Read(SeveralProblems, ImfMessages.AcceptAll());

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().HaveCount(4);
        }

        [Fact]
        public void Validate_NewMessage_GivesTheMissingFieldsWithNoPosition()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().HaveCount(2);
            testResult[0].BytePosition.Should().Be(-1);
        }

        [Fact]
        public void Validate_FieldReadAtAPosition_GivesThatPosition()
        {
            // arrange
            var subject = ImfMessages.Read(InvalidFromAtPosition, ImfMessages.AcceptAll());

            // act
            var testResult = subject.Validate();

            // assert
            testResult.Should().ContainSingle();
            testResult[0].BytePosition.Should().Be(InvalidFromPosition);
        }

        [Fact]
        public void Date_WrongDayOfWeekWithOption_GivesTheValue()
        {
            // arrange
            var subject = ImfMessages.Read(WrongDayMessage, ImfMessages.AcceptAll());

            // act
            var testResult = subject.Date;

            // assert
            testResult.ToString().Should().Be(SampleDate);
        }

        [Fact]
        public void WriteMessage_FieldSetToFixAViolation_Succeeds()
        {
            // arrange
            var subject = ImfMessages.Read(OnlyFrom, ImfMessages.AcceptAll());
            subject.Date = ImfDateTime.Parse(SampleDate);

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(FixedMessage);
        }

        [Fact]
        public void ReadMessage_MissingField_GivesTheEndOfTheHeaderAsThePosition()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(OnlyFromWithBody)), ImfMessages.AcceptAll()))
            {
                // act
                reader.ReadMessage();

                // assert
                reader.Diagnostics[0].BytePosition.Should().Be(OnlyFromLength);
            }
        }

        [Fact]
        public void ReadMessage_ViolationAtFault_LeavesTheBodyUnread()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(InvalidDateWithBody), BlockStreamMode.SynchronousOnly, InvalidDateMessageLength);
            using (var reader = new ImfReader(stream))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.BytesGiven.Should().Be(InvalidDateMessageLength);
            }
        }
    }
}
