using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of what an entity gives for what was read: the typed fields, the content and the text.
    /// </summary>
    public class ImfEntityTests
    {
        private const string Header = MimeShapes.Header;

        // ref. RFC 2045 §6.8: "%PDF" in base64.
        private const string PdfMagic = "%PDF";

        // ref. RFC 2045 §6.7: "café" in UTF-8, in quoted-printable.
        private const string Cafe = "caf\x00E9";

        // Hiragana letter A in Shift_JIS, 0x82 0xA0, in base64.
        private const string HiraganaA = "\x3042";

        private const string ShiftJisInBase64 = Header + "Content-Type: text/plain; charset=shift_jis\r\nContent-Transfer-Encoding: base64\r\n\r\ngqA=\r\n";
        private const string Latin1EightBit = Header + "Content-Type: text/plain; charset=iso-8859-1\r\nContent-Transfer-Encoding: 8bit\r\n\r\ncaf\x00E9";
        private const string NoContentType = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n\r\nplain";
        private const string EightBitWithNoCharset = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n\r\ncaf\x00E9";
        private const string UnknownCharset = Header + "Content-Type: text/plain; charset=x-no-such-charset\r\n\r\nplain";
        private const string HtmlWithNoCharset = Header + "Content-Type: text/html\r\n\r\n<p>x</p>";
        private const string Base64Pdf = Header + "Content-Type: application/pdf\r\nContent-Transfer-Encoding: base64\r\n\r\nJVBERg==\r\n";

        // ref. RFC 2045 §6.8: characters outside the alphabet are ignored, so what makes this invalid is that it stops part of the way through a group of four.
        private const string InvalidBase64 = Header + "Content-Type: application/pdf\r\nContent-Transfer-Encoding: base64\r\n\r\nJVB\r\n";
        private const string InvalidQuotedPrintable = Header + "Content-Type: text/plain\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n=ZZ\r\n";
        private const string ExtensionEncoding = Header + "Content-Type: application/pdf\r\nContent-Transfer-Encoding: x-private\r\n\r\ndata\r\n";

        private const string AllFields =
            Header +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-Transfer-Encoding: 7bit\r\n" +
            "Content-Disposition: inline; filename=a.txt\r\n" +
            "Content-ID: <id@b.test>\r\n" +
            "Content-Description: =?UTF-8?Q?caf=C3=A9?=\r\n" +
            "Content-Language: en, fr\r\n" +
            "\r\n" +
            "body";

        private const string Digest =
            Header +
            "Content-Type: multipart/digest; boundary=d\r\n" +
            "\r\n" +
            "--d\r\n" +
            "\r\n" +
            "From: c@d.test\r\n" +
            "\r\n" +
            "digested\r\n" +
            "--d--\r\n";

        private const string InvalidDisposition = Header + "Content-Disposition: ;;;\r\n\r\nbody";

        private const string BothFileNames = Header + "Content-Type: application/pdf; name=name.pdf\r\nContent-Disposition: attachment; filename=plain.pdf; filename*=UTF-8''caf%C3%A9.pdf\r\n\r\nbody";
        private const string FileNameStar = Header + "Content-Type: application/pdf; name=name.pdf\r\nContent-Disposition: attachment; filename*=UTF-8''caf%C3%A9.pdf\r\n\r\nbody";
        private const string FileNamePlain = Header + "Content-Type: application/pdf; name=name.pdf\r\nContent-Disposition: attachment; filename=plain.pdf\r\n\r\nbody";
        private const string FileNameFromName = Header + "Content-Type: application/pdf; name=name.pdf\r\n\r\nbody";

        // ref. RFC 2047 §5: not allowed in a parameter, though mail programs write it.
        private const string EncodedName = "=?UTF-8?Q?caf=C3=A9?=";
        private const string EncodedWordName = Header + "Content-Type: application/pdf; name=\"" + EncodedName + "\"\r\n\r\nbody";
        private const string NoFileName = Header + "Content-Type: application/pdf\r\n\r\nbody";

        public static TheoryData<string, string> FileNames => new TheoryData<string, string>
        {
            { FileNameStar, "caf\x00E9.pdf" },
            { BothFileNames, "caf\x00E9.pdf" },
            { FileNamePlain, "plain.pdf" },
            { FileNameFromName, "name.pdf" },
        };

        public static TheoryData<string, string> Texts => new TheoryData<string, string>
        {
            { MimeShapes.TextOnly, Cafe + "\r\n" },
            { Latin1EightBit, Cafe },
            { NoContentType, "plain" },
            { HtmlWithNoCharset, "<p>x</p>" },
        };

        public static TheoryData<string> UndecodableContent => new TheoryData<string>
        {
            InvalidBase64,
            InvalidQuotedPrintable,
            ExtensionEncoding,
        };

        [Fact]
        public void ContentType_NoField_IsTheDefaultOfRfc2045()
        {
            // arrange
            var subject = EntityTrees.Read(NoContentType);

            // act
            var testResult = subject.Body.ContentType.ToString();

            // assert
            testResult.Should().Be("text/plain;charset=us-ascii");
        }

        [Fact]
        public void ContentType_PartOfDigestWithNoField_IsAMessage()
        {
            // arrange
            var subject = EntityTrees.Read(Digest);

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/digest", "1 1 part message/rfc822", "1 2 leaf text/plain");
        }

        [Fact]
        public void ContentTransferEncoding_NoField_Is7bit()
        {
            // arrange
            var subject = EntityTrees.Read(NoContentType);

            // act
            var testResult = subject.Body.ContentTransferEncoding;

            // assert
            testResult.Should().Be(MimeTransferEncoding.SevenBit);
        }

        [Fact]
        public void Accessors_AllFields_GiveTheirValues()
        {
            // arrange
            var subject = EntityTrees.Read(AllFields).Body;

            // act
            var testResult = new object[]
            {
                subject.ContentType.ToString(),
                subject.ContentTransferEncoding.Name,
                subject.ContentDisposition.ToString(),
                subject.ContentId.ToString(),
                subject.ContentDescription,
                subject.ContentLanguage.Count,
            };

            // assert
            testResult.Should().Equal("text/plain;charset=us-ascii", "7bit", "inline; filename=a.txt", "<id@b.test>", Cafe, 2);
        }

        [Fact]
        public void ContentDisposition_InvalidField_IsNull()
        {
            // arrange
            var subject = EntityTrees.Read(InvalidDisposition).Body;

            // act
            var testResult = subject.ContentDisposition;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Accessors_Set_ReplaceTheirFields()
        {
            // arrange
            var subject = new ImfEntity
            {
                ContentType = MimeMediaType.ApplicationOctetStream,
                ContentDisposition = ImfContentDisposition.Attachment,
                ContentId = ImfMessageId.Parse("<id@b.test>"),
                ContentDescription = "A file",
                ContentLanguage = new[] { Bcp47LanguageTag.Create("en") },
                ContentTransferEncoding = MimeTransferEncoding.Base64,
            };

            // act
            var testResult = EntityTrees.DescribeFields(subject);

            // assert
            testResult.Should().Equal(
                "Content-Type:application/octet-stream",
                "Content-Disposition:attachment",
                "Content-ID:<id@b.test>",
                "Content-Description:A file",
                "Content-Language:en",
                "Content-Transfer-Encoding:base64");
        }

        [Fact]
        public void Accessors_SetToNull_RemoveTheirFields()
        {
            // arrange
            var subject = EntityTrees.Read(AllFields).Body;
            subject.ContentDisposition = null;
            subject.ContentId = null;
            subject.ContentDescription = null;
            subject.ContentLanguage = null;
            subject.ContentTransferEncoding = default;

            // act
            var testResult = subject.Fields;

            // assert
            testResult.Should().HaveCount(4);
        }

        [Fact]
        public void ContentType_SetToDefault_Throws()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            Action testAction = () => subject.ContentType = default;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ContentType_SetToMultipartOnLeaf_Throws()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            Action testAction = () => subject.ContentType = MimeMediaType.MultipartMixed;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(FileNames))]
        public void FileName_EachSource_PrefersTheExtendedFormThenFilenameThenName(string message, string expected)
        {
            // arrange
            var subject = EntityTrees.Read(message).Body;

            // act
            var testResult = subject.FileName;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void FileName_NoParameter_IsNull()
        {
            // arrange
            var subject = EntityTrees.Read(NoFileName).Body;

            // act
            var testResult = subject.FileName;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ContentType_EncodedWordInParameter_IsWhatTheReaderFound()
        {
            // arrange
            var subject = EntityTrees.Read(EncodedWordName).Body;

            // act
            var testResult = subject.ContentType.Subtype;

            // assert
            testResult.Should().Be("pdf");
        }

        [Fact]
        public void FileName_EncodedWordInName_StaysAsItWasWritten()
        {
            // arrange
            var subject = EntityTrees.Read(EncodedWordName).Body;

            // act
            var testResult = subject.FileName;

            // assert
            testResult.Should().Be(EncodedName);
        }

        [Fact]
        public void FileName_EncodedWordInNameWithOption_IsDecoded()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedParameters = true };
            var subject = EntityTrees.Read(EncodedWordName, options).Body;

            // act
            var testResult = subject.FileName;

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetContent_Base64_IsDecoded()
        {
            // arrange
            var subject = EntityTrees.Read(Base64Pdf).Body;

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be(PdfMagic);
        }

        [Fact]
        public void GetContent_QuotedPrintable_IsDecoded()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly).Body;

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be("caf\x00C3\x00A9\r\n");
        }

        [Fact]
        public void GetContent_Identity_IsTheBytesAsTheyAre()
        {
            // arrange
            var subject = EntityTrees.Read(Latin1EightBit).Body;

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be(Cafe);
        }

        [Fact]
        public void GetContent_ReadWithDecodingOption_IsDecodedOnce()
        {
            // arrange
            var options = new ImfOptions { DecodeTransferEncoding = true };
            var subject = EntityTrees.Read(Base64Pdf, options).Body;

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be(PdfMagic);
        }

        [Theory]
        [MemberData(nameof(UndecodableContent))]
        public void GetContent_NotValidInItsEncoding_Throws(string message)
        {
            // arrange
            var subject = EntityTrees.Read(message).Body;

            // act
            Action testAction = () => subject.GetContent();

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void GetContent_Multipart_Throws()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextAndHtml).Body;

            // act
            Action testAction = () => subject.GetContent();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetContent_MessagePart_IsTheWholeMessage()
        {
            // arrange
            var subject = EntityTrees.At(EntityTrees.Read(Digest), "1");

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be("From: c@d.test\r\n\r\ndigested");
        }

        [Fact]
        public void OpenContentStream_Base64_ReadsTheDecodedBytes()
        {
            // arrange
            var subject = EntityTrees.Read(Base64Pdf).Body;

            // act
            var testResult = new StreamReader(subject.OpenContentStream()).ReadToEnd();

            // assert
            testResult.Should().Be(PdfMagic);
        }

        [Fact]
        public void OpenContentStream_AnyContent_IsReadOnly()
        {
            // arrange
            var subject = EntityTrees.Read(Latin1EightBit).Body;

            // act
            var testResult = subject.OpenContentStream();

            // assert
            testResult.CanWrite.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Texts))]
        public void GetText_KnownCharacterSet_IsDecoded(string message, string expected)
        {
            // arrange
            var subject = EntityTrees.Read(message).Body;

            // act
            var testResult = subject.GetText();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetText_CodePageThatNeedsAProvider_IsDecodedThroughTheResolver()
        {
            // arrange
            var options = new ImfOptions { CharsetResolver = new MimeCharsetResolver(new CodePageCharsetResolver()) };
            var subject = EntityTrees.Read(ShiftJisInBase64, options).Body;

            // act
            var testResult = subject.GetText();

            // assert
            testResult.Should().Be(HiraganaA);
        }

        [Fact]
        public void GetText_OctetsOutsideTheDefaultCharacterSet_Throws()
        {
            // arrange
            var subject = EntityTrees.Read(EightBitWithNoCharset, new ImfOptions { AllowInvalidBodyOctets = true }).Body;

            // act
            Action testAction = () => subject.GetText();

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void GetText_OctetsOutsideTheDefaultCharacterSetWithOption_ReplacesThem()
        {
            // arrange
            var options = new ImfOptions { AllowInvalidTextOctets = true, AllowInvalidBodyOctets = true };
            var subject = EntityTrees.Read(EightBitWithNoCharset, options).Body;

            // act
            var testResult = subject.GetText();

            // assert
            testResult.Should().Be("caf\xFFFD");
        }

        [Fact]
        public void GetText_UnknownCharacterSet_Throws()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedParameters = true };
            var subject = EntityTrees.Read(UnknownCharset, options).Body;

            // act
            Action testAction = () => subject.GetText();

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void GetText_NotText_Throws()
        {
            // arrange
            var subject = EntityTrees.Read(Base64Pdf).Body;

            // act
            Action testAction = () => subject.GetText();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void SetContent_AtMaxEntitySize_IsHeld()
        {
            // arrange
            var subject = new ImfEntity(new ImfOptions { MaxEntitySize = 4 });

            // act
            subject.SetContent(EntityTrees.Bytes(PdfMagic));
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be(PdfMagic);
        }

        [Fact]
        public void SetContent_PastMaxEntitySize_Throws()
        {
            // arrange
            var subject = new ImfEntity(new ImfOptions { MaxEntitySize = 3 });

            // act
            Action testAction = () => subject.SetContent(EntityTrees.Bytes(PdfMagic));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetContent_Multipart_Throws()
        {
            // arrange
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            Action testAction = () => subject.SetContent(EntityTrees.Bytes(PdfMagic));

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void SetContent_MessagePart_Throws()
        {
            // arrange
            var subject = new ImfMessagePart(new ImfMessage());

            // act
            Action testAction = () => subject.SetContent(EntityTrees.Bytes(PdfMagic));

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void SetText_AnyLineBreak_BecomesCrLf()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            subject.SetText("a\nb\rc\r\nd");
            var testResult = subject.GetText();

            // assert
            testResult.Should().Be("a\r\nb\r\nc\r\nd");
        }

        [Fact]
        public void SetText_CharacterSet_IsWrittenInTheContentType()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            subject.SetText(Cafe, MimeCharset.Iso88591);
            var testResult = subject.ContentType.ToString();

            // assert
            testResult.Should().Be("text/plain;charset=ISO-8859-1");
        }

        [Fact]
        public void SetText_CharacterSetCantHoldTheText_Throws()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            Action testAction = () => subject.SetText(Cafe, MimeCharset.UsAscii);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetText_UnresolvedCharacterSet_Throws()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            Action testAction = () => subject.SetText(Cafe, MimeCharset.Parse("x-no-such-charset"));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetText_NotText_Throws()
        {
            // arrange
            var subject = new ImfEntity { ContentType = MimeMediaType.ApplicationOctetStream };

            // act
            Action testAction = () => subject.SetText(Cafe);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }
    }
}
