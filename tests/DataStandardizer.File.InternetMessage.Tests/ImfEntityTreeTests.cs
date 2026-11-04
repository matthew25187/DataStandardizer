using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the tree of a message: how it is read, how it is found one's way around, and that writing it changes only what has changed.
    /// </summary>
    public class ImfEntityTreeTests
    {
        private const string Header = MimeShapes.Header;

        private const string NoMimeVersion = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nContent-Type: text/plain\r\n\r\nbody";

        private const string NoCloseDelimiter =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "\r\n" +
            "old";

        private const string NoCloseDelimiterRewritten =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "new\r\n" +
            "--b--";

        private const string TwoParts =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "\r\n" +
            "one\r\n" +
            "--b\r\n" +
            "\r\n" +
            "two\r\n" +
            "--b--\r\n";

        private const string ThreeParts =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "\r\n" +
            "one\r\n" +
            "--b\r\n" +
            "\r\n" +
            "two\r\n" +
            "--b\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "three\r\n" +
            "--b--\r\n";

        private const string OnePart =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "\r\n" +
            "two\r\n" +
            "--b--\r\n";

        private const string TwoPartsWithPreamble =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "Hello\r\n" +
            "--b\r\n" +
            "\r\n" +
            "one\r\n" +
            "--b\r\n" +
            "\r\n" +
            "two\r\n" +
            "--b--\r\n";

        private const string SecondPartWithDescription =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "\r\n" +
            "one\r\n" +
            "--b\r\n" +
            "Content-Description: Second\r\n" +
            "\r\n" +
            "two\r\n" +
            "--b--\r\n";

        private const string ForwardedWithNewSubject =
            Header +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "See below.\r\n" +
            "--mix\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "Content-Disposition: attachment\r\n" +
            "\r\n" +
            "Date: Thu, 20 Nov 1997 09:55:06 -0600\r\nFrom: c@d.test\r\nSubject: Changed\r\nMIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=inner\r\n" +
            "\r\n" +
            "--inner\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-ID: <inner@d.test>\r\n" +
            "\r\n" +
            "original text\r\n" +
            "--inner\r\n" +
            "Content-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=inner.pdf\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "JVBERg==\r\n" +
            "--inner--\r\n" +
            "\r\n" +
            "--mix--\r\n";

        private const string ForwardedWithNewInnerText =
            Header +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "See below.\r\n" +
            "--mix\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "Content-Disposition: attachment\r\n" +
            "\r\n" +
            MimeShapes.EmbeddedHeader +
            "Content-Type: multipart/mixed; boundary=inner\r\n" +
            "\r\n" +
            "--inner\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-ID: <inner@d.test>\r\n" +
            "\r\n" +
            "changed\r\n" +
            "--inner\r\n" +
            "Content-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=inner.pdf\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "JVBERg==\r\n" +
            "--inner--\r\n" +
            "\r\n" +
            "--mix--\r\n";

        private const string TwoPartsWithNewBoundary =
            Header +
            "Content-Type: multipart/mixed; boundary=c\r\n" +
            "\r\n" +
            "--c\r\n" +
            "\r\n" +
            "one\r\n" +
            "--c\r\n" +
            "\r\n" +
            "two\r\n" +
            "--c--\r\n";

        private const string TwoPartsWithFirstEncoded = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: text/plain; charset=us-ascii\r\nContent-Transfer-Encoding: base64\r\n\r\nY2Fmw6k=\r\n--b\r\n\r\ntwo\r\n--b--\r\n";
        private const string TextOnlyWithNewBody = Header + "Content-Type: text/plain; charset=utf-8\r\n\r\nnew";
        private const string TextOnlyInBase64 = Header + "Content-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: base64\r\n\r\nY2Fmw6kNCg==";
        private const string TwoPartsHeaderWithNewRawBody = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\nnew";
        private const string HeaderWithNewBodyEntity = Header + "Content-Type: application/octet-stream\r\n\r\nx";

        private const string AttachedAlternative =
            Header +
            "Content-Type: multipart/mixed; boundary=b\r\n" +
            "\r\n" +
            "--b\r\n" +
            "Content-Type: multipart/alternative; boundary=c\r\n" +
            "Content-Disposition: attachment; filename=both.txt\r\n" +
            "\r\n" +
            "--c\r\n" +
            "\r\n" +
            "attached\r\n" +
            "--c\r\n" +
            "Content-Type: application/pdf; name=inner.pdf\r\n" +
            "\r\n" +
            "x\r\n" +
            "--c--\r\n" +
            "--b\r\n" +
            "\r\n" +
            "body\r\n" +
            "--b--\r\n";

        private const string EmptyEmbeddedMessage = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: message/rfc822\r\n\r\n--b--\r\n";
        private const string EmbeddedMessageGivenASubject = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: message/rfc822\r\n\r\nSubject: New\r\n\r\n--b--\r\n";

        // ref. RFC 5322 §3.5: a message need have no body, and here no empty line either.
        private const string NoHeaderEnd = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n";
        private const string NoHeaderEndGivenABody = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=us-ascii\r\n\r\nnew";

        private const string BareLineFeedHeaderEnd = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: text/plain\r\n\nold";
        private const string BareLineFeedHeaderEndGivenABody = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: text/plain\r\n\nnew";

        private const int DeeperThanTheStackCanFollow = 100000;

        private const string NewBody = "new";

        public static TheoryData<string, string[]> Trees => new TheoryData<string, string[]>
        {
            { MimeShapes.TextOnly, new[] { "/ 0 leaf text/plain" } },
            { MimeShapes.TextAndHtml, new[] { "/ 0 multipart multipart/alternative", "1 1 leaf text/plain", "2 1 leaf text/html" } },
            { MimeShapes.HtmlWithInlineImage, new[] { "/ 0 multipart multipart/related", "1 1 leaf text/html", "2 1 leaf image/png" } },
            {
                MimeShapes.MixedWithAttachments, new[]
                {
                    "/ 0 multipart multipart/mixed", "1 1 multipart multipart/alternative", "1.1 2 leaf text/plain", "1.2 2 leaf text/html", "2 1 leaf application/pdf", "3 1 leaf image/png", "4 1 leaf text/plain",
                }
            },
            {
                MimeShapes.Forwarded, new[]
                {
                    "/ 0 multipart multipart/mixed", "1 1 leaf text/plain", "2 1 part message/rfc822", "2 2 multipart multipart/mixed", "2.1 3 leaf text/plain", "2.2 3 leaf application/pdf",
                }
            },
        };

        public static TheoryData<string> AllShapes => new TheoryData<string>
        {
            MimeShapes.TextOnly,
            MimeShapes.TextAndHtml,
            MimeShapes.HtmlWithInlineImage,
            MimeShapes.HtmlRootNamedByStart,
            MimeShapes.MixedWithAttachments,
            MimeShapes.Forwarded,
            MimeShapes.OddFraming,
            Header + "Content-Type: message/rfc822\r\n\r\nFrom: c@d.test\r\n\r\nx",
        };

        public static TheoryData<string> SignedShapes => new TheoryData<string>
        {
            SignedMessages.MultipartSigned,
            SignedMessages.MultipartSignedWithPadding,
        };

        public static TheoryData<string, string> TextBodies => new TheoryData<string, string>
        {
            { MimeShapes.TextOnly, "caf\x00E9\r\n" },
            { MimeShapes.TextAndHtml, "plain" },
            { MimeShapes.MixedWithAttachments, "plain" },
            { MimeShapes.Forwarded, "See below." },
        };

        public static TheoryData<string, string> HtmlBodies => new TheoryData<string, string>
        {
            { MimeShapes.TextAndHtml, "<p>html</p>" },
            { MimeShapes.HtmlWithInlineImage, "<img src=\"cid:img@b.test\">" },
            { MimeShapes.HtmlRootNamedByStart, "<p>root</p>" },
            { MimeShapes.MixedWithAttachments, "<p>html</p>" },
        };

        public static TheoryData<string> NoTextBody => new TheoryData<string>
        {
            MimeShapes.HtmlWithInlineImage,
            MimeShapes.HtmlRootNamedByStart,
        };

        public static TheoryData<string> NoHtmlBody => new TheoryData<string>
        {
            MimeShapes.TextOnly,
            MimeShapes.Forwarded,
        };

        public static TheoryData<string, string[]> AttachmentPaths => new TheoryData<string, string[]>
        {
            { MimeShapes.TextOnly, new string[0] },
            { MimeShapes.MixedWithAttachments, new[] { "application/pdf", "image/png", "text/plain" } },
            { MimeShapes.Forwarded, new[] { "message/rfc822" } },
        };

        [Theory]
        [MemberData(nameof(Trees))]
        public void EnumerateEntities_EachShape_GivesTheTreeDepthFirst(string message, string[] expected)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void EnumerateEntities_MessageBodyIsAMessage_NumbersItLikeTheReader()
        {
            // arrange
            const string Nested = Header + "Content-Type: message/rfc822\r\n\r\nContent-Type: message/rfc822\r\n\r\nFrom: c@d.test\r\n\r\nx";
            var subject = EntityTrees.Read(Nested);

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal("/ 0 part message/rfc822", "1 1 part message/rfc822", "1.1 2 leaf text/plain");
        }

        [Fact]
        public void EnumerateEntities_PartWithEmbeddedMessageAndNothingInIt_GivesAnEmptyMessage()
        {
            // arrange
            const string Empty = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: message/rfc822\r\n\r\n--b--\r\n";
            var subject = EntityTrees.Read(Empty);

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/mixed", "1 1 part message/rfc822", "1 2 leaf text/plain");
        }

        [Theory]
        [MemberData(nameof(TextBodies))]
        public void TextBody_EachShape_IsThePreferredPlainText(string message, string expected)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = subject.TextBody.GetText();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(NoTextBody))]
        public void TextBody_NoPlainTextInTheMessage_IsNull(string message)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = subject.TextBody;

            // assert
            testResult.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(HtmlBodies))]
        public void HtmlBody_EachShape_IsThePreferredHtml(string message, string expected)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = subject.HtmlBody.GetText();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(NoHtmlBody))]
        public void HtmlBody_NoHtmlInTheMessage_IsNull(string message)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = subject.HtmlBody;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void TextBody_SeveralAlternatives_TakesTheLast()
        {
            // arrange (ref. RFC 2046 §5.1.4: the best is last)
            const string Alternatives = Header + "Content-Type: multipart/alternative; boundary=b\r\n\r\n--b\r\n\r\nfirst\r\n--b\r\n\r\nlast\r\n--b--\r\n";
            var subject = EntityTrees.Read(Alternatives);

            // act
            var testResult = subject.TextBody.GetText();

            // assert
            testResult.Should().Be("last");
        }

        [Fact]
        public void TextBody_FirstPartIsAnAttachment_SkipsIt()
        {
            // arrange
            const string AttachedFirst = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Disposition: attachment\r\n\r\nfile\r\n--b\r\n\r\nbody\r\n--b--\r\n";
            var subject = EntityTrees.Read(AttachedFirst);

            // act
            var testResult = subject.TextBody.GetText();

            // assert
            testResult.Should().Be("body");
        }

        [Theory]
        [MemberData(nameof(AttachmentPaths))]
        public void Attachments_EachShape_ListsDispositionsAndNamedNonTextLeaves(string message, string[] expected)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = EntityTrees.TypesOf(subject.Attachments);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void FindByContentId_Identifier_FindsTheEntity()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.HtmlWithInlineImage);

            // act
            var testResult = subject.FindByContentId(ImfMessageId.Parse("<img@b.test>")).ContentType.Type;

            // assert
            testResult.Should().Be("image");
        }

        [Fact]
        public void FindByContentId_CidUri_FindsTheEntity()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.HtmlWithInlineImage);

            // act
            var testResult = subject.FindByContentId("cid:img@b.test").ContentType.Type;

            // assert
            testResult.Should().Be("image");
        }

        [Fact]
        public void FindByContentId_NotInTheMessage_IsNull()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.HtmlWithInlineImage);

            // act
            var testResult = subject.FindByContentId("cid:other@b.test");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void FindByContentId_InsideAnEmbeddedMessage_IsNull()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.Forwarded);

            // act
            var testResult = subject.FindByContentId("cid:inner@d.test");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void FindByContentId_NotACidUri_Throws()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.HtmlWithInlineImage);

            // act
            Action testAction = () => subject.FindByContentId("http://b.test/");

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Message_EmbeddedMessage_HasItsOwnHeader()
        {
            // arrange
            var subject = (ImfMessagePart)EntityTrees.At(EntityTrees.Read(MimeShapes.Forwarded), "2");

            // act
            var testResult = subject.Message.Subject;

            // assert
            testResult.Should().Be("Original");
        }

        [Fact]
        public void Multipart_PreambleAndEpilogue_AreRead()
        {
            // arrange
            var subject = (ImfMultipart)EntityTrees.Read(MimeShapes.OddFraming).Body;

            // act
            var testResult = new[] { Octets.ToText(subject.Preamble.Value.Span), Octets.ToText(subject.Epilogue.Value.Span) };

            // assert
            testResult.Should().Equal("A preamble.", "An epilogue.\r\n");
        }

        [Fact]
        public void Parts_MissingCloseDelimiter_IsAnError()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(NoCloseDelimiter))))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void Parts_MissingCloseDelimiterWithOption_IsRead()
        {
            // arrange
            var options = new ImfOptions { AllowMissingBoundaryTerminator = true };

            // act
            var testResult = EntityTrees.Describe(EntityTrees.Read(NoCloseDelimiter, options));

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/mixed", "1 1 leaf text/plain");
        }

        [Fact]
        public void ReadMessage_PastMaxPartCount_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxPartCount = 1 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(TwoParts)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadMessage_PastMaxNestingDepth_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = 1 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(MimeShapes.MixedWithAttachments)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadMessage_MimeFieldWithNoMimeVersion_RecordsADiagnostic()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(NoMimeVersion))))
            {
                // act
                reader.ReadMessage();
                var testResult = reader.Diagnostics;

                // assert
                testResult.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.MissingMimeVersion);
            }
        }

        [Fact]
        public void ReadMessage_PartLongerThanABlockOfTheReader_GivesAllOfIt()
        {
            // arrange
            const int LongerThanABlock = 40000;
            var message = EntityTrees.MessageWithPartOfLength(LongerThanABlock);

            // act
            var testResult = EntityTrees.At(EntityTrees.Read(message), "1").GetContent().Length;

            // assert
            testResult.Should().Be(LongerThanABlock);
        }

        [Fact]
        public async Task ReadMessageAsync_Multipart_GivesTheTree()
        {
            // arrange
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(MimeShapes.TextAndHtml))))
            {
                // act
                var testResult = EntityTrees.Describe(await reader.ReadMessageAsync());

                // assert
                testResult.Should().Equal("/ 0 multipart multipart/alternative", "1 1 leaf text/plain", "2 1 leaf text/html");
            }
        }

        [Fact]
        public void ReadMessage_BodyAtMaxEntitySize_GivesItsPart()
        {
            // arrange
            const string Message = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--";
            var options = new ImfOptions { MaxEntitySize = 15 };

            // act
            var testResult = EntityTrees.At(EntityTrees.Read(Message, options), "1").GetText();

            // assert
            testResult.Should().Be("x");
        }

        [Fact]
        public void ReadMessage_BodyPastMaxEntitySize_Throws()
        {
            // arrange
            const string Message = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--";
            var options = new ImfOptions { MaxEntitySize = 14 };
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(Message)), options))
            {
                // act
                Action testAction = () => reader.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(AllShapes))]
        public void WriteMessage_NothingChanged_WritesTheSameBytes(string message)
        {
            // arrange
            var subject = EntityTrees.Read(message);

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(message);
        }

        [Theory]
        [MemberData(nameof(SignedShapes))]
        public void WriteMessage_SignedAndNothingChanged_WritesTheSameBytes(string message)
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            var subject = EntityTrees.Read(message, options);

            // act
            var testResult = EntityTrees.Write(subject, options);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void WriteMessage_ContentOfOnePartSet_RewritesThatBodyOnly()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.OddFraming);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(MimeShapes.OddFramingWithNewFirstPart);
        }

        [Fact]
        public async Task WriteMessageAsync_ContentOfOnePartSet_RewritesThatBodyOnly()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.OddFraming);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = await EntityTrees.WriteAsync(subject);

            // assert
            testResult.Should().Be(MimeShapes.OddFramingWithNewFirstPart);
        }

        [Fact]
        public void WriteMessage_FieldOfOnePartSet_RewritesThatHeaderOnly()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            EntityTrees.At(subject, "2").ContentDescription = "Second";

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(SecondPartWithDescription);
        }

        [Fact]
        public void WriteMessage_FieldOfEmbeddedMessageSet_RewritesThatFieldOnly()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.Forwarded);
            ((ImfMessagePart)EntityTrees.At(subject, "2")).Message.Subject = "Changed";

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(ForwardedWithNewSubject);
        }

        [Fact]
        public void WriteMessage_PartInEmbeddedMessageSet_ReadsBackWithTheRestUnchanged()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.Forwarded);
            EntityTrees.At(subject, "2.1").SetContent(EntityTrees.Bytes("changed"));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(ForwardedWithNewInnerText);
        }

        [Fact]
        public void WriteMessage_NonAsciiContentSet_ChoosesATransferEncoding()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes("caf\x00C3\x00A9"));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TwoPartsWithFirstEncoded);
        }

        [Fact]
        public void WriteMessage_ContentSetOnAPartThatHadAnEncoding_ChoosesAgain()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TextOnlyWithNewBody);
        }

        [Fact]
        public void WriteMessage_TransferEncodingSetOnWhatWasRead_EncodesTheSameContentAgain()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            subject.Body.ContentTransferEncoding = MimeTransferEncoding.Base64;

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TextOnlyInBase64);
        }

        [Fact]
        public void WriteMessage_FieldOfTheMessageSet_LeavesTheBodyAsItIs()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.OddFraming);
            subject.Date = ImfDateTime.Parse("Fri, 21 Nov 1997 09:55:06 -0600");

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(MimeShapes.OddFraming);
        }

        [Fact]
        public void WriteMessage_PartAdded_WritesNewDelimitersAroundWhatWasRead()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            var part = new ImfEntity();
            part.SetContent(EntityTrees.Bytes("three"));
            ((ImfMultipart)subject.Body).Parts.Add(part);

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(ThreeParts);
        }

        [Fact]
        public void WriteMessage_PartRemoved_WritesTheOthersAsTheyWere()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            ((ImfMultipart)subject.Body).Parts.RemoveAt(0);

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(OnePart);
        }

        [Fact]
        public void WriteMessage_PreambleSet_WritesNewDelimiters()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            ((ImfMultipart)subject.Body).Preamble = EntityTrees.Bytes("Hello");

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TwoPartsWithPreamble);
        }

        [Fact]
        public void WriteMessage_BoundaryChanged_WritesNewDelimiters()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            subject.Body.ContentType = MimeMediaType.MultipartMixed.WithParameter("boundary", "c");

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TwoPartsWithNewBoundary);
        }

        [Fact]
        public void WriteMessage_AllPartsRemoved_ThrowsAndWritesNothing()
        {
            // arrange
            var subject = EntityTrees.Read(OnePart);
            ((ImfMultipart)subject.Body).Parts.RemoveAt(0);
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_NewContentHoldsTheBoundary_ThrowsAndWritesNothing()
        {
            // arrange (ref. RFC 2046 §5.1.1: a boundary that was given is not changed, and must not be in the content)
            var subject = EntityTrees.Read(TwoParts);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes("--b\r\n"));
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ConformanceProblemWithChangedBody_ThrowsAndWritesNothing()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));
            subject.From = null;
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ReadWithNoCloseDelimiterAndChanged_AddsOne()
        {
            // arrange
            var options = new ImfOptions { AllowMissingBoundaryTerminator = true };
            var subject = EntityTrees.Read(NoCloseDelimiter, options);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject, options);

            // assert
            testResult.Should().Be(NoCloseDelimiterRewritten);
        }

        [Fact]
        public void WriteMessage_ReadWithNoCloseDelimiterAndNotChanged_WritesTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowMissingBoundaryTerminator = true };
            var subject = EntityTrees.Read(NoCloseDelimiter, options);

            // act
            var testResult = EntityTrees.Write(subject, options);

            // assert
            testResult.Should().Be(NoCloseDelimiter);
        }

        [Fact]
        public void WriteMessage_ChangedBody_CountsOneMessage()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));
            var progress = new List<ImfWriteProgress>();
            using (var writer = new ImfWriter(new MemoryStream()))
            {
                // act
                writer.WriteMessage(subject, new SynchronousProgress(progress));
                var testResult = new[] { progress[progress.Count - 1].EntitiesWritten, writer.EntitiesWritten };

                // assert
                testResult.Should().Equal(1L, 1L);
            }
        }

        [Fact]
        public void Body_RawBodySet_IsReadFromTheNewBytes()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            subject.Fields.RemoveAll("Content-Transfer-Encoding");
            subject.RawBody = EntityTrees.Bytes(NewBody);

            // act
            var testResult = EntityTrees.ContentOf(subject.Body);

            // assert
            testResult.Should().Be(NewBody);
        }

        [Fact]
        public void Body_RawBodySetThatBreaksTheStructure_ThrowsWhenAskedFor()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            subject.RawBody = EntityTrees.Bytes(NewBody);

            // act
            Action testAction = () => _ = subject.Body;

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void WriteMessage_RawBodySetAndBodyNeverAskedFor_WritesItAsItIs()
        {
            // arrange
            var subject = EntityTrees.Read(TwoParts);
            subject.RawBody = EntityTrees.Bytes(NewBody);

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(TwoPartsHeaderWithNewRawBody);
        }

        [Fact]
        public void Body_Set_ReplacesTheContentFieldsAndKeepsTheOthers()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            var body = new ImfEntity { ContentType = MimeMediaType.ApplicationOctetStream };
            body.SetContent(EntityTrees.Bytes("x"));
            subject.Body = body;

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(HeaderWithNewBodyEntity);
        }

        [Fact]
        public void Body_SetToAPartThatWasRead_WritesItWithAMimeVersion()
        {
            // arrange
            var source = EntityTrees.Read(TwoParts);
            var moved = EntityTrees.At(source, "1");
            ((ImfMultipart)source.Body).Parts.RemoveAt(0);
            var subject = new ImfMessage { From = ImfAddressList.Parse("a@b.test"), Date = ImfDateTime.Parse("Fri, 21 Nov 1997 09:55:06 -0600") };
            subject.Body = moved;

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be("From: a@b.test\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMIME-Version: 1.0\r\n\r\none");
        }

        [Fact]
        public void WriteMessage_PartMovedUnderAKeptBoundaryThatItHolds_Throws()
        {
            // arrange (what was read was found not to hold the delimiters of the place it was read from, and that says nothing about any other)
            const string HoldsDelimiter = Header + "Content-Type: multipart/mixed; boundary=other\r\n\r\n--other\r\n\r\n--b\r\n--other--\r\n";
            const string Nested = Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: multipart/mixed; boundary=c\r\n\r\n--c\r\n\r\none\r\n--c--\r\n--b--\r\n";
            var source = EntityTrees.Read(HoldsDelimiter);
            var moved = EntityTrees.At(source, "1");
            ((ImfMultipart)source.Body).Parts.RemoveAt(0);
            var subject = EntityTrees.Read(Nested);
            ((ImfMultipart)EntityTrees.At(subject, "1")).Parts.Add(moved);
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_LineFeedMessageWithOnePartSet_KeepsItsLineBreaks()
        {
            // arrange
            const string Message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\n\none\n--b\n\ntwo\n--b--\n";
            var subject = EntityTrees.ReadWithLineFeeds(Message);
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.WriteWithLineFeeds(subject);

            // assert
            testResult.Should().Be("Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain; charset=us-ascii\n\nnew\n--b\n\ntwo\n--b--\n");
        }

        [Fact]
        public void GetContent_LineFeedMessageInQuotedPrintable_IsDecoded()
        {
            // arrange
            const string Message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Transfer-Encoding: quoted-printable\n\na=3Db\n";
            var subject = EntityTrees.ReadWithLineFeeds(Message).Body;

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be("a=b\n");
        }

        [Fact]
        public void WriteMessage_ContentSetAndContentTypeFieldNotValid_ThrowsAndWritesNothing()
        {
            // arrange
            var subject = EntityTrees.Read(MimeShapes.TextOnly);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));
            subject.Fields.RemoveAll("Content-Type");
            subject.Fields.Add(new ImfHeaderField("Content-Type", ";;;", ImfHeaderFieldKind.Structured));
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ContentSetAndEncodedWordInParameter_KeepsTheFieldAsItIs()
        {
            // arrange (ref. RFC 2047 §5: not allowed in a parameter, though a reader goes by the parameters it needs)
            const string Message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: application/pdf; name=\"=?UTF-8?Q?a?=\"\r\n\r\nold";
            const string Expected = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: application/pdf; name=\"=?UTF-8?Q?a?=\"\r\n\r\nnew";
            var subject = EntityTrees.Read(Message);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(Expected);
        }

        [Fact]
        public void WriteMessage_AllowedProblemAndBodyRefused_RecordsNoDiagnostic()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            var subject = EntityTrees.Read(TwoParts, options);
            subject.Date = null;
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes("--b\r\n"));
            using (var writer = new ImfWriter(new MemoryStream(), options))
            {
                // act
                MessageWrites.IgnoreFailure(() => writer.WriteMessage(subject));
                var testResult = writer.Diagnostics;

                // assert
                testResult.Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteMessage_AllowedProblemAndBodyWritten_RecordsADiagnostic()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            var subject = EntityTrees.Read(TwoParts, options);
            subject.Date = null;
            EntityTrees.At(subject, "1").SetContent(EntityTrees.Bytes(NewBody));
            using (var writer = new ImfWriter(new MemoryStream(), options))
            {
                // act
                writer.WriteMessage(subject);
                var testResult = writer.Diagnostics;

                // assert
                testResult.Should().ContainSingle();
            }
        }

        [Fact]
        public void WriteMessage_BareLineFeedEndedTheHeaderAndContentSet_KeepsIt()
        {
            // arrange
            var options = new ImfOptions { AcceptBareLineFeed = true };
            var subject = EntityTrees.Read(BareLineFeedHeaderEnd, options);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject, options);

            // assert
            testResult.Should().Be(BareLineFeedHeaderEndGivenABody);
        }

        [Fact]
        public void WriteMessage_NoEmptyLineAfterTheHeaderAndContentSet_AddsOne()
        {
            // arrange
            var subject = EntityTrees.Read(NoHeaderEnd);
            subject.Body.SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(NoHeaderEndGivenABody);
        }

        [Fact]
        public void WriteMessage_MessagePartWithNoBodyGivenAMessage_SeparatesItFromTheHeader()
        {
            // arrange
            var subject = EntityTrees.Read(EmptyEmbeddedMessage);
            ((ImfMessagePart)EntityTrees.At(subject, "1")).Message.Subject = "New";

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(EmbeddedMessageGivenASubject);
        }

        [Fact]
        public void WriteMessage_NestedDeeperThanTheStackCanFollow_ThrowsAndWritesNothing()
        {
            // arrange
            var options = new ImfOptions { AllowMissingRequiredFields = true };
            var subject = EntityTrees.MessageNestedToDepth(DeeperThanTheStackCanFollow, options);
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, options))
            {
                // act
                Action testAction = () => writer.WriteMessage(subject);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void TextBody_NestedDeeperThanTheStackCanFollow_IsFound()
        {
            // arrange
            var subject = EntityTrees.MessageNestedToDepth(DeeperThanTheStackCanFollow, new ImfOptions());

            // act
            var testResult = subject.TextBody.GetText();

            // assert
            testResult.Should().Be("x");
        }

        [Fact]
        public void TextBody_InsideAMultipartThatIsAnAttachment_IsNotTheTextOfTheMessage()
        {
            // arrange
            var subject = EntityTrees.Read(AttachedAlternative);

            // act
            var testResult = subject.TextBody.GetText();

            // assert
            testResult.Should().Be("body");
        }

        [Fact]
        public void Attachments_MultipartThatIsAnAttachment_IsListedAndNotLookedInside()
        {
            // arrange
            var subject = EntityTrees.Read(AttachedAlternative);

            // act
            var testResult = EntityTrees.TypesOf(subject.Attachments);

            // assert
            testResult.Should().Equal("multipart/alternative");
        }

        [Fact]
        public void GetContent_LineFeedMessageWithEmbeddedMessageChanged_KeepsItsLineBreaks()
        {
            // arrange
            const string Message = "Date: Fri, 21 Nov 1997 09:55:06 -0600\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: message/rfc822\n\nSubject: Old\n\nbody";
            var subject = (ImfMessagePart)EntityTrees.ReadWithLineFeeds(Message).Body;
            subject.Message.Body.SetContent(EntityTrees.Bytes(NewBody));

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be("Subject: Old\nMIME-Version: 1.0\nContent-Type: text/plain; charset=us-ascii\n\nnew");
        }

        [Fact]
        public void Body_SetToNull_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Body = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Body_SetToAnEntityInUse_Throws()
        {
            // arrange
            var part = new ImfEntity();
            new ImfMultipart(MimeMediaType.MultipartMixed).Parts.Add(part);
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Body = part;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Body_SetToAnEntityThatHoldsTheMessage_Throws()
        {
            // arrange
            var subject = new ImfMessage();
            var holder = new ImfMessagePart(subject);

            // act
            Action testAction = () => subject.Body = holder;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Parts_AddItself_Throws()
        {
            // arrange
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            Action testAction = () => subject.Parts.Add(subject);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Parts_AddAnEntityThatHoldsIt_Throws()
        {
            // arrange
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);
            var outer = new ImfMultipart(MimeMediaType.MultipartMixed);
            outer.Parts.Add(subject);

            // act
            Action testAction = () => subject.Parts.Add(outer);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Parts_AddAnEntityInUse_Throws()
        {
            // arrange
            var part = new ImfEntity();
            new ImfMultipart(MimeMediaType.MultipartMixed).Parts.Add(part);
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            Action testAction = () => subject.Parts.Add(part);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Parts_AddAnEntityThatWasRemoved_IsAllowed()
        {
            // arrange
            var part = new ImfEntity();
            var first = new ImfMultipart(MimeMediaType.MultipartMixed);
            first.Parts.Add(part);
            first.Parts.RemoveAt(0);
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            subject.Parts.Add(part);
            var testResult = subject.Parts.Count;

            // assert
            testResult.Should().Be(1);
        }

        [Fact]
        public void Message_SetToAMessageThatHoldsThePart_Throws()
        {
            // arrange
            var subject = new ImfMessagePart(new ImfMessage());
            var other = new ImfMessage { Body = subject };

            // act
            Action testAction = () => subject.Message = other;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ContentType_MultipartSetToAnotherType_Throws()
        {
            // arrange
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            Action testAction = () => subject.ContentType = MimeMediaType.TextPlain;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ContentType_MessagePartSetToAnotherType_Throws()
        {
            // arrange
            var subject = new ImfMessagePart(new ImfMessage());

            // act
            Action testAction = () => subject.ContentType = MimeMediaType.TextPlain;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ContentTransferEncoding_MultipartSetToBase64_Throws()
        {
            // arrange
            var subject = new ImfMultipart(MimeMediaType.MultipartMixed);

            // act
            Action testAction = () => subject.ContentTransferEncoding = MimeTransferEncoding.Base64;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ContentTransferEncoding_MessagePartSetToBase64_Throws()
        {
            // arrange
            var subject = new ImfMessagePart(new ImfMessage());

            // act
            Action testAction = () => subject.ContentTransferEncoding = MimeTransferEncoding.Base64;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetContent_EmbeddedMessageChanged_IsTheMessageAsItIsNow()
        {
            // arrange
            const string Forward = Header + "Content-Type: message/rfc822\r\n\r\nSubject: Old\r\n\r\nbody";
            var message = EntityTrees.Read(Forward);
            var subject = (ImfMessagePart)message.Body;
            subject.Message.Subject = "New";

            // act
            var testResult = EntityTrees.ContentOf(subject);

            // assert
            testResult.Should().Be("Subject: New\r\n\r\nbody");
        }

        private sealed class SynchronousProgress : IProgress<ImfWriteProgress>
        {
            private readonly List<ImfWriteProgress> _reports;

            public SynchronousProgress(List<ImfWriteProgress> reports)
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
