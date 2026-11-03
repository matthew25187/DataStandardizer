using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfContentDispositionTests
    {
        // ref. RFC 2183 §3: the examples, the second of which ends with a semicolon that the grammar of §2 has no place for.
        private const string InlineExample = "inline";
        private const string AttachmentExample = "attachment; filename=genome.jpeg;\r\n  modification-date=\"Wed, 12 Feb 1997 16:29:51 -0500\";";
        private const string GenomeFileName = "genome.jpeg";
        private const string ExampleDate = "Wed, 12 Feb 1997 16:29:51 -0500";

        private const string AllParameters = "inline; filename=genome.jpeg; creation-date=\"" + ExampleDate + "\"; modification-date=\"" + ExampleDate + "\"; read-date=\"" + ExampleDate + "\"; size=1";

        // ref. RFC 2183 §2: numeric timezones MUST be used.
        private const string NamedZoneDate = "attachment; creation-date=\"Wed, 12 Feb 1997 16:29:51 EST\"";

        // The day of the week of this date is a Wednesday (RFC 5322 §3.3).
        private const string WrongDayDate = "attachment; read-date=\"Tue, 12 Feb 1997 16:29:51 -0500\"";

        private const string ExampleFileNameLetters65 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string ExampleFileNameLetters15 = "aaaaaaaaaaaaaaa";
        private const string ExampleFileNameLetters80 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        private const string AccentedFileName = "caf\x00E9.txt";
        private const string LongAccentedFileName = ExampleFileNameLetters80 + AccentedFileName;
        private const string EncodedWordFileName = "attachment; filename=\"=?UTF-8?Q?caf=C3=A9.txt?=\"";
        private const string EncodedWordOther = "attachment; other=\"=?UTF-8?Q?caf=C3=A9.txt?=\"";
        private const string EncodedWordText = "=?UTF-8?Q?caf=C3=A9.txt?=";

        private static readonly ImfOptions MalformedParameters = new ImfOptions { AllowMalformedParameters = true };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            "",
            "(a)",
            ";",
            "; filename=a",
            "a b",
            "a/b",
            "attachment;;",
            "attachment; filename",
            "attachment; filename=",
            "attachment; filename=a b",
            "attachment; filename=\"a",
            "attachment; filename=a; filename=b",
            "attachment; filename=\"caf\x00E9.txt\"",

            // ref. RFC 2231 §3 and §7
            "attachment; filename*1=a",
            "attachment; filename*=a",
            "attachment; filename*=UTF-8''%FF",
            "attachment; filename*=x-unknown''a",

            // ref. RFC 2047 §5
            EncodedWordFileName,
            EncodedWordOther,

            // ref. RFC 2183 §2
            NamedZoneDate,
            WrongDayDate,
            "attachment; modification-date=yesterday",
            "attachment; size=a",
            "attachment; size=\"\"",
            "attachment; size=-1",
            "attachment; size=99999999999999999999",
        };

        public static TheoryData<string, string> FileNames => new TheoryData<string, string>
        {
            { "attachment; filename=a.txt", "a.txt" },
            { "attachment; FILENAME=a.txt", "a.txt" },
            { "attachment (a); (b) filename (c) = (d) a.txt (e)", "a.txt" },
            { "attachment; filename=\"a b.txt\"", "a b.txt" },
            { "attachment; filename=\"a\r\n b.txt\"", "a b.txt" },
            { "attachment; filename=\"a\\\"b.txt\"", "a\"b.txt" },
            { "attachment; filename=\"=?x?=\"", "=?x?=" },

            // ref. RFC 2231 §4 and §4.1
            { "attachment; filename*=UTF-8''caf%C3%A9.txt", AccentedFileName },
            { "attachment; filename*=ISO-8859-1''caf%E9.txt", AccentedFileName },
            { "attachment; filename*0=\"a \"; filename*1=b.txt", "a b.txt" },
            { "attachment; filename*0*=UTF-8''caf%C3%A9;\r\n filename*1*=.t;\r\n filename*2=xt", AccentedFileName },

            // A character may span sections (RFC 2231 §4.1).
            { "attachment; filename*0*=UTF-8''caf%C3; filename*1*=%A9.txt", AccentedFileName },

            // ref. RFC 2231 §4: the extended form is used in preference to the plain one.
            { "attachment; filename=cafe.txt; filename*=UTF-8''caf%C3%A9.txt", AccentedFileName },
        };

        public static TheoryData<string> Dispositions => new TheoryData<string>
        {
            // ref. RFC 2183 §2: each is already in the form that it is written in.
            "inline",
            "attachment; filename=a.txt",
            "attachment; filename=\"a b.txt\"",
            "attachment; size=1",
            "x-other; a=b",
            "attachment; modification-date=\"" + ExampleDate + "\"",

            // Long or non-ASCII values take the forms of RFC 2231, as RFC 2183 §2 requires.
            "attachment; filename*=UTF-8''caf%C3%A9.txt",
        };

        [Fact]
        public void Parse_InlineExample_GivesAnInlineDisposition()
        {
            // act
            var testResult = ImfContentDisposition.Parse(InlineExample);

            // assert
            testResult.Type.Should().Be(InlineExample);
            testResult.IsInline.Should().BeTrue();
            testResult.IsAttachment.Should().BeFalse();
            testResult.Parameters.Should().BeEmpty();
            testResult.FileName.Should().BeNull();
            testResult.CreationDate.Should().BeNull();
            testResult.ModificationDate.Should().BeNull();
            testResult.ReadDate.Should().BeNull();
            testResult.Size.Should().BeNull();
        }

        [Fact]
        public void Parse_AttachmentExample_GivesItsFileNameAndDate()
        {
            // act
            var testResult = ImfContentDisposition.Parse(AttachmentExample);

            // assert
            testResult.IsAttachment.Should().BeTrue();
            testResult.FileName.Should().Be(GenomeFileName);
            testResult.ModificationDate.ToString().Should().Be(ExampleDate);
        }

        [Fact]
        public void Parse_Bytes_GivesTheDisposition()
        {
            // arrange
            var value = Encoding.ASCII.GetBytes(AttachmentExample);

            // act
            var testResult = ImfContentDisposition.Parse(value);

            // assert
            testResult.FileName.Should().Be(GenomeFileName);
        }

        [Fact]
        public void Parse_ExtensionType_IsTreatedAsAnAttachment()
        {
            // act
            var testResult = ImfContentDisposition.Parse("X-Other");

            // assert
            testResult.Type.Should().Be("X-Other");
            testResult.IsInline.Should().BeFalse();
            testResult.IsAttachment.Should().BeTrue();
        }

        [Fact]
        public void Parse_InlineInUpperCase_IsInline()
        {
            // act
            var testResult = ImfContentDisposition.Parse("INLINE");

            // assert
            testResult.IsInline.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(FileNames))]
        public void Parse_FileName_GivesTheDecodedName(string value, string expected)
        {
            // act
            var testResult = ImfContentDisposition.Parse(value);

            // assert
            testResult.FileName.Should().Be(expected);
        }

        [Fact]
        public void Parse_AllDatesAndSize_GivesThem()
        {
            // arrange
            const string value = "attachment; creation-date=\"1 Jan 2000 00:00:01 +0000\"; modification-date=\"2 Jan 2000 00:00:02 +0100\"; read-date=\"3 Jan 2000 00:00:03 -0000\"; size=1234";

            // act
            var testResult = ImfContentDisposition.Parse(value);

            // assert
            testResult.CreationDate.ToString().Should().Be("1 Jan 2000 00:00:01 +0000");
            testResult.ModificationDate.ToString().Should().Be("2 Jan 2000 00:00:02 +0100");
            testResult.ReadDate.ToString().Should().Be("3 Jan 2000 00:00:03 -0000");
            testResult.Size.Should().Be(1234);
        }

        [Fact]
        public void Parse_UnknownParameter_IsKept()
        {
            // act
            var testResult = ImfContentDisposition.Parse("attachment; a=b");

            // assert
            testResult.Parameters.Should().ContainSingle().Which.Should().Be(new MimeParameter("a", "b"));
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidValue_Throws(string value)
        {
            // act
            Action testAction = () => ImfContentDisposition.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_Null_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Parse((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Parse_NullOptions_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Parse(InlineExample, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            // act
            var testResult = ImfContentDisposition.TryParse((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_InvalidUtf8_ReturnsFalse()
        {
            // arrange
            var value = new byte[] { (byte)'a', 0xFF };

            // act
            var testResult = ImfContentDisposition.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_EncodedWordFileNameWithAllowMalformedParameters_DecodesIt()
        {
            // act
            var testResult = ImfContentDisposition.Parse(EncodedWordFileName, MalformedParameters);

            // assert
            testResult.FileName.Should().Be(AccentedFileName);
        }

        [Fact]
        public void Parse_EncodedWordInOtherParameterWithAllowMalformedParameters_LeavesItAsWritten()
        {
            // act
            var testResult = ImfContentDisposition.Parse(EncodedWordOther, MalformedParameters);

            // assert
            testResult.Parameters[0].Value.Should().Be(EncodedWordText);
        }

        [Fact]
        public void Parse_MissingSectionWithAllowMalformedParameters_JoinsWhatThereIs()
        {
            // act
            var testResult = ImfContentDisposition.Parse("attachment; filename*1=a", MalformedParameters);

            // assert
            testResult.FileName.Should().Be("a");
        }

        [Fact]
        public void Parse_InvalidDateWithAllowMalformedParameters_KeepsOnlyTheParameter()
        {
            // act
            var testResult = ImfContentDisposition.Parse(NamedZoneDate, MalformedParameters);

            // assert
            testResult.CreationDate.Should().BeNull();
            testResult.Parameters[0].Value.Should().Be("Wed, 12 Feb 1997 16:29:51 EST");
        }

        [Fact]
        public void Parse_InvalidSizeWithAllowMalformedParameters_KeepsOnlyTheParameter()
        {
            // act
            var testResult = ImfContentDisposition.Parse("attachment; size=a", MalformedParameters);

            // assert
            testResult.Size.Should().BeNull();
            testResult.Parameters[0].Value.Should().Be("a");
        }

        [Fact]
        public void Parse_WrongDayOfWeekWithAllowInvalidValues_TakesTheDayFromTheDate()
        {
            // act
            var testResult = ImfContentDisposition.Parse(WrongDayDate, new ImfOptions { AllowInvalidValues = true });

            // assert
            testResult.ReadDate.ToString().Should().Be(ExampleDate);
        }

        [Fact]
        public void Parse_EightBitBytesWithAllowEightBitHeaders_DecodesThemForTheParser()
        {
            // arrange
            var value = new byte[] { (byte)'a', (byte)' ', (byte)'(', 0xFF, (byte)')' };
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = ImfContentDisposition.Parse(value, options);

            // assert
            testResult.Type.Should().Be("a");
        }

        [Theory]
        [MemberData(nameof(Dispositions))]
        public void ToString_ParsedValue_WritesItWithASpaceWhereItCanBeFolded(string value)
        {
            // arrange
            var disposition = ImfContentDisposition.Parse(value);

            // act
            var testResult = disposition.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(Dispositions))]
        public void Parse_WrittenByAWriter_FormatsAsTheSameValue(string value)
        {
            // arrange
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Disposition", value);

            // act
            var testResult = ImfContentDisposition.Parse(written);

            // assert
            testResult.ToString().Should().Be(value);
        }

        [Fact]
        public void ToString_LongFileName_SplitsItIntoSections()
        {
            // arrange
            var disposition = ImfContentDisposition.Attachment.WithFileName(ExampleFileNameLetters80);

            // act
            var testResult = disposition.ToString();

            // assert
            testResult.Should().Be("attachment; filename*0=" + ExampleFileNameLetters65 + "; filename*1=" + ExampleFileNameLetters15);
        }

        [Fact]
        public void WriteField_LongFileName_FoldsEachSectionOntoALine()
        {
            // arrange
            var body = ImfContentDisposition.Attachment.WithFileName(ExampleFileNameLetters80).ToString();

            // act
            var testResult = MessageWrites.WriteField("Content-Disposition", body, ImfHeaderFieldKind.Structured);

            // assert
            testResult.Should().Be("Content-Disposition: attachment;\r\n filename*0=" + ExampleFileNameLetters65 + ";\r\n filename*1=" + ExampleFileNameLetters15 + "\r\n");
        }

        [Fact]
        public void Parse_LongNonAsciiFileNameWrittenByAWriter_GivesTheSameName()
        {
            // arrange
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Disposition", ImfContentDisposition.Attachment.WithFileName(LongAccentedFileName).ToString());

            // act
            var testResult = ImfContentDisposition.Parse(written);

            // assert
            testResult.FileName.Should().Be(LongAccentedFileName);
        }

        [Fact]
        public void WithFileName_NonAsciiName_WritesAnExtendedValue()
        {
            // act
            var testResult = ImfContentDisposition.Attachment.WithFileName(AccentedFileName);

            // assert
            testResult.FileName.Should().Be(AccentedFileName);
            testResult.ToString().Should().Be("attachment; filename*=UTF-8''caf%C3%A9.txt");
        }

        [Fact]
        public void WithMethods_AllParameters_WritesThemInOrder()
        {
            // arrange
            var date = ImfDateTime.Parse(ExampleDate);

            // act
            var testResult = ImfContentDisposition.Inline.WithFileName(GenomeFileName).WithCreationDate(date).WithModificationDate(date).WithReadDate(date).WithSize(1);

            // assert
            testResult.ToString().Should().Be(AllParameters);
            testResult.CreationDate.Should().Be(date);
            testResult.ModificationDate.Should().Be(date);
            testResult.ReadDate.Should().Be(date);
            testResult.Size.Should().Be(1);
        }

        [Fact]
        public void WithFileName_ExistingName_ReplacesIt()
        {
            // act
            var testResult = ImfContentDisposition.Attachment.WithFileName("a").WithFileName("b");

            // assert
            testResult.FileName.Should().Be("b");
            testResult.Parameters.Should().ContainSingle();
        }

        [Fact]
        public void WithFileName_MalformedDateKept_KeepsItAndAddsTheName()
        {
            // arrange
            var disposition = ImfContentDisposition.Parse(NamedZoneDate, MalformedParameters);

            // act
            var testResult = disposition.WithFileName("a");

            // assert
            testResult.FileName.Should().Be("a");
            testResult.CreationDate.Should().BeNull();
            testResult.Parameters.Should().HaveCount(2);
        }

        [Fact]
        public void WithCreationDate_Default_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Attachment.WithCreationDate(default);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithSize_Negative_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Attachment.WithSize(-1);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WithParameter_InvalidSize_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Attachment.WithParameter(new MimeParameter("size", "a"));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithParameter_Uninitialised_Throws()
        {
            // act
            Action testAction = () => ImfContentDisposition.Attachment.WithParameter(default);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_TypeAndParameters_GivesThem()
        {
            // arrange
            var parameters = new[] { new MimeParameter("filename", "a"), new MimeParameter("size", "1") };

            // act
            var testResult = new ImfContentDisposition("x-other", parameters);

            // assert
            testResult.Type.Should().Be("x-other");
            testResult.FileName.Should().Be("a");
            testResult.Size.Should().Be(1);
        }

        [Fact]
        public void Constructor_NullType_Throws()
        {
            // act
            Action testAction = () => new ImfContentDisposition(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullParameters_Throws()
        {
            // act
            Action testAction = () => new ImfContentDisposition(InlineExample, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("a b")]
        [InlineData("a/b")]
        [InlineData("a;b=c")]
        public void Constructor_InvalidType_Throws(string type)
        {
            // act
            Action testAction = () => new ImfContentDisposition(type);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("type");
        }

        [Fact]
        public void Constructor_InvalidDate_Throws()
        {
            // arrange
            var parameters = new[] { new MimeParameter("read-date", "yesterday") };

            // act
            Action testAction = () => new ImfContentDisposition(InlineExample, parameters);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("parameters");
        }

        [Fact]
        public void Equals_SameValueInAnotherForm_IsTrue()
        {
            // arrange
            var left = ImfContentDisposition.Parse("ATTACHMENT; FileName=a; size=1");
            var right = ImfContentDisposition.Parse("attachment; size=1; filename=\"a\"");

            // act
            var testResult = left.Equals(right);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("inline; filename=a")]
        [InlineData("attachment; filename=A")]
        [InlineData("attachment")]
        public void Equals_DifferentValue_IsFalse(string value)
        {
            // arrange
            var left = ImfContentDisposition.Parse("attachment; filename=a");
            var right = ImfContentDisposition.Parse(value);

            // act
            var testResult = left == right;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_SameValueInAnotherForm_IsTheSame()
        {
            // arrange
            var expected = ImfContentDisposition.Parse("attachment; size=1; filename=a").GetHashCode();
            var disposition = ImfContentDisposition.Parse("ATTACHMENT; FILENAME=a; SIZE=1");

            // act
            var testResult = disposition.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }
    }
}
