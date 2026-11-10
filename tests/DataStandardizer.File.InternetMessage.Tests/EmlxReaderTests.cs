using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="EmlxReader"/>.
    /// </summary>
    public class EmlxReaderTests
    {
        private const string Metadata = EmlxFiles.Metadata;
        private const string Message = EmlxFiles.Message;

        private const string NotANumber = "abc\n" + Message + Metadata;
        private const string EmptyCount = "\n" + Message + Metadata;
        private const string CarriageReturnCount = "57\r\n" + Message + Metadata;
        private const string OverflowingCount = "99999999999999999999\n" + Message + Metadata;
        private const string NoLineFeed = "57";
        private const string TooLargeCount = "9999\n" + Message + Metadata;
        private const string TooSmallCount = "10\n" + Message + Metadata;
        private const string CountIntoMetadata = "60\n" + Message + Metadata;
        private const string NoTrailer = "57\n" + Message;
        private const string WrongCountNoTrailer = "9999\n" + Message;
        private const string NotXmlTrailer = "57\n" + Message + "<?xml nonsense";
        private const string UnclosedTrailer = "57\n" + Message + "<plist version=\"1.0\"><dict>";
        private const string NotPlistRoot = "57\n" + Message + "<?xml version=\"1.0\"?><other/>";
        private const string ArrayRoot = "57\n" + Message + "<plist><array/></plist>";
        private const string EmptyPlist = "57\n" + Message + "<plist/>";
        private const string TwoValues = "57\n" + Message + "<plist><dict/><dict/></plist>";
        private const string KeyWithNoValue = "57\n" + Message + "<plist><dict><key>a</key></dict></plist>";
        private const string ValueWithNoKey = "57\n" + Message + "<plist><dict><string>a</string></dict></plist>";
        private const string KeyInArray = "57\n" + Message + "<plist><dict><key>a</key><array><key>b</key></array></dict></plist>";
        private const string UnknownElement = "57\n" + Message + "<plist><dict><key>a</key><uid>1</uid></dict></plist>";
        private const string BadInteger = "57\n" + Message + "<plist><dict><key>a</key><integer>x</integer></dict></plist>";
        private const string BadReal = "57\n" + Message + "<plist><dict><key>a</key><real>1e</real></dict></plist>";
        private const string BadDate = "57\n" + Message + "<plist><dict><key>a</key><date>soon</date></dict></plist>";
        private const string BadData = "57\n" + Message + "<plist><dict><key>a</key><data>!</data></dict></plist>";
        private const string TextInTrue = "57\n" + Message + "<plist><dict><key>a</key><true>x</true></dict></plist>";
        private const string StrayText = "57\n" + Message + "<plist><dict>x</dict></plist>";
        private const string TextAfterPlist = "57\n" + Message + "<plist><dict/></plist>x";
        private const string EntityExpansion = "57\n" + Message + "<?xml version=\"1.0\"?><!DOCTYPE plist [<!ENTITY a \"b\">]><plist><dict><key>a</key><string>&a;</string></dict></plist>";

        private const string SmallValues = "57\n" + Message + "<plist><dict><key>r</key><real>2.5</real><key>n</key><real>nan</real><key>h</key><integer>0x10</integer><key>e</key><string></string><key>w</key><string> </string><key>s</key><array/><key>m</key><dict/></dict></plist>";
        private const string RepeatedKey = "57\n" + Message + "<plist><dict><key>subject</key><string>one</string><key>subject</key><string>two</string></dict></plist>";
        private const string WrongKinds = "57\n" + Message + "<plist><dict><key>flags</key><string>1</string><key>date-received</key><string>x</string><key>remote-id</key><true/><key>subject</key><integer>1</integer></dict></plist>";
        private const string OtherDateForms = "57\n" + Message + "<plist><dict><key>date-received</key><date>2026-01-01T00:00:00Z</date><key>date-last-viewed</key><real>1767225600.5</real></dict></plist>";
        private const string DeepPlist = "57\n" + Message + "<plist><array><array><array/></array></array></plist>";
        private const string TooDeepInDict = "57\n" + Message + "<plist><dict><key>a</key><array><array><array/></array></array></dict></plist>";

        private const string CrLfMessage = "59\nDate: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n\r\none\r\n" + Metadata;
        private const string TooLargeCountWithByteOrderMark = "9999\n" + Message + EmlxFiles.ByteOrderMark + Metadata;
        private const string BadMessage = "11\nDate: x\n\ny\n" + Metadata;

        // ref. the bits in EmlxFiles.KnownAndUnknownFlags.
        private const EmlxMessageFlags ExpectedFlags = EmlxMessageFlags.Read | EmlxMessageFlags.Answered;
        private const int ExpectedAttachmentCount = 2;
        private const int ExpectedPriority = 3;

        private const string ExpectedSubject = "Hello & welcome";
        private const string ExpectedRemoteId = "42";
        private const long DateReceivedTicks = 639028224000000000;
        private const long DateLastViewedTicks = 639028224600000000;

        // Half a second after DateReceivedTicks.
        private const long DateLastViewedAsRealTicks = 639028224005000000;
        private const long PositionAfterCount = 60;
        private const long LineAfterMessage = 6;
        private const int ShallowDepth = 2;
        private const int SmallLimit = 10;
        private const int PartLimit = 500;
        private const long PartLength = 4;
        private const string PartPath = "2";
        private const string SinglePartPath = "1";
        private const string CrLfPart = "AA\r\nEC";
        private const string LineFeedPartInMessage = "attachment\n\nAA\nEC\n--b--\n";

        // The places of the keys of EmlxFiles.UnknownKeysMetadata.
        private const int RealIndex = 0;
        private const int BigIntegerIndex = 1;
        private const int ArrayIndex = 2;
        private const int DictIndex = 3;

        // The places of the two strings of SmallValues.
        private const int EmptyStringIndex = 3;
        private const int SpaceStringIndex = 4;

        // The three octets that the base64 of EmlxFiles.PartContent decodes to.
        private static readonly byte[] DecodedPart = { 0, 1, 2 };

        // The limits that give a message and its parts 1000 octets, for a message of 313, so that after a first part of 400 the second has room for 287, and is read as far as one past that.
        private const int TotalLimitPerSection = 500;
        private const int FirstPartLength = 400;
        private const long SecondPartBytesRead = 288;

        // Under ten octets for each of three limits, and 64 for the count, a file of more is read one octet past those 94 before it is refused.
        private const long BytesReadPastTheLimits = 95;

        private static readonly byte[] FirstPartOctets = Octets.Of(new string('A', FirstPartLength));

        // A part over PartLimit.
        private static readonly string LargePart = new string('A', 2 * PartLimit);

        private static readonly DateTimeOffset DateReceived = new DateTimeOffset(DateReceivedTicks, TimeSpan.Zero);
        private static readonly DateTimeOffset DateLastViewed = new DateTimeOffset(DateLastViewedTicks, TimeSpan.Zero);
        private static readonly DateTimeOffset DateLastViewedAsReal = new DateTimeOffset(DateLastViewedAsRealTicks, TimeSpan.Zero);

        public static TheoryData<string> BadByteCounts => new TheoryData<string>
        {
            NotANumber, EmptyCount, CarriageReturnCount, OverflowingCount, NoLineFeed, TooLargeCount, TooSmallCount, CountIntoMetadata, string.Empty,
        };

        public static TheoryData<string> BadPlists => new TheoryData<string>
        {
            NoTrailer, NotXmlTrailer, UnclosedTrailer, NotPlistRoot, ArrayRoot, EmptyPlist, TwoValues, KeyWithNoValue, ValueWithNoKey, KeyInArray, UnknownElement, BadInteger,
            BadReal, BadDate, BadData, TextInTrue, StrayText, TextAfterPlist, EntityExpansion,
        };

        public static TheoryData<string, string> KeptPlists => new TheoryData<string, string>
        {
            { NoTrailer, string.Empty },
            { UnclosedTrailer, "<plist version=\"1.0\"><dict>" },
            { NotXmlTrailer, "<?xml nonsense" },
            { ArrayRoot, "<plist><array/></plist>" },
        };

        public static TheoryData<string, string> RecoveredMessages => new TheoryData<string, string>
        {
            { NotANumber, Message },
            { TooLargeCount, Message },
            { TooSmallCount, Message },
            { CountIntoMetadata, Message },
            { WrongCountNoTrailer, Message },
            { TooLargeCountWithByteOrderMark, Message },
        };

        [Fact]
        public void ReadEntry_PlainFile_ReadsTheMessageWithLineFeeds()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Plain);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(Message);
        }

        [Fact]
        public void ReadEntry_PlainFile_HasNoDiagnosticsOrMissingParts()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Plain);

            // assert
            testResult.Diagnostics.Should().BeEmpty();
            testResult.MissingParts.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntry_PlainFile_GivesTheWellKnownKeys()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Plain).Metadata;

            // assert
            testResult.IsWellFormed.Should().BeTrue();
            testResult.Flags.Should().Be(EmlxFiles.KnownAndUnknownFlags);
            testResult.MessageFlags.Should().Be(ExpectedFlags);
            testResult.AttachmentCount.Should().Be(ExpectedAttachmentCount);
            testResult.PriorityLevel.Should().Be(ExpectedPriority);
            testResult.DateReceived.Should().Be(DateReceived);
            testResult.DateLastViewed.Should().Be(DateLastViewed);
            testResult.RemoteId.Should().Be(ExpectedRemoteId);
            testResult.Subject.Should().Be(ExpectedSubject);
        }

        [Fact]
        public void ReadEntry_PaddedCount_ReadsTheMessage()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Padded);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(Message);
            testResult.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntry_MessageWithNoFinalLineBreak_ReadsTheMessage()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.NoFinalLineBreak);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.MessageNoFinalLineBreak);
        }

        [Fact]
        public void ReadEntry_Attachment_ReadsTheParts()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Attachment);

            // assert
            testResult.Message.Attachments.Should().ContainSingle().Which.GetContent().ToArray().Should().Equal(DecodedPart);
            testResult.MissingParts.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntry_UnknownKeys_KeepsThemInOrder()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.UnknownKeys).Metadata;

            // assert
            testResult.Entries.Select(entry => entry.Key).Should().Equal("x-color", "x-big", "x-list", "x-map", "subject");
        }

        [Fact]
        public void ReadEntry_UnknownKeys_KeepsTheirValues()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.UnknownKeys).Metadata;

            // assert
            testResult.Entries[RealIndex].Value.Text.Should().Be("1.50");
            testResult.Entries[BigIntegerIndex].Value.Text.Should().Be("123456789012345678901234567890");
            testResult.Entries[ArrayIndex].Value.Items.Select(item => item.Kind).Should().Equal(EmlxPlistValueKind.String, EmlxPlistValueKind.True, EmlxPlistValueKind.False);
            testResult.Entries[DictIndex].Value.Entries.Select(entry => entry.Value.Kind).Should().Equal(EmlxPlistValueKind.Date, EmlxPlistValueKind.Data);
            testResult.Subject.Should().Be("  spaced  ");
        }

        [Fact]
        public void ReadEntry_SmallValues_KeepsThemAsTheyAre()
        {
            // act
            var testResult = EmlxFiles.Read(SmallValues).Metadata.Entries;

            // assert
            testResult.Select(entry => entry.Value.Kind).Should().Equal(EmlxPlistValueKind.Real, EmlxPlistValueKind.Real, EmlxPlistValueKind.Integer, EmlxPlistValueKind.String,
                EmlxPlistValueKind.String, EmlxPlistValueKind.Array, EmlxPlistValueKind.Dictionary);
            testResult[EmptyStringIndex].Value.Text.Should().BeEmpty();
            testResult[SpaceStringIndex].Value.Text.Should().Be(" ");
        }

        [Fact]
        public void ReadEntry_RepeatedKey_KeepsBothAndLooksUpTheLast()
        {
            // act
            var testResult = EmlxFiles.Read(RepeatedKey).Metadata;

            // assert
            testResult.Entries.Should().HaveCount(2);
            testResult.Subject.Should().Be("two");
        }

        [Fact]
        public void ReadEntry_WellKnownKeysOfOtherKinds_GiveNullAndKeepTheValues()
        {
            // act
            var testResult = EmlxFiles.Read(WrongKinds).Metadata;

            // assert
            testResult.Flags.Should().BeNull();
            testResult.MessageFlags.Should().BeNull();
            testResult.AttachmentCount.Should().BeNull();
            testResult.PriorityLevel.Should().BeNull();
            testResult.DateReceived.Should().BeNull();
            testResult.RemoteId.Should().BeNull();
            testResult.Subject.Should().BeNull();
            testResult.Entries.Should().HaveCount(4);
        }

        [Fact]
        public void ReadEntry_DatesAsDateAndReal_GivesThem()
        {
            // act
            var testResult = EmlxFiles.Read(OtherDateForms).Metadata;

            // assert
            testResult.DateReceived.Should().Be(DateReceived);
            testResult.DateLastViewed.Should().Be(DateLastViewedAsReal);
        }

        [Theory]
        [MemberData(nameof(BadByteCounts))]
        public void ReadEntry_BadByteCount_ThrowsByDefault(string file)
        {
            // act
            Action testAction = () => EmlxFiles.Read(file);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxByteCount);
        }

        [Theory]
        [MemberData(nameof(RecoveredMessages))]
        public void ReadEntry_BadByteCountWithOption_FindsTheMessage(string file, string expected)
        {
            // arrange
            var options = EmlxFiles.Lenient;

            // act
            var testResult = EmlxFiles.Read(file, options);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(expected);
            testResult.Diagnostics.Select(diagnostic => diagnostic.Kind).Should().Contain(ImfDiagnosticKind.InvalidEmlxByteCount);
        }

        [Fact]
        public void ReadEntry_BadByteCountWithOnlyTheCountOption_DoesNotAcceptAMissingTrailer()
        {
            // arrange
            var options = new ImfOptions { AllowInvalidEmlxByteCount = true };

            // act
            Action testAction = () => EmlxFiles.Read(WrongCountNoTrailer, options);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Fact]
        public void ReadEntry_BadByteCount_GivesItsPosition()
        {
            // act
            Action testAction = () => EmlxFiles.Read(TooLargeCount);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.BytePosition].Should().Be(0L);
        }

        [Theory]
        [MemberData(nameof(BadPlists))]
        public void ReadEntry_BadPlist_ThrowsByDefault(string file)
        {
            // act
            Action testAction = () => EmlxFiles.Read(file);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Theory]
        [MemberData(nameof(BadPlists))]
        public void ReadEntry_BadPlistWithOption_RecordsADiagnostic(string file)
        {
            // arrange
            var options = new ImfOptions { AllowInvalidEmlxMetadata = true };

            // act
            var testResult = EmlxFiles.Read(file, options);

            // assert
            testResult.Metadata.IsWellFormed.Should().BeFalse();
            testResult.Metadata.Entries.Should().BeEmpty();
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Theory]
        [MemberData(nameof(KeptPlists))]
        public void ReadEntry_BadPlistWithOption_KeepsTheBytes(string file, string expected)
        {
            // arrange
            var options = new ImfOptions { AllowInvalidEmlxMetadata = true };

            // act
            var testResult = EmlxFiles.Read(file, options).Metadata.RawBytes;

            // assert
            Octets.ToText(testResult.Span).Should().Be(expected);
        }

        [Fact]
        public void ReadEntry_BadPlist_GivesItsPosition()
        {
            // act
            Action testAction = () => EmlxFiles.Read(NoTrailer);

            // assert
            var testResult = testAction.Should().Throw<ImfFileException>().Which.Data;
            testResult[ImfFileException.DataItemName.BytePosition].Should().Be(PositionAfterCount);
            testResult[ImfFileException.DataItemName.LineNumber].Should().Be(LineAfterMessage);
        }

        [Fact]
        public void ReadEntry_PlistPastTheDepthLimit_IsNotWellFormed()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = ShallowDepth };

            // act
            Action testAction = () => EmlxFiles.Read(TooDeepInDict, options);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Fact]
        public void ReadEntry_PlistWithANonDictRootInsideTheDepthLimit_IsNotWellFormed()
        {
            // act
            Action testAction = () => EmlxFiles.Read(DeepPlist);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Fact]
        public void ReadEntry_PlistPastTheLengthLimit_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEmlxMetadataLength = SmallLimit };

            // act
            Action testAction = () => EmlxFiles.Read(EmlxFiles.Plain, options);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data.Contains(ImfFileException.DataItemName.DiagnosticKind).Should().BeFalse();
        }

        [Fact]
        public void ReadEntry_FilePastTheLimits_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = SmallLimit, MaxHeaderSectionLength = SmallLimit, MaxEmlxMetadataLength = SmallLimit };

            // act
            Action testAction = () => EmlxFiles.Read(EmlxFiles.Plain, options);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void BytePosition_FilePastTheLimits_CountsWhatWasRead()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = SmallLimit, MaxHeaderSectionLength = SmallLimit, MaxEmlxMetadataLength = SmallLimit };
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)), options);
            EmlxFiles.TryRefuse(subject);

            // act
            var testResult = subject.BytePosition;

            // assert
            testResult.Should().Be(BytesReadPastTheLimits);
        }

        [Fact]
        public void ReadEntry_ByteOrderMarkBeforeThePlist_ReadsIt()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.WithByteOrderMark);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(Message);
            testResult.Metadata.Subject.Should().Be(ExpectedSubject);
            testResult.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntry_PartsThatTogetherPassTheLimit_StopsReadingAtTheLimit()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = TotalLimitPerSection, MaxHeaderSectionLength = TotalLimitPerSection };
            var second = new CountingStream(FirstPartOctets);
            var resolver = EmlxFiles.ResolveTo(new MemoryStream(FirstPartOctets), second);

            // act
            Action testAction = () => EmlxFiles.Read(EmlxFiles.TwoParts, options, resolver);

            // assert
            testAction.Should().Throw<ImfFileException>();
            second.BytesRead.Should().Be(SecondPartBytesRead);
        }

        [Fact]
        public async Task ReadEntryAsync_PartsThatTogetherPassTheLimit_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = TotalLimitPerSection, MaxHeaderSectionLength = TotalLimitPerSection };
            var resolver = EmlxFiles.ResolveTo(new MemoryStream(FirstPartOctets), new MemoryStream(FirstPartOctets));
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.TwoParts)), options);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadEntryAsync(resolver);

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public void TryRefuse_FileThatIsRefused_GivesTrue()
        {
            // arrange
            var reader = new EmlxReader(new MemoryStream(Octets.Of(NoTrailer)));

            // act
            var testResult = EmlxFiles.TryRefuse(reader);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void TryRefuse_FileThatIsRead_GivesFalse()
        {
            // arrange
            var reader = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));

            // act
            var testResult = EmlxFiles.TryRefuse(reader);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ReadEntry_MessageWithCrLfLineBreaks_Throws()
        {
            // act
            Action testAction = () => EmlxFiles.Read(CrLfMessage);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void ReadEntry_MessageThatBreaksItsRules_ThrowsLikeImfReader()
        {
            // act
            Action testAction = () => EmlxFiles.Read(BadMessage);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidFieldValue);
        }

        [Fact]
        public void ReadEntry_PartialWithNoResolver_ReadsWithoutThePart()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.PartialMessage);
            var part = testResult.MissingParts.Should().ContainSingle().Which;
            part.Path.Should().Be(PartPath);
            part.ContentLength.Should().Be(PartLength);
            part.Entity.GetContent().IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void ReadEntry_PartialWithNoResolver_RecordsADiagnosticForThePart()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial).Diagnostics;

            // assert
            var diagnostic = testResult.Should().ContainSingle().Which;
            diagnostic.Kind.Should().Be(ImfDiagnosticKind.MissingEmlxPart);
            diagnostic.EntityPath.Should().Be(PartPath);
        }

        [Fact]
        public void ReadEntry_PartialWithResolver_PutsThePartInTheMessage()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial, null, EmlxFiles.ResolveTo(EmlxFiles.PartContent));

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.WholeMessage);
            testResult.Message.Attachments.Should().ContainSingle().Which.GetContent().ToArray().Should().Equal(DecodedPart);
            testResult.MissingParts.Should().BeEmpty();
            testResult.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntry_TwoParts_PutsEachInItsPlace()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.TwoParts, null, EmlxFiles.ResolveTwoParts());

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.TwoPartsWholeMessage);
            testResult.MissingParts.Should().BeEmpty();
        }

        [Fact]
        public void TryRefuse_EntryThatIsRefused_GivesTrue()
        {
            // arrange
            var entry = EmlxFiles.Read(NoTrailer, EmlxFiles.Lenient);
            var writer = new EmlxWriter(new MemoryStream());

            // act
            var testResult = EmlxFiles.TryRefuse(writer, entry);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void TryRefuse_EntryThatIsWritten_GivesFalse()
        {
            // arrange
            var entry = EmlxFiles.Read(EmlxFiles.Plain);
            var writer = new EmlxWriter(new MemoryStream());

            // act
            var testResult = EmlxFiles.TryRefuse(writer, entry);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ReadEntry_PartialStoppingBeforeItsEmptyLine_AddsTheEmptyLine()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.NoEmptyLine, null, EmlxFiles.ResolveTo(EmlxFiles.PartContent));

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.WholeMessage);
        }

        [Fact]
        public void ReadEntry_PartWithCrLfLineBreaks_ChangesThemToLineFeeds()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial, null, EmlxFiles.ResolveTo(CrLfPart));

            // assert
            testResult.Message.Attachments.Should().ContainSingle().Which.GetContent().ToArray().Should().Equal(DecodedPart);
            Mailboxes.Describe(testResult.Message).Should().Contain(LineFeedPartInMessage);
        }

        [Fact]
        public void ReadEntry_ResolverWithNoContent_LeavesThePartMissing()
        {
            // arrange
            EmlxPartResolver resolver = part => null;

            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial, null, resolver);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.PartialMessage);
            testResult.MissingParts.Should().ContainSingle();
        }

        [Fact]
        public void ReadEntry_ResolverWithAnEmptyStream_LeavesThePartMissing()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.Partial, null, EmlxFiles.ResolveTo(string.Empty));

            // assert
            testResult.MissingParts.Should().ContainSingle();
        }

        [Fact]
        public void ReadEntry_ResolverThatIsTooLarge_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEntitySize = PartLimit };

            // act
            Action testAction = () => EmlxFiles.Read(EmlxFiles.Partial, options, EmlxFiles.ResolveTo(LargePart));

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void ReadEntry_BodyOfAMessageWithNoParts_IsPartOne()
        {
            // act
            var testResult = EmlxFiles.Read(EmlxFiles.SinglePart).MissingParts;

            // assert
            var part = testResult.Should().ContainSingle().Which;
            part.Path.Should().Be(SinglePartPath);
            part.ContentLength.Should().BeNull();
        }

        [Fact]
        public async Task ReadEntryAsync_PlainFile_GivesWhatTheSynchronousReadGives()
        {
            // arrange
            var subject = new EmlxReader(new BlockStream(Octets.Of(EmlxFiles.Plain), BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                var testResult = await subject.ReadEntryAsync();

                // assert
                Mailboxes.Describe(testResult.Message).Should().Be(Message);
                testResult.Metadata.Subject.Should().Be(ExpectedSubject);
            });
        }

        [Fact]
        public async Task ReadEntryAsync_PartialWithResolver_ReadsThePartAsynchronously()
        {
            // arrange
            EmlxPartResolver resolver = part => new BlockStream(Octets.Of(EmlxFiles.PartContent), BlockStreamMode.AsynchronousOnly);
            var subject = new EmlxReader(new BlockStream(Octets.Of(EmlxFiles.Partial), BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                var testResult = await subject.ReadEntryAsync(resolver);

                // assert
                Mailboxes.Describe(testResult.Message).Should().Be(EmlxFiles.WholeMessage);
            });
        }

        [Fact]
        public async Task ReadEntryAsync_BadPlist_ThrowsThroughTheTask()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(NoTrailer)));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadEntryAsync();

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public async Task ReadEntryAsync_Canceled_ThrowsAndEndsReading()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadEntryAsync(null, canceled);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public void BytePosition_AfterARead_IsTheLengthOfTheFile()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(NoTrailer)), new ImfOptions { AllowInvalidEmlxMetadata = true });
            subject.ReadEntry();

            // act
            var testResult = subject.BytePosition;

            // assert
            testResult.Should().Be(PositionAfterCount);
        }

        [Fact]
        public void Diagnostics_AfterARead_HoldThoseOfTheEntry()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Partial)));
            subject.ReadEntry();

            // act
            var testResult = subject.Diagnostics;

            // assert
            testResult.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingEmlxPart);
        }

        [Fact]
        public void ReadEntry_ReadTwice_Throws()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));
            subject.ReadEntry();

            // act
            Action testAction = () => subject.ReadEntry();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ReadEntry_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));
            subject.Dispose();

            // act
            Action testAction = () => subject.ReadEntry();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));
            subject.Dispose();

            // act
            Action testAction = () => _ = subject.Diagnostics;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxReader(new MemoryStream(Octets.Of(EmlxFiles.Plain)));
            subject.Dispose();

            // act
            Action testAction = () => _ = subject.BytePosition;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream(Octets.Of(EmlxFiles.Plain));
            var subject = new EmlxReader(stream);

            // act
            subject.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new EmlxReader((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new EmlxReader(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_MetadataLimitBelowOne_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxEmlxMetadataLength = 0 };

            // act
            Action testAction = () => new EmlxReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("123.partial.emlx", "123.2.emlxpart")]
        [InlineData("123.emlx", "123.2.emlxpart")]
        [InlineData("dir/123.PARTIAL.EMLX", "dir/123.2.emlxpart")]
        [InlineData("123", "123.2.emlxpart")]
        public void GetPartFileName_MessageFileName_FollowsTheConvention(string messageFileName, string expected)
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.Partial).MissingParts[0];

            // act
            var testResult = subject.GetPartFileName(messageFileName);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetPartFileName_Null_Throws()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.Partial).MissingParts[0];

            // act
            Action testAction = () => subject.GetPartFileName(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
