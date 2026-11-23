using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="EmlxWriter"/>, and of moving a message between EMLX and <c>.eml</c> files.
    /// </summary>
    public class EmlxWriterTests
    {
        private const int TryGetValueOutParameterIndex = 1;

        /// <summary>
        /// A double that needs 16 significant digits to read back as the same value, which .NET Framework's <c>"R"</c> writes with 15
        /// (<c>901.742658997766</c>), a different double.
        /// </summary>
        private const double SixteenDigitDouble = 901.7426589977661;

        /// <summary>
        /// The sum of 0.1 and 0.7, which needs 16 significant digits, and which .NET Framework's <c>"R"</c> writes with 17
        /// (<c>0.79999999999999993</c>).
        /// </summary>
        private const double SumOfOneTenthAndSevenTenths = 0.7999999999999999;

        // The smallest normal double, which needs 17 significant digits.
        private const double SmallestNormalDouble = 2.2250738585072014E-308;

        // 2^60 and 2^-20, powers of two whose neighbour below is half as far away as the one above.
        private const double TwoToTheSixtieth = 1152921504606846976.0;
        private const double TwoToTheMinusTwentieth = 9.5367431640625E-07;

        // Doubles in delegates, since xUnit on .NET Framework serializes a double in a theory row with that runtime's "R", which changes some.  The
        // texts are those "R" gives on .NET Core 3.0 and later.
        public static TheoryData<string, Func<double>, string> ShortestRealTexts => new TheoryData<string, Func<double>, string>
        {
            { "needs 16 digits", () => SixteenDigitDouble, "901.7426589977661" },
            { "needs 16 digits, below 1", () => SumOfOneTenthAndSevenTenths, "0.7999999999999999" },
            { "negative zero", () => -0.0, "-0" },
            { "whole number of 17 digits", () => 1E+16, "10000000000000000" },
            { "whole number of 18 digits", () => 1E+17, "1E+17" },
            { "decimal point three places before the first digit", () => 0.0001, "0.0001" },
            { "decimal point four places before the first digit", () => 0.00001, "1E-05" },
            { "smallest normal", () => SmallestNormalDouble, "2.2250738585072014E-308" },
            { "smallest subnormal", () => double.Epsilon, "5E-324" },
            { "power of two, above 1", () => TwoToTheSixtieth, "1.152921504606847E+18" },
            { "power of two, below 1", () => TwoToTheMinusTwentieth, "9.5367431640625E-07" },
            { "negative, in scientific notation", () => -1.5E+300, "-1.5E+300" },
        };

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
        private const string AttachmentFileName = "a.png";
        private const string AttachmentMediaType = "image/png";
        private const int AttachmentPartIndex = 1;

        // The attachment of a message built with no text part, which is the first part.
        private const int OnlyAttachmentPartIndex = 0;

        // ref. RFC 3501 §6.4.5: the first part of a multipart message body is part 1.
        private const string FirstPartPath = "1";
        private const string BuiltDate = "Thu, 1 Jan 2026 00:00:00 +0000";
        private const string BuiltFrom = "a@b.test";

        // ref. PNG (ISO/IEC 15948) §5.2: the PNG signature, which holds CRLF, LF and control octets, followed by more of each, as content that is not text.
        private static readonly byte[] AttachmentWithLineBreaks = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x0A, 0x0D, 0x0A, 0xFF };

        private const string DataFileName = "a.bin";
        private const string DataMediaType = "application/octet-stream";
        private const string TextMediaType = "text/plain";

        // Content of seven-bit octets whose CRLF is data, as it isn't text (RFC 2046 §4.1.1), so 7bit would cover it if the line breaks didn't change.
        private static readonly byte[] SevenBitDataWithCrLf = { 0x61, 0x0D, 0x0A, 0x62 };

        // Content with an octet above 127 and a CRLF that is data, which 8bit would cover if the line breaks didn't change.
        private static readonly byte[] EightBitDataWithCrLf = { 0x61, 0x0D, 0x0A, 0xE9 };

        // ref. RFC 2045 §6.2: content with a CRLF that is data, each with an identity encoding other than binary that covers its octets, and so would let a change of line
        // breaks alter it.  Binary has its own test.
        public static TheoryData<byte[], MimeTransferEncoding> DataWithCrLfLabelledWithAnIdentityEncoding => new TheoryData<byte[], MimeTransferEncoding>
        {
            { SevenBitDataWithCrLf, MimeTransferEncoding.SevenBit },
            { SevenBitDataWithCrLf, MimeTransferEncoding.EightBit },
            { EightBitDataWithCrLf, MimeTransferEncoding.EightBit },
        };

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

        /// <summary>
        /// The double nearest to <c>5.882948112288285E-204</c>.  .NET Framework reads that text as the double above it,
        /// <c>5.8829481122882854E-204</c>.
        /// </summary>
        private const double SixteenDigitExponentDouble = 5.8829481122882847E-204;

        /// <summary>
        /// 2^53 + 2, the double above 2^53 + 1.
        /// </summary>
        private const double TwoToThe53PlusTwo = 9007199254740994d;

        /// <summary>
        /// 2^53 + 1, with a last digit beyond the 17 significant digits a double needs, which puts it just above halfway between 2^53 and
        /// <see cref="TwoToThe53PlusTwo"/>.  .NET Framework ignores the last digit and reads 2^53.
        /// </summary>
        private const string JustAboveHalfwayText = "9007199254740993.0000000000000001";

        // The bits of positive and negative zero (IEEE 754 binary64: negative zero has only the sign bit set).
        private const long PositiveZeroBits = 0;
        private const long NegativeZeroBits = long.MinValue;

        /// <summary>
        /// 2^64 + 2049, just above halfway between 2^64 and the double above it, 2^64 + 4096, and too large for a <see cref="long"/>.  .NET
        /// Framework reads it as 2^64.
        /// </summary>
        private const string JustAboveHalfwayIntegerText = "18446744073709553665";

        /// <summary>
        /// The bits of 2^64 + 4096, the double above 2^64.
        /// </summary>
        private const long TwoToThe64PlusFourThousandNinetySixBits = 0x43F0000000000001;

        /// <summary>
        /// 2^65 + 4352, more than halfway between 2^65 and the double above it, 2^65 + 8192, in hexadecimal (#1104).
        /// </summary>
        private const string AboveHalfwayHexIntegerText = "0x" + AboveHalfwayHexDigits;

        /// <summary>
        /// The digits of <see cref="AboveHalfwayHexIntegerText"/>, without the <c>0x</c> prefix.
        /// </summary>
        private const string AboveHalfwayHexDigits = "20000000000001100";

        /// <summary>
        /// 2^65 + 4096, exactly halfway between 2^65 and 2^65 + 8192, in hexadecimal.  The tie goes to 2^65, whose significand is even.
        /// </summary>
        private const string HalfwayHexIntegerText = "0x20000000000001000";

        /// <summary>
        /// The bits of 2^65.
        /// </summary>
        private const long TwoToThe65Bits = 0x4400000000000000;

        /// <summary>
        /// The bits of 2^65 + 8192, the double above 2^65.
        /// </summary>
        private const long TwoToThe65PlusEightThousandOneHundredNinetyTwoBits = 0x4400000000000001;

        /// <summary>
        /// The significant hexadecimal digits of <see cref="double.MaxValue"/>, (2^53 - 1) * 2^971, before its 242 trailing zeros.
        /// </summary>
        private const string MaxDoubleHexDigits = "FFFFFFFFFFFFF8";

        /// <summary>
        /// The significant hexadecimal digits of halfway between <see cref="double.MaxValue"/> and 2^1024, before its 242 trailing zeros.  The
        /// tie goes to 2^1024, past the range of a double.
        /// </summary>
        private const string HalfwayAboveMaxDoubleHexDigits = "FFFFFFFFFFFFFC";

        /// <summary>
        /// The number of zeros after the significant digits of <see cref="MaxDoubleHexDigits"/> and <see cref="HalfwayAboveMaxDoubleHexDigits"/>,
        /// which brings each to 256 digits, the most that a double holds.
        /// </summary>
        private const int MaxDoubleHexTrailingZeros = 242;

        /// <summary>
        /// The bits of <see cref="double.MaxValue"/>.
        /// </summary>
        private const long MaxDoubleBits = 0x7FEFFFFFFFFFFFFF;

        /// <summary>
        /// More leading zeros than the 256 significant hexadecimal digits that a double holds.
        /// </summary>
        private const int ManyLeadingZeros = 300;

        /// <summary>
        /// The zeros after the 1 of 2^1024 = 16^256 in hexadecimal, one digit more than a double holds.
        /// </summary>
        private const int TwoToThe1024HexZeros = 256;

        // The expected doubles are their bits, so that negative zero differs from positive zero, and so that xUnit on .NET Framework doesn't
        // serialize a double in a theory row with that runtime's "R".
        public static TheoryData<string, string, long> NearestRealCases => new TheoryData<string, string, long>
        {
            { "negative zero", "-0", NegativeZeroBits },
            { "negative zero with a fraction and an exponent", "-0.0E0", NegativeZeroBits },
            { "needs 16 digits", "901.7426589977661", BitConverter.DoubleToInt64Bits(SixteenDigitDouble) },
            { "16 digits and an exponent", "5.882948112288285E-204", BitConverter.DoubleToInt64Bits(SixteenDigitExponentDouble) },
            { "smallest normal, rounded up", "2.2250738585072012E-308", BitConverter.DoubleToInt64Bits(SmallestNormalDouble) },
            { "just above halfway", JustAboveHalfwayText, BitConverter.DoubleToInt64Bits(TwoToThe53PlusTwo) },
            { "just below half the smallest subnormal", "2.4703282292062327E-324", PositiveZeroBits },
            { "decimal point with no digits after it", "+5.", BitConverter.DoubleToInt64Bits(5.0) },
            { "decimal point with no digits before it", ".5", BitConverter.DoubleToInt64Bits(0.5) },
        };

        public static TheoryData<string, string, long> LargeIntegerCases => new TheoryData<string, string, long>
        {
            { "decimal, just above halfway", JustAboveHalfwayIntegerText, TwoToThe64PlusFourThousandNinetySixBits },
            { "hexadecimal, above halfway", AboveHalfwayHexIntegerText, TwoToThe65PlusEightThousandOneHundredNinetyTwoBits },
            { "hexadecimal, halfway, ties to even", HalfwayHexIntegerText, TwoToThe65Bits },
            { "hexadecimal, leading zeros", "0x" + new string('0', ManyLeadingZeros) + AboveHalfwayHexDigits, TwoToThe65PlusEightThousandOneHundredNinetyTwoBits },
            { "hexadecimal, largest double", "0x" + MaxDoubleHexDigits + new string('0', MaxDoubleHexTrailingZeros), MaxDoubleBits },
        };

        public static TheoryData<EmlxPlistValueKind, string> NumbersPastTheRange => new TheoryData<EmlxPlistValueKind, string>
        {
            { EmlxPlistValueKind.Real, "1e999" },
            { EmlxPlistValueKind.Integer, new string('9', 400) },
            { EmlxPlistValueKind.Integer, "0x" + new string('F', 300) },
            { EmlxPlistValueKind.Integer, "0x1" + new string('0', TwoToThe1024HexZeros) },
            { EmlxPlistValueKind.Integer, "0x" + HalfwayAboveMaxDoubleHexDigits + new string('0', MaxDoubleHexTrailingZeros) },
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
        public void WriteEntry_BuiltAttachmentWithBinaryAllowed_ReadsBackWithTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).SetText("hi").AddAttachment(AttachmentFileName, MimeMediaType.Parse(AttachmentMediaType), AttachmentWithLineBreaks);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var file = EmlxFiles.Write(new EmlxEntry(builder.Build(), new EmlxMetadata()), options);
            var body = (ImfMultipart)EmlxFiles.Read(file, options).Message.Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(AttachmentWithLineBreaks);
        }

        [Fact]
        public void WriteEntry_BuiltPartLabelledBinary_Throws()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).AddAttachment(AttachmentFileName, MimeMediaType.Parse(AttachmentMediaType), AttachmentWithLineBreaks);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var message = builder.Build();
            ((ImfMultipart)message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding = MimeTransferEncoding.Binary;
            var entry = new EmlxEntry(message, new EmlxMetadata());

            // act
            Action testAction = () => EmlxFiles.Write(entry, options);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(FirstPartPath);
        }

        [Fact]
        public void WriteEntry_BuiltAttachmentOfSevenBitOctetsWithCrLf_ReadsBackWithTheSameBytes()
        {
            // arrange
            var builder = new ImfMessageBuilder().AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), SevenBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var entry = new EmlxEntry(builder.Build(), new EmlxMetadata());

            // act
            var file = EmlxFiles.Write(entry);
            var testResult = ((ImfMultipart)EmlxFiles.Read(file).Message.Body).Parts[OnlyAttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(SevenBitDataWithCrLf);
        }

        [Fact]
        public void WriteEntry_BuiltAttachmentOfEightBitOctetsWithCrLfAndEightBitAllowed_ReadsBackWithTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), EightBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var entry = new EmlxEntry(builder.Build(), new EmlxMetadata());

            // act
            var file = EmlxFiles.Write(entry, options);
            var testResult = ((ImfMultipart)EmlxFiles.Read(file, options).Message.Body).Parts[OnlyAttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(EightBitDataWithCrLf);
        }

        [Fact]
        public void WriteEntry_BuiltAttachmentWithCrLf_EncodesItQuotedPrintable()
        {
            // arrange
            var builder = new ImfMessageBuilder().AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), SevenBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var entry = new EmlxEntry(builder.Build(), new EmlxMetadata());

            // act
            var file = EmlxFiles.Write(entry);
            var testResult = ((ImfMultipart)EmlxFiles.Read(file).Message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding;

            // assert
            testResult.Should().Be(MimeTransferEncoding.QuotedPrintable);
        }

        [Theory]
        [MemberData(nameof(DataWithCrLfLabelledWithAnIdentityEncoding), DisableDiscoveryEnumeration = true)]
        public void WriteEntry_BuiltAttachmentWithCrLfLabelledWithAnIdentityEncoding_Throws(byte[] content, MimeTransferEncoding encoding)
        {
            // arrange
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), content);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var message = builder.Build();
            ((ImfMultipart)message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding = encoding;
            var entry = new EmlxEntry(message, new EmlxMetadata());

            // act
            Action testAction = () => EmlxFiles.Write(entry, options);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(FirstPartPath);
        }

        [Fact]
        public void WriteEntry_BuiltTextWithCrLf_LabelsItSevenBit()
        {
            // arrange
            var builder = new ImfMessageBuilder().AddAttachment(DataFileName, MimeMediaType.Parse(TextMediaType), SevenBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var entry = new EmlxEntry(builder.Build(), new EmlxMetadata());

            // act
            var file = EmlxFiles.Write(entry);
            var testResult = ((ImfMultipart)EmlxFiles.Read(file).Message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding;

            // assert
            testResult.Should().Be(MimeTransferEncoding.SevenBit);
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
        public void TryGetValue_OutParameter_IsMaybeNullOnlyWhenFalse()
        {
            // arrange
            var parameter = typeof(EmlxMetadata).GetMethod(nameof(EmlxMetadata.TryGetValue)).GetParameters()[TryGetValueOutParameterIndex];

            // act
            var testResult = parameter.GetCustomAttribute<MaybeNullWhenAttribute>();

            // assert
            testResult.Should().NotBeNull();
            testResult.ReturnValue.Should().BeFalse();
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
        [MemberData(nameof(ShortestRealTexts))]
        public void FromReal_Number_WritesTheFewestDigitsThatReadBack(string scenario, Func<double> value, string expected)
        {
            // arrange
            _ = scenario;
            var number = value();

            // act
            var testResult = EmlxPlistValue.FromReal(number);

            // assert
            testResult.Text.Should().Be(expected);
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
        [MemberData(nameof(NearestRealCases))]
        public void TryGetDouble_Real_GivesTheNearestDoubleOnEveryTarget(string scenario, string text, long expectedBits)
        {
            // arrange
            _ = scenario;
            var subject = EmlxPlistValue.CreateScalar(EmlxPlistValueKind.Real, text);

            // act
            var testResult = subject.TryGetDouble(out var value);

            // assert
            testResult.Should().BeTrue();
            BitConverter.DoubleToInt64Bits(value).Should().Be(expectedBits);
        }

        [Theory]
        [MemberData(nameof(LargeIntegerCases))]
        public void TryGetDouble_IntegerTooLargeForInt64_GivesTheNearestDoubleOnEveryTarget(string scenario, string text, long expectedBits)
        {
            // arrange
            _ = scenario;
            var subject = EmlxPlistValue.CreateScalar(EmlxPlistValueKind.Integer, text);

            // act
            var testResult = subject.TryGetDouble(out var value);

            // assert
            testResult.Should().BeTrue();
            BitConverter.DoubleToInt64Bits(value).Should().Be(expectedBits);
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
