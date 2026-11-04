using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="EmlxWriter"/>, and of moving a message between EMLX and <c>.eml</c> files.
    /// </summary>
    public class EmlxWriterTests
    {
        private const string Message = EmlxFiles.Message;
        private const string Metadata = EmlxFiles.Metadata;

        private const string EmptyMetadata = EmlxFiles.PlistStart + "<dict/>\n" + EmlxFiles.PlistEnd;
        private const string SubjectMetadata = EmlxFiles.PlistStart + "<dict>\n\t<key>subject</key>\n\t<string>Hi</string>\n</dict>\n" + EmlxFiles.PlistEnd;
        private const string EscapedMetadata = EmlxFiles.PlistStart + "<dict>\n\t<key>subject</key>\n\t<string>a&amp;b&lt;c&#xD;d</string>\n</dict>\n" + EmlxFiles.PlistEnd;
        private const string NestedMetadata = EmlxFiles.PlistStart + "<dict>\n\t<key>a</key>\n\t<array>\n\t\t<string>x</string>\n\t\t<true/>\n\t\t<false/>\n\t\t<integer>7</integer>\n\t\t<real>1.5</real>\n\t\t<date>2026-01-01T00:00:00Z</date>\n\t\t<data>AAEC</data>\n\t\t<array/>\n\t\t<dict>\n\t\t\t<key>b</key>\n\t\t\t<dict/>\n\t\t</dict>\n\t</array>\n</dict>\n" + EmlxFiles.PlistEnd;
        private const string ChangedKeepsOrderMetadata = EmlxFiles.PlistStart + "<dict>\n\t<key>x-color</key>\n\t<real>1.50</real>\n\t<key>x-big</key>\n\t<integer>123456789012345678901234567890</integer>\n\t<key>x-list</key>\n\t<array>\n\t\t<string>a</string>\n\t\t<true/>\n\t\t<false/>\n\t</array>\n\t<key>x-map</key>\n\t<dict>\n\t\t<key>when</key>\n\t\t<date>2026-01-01T00:00:00Z</date>\n\t\t<key>bytes</key>\n\t\t<data>AAEC</data>\n\t</dict>\n\t<key>subject</key>\n\t<string>Hi</string>\n</dict>\n" + EmlxFiles.PlistEnd;

        private const string PlainWithEmptyMetadata = "57\n" + Message + EmptyMetadata;
        private const string BadPlistFile = "57\n" + Message + "<plist>";
        private const string NoTrailerFile = "57\n" + Message;
        private const string TooLargeCountFile = "9999\n" + Message + Metadata;
        private const string TooLargeCountFixed = "57  \n" + Message + Metadata;
        private const string NotANumberFile = "abc\n" + Message + Metadata;
        private const string RightCountFile = "57\n" + Message + Metadata;
        private const string UnknownKeysChangedFile = "57\n" + Message + ChangedKeepsOrderMetadata;
        private const string EscapedFile = "57\n" + Message + EscapedMetadata;
        private const string NestedFile = "57\n" + Message + NestedMetadata;
        private const string RepeatedSubjectFile = "57\n" + Message + EmlxFiles.PlistStart + "<dict>\n\t<key>subject</key>\n\t<string>one</string>\n\t<key>flags</key>\n\t<integer>1</integer>\n\t<key>subject</key>\n\t<string>two</string>\n</dict>\n" + EmlxFiles.PlistEnd;
        private const string BinaryEml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: binary\r\n\r\nx\r\n";
        private const string ChangedSubject = "Hi";

        // A property list of 22 octets.
        private const string ShortMetadataFile = "57\n" + Message + "<plist><dict/></plist>";
        private const int ShortMetadataLength = 22;

        // The dict at the root of a property list is at depth 1, and the two arrays are at 2 and 3.
        private const int DepthOfNestedArrays = 3;
        private const int BelowDepthOfNestedArrays = 2;
        private const string NestedArraysKey = "a";

        // The message with one more field, in a file whose count was padded to 10 characters.
        private const string PaddedAfterChange = "84        \nDate: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nSubject: Hello to you, too\n\none\n" + Metadata;
        private const string ChangedFieldSubject = "Hello to you, too";

        private const long FlagsValue = 5;
        private static readonly DateTimeOffset DateValue = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private const string DateValueSeconds = "1767225600";
        private const string EscapableSubject = "a&b<c\rd";
        private const string RemoteId = "9";
        private const string NewKey = "x";
        private const string SubjectKey = "subject";
        private const int RepeatedSubjectEntryCount = 2;
        private const double RealValue = 1.5;
        private const long IntegerValue = 7;
        private const int ExpectedFieldCount = 2;
        private const int RealIndex = 0;
        private const int BigIntegerIndex = 1;

        private static readonly byte[] DataValue = { 0, 1, 2 };

        public static TheoryData<string> UnchangedFiles => new TheoryData<string>
        {
            EmlxFiles.Plain,
            EmlxFiles.UnknownKeys,
            EmlxFiles.WithByteOrderMark,
            EmlxFiles.Padded,
            EmlxFiles.NoFinalLineBreak,
            EmlxFiles.Attachment,
            EmlxFiles.Partial,
            EmlxFiles.NoEmptyLine,
        };

        public static TheoryData<EmlxPlistValueKind, string> NumbersPastTheRange => new TheoryData<EmlxPlistValueKind, string>
        {
            { EmlxPlistValueKind.Real, "1e999" },
            { EmlxPlistValueKind.Integer, new string('9', 400) },
            { EmlxPlistValueKind.Integer, "0x" + new string('F', 300) },
        };

        public static TheoryData<string> LenientFiles => new TheoryData<string>
        {
            BadPlistFile,
            NoTrailerFile,
        };

        [Theory]
        [MemberData(nameof(UnchangedFiles))]
        public void WriteEntry_UnchangedFile_WritesItBackByteForByte(string file)
        {
            // arrange
            var entry = EmlxFiles.Read(file);

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(file);
        }

        [Fact]
        public void WriteEntry_PartialWithResolver_WritesAWholeFile()
        {
            // arrange
            var entry = EmlxFiles.Read(EmlxFiles.Partial, null, EmlxFiles.ResolveTo(EmlxFiles.PartContent));

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(EmlxFiles.Whole);
        }

        [Theory]
        [MemberData(nameof(LenientFiles))]
        public void WriteEntry_BadPlistWithOption_WritesItBackWithADiagnostic(string file)
        {
            // arrange
            var entry = EmlxFiles.Read(file, EmlxFiles.Lenient);
            var output = new MemoryStream();
            var subject = new EmlxWriter(output, EmlxFiles.Lenient);

            // act
            subject.WriteEntry(entry);

            // assert
            Octets.ToText(output.ToArray()).Should().Be(file);
            subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Theory]
        [MemberData(nameof(LenientFiles))]
        public void WriteEntry_BadPlistByDefault_ThrowsAndWritesNothing(string file)
        {
            // arrange
            var entry = EmlxFiles.Read(file, EmlxFiles.Lenient);
            var output = new MemoryStream();
            var subject = new EmlxWriter(output);

            // act
            Action testAction = () => subject.WriteEntry(entry);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
            output.Length.Should().Be(0);
        }

        [Fact]
        public void WriteEntry_WrongCountThatWasRead_WritesTheRightCount()
        {
            // arrange
            var entry = EmlxFiles.Read(TooLargeCountFile, EmlxFiles.Lenient);

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(TooLargeCountFixed);
        }

        [Fact]
        public void WriteEntry_NotANumberThatWasRead_WritesTheRightCount()
        {
            // arrange
            var entry = EmlxFiles.Read(NotANumberFile, EmlxFiles.Lenient);

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(RightCountFile);
        }

        [Fact]
        public void WriteEntry_NewEntryFromAnEmlFile_ChangesOnlyTheLineBreaksAndTheWrapper()
        {
            // arrange
            var message = new ImfReader(new MemoryStream(Octets.Of(EmlxFiles.Eml))).ReadMessage();
            var entry = new EmlxEntry(message, new EmlxMetadata());

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(PlainWithEmptyMetadata);
        }

        [Fact]
        public void WriteMessage_MessageFromAnEmlxFile_WritesTheEmlFileItCameFrom()
        {
            // arrange
            var message = EmlxFiles.Read(EmlxFiles.Plain).Message;
            var output = new MemoryStream();
            var subject = new ImfWriter(output);

            // act
            subject.WriteMessage(message);

            // assert
            Octets.ToText(output.ToArray()).Should().Be(EmlxFiles.Eml);
        }

        [Fact]
        public void WriteEntry_BinaryEntityFromAnEmlFile_Throws()
        {
            // arrange
            var message = new ImfReader(new MemoryStream(Octets.Of(BinaryEml))).ReadMessage();
            var entry = new EmlxEntry(message, new EmlxMetadata());

            // act
            Action testAction = () => EmlxFiles.Write(entry);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void WriteEntry_ChangedMetadata_WritesThePlistAgainInOrder()
        {
            // arrange
            var entry = EmlxFiles.Read(EmlxFiles.UnknownKeys);
            entry.Metadata.Subject = ChangedSubject;

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(UnknownKeysChangedFile);
        }

        [Fact]
        public void WriteEntry_ChangedMessageInAPaddedFile_KeepsTheWidthOfTheCount()
        {
            // arrange
            var entry = EmlxFiles.Read(EmlxFiles.Padded);
            entry.Message.Subject = ChangedFieldSubject;

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(PaddedAfterChange);
        }

        [Fact]
        public void WriteEntry_MetadataWithCharactersToEscape_EscapesThem()
        {
            // arrange
            var metadata = new EmlxMetadata { Subject = EscapableSubject };
            var entry = new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, metadata);

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(EscapedFile);
        }

        [Fact]
        public void WriteEntry_MetadataOfEveryKind_LaysThemOutAsAppleDoes()
        {
            // arrange
            var metadata = new EmlxMetadata();
            metadata.Set("a", EmlxPlistValue.FromArray(new[]
            {
                EmlxPlistValue.FromString("x"),
                EmlxPlistValue.FromBoolean(true),
                EmlxPlistValue.FromBoolean(false),
                EmlxPlistValue.FromInteger(IntegerValue),
                EmlxPlistValue.FromReal(RealValue),
                EmlxPlistValue.FromDate(DateValue),
                EmlxPlistValue.FromData(DataValue),
                EmlxPlistValue.FromArray(new EmlxPlistValue[0]),
                EmlxPlistValue.FromDictionary(new[] { new KeyValuePair<string, EmlxPlistValue>("b", EmlxPlistValue.FromDictionary(new KeyValuePair<string, EmlxPlistValue>[0])) }),
            }));
            var entry = new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, metadata);

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(NestedFile);
        }

        [Fact]
        public void WriteEntry_MetadataWrittenThenRead_GivesTheSameValues()
        {
            // arrange
            var metadata = new EmlxMetadata { Flags = FlagsValue, DateReceived = DateValue, DateLastViewed = DateValue, RemoteId = RemoteId, Subject = ChangedSubject };
            var written = EmlxFiles.Write(new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, metadata));

            // act
            var testResult = EmlxFiles.Read(written).Metadata;

            // assert
            testResult.Flags.Should().Be(FlagsValue);
            testResult.MessageFlags.Should().Be(EmlxMessageFlags.Read | EmlxMessageFlags.Answered);
            testResult.DateReceived.Should().Be(DateValue);
            testResult.DateLastViewed.Should().Be(DateValue);
            testResult.RemoteId.Should().Be(RemoteId);
            testResult.Subject.Should().Be(ChangedSubject);
        }

        [Fact]
        public void DateReceived_Set_HoldsSecondsAsAnInteger()
        {
            // arrange
            var subject = new EmlxMetadata();

            // act
            subject.DateReceived = DateValue;

            // assert
            var value = subject.Entries.Should().ContainSingle().Which.Value;
            value.Kind.Should().Be(EmlxPlistValueKind.Integer);
            value.Text.Should().Be(DateValueSeconds);
        }

        [Fact]
        public void Set_NewKey_GoesAtTheEnd()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.Plain).Metadata;

            // act
            subject.Set(NewKey, EmlxPlistValue.FromBoolean(true));

            // assert
            subject.Entries.Last().Key.Should().Be(NewKey);
            subject.RawBytes.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Set_RepeatedKey_KeepsThePlaceOfTheFirstAndDropsTheRest()
        {
            // arrange
            var subject = EmlxFiles.Read(RepeatedSubjectFile).Metadata;

            // act
            subject.Subject = ChangedSubject;

            // assert
            subject.Entries.Should().HaveCount(RepeatedSubjectEntryCount);
            subject.Entries.First().Key.Should().Be(SubjectKey);
            subject.Subject.Should().Be(ChangedSubject);
        }

        [Fact]
        public void Remove_KeyThatIsThere_RemovesItAndChangesTheBytes()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.Plain).Metadata;

            // act
            var testResult = subject.Remove(SubjectKey);

            // assert
            testResult.Should().BeTrue();
            subject.Subject.Should().BeNull();
            subject.RawBytes.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Remove_KeyThatIsNotThere_LeavesTheBytes()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.Plain).Metadata;

            // act
            var testResult = subject.Remove("nothing");

            // assert
            testResult.Should().BeFalse();
            subject.RawBytes.IsEmpty.Should().BeFalse();
        }

        [Fact]
        public void Subject_SetToNull_RemovesTheKey()
        {
            // arrange
            var subject = new EmlxMetadata { Subject = ChangedSubject };

            // act
            subject.Subject = null;

            // assert
            subject.Entries.Should().BeEmpty();
        }

        [Fact]
        public void Set_OnMetadataThatWasNotWellFormed_MakesItWellFormed()
        {
            // arrange
            var subject = EmlxFiles.Read(BadPlistFile, EmlxFiles.Lenient).Metadata;

            // act
            subject.Subject = ChangedSubject;

            // assert
            subject.IsWellFormed.Should().BeTrue();
            subject.RawBytes.IsEmpty.Should().BeTrue();
        }

        [Theory]
        [InlineData("a\0b")]
        [InlineData("a\xFFFEb")]
        public void Subject_SetWithACharacterThatXmlDoesNotAllow_Throws(string text)
        {
            // arrange
            var subject = new EmlxMetadata();

            // act
            Action testAction = () => subject.Subject = text;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        // A fact, as a theory's arguments can't carry an unpaired surrogate through the test runner.
        [Fact]
        public void Subject_SetWithAnUnpairedSurrogate_Throws()
        {
            // arrange
            var subject = new EmlxMetadata();

            // act
            Action testAction = () => subject.Subject = "a\xD800b";

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryGetValue_KeyThatIsThere_GivesIt()
        {
            // arrange
            var subject = new EmlxMetadata { Subject = ChangedSubject };

            // act
            var testResult = subject.TryGetValue(SubjectKey, out var value);

            // assert
            testResult.Should().BeTrue();
            value.Text.Should().Be(ChangedSubject);
        }

        [Fact]
        public void TryGetValue_NullKey_Throws()
        {
            // arrange
            var subject = new EmlxMetadata();

            // act
            Action testAction = () => subject.TryGetValue(null, out _);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Entries_OfMetadata_CannotBeChangedByACast()
        {
            // arrange
            var subject = new EmlxMetadata { Subject = ChangedSubject };

            // act
            var testResult = subject.Entries as IList<KeyValuePair<string, EmlxPlistValue>>;

            // assert
            testResult.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void TryGetInt64_LargerThanALong_GivesFalse()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.UnknownKeys).Metadata.Entries[BigIntegerIndex].Value;

            // act
            var testResult = subject.TryGetInt64(out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryGetDouble_Real_GivesTheNumber()
        {
            // arrange
            var subject = EmlxFiles.Read(EmlxFiles.UnknownKeys).Metadata.Entries[RealIndex].Value;

            // act
            var testResult = subject.TryGetDouble(out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().Be(RealValue);
        }

        [Theory]
        [InlineData(double.NaN, "nan")]
        [InlineData(double.PositiveInfinity, "+infinity")]
        [InlineData(double.NegativeInfinity, "-infinity")]
        public void FromReal_ValueWithNoDigits_UsesAWord(double value, string expected)
        {
            // act
            var testResult = EmlxPlistValue.FromReal(value);

            // assert
            testResult.Text.Should().Be(expected);
        }

        [Fact]
        public void TryGetData_Data_GivesTheBytes()
        {
            // arrange
            var subject = EmlxPlistValue.FromData(DataValue);

            // act
            var testResult = subject.TryGetData(out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().Equal(DataValue);
        }

        [Fact]
        public void TryGetDate_Date_GivesTheDate()
        {
            // arrange
            var subject = EmlxPlistValue.FromDate(DateValue);

            // act
            var testResult = subject.TryGetDate(out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().Be(DateValue);
        }

        [Fact]
        public void FromArray_NullValue_Throws()
        {
            // act
            Action testAction = () => EmlxPlistValue.FromArray(new EmlxPlistValue[] { null });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FromDictionary_NullKey_Throws()
        {
            // act
            Action testAction = () => EmlxPlistValue.FromDictionary(new[] { new KeyValuePair<string, EmlxPlistValue>(null, EmlxPlistValue.FromBoolean(true)) });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WriteEntry_SecondEntry_Throws()
        {
            // arrange
            var entry = EmlxFiles.Read(EmlxFiles.Plain);
            var subject = new EmlxWriter(new MemoryStream());
            subject.WriteEntry(entry);

            // act
            Action testAction = () => subject.WriteEntry(entry);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void WriteEntry_AfterAWriteThatWasRefused_WritesTheNextEntry()
        {
            // arrange
            var refused = EmlxFiles.Read(BadPlistFile, EmlxFiles.Lenient);
            var output = new MemoryStream();
            var subject = new EmlxWriter(output);
            EmlxFiles.TryRefuse(subject, refused);

            // act
            subject.WriteEntry(EmlxFiles.Read(EmlxFiles.Plain));

            // assert
            Octets.ToText(output.ToArray()).Should().Be(EmlxFiles.Plain);
        }

        [Fact]
        public void WriteEntry_WithProgress_ReportsTheEntryAtTheEnd()
        {
            // arrange
            var reports = new List<ImfWriteProgress>();
            var progress = new SynchronousProgress(reports);
            var subject = new EmlxWriter(new MemoryStream());

            // act
            subject.WriteEntry(EmlxFiles.Read(EmlxFiles.Plain), progress);

            // assert
            var last = reports.Last();
            last.EntitiesWritten.Should().Be(1);
            last.FieldsWritten.Should().Be(ExpectedFieldCount);
            last.BytesWritten.Should().Be(subject.BytePosition);
        }

        [Fact]
        public async Task WriteEntryAsync_UnchangedFile_WritesItBackByteForByte()
        {
            // arrange
            var output = new MemoryStream();
            var subject = new EmlxWriter(output);
            var entry = EmlxFiles.Read(EmlxFiles.Plain);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteEntryAsync(entry);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(EmlxFiles.Plain);
            });
        }

        [Fact]
        public async Task WriteEntryAsync_Canceled_ThrowsAndEndsWriting()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var output = new MemoryStream();
            var subject = new EmlxWriter(output);
            var entry = EmlxFiles.Read(EmlxFiles.Plain);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.WriteEntryAsync(entry, null, canceled);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
                output.Length.Should().Be(0);
            });
        }

        [Fact]
        public async Task FlushAsync_AfterAWrite_Completes()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());
            subject.WriteEntry(EmlxFiles.Read(EmlxFiles.Plain));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.FlushAsync();

                // assert
                await testAction.Should().NotThrowAsync();
            });
        }

        [Fact]
        public void Flush_AfterAWrite_DoesNotThrow()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());
            subject.WriteEntry(EmlxFiles.Read(EmlxFiles.Plain));

            // act
            Action testAction = () => subject.Flush();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void WriteEntry_MetadataPastTheLengthLimit_ThrowsAndWritesNothing()
        {
            // arrange
            var options = new ImfOptions { MaxEmlxMetadataLength = ShortMetadataLength - 1 };
            var entry = EmlxFiles.Read(ShortMetadataFile);
            var output = new MemoryStream();
            var subject = new EmlxWriter(output, options);

            // act
            Action testAction = () => subject.WriteEntry(entry);

            // assert
            testAction.Should().Throw<ImfFileException>();
            output.Length.Should().Be(0);
        }

        [Fact]
        public void WriteEntry_MetadataAtTheLengthLimit_WritesIt()
        {
            // arrange
            var options = new ImfOptions { MaxEmlxMetadataLength = ShortMetadataLength };
            var entry = EmlxFiles.Read(ShortMetadataFile);

            // act
            var testResult = EmlxFiles.Write(entry, options);

            // assert
            testResult.Should().Be(ShortMetadataFile);
        }

        [Fact]
        public void WriteEntry_MetadataPastTheDepthLimit_ThrowsByDefault()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = BelowDepthOfNestedArrays };
            var entry = new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, CreateNestedArrays());
            var subject = new EmlxWriter(new MemoryStream(), options);

            // act
            Action testAction = () => subject.WriteEntry(entry);

            // assert
            testAction.Should().Throw<ImfFileException>()
                .Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Fact]
        public void WriteEntry_MetadataPastTheDepthLimitWithOption_RecordsADiagnostic()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = BelowDepthOfNestedArrays, AllowInvalidEmlxMetadata = true };
            var entry = new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, CreateNestedArrays());
            var subject = new EmlxWriter(new MemoryStream(), options);

            // act
            subject.WriteEntry(entry);

            // assert
            subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidEmlxMetadata);
        }

        [Fact]
        public void WriteEntry_MetadataAtTheDepthLimit_CanBeReadBackUnderTheSameLimit()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = DepthOfNestedArrays };
            var written = EmlxFiles.Write(new EmlxEntry(EmlxFiles.Read(EmlxFiles.Plain).Message, CreateNestedArrays()), options);

            // act
            var testResult = EmlxFiles.Read(written, options);

            // assert
            testResult.Metadata.Entries.Should().ContainSingle();
        }

        [Theory]
        [InlineData("9223372036854775808", 9223372036854775808d)]
        [InlineData("-9223372036854775809", -9223372036854775808d)]
        [InlineData("0xFFFFFFFFFFFFFFFF", 18446744073709551615d)]
        [InlineData("0x1F", 31d)]
        [InlineData("5", 5d)]
        public void TryGetDouble_Integer_GivesTheNumber(string text, double expected)
        {
            // arrange
            var subject = EmlxPlistValue.CreateScalar(EmlxPlistValueKind.Integer, text);

            // act
            var testResult = subject.TryGetDouble(out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(NumbersPastTheRange))]
        public void TryGetDouble_NumberPastTheRange_GivesFalseAndZero(EmlxPlistValueKind kind, string text)
        {
            // arrange
            var subject = EmlxPlistValue.CreateScalar(kind, text);

            // act
            var testResult = subject.TryGetDouble(out var value);

            // assert
            testResult.Should().BeFalse();
            value.Should().Be(0);
        }

        [Fact]
        public void WriteEntry_NullEntry_Throws()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());

            // act
            Action testAction = () => subject.WriteEntry(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WriteEntry_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.WriteEntry(EmlxFiles.Read(EmlxFiles.Plain));

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => _ = subject.BytePosition;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var subject = new EmlxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => _ = subject.Diagnostics;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new EmlxWriter((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullMessage_Throws()
        {
            // act
            Action testAction = () => new EmlxEntry(null, new EmlxMetadata());

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullMetadata_Throws()
        {
            // arrange
            var message = EmlxFiles.Read(EmlxFiles.Plain).Message;

            // act
            Action testAction = () => new EmlxEntry(message, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        private static EmlxMetadata CreateNestedArrays()
        {
            var metadata = new EmlxMetadata();
            metadata.Set(NestedArraysKey, EmlxPlistValue.FromArray(new[] { EmlxPlistValue.FromArray(new EmlxPlistValue[0]) }));
            return metadata;
        }

        private sealed class SynchronousProgress : IProgress<ImfWriteProgress>
        {
            private readonly List<ImfWriteProgress> _reports;

            internal SynchronousProgress(List<ImfWriteProgress> reports)
            {
                _reports = reports;
            }

            public void Report(ImfWriteProgress value)
            {
                _reports.Add(value);
            }
        }
    }
}
