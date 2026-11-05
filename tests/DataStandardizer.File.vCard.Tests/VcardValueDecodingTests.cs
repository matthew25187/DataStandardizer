using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of decoding a line's value by its <c>ENCODING</c> and <c>CHARSET</c> parameters.
    /// </summary>
    public class VcardValueDecodingTests
    {
        private const string BeginCard = "BEGIN:VCARD";
        private const string Version21 = "VERSION:2.1";
        private const string Version30 = "VERSION:3.0";
        private const string Version40 = "VERSION:4.0";

        // "cafe" with an acute accent: E9 in ISO-8859-1, and C3 A9 in UTF-8.
        private const string Cafe = "caf\x00E9";
        private const string Latin1QuotedPrintable = "caf=E9";
        private const string Utf8QuotedPrintable = "caf=C3=A9";

        // A name no resolver knows.
        private const string UnknownCharsetName = "X-UNKNOWN-CHARSET";

        private static readonly VcardOptions AcceptLegacy = new VcardOptions { AcceptLegacySyntax = true };

        // "NOTE;ENCODING=8BIT;CHARSET=ISO-8859-1:caf", E9 and a line break.
        private static readonly byte[] RawLatin1Line = Concat(Encoding.ASCII.GetBytes("NOTE;ENCODING=8BIT;CHARSET=ISO-8859-1:caf"), new byte[] { 0xE9, 0x0D, 0x0A });

        // "NOTE;ENCODING=8BIT;CHARSET=UTF-8:caf", C3 A9 and a line break.
        private static readonly byte[] RawUtf8Line = Concat(Encoding.ASCII.GetBytes("NOTE;ENCODING=8BIT;CHARSET=UTF-8:caf"), new byte[] { 0xC3, 0xA9, 0x0D, 0x0A });

        // "NOTE;ENCODING=8BIT;CHARSET=ISO-8859-1:", C3 A9 and a line break.
        private static readonly byte[] RawUtf8LineNamedLatin1 = Concat(Encoding.ASCII.GetBytes("NOTE;ENCODING=8BIT;CHARSET=ISO-8859-1:"), new byte[] { 0xC3, 0xA9, 0x0D, 0x0A });

        private static VcardContentLine ReadVersion21Line(string line, VcardOptions options = null)
        {
            using (var reader = CreateReader(Crlf(BeginCard, Version21, line), options))
            {
                return ReadLast(reader);
            }
        }

        private static VcardContentLine ReadStreamLine(byte[] line, VcardOptions options)
        {
            using (var reader = CreateStreamReader(Concat(Encoding.ASCII.GetBytes(Crlf(BeginCard, Version21)), line), options))
            {
                return ReadLast(reader);
            }
        }

        [Theory]
        [InlineData("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=ISO-8859-1:" + Latin1QuotedPrintable)]
        [InlineData("NOTE;CHARSET=latin1;QUOTED-PRINTABLE:" + Latin1QuotedPrintable)]
        [InlineData("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:" + Utf8QuotedPrintable)]
        public void GetDecodedText_QuotedPrintableInCharset_GivesTheText(string line)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_ViewOfQuotedPrintableInCharset_GivesTheText()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "NOTE;QUOTED-PRINTABLE;CHARSET=UTF-8:" + Utf8QuotedPrintable)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetDecodedText();

                // assert
                testResult.Should().Be(Cafe);
            }
        }

        [Fact]
        public void GetDecodedText_InvalidUtf8_Throws()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-8:" + Latin1QuotedPrintable);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid in the character set UTF-8.");
        }

        [Fact]
        public void GetDecodedText_QuotedPrintableWithNoCharsetInVersion21_IsUsAscii()
        {
            // arrange
            // ref. vCard 2.1 §2.1.6: "The default character set is ASCII."
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE:" + Latin1QuotedPrintable);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid in the character set US-ASCII.");
        }

        [Fact]
        public void GetDecodedText_QuotedPrintableWithNoCharsetInLaterVersionWithAcceptLegacySyntax_IsUtf8()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version30, "NOTE;QUOTED-PRINTABLE:" + Utf8QuotedPrintable), AcceptLegacy))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.GetDecodedText();

                // assert
                testResult.Should().Be(Cafe);
            }
        }

        [Fact]
        public void GetDecodedText_UnknownCharset_Throws()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=" + UnknownCharsetName + ":" + Latin1QuotedPrintable);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The character set \"X-UNKNOWN-CHARSET\" is not known.");
        }

        [Fact]
        public void GetDecodedText_CharsetWithInvalidName_Throws()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=\"a b\":" + Latin1QuotedPrintable);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The character set \"a b\" is not known.");
        }

        [Fact]
        public void GetDecodedText_UnknownCharsetWithFallback_DecodesItAsLatin1()
        {
            // arrange
            var options = new VcardOptions { AllowUnknownCharsetAsLatin1 = true };
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=" + UnknownCharsetName + ":" + Latin1QuotedPrintable, options);

            // act
            var testResult = subject.GetDecodedText(out var testUsedFallback);

            // assert
            testResult.Should().Be(Cafe);
            testUsedFallback.Should().BeTrue();
        }

        [Fact]
        public void GetDecodedText_KnownCharsetWithFallback_SaysItDidNotUseIt()
        {
            // arrange
            var options = new VcardOptions { AllowUnknownCharsetAsLatin1 = true };
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-8:" + Utf8QuotedPrintable, options);

            // act
            var testResult = subject.GetDecodedText(out var testUsedFallback);

            // assert
            testResult.Should().Be(Cafe);
            testUsedFallback.Should().BeFalse();
        }

        [Fact]
        public void GetDecodedText_ViewOfUnknownCharsetWithFallback_SaysItUsedIt()
        {
            // arrange
            var options = new VcardOptions { AcceptLegacySyntax = true, AllowUnknownCharsetAsLatin1 = true };
            using (var reader = CreateReader(Crlf("NOTE;QUOTED-PRINTABLE;CHARSET=" + UnknownCharsetName + ":" + Latin1QuotedPrintable), options))
            {
                reader.ReadContentLine();

                // act
                var testResult = reader.CurrentLine.GetDecodedText(out var testUsedFallback);

                // assert
                testResult.Should().Be(Cafe);
                testUsedFallback.Should().BeTrue();
            }
        }

        [Fact]
        public void GetDecodedText_CharsetFromCallersResolver_UsesIt()
        {
            // arrange
            var options = new VcardOptions { CharsetResolver = new SingleCharsetResolver(UnknownCharsetName, Latin1) };
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=" + UnknownCharsetName + ":" + Latin1QuotedPrintable, options);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_BuiltInCharsetWithCallersResolver_IsNotAskedOfIt()
        {
            // arrange
            var options = new VcardOptions { CharsetResolver = new SingleCharsetResolver("UTF-8", Latin1) };
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-8:" + Utf8QuotedPrintable, options);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_MultiByteCharsetFromCallersResolver_GivesTheText()
        {
            // arrange
            // E9 00 is U+00E9 in UTF-16 LE.
            var options = new VcardOptions { CharsetResolver = new SingleCharsetResolver(UnknownCharsetName, Encoding.Unicode) };
            var subject = ReadVersion21Line("NOTE;QUOTED-PRINTABLE;CHARSET=" + UnknownCharsetName + ":=E9=00", options);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be("\x00E9");
        }

        [Theory]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=US-ASCII:caf=65", "cafe")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=ISO-8859-1:=E9", "\x00E9")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-8:=C3=A9", "\x00E9")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=E9=00", "\x00E9")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16BE:=00=E9", "\x00E9")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32LE:=E9=00=00=00", "\x00E9")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32BE:=00=00=00=E9", "\x00E9")]
        public void GetDecodedText_BuiltInCharset_GivesTheText(string line, string expected)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        // Lone surrogates (RFC 2781 §2.2), in both byte orders.
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=00=D8", "UTF-16LE")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=00=DC", "UTF-16LE")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16BE:=D8=00", "UTF-16BE")]
        // Half of a code unit.
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=41", "UTF-16LE")]
        // A value above U+10FFFF, and a surrogate.
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32LE:=00=00=11=00", "UTF-32LE")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32BE:=00=00=D8=00", "UTF-32BE")]
        // Part of a code unit.
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32LE:=41=00", "UTF-32LE")]
        public void GetDecodedText_MalformedUnicode_Throws(string line, string charset)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Message.Should().Contain(charset);
        }

        [Theory]
        // The replacement character itself, U+FFFD, which valid data can hold, alone and with a surrogate pair for U+1F600.
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=FD=FF", "\xFFFD")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16BE:=FF=FD", "\xFFFD")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16LE:=FD=FF=3D=D8=00=DE", "\xFFFD\xD83D\xDE00")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-16:=FF=FE=FD=FF", "\xFFFD")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32LE:=FD=FF=00=00", "\xFFFD")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=UTF-32BE:=00=00=FF=FD", "\xFFFD")]
        public void GetDecodedText_ReplacementCharacterInUnicode_GivesIt(string line, string expected)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(expected);
        }

#if !NETFRAMEWORK
        [Theory]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=windows-1252:=80", "The character set \"windows-1252\" is not known.")]
        [InlineData("NOTE;QUOTED-PRINTABLE;CHARSET=Shift_JIS:=93=FA", "The character set \"Shift_JIS\" is not known.")]
        public void GetDecodedText_CodePageWithoutItsProvider_Throws(string line, string expectedMessage)
        {
            // arrange
            // .NET has these only once CodePagesEncodingProvider is registered, which no unit test does.
            var subject = ReadVersion21Line(line);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage(expectedMessage);
        }
#endif

        [Theory]
        [InlineData("NOTE:" + Cafe)]
        [InlineData("NOTE;ENCODING=7BIT:" + Cafe)]
        [InlineData("NOTE;CHARSET=UTF-8:" + Cafe)]
        public void GetDecodedText_EightBitValueWithoutEncodingInVersion21_Throws(string line)
        {
            // arrange
            // ref. vCard 2.1 §2.1.5: "The default encoding for the vCard object is 7-Bit."
            var subject = ReadVersion21Line(line);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The value holds a character outside US-ASCII, but has no ENCODING=8BIT.*");
        }

        [Fact]
        public void GetDecodedText_EightBitValueWithoutEncodingWithOption_DecodesItByItsCharset()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;CHARSET=UTF-8:" + Cafe, new VcardOptions { AcceptEightBitWithoutEncoding = true });

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_EightBitValueInItsCharset_GivesTheText()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;ENCODING=8BIT;CHARSET=UTF-8:" + Cafe);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_EightBitValueWithNoCharset_IsNotValidUsAscii()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE;ENCODING=8BIT:" + Cafe);

            // act
            Action testAction = () => subject.GetDecodedText();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid in the character set US-ASCII.");
        }

        [Fact]
        public void GetDecodedText_AsciiValueWithNoParameters_GivesIt()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE:plain");

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be("plain");
        }

        [Fact]
        public void ReadLast_RawLatin1ValueInUtf8Stream_Throws()
        {
            // arrange
            // The default encoding of a stream is strict UTF-8, which can't read E9 here.
            var options = new VcardOptions();

            // act
            Action testAction = () => ReadStreamLine(RawLatin1Line, options);

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The data could not be decoded.*");
        }

        [Fact]
        public void GetDecodedText_RawLatin1ValueReadByteTransparently_GivesTheText()
        {
            // arrange
            var subject = ReadStreamLine(RawLatin1Line, new VcardOptions { Encoding = Latin1 });

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_RawUtf8ValueReadByteTransparently_RedecodesIt()
        {
            // arrange
            var subject = ReadStreamLine(RawUtf8Line, new VcardOptions { Encoding = Latin1 });

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetDecodedText_ValueInCharsetWhoseOctetsAreValidUtf8_KeepsTheOctets()
        {
            // arrange
            // C3 A9 in a UTF-8 stream is one character, but in ISO-8859-1 it is two.
            var subject = ReadStreamLine(RawUtf8LineNamedLatin1, new VcardOptions());

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be("\x00C3\x00A9");
        }

        [Fact]
        public void GetDecodedValue_ValueWithNoEncoding_IsTheOctetsOfTheSource()
        {
            // arrange
            var subject = ReadVersion21Line("NOTE:" + Cafe);

            // act
            var testResult = subject.GetDecodedValue();

            // assert
            testResult.Should().Equal(0x63, 0x61, 0x66, 0xC3, 0xA9);
        }

        [Theory]
        [InlineData("NOTE;QUOTED-PRINTABLE:a=3d")]
        [InlineData("NOTE;QUOTED-PRINTABLE:a=G0")]
        [InlineData("NOTE;QUOTED-PRINTABLE:" + Cafe)]
        public void GetDecodedValue_InvalidQuotedPrintable_Throws(string line)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            Action testAction = () => subject.GetDecodedValue();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid quoted-printable.");
        }

        [Fact]
        public void GetDecodedValue_InvalidBase64_Throws()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, "KEY;BASE64:SGVsbG8", string.Empty)))
            {
                var subject = ReadLast(reader);

                // act
                Action testAction = () => subject.GetDecodedValue();

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid base64.");
            }
        }

        [Theory]
        [InlineData("NOTE;ENCODING=X-OTHER:a", "The encoding \"X-OTHER\" is not known.")]
        [InlineData("NOTE;ENCODING=binary:a", "The encoding \"binary\" is not known.")]
        [InlineData("NOTE;ENCODING=\"a b\":a", "The encoding \"a b\" is not known.")]
        public void GetDecodedValue_UnknownEncoding_Throws(string line, string expectedMessage)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            Action testAction = () => subject.GetDecodedValue();

            // assert
            testAction.Should().Throw<VcardFileException>().WithMessage(expectedMessage);
        }

        [Fact]
        public void GetDecodedValue_ErrorOnLineRead_ReportsWhere()
        {
            // arrange
            var subject = ReadVersion21Line("a.NOTE;ENCODING=X-OTHER:a");

            // act
            Action testAction = () => subject.GetDecodedValue();

            // assert
            var testException = testAction.Should().Throw<VcardFileException>().Which;
            testException.Data[VcardFileException.DataItemName.LineNumber].Should().Be(3L);
            testException.Data[VcardFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
            testException.Data[VcardFileException.DataItemName.PropertyName].Should().Be("NOTE");
            testException.Data[VcardFileException.DataItemName.Group].Should().Be("a");
            testException.Data[VcardFileException.DataItemName.CardIndex].Should().Be(0);
        }

        [Theory]
        [InlineData(Version30)]
        [InlineData(Version21)]
        public void GetDecodedValue_Version30Base64_DecodesIt(string version)
        {
            // arrange
            // ref. RFC 2426 §4: ENCODING=b.  "SGVsbG8=" is "Hello" (RFC 4648 §4).
            using (var reader = CreateReader(Crlf(BeginCard, version, "KEY;ENCODING=b:SGVsbG8=")))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.GetDecodedValue();

                // assert
                testResult.Should().Equal(0x48, 0x65, 0x6C, 0x6C, 0x6F);
            }
        }

        [Fact]
        public void GetDecodedValue_Version30Base64WithWhitespace_Throws()
        {
            // arrange
            // ref. RFC 2047 §4.1: the "B" encoding has no line breaks or white space.
            using (var reader = CreateReader(Crlf(BeginCard, Version30, "KEY;ENCODING=b:SGVs bG8=")))
            {
                var subject = ReadLast(reader);

                // act
                Action testAction = () => subject.GetDecodedValue();

                // assert
                testAction.Should().Throw<VcardFileException>().WithMessage("The value is not valid base64.");
            }
        }

        [Theory]
        [InlineData("KEY;ENCODING=b:SGVsbG8=", "SGVsbG8=")]
        [InlineData("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=X-UNKNOWN-CHARSET:a=3D", "a=3D")]
        public void GetDecodedText_ParametersRfc6350DoesNotDefine_HaveNoMeaningInVersion40Card(string line, string expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, line)))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.GetDecodedText();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void GetDecodedText_NonAsciiValueInLaterVersion_GivesItAsItIs()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "NOTE:" + Cafe)))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.GetDecodedText();

                // assert
                testResult.Should().Be(Cafe);
            }
        }

        [Fact]
        public void GetDecodedText_LineNotReadFromSource_AppliesItsParameters()
        {
            // arrange
            var subject = new VcardContentLine("NOTE", new[]
            {
                new VcardContentLineParameter("ENCODING", "QUOTED-PRINTABLE"),
                new VcardContentLineParameter("CHARSET", "ISO-8859-1")
            }, Latin1QuotedPrintable);

            // act
            var testResult = subject.GetDecodedText();

            // assert
            testResult.Should().Be(Cafe);
        }

        [Theory]
        [InlineData("NOTE;QUOTED-PRINTABLE:a", "quoted-printable", true)]
        [InlineData("KEY;ENCODING=BASE64:", "base64", true)]
        [InlineData("KEY;ENCODING=b:a", "base64", true)]
        [InlineData("NOTE;8BIT:a", "8bit", true)]
        [InlineData("NOTE;ENCODING=X-OTHER:a", "X-OTHER", true)]
        [InlineData("NOTE;ENCODING=\"a b\":a", "", false)]
        [InlineData("NOTE:a", "", false)]
        public void TryGetTransferEncoding_LineInVersion21Card_GivesTheEncoding(string line, string expectedName, bool expected)
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version21, line, string.Empty)))
            {
                reader.ReadLine();
                reader.ReadLine();
                var subject = reader.ReadLine();

                // act
                var testResult = subject.TryGetTransferEncoding(out var testEncoding);

                // assert
                testResult.Should().Be(expected);
                testEncoding.ToString().Should().Be(expectedName);
            }
        }

        [Fact]
        public void TryGetTransferEncoding_QuotedPrintableInVersion40Card_HasNone()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "NOTE;ENCODING=QUOTED-PRINTABLE:a")))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.TryGetTransferEncoding(out _);

                // assert
                testResult.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData("NOTE;CHARSET=latin1:a", "latin1", true)]
        [InlineData("NOTE;CHARSET=\"a b\":a", "", false)]
        [InlineData("NOTE:a", "", false)]
        public void TryGetCharset_LineInVersion21Card_GivesTheCharset(string line, string expectedName, bool expected)
        {
            // arrange
            var subject = ReadVersion21Line(line);

            // act
            var testResult = subject.TryGetCharset(out var testCharset);

            // assert
            testResult.Should().Be(expected);
            testCharset.ToString().Should().Be(expectedName);
        }

        [Fact]
        public void TryGetCharset_CharsetInVersion40Card_HasNone()
        {
            // arrange
            using (var reader = CreateReader(Crlf(BeginCard, Version40, "NOTE;CHARSET=UTF-8:a")))
            {
                var subject = ReadLast(reader);

                // act
                var testResult = subject.TryGetCharset(out _);

                // assert
                testResult.Should().BeFalse();
            }
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Array.Copy(first, result, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);
            return result;
        }

        /// <summary>
        /// Resolves one name to one encoding, and no other.
        /// </summary>
        private sealed class SingleCharsetResolver : IMimeCharsetResolver
        {
            private readonly Encoding _encoding;
            private readonly string _name;

            internal SingleCharsetResolver(string name, Encoding encoding)
            {
                _name = name;
                _encoding = encoding;
            }

            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                encoding = charset == MimeCharset.Parse(_name) ? _encoding : null;
                return encoding != null;
            }
        }
    }
}
