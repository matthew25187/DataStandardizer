using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfMessageBuilder"/>: the structure it gives, what it writes, and that what it writes reads back as the same tree.
    /// </summary>
    public class ImfMessageBuilderTests
    {
        private const string Text = "plain";
        private const string Html = "<p>html</p>";
        private const string SampleDate = "Fri, 21 Nov 1997 09:55:06 -0600";
        private const string SampleFrom = "a@b.test";
        private const string Header = "From: a@b.test\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nSubject: Hi\r\n";
        private const string FileName = "report.pdf";
        private const string InlineId = "<img@b.test>";

        private const string BuiltTextOnly = Header + "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\n\r\nplain";
        private const string BuiltNothing = Header + "\r\n";

        private const string BuiltTextAndHtmlAndAttachment =
            Header +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=\"=_1\"\r\n" +
            "\r\n" +
            "--=_1\r\n" +
            "Content-Type: multipart/alternative; boundary=\"=_2\"\r\n" +
            "\r\n" +
            "--=_2\r\n" +
            "Content-Type: text/plain; charset=UTF-8\r\n" +
            "\r\n" +
            "plain\r\n" +
            "--=_2\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "\r\n" +
            "<p>html</p>\r\n" +
            "--=_2--\r\n" +
            "--=_1\r\n" +
            "Content-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=report.pdf\r\n" +
            "\r\n" +
            "%PDF\r\n" +
            "--=_1--";

        private const string BuiltForward =
            Header +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=\"=_1\"\r\n" +
            "\r\n" +
            "--=_1\r\n" +
            "Content-Type: text/plain; charset=UTF-8\r\n" +
            "\r\n" +
            "plain\r\n" +
            "--=_1\r\n" +
            "Content-Disposition: attachment\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "\r\n" +
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n" +
            "From: a@b.test\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "\r\n" +
            "caf=C3=A9\r\n" +
            "\r\n" +
            "--=_1--";

        private static readonly MimeMediaType Pdf = new MimeMediaType("application", "pdf");
        private static readonly MimeMediaType Png = new MimeMediaType("image", "png");
        private static readonly ReadOnlyMemory<byte> PdfBytes = Octets.Of("%PDF");
        private static readonly ReadOnlyMemory<byte> PngBytes = Octets.Of("\x0089PNG");

        public static TheoryData<Func<ImfMessageBuilder, ImfMessageBuilder>, string[]> Structures => new TheoryData<Func<ImfMessageBuilder, ImfMessageBuilder>, string[]>
        {
            { b => b.SetText(Text), new[] { "/ 0 leaf text/plain" } },
            { b => b.SetHtml(Html), new[] { "/ 0 leaf text/html" } },
            { b => b.SetText(Text).SetHtml(Html), new[] { "/ 0 multipart multipart/alternative", "1 1 leaf text/plain", "2 1 leaf text/html" } },
            {
                b => b.SetHtml(Html).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes),
                new[] { "/ 0 multipart multipart/related", "1 1 leaf text/html", "2 1 leaf image/png" }
            },
            {
                b => b.SetText(Text).SetHtml(Html).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes),
                new[] { "/ 0 multipart multipart/alternative", "1 1 leaf text/plain", "2 1 multipart multipart/related", "2.1 2 leaf text/html", "2.2 2 leaf image/png" }
            },
            { b => b.SetText(Text).AddAttachment(FileName, Pdf, PdfBytes), new[] { "/ 0 multipart multipart/mixed", "1 1 leaf text/plain", "2 1 leaf application/pdf" } },
            { b => b.AddAttachment(FileName, Pdf, PdfBytes), new[] { "/ 0 multipart multipart/mixed", "1 1 leaf application/pdf" } },
            {
                b => b.SetText(Text).SetHtml(Html).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes).AddAttachment(FileName, Pdf, PdfBytes),
                new[]
                {
                    "/ 0 multipart multipart/mixed", "1 1 multipart multipart/alternative", "1.1 2 leaf text/plain", "1.2 2 multipart multipart/related", "1.2.1 3 leaf text/html", "1.2.2 3 leaf image/png", "2 1 leaf application/pdf",
                }
            },
        };

        [Theory]
        [MemberData(nameof(Structures))]
        public void Build_WhatItHas_GivesTheStructureItCallsFor(Func<ImfMessageBuilder, ImfMessageBuilder> setUp, string[] expected)
        {
            // arrange
            var subject = setUp(CreateBuilder());

            // act
            var testResult = EntityTrees.Describe(subject.Build());

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(Structures))]
        public void Build_WhatItWrites_ReadsBackAsTheSameTree(Func<ImfMessageBuilder, ImfMessageBuilder> setUp, string[] expected)
        {
            // arrange
            var subject = setUp(CreateBuilder()).Build();

            // act
            var testResult = EntityTrees.Describe(EntityTrees.Read(EntityTrees.Write(subject)));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void Build_TextOnly_WritesExactlyThis()
        {
            // arrange
            var subject = CreateBuilder().SetText(Text).Build();

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(BuiltTextOnly);
        }

        [Fact]
        public void Build_TextAndHtmlAndAttachment_WritesExactlyThis()
        {
            // arrange
            var subject = CreateBuilder().SetText(Text).SetHtml(Html).AddAttachment(FileName, Pdf, PdfBytes).Build();

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(BuiltTextAndHtmlAndAttachment);
        }

        [Fact]
        public void Build_WhatItWrites_ReadsBackWithTheSameContent()
        {
            // arrange
            var subject = CreateBuilder().SetText("caf\x00E9\nline").SetHtml(Html).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes).AddAttachment("caf\x00E9.pdf", Pdf, PdfBytes).Build();
            var readBack = EntityTrees.Read(EntityTrees.Write(subject));

            // act
            var testResult = new[]
            {
                readBack.TextBody.GetText(),
                readBack.HtmlBody.GetText(),
                EntityTrees.ContentOf(readBack.FindByContentId(ImfMessageId.Parse(InlineId))),
                readBack.Attachments[0].FileName,
                EntityTrees.ContentOf(readBack.Attachments[0]),
            };

            // assert
            testResult.Should().Equal("caf\x00E9\r\nline", Html, "\x0089PNG", "caf\x00E9.pdf", "%PDF");
        }

        [Fact]
        public void Build_RelatedEntity_NamesTheTypeOfItsRoot()
        {
            // arrange (ref. RFC 2387 §3.1: the type parameter is required)
            var subject = CreateBuilder().SetHtml(Html).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes);

            // act
            var testResult = subject.Build().Body.ContentType.ToString();

            // assert
            testResult.Should().Be("multipart/related;type=\"text/html\"");
        }

        [Fact]
        public void Build_ForwardedMessage_HoldsItUnchanged()
        {
            // arrange
            var forwarded = EntityTrees.Read(MimeShapes.TextOnly);
            var subject = CreateBuilder().SetText(Text).AddAttachment(forwarded).Build();

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(BuiltForward);
        }

        [Fact]
        public void Build_Nothing_HasNoBody()
        {
            // arrange
            var subject = CreateBuilder().Build();

            // act
            var testResult = EntityTrees.Write(subject);

            // assert
            testResult.Should().Be(BuiltNothing);
        }

        [Fact]
        public void Build_Twice_GivesMessagesThatShareNothingThatChanges()
        {
            // arrange
            var subject = CreateBuilder().SetText(Text).AddAttachment(FileName, Pdf, PdfBytes);
            var first = subject.Build();
            var second = subject.Build();

            // act
            first.Attachments[0].ContentDescription = "Changed";
            var testResult = second.Attachments[0].ContentDescription;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Build_InlineResourceWithNoHtml_Throws()
        {
            // arrange
            var subject = CreateBuilder().SetText(Text).AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes);

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Build_TextThatItsCharacterSetCantHold_Throws()
        {
            // arrange
            var subject = CreateBuilder().SetText("caf\x00E9", MimeCharset.UsAscii);

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Build_ContentPastMaxEntitySize_Throws()
        {
            // arrange
            var subject = new ImfMessageBuilder(new ImfOptions { MaxEntitySize = 3 }).AddAttachment(FileName, Pdf, PdfBytes);

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddAttachment_MultipartType_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.AddAttachment(FileName, MimeMediaType.MultipartMixed, PdfBytes);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddInlineResource_RepeatedIdentifier_Throws()
        {
            // arrange (ref. RFC 2045 §7: a Content-ID must be unique)
            var subject = CreateBuilder().AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes);

            // act
            Action testAction = () => subject.AddInlineResource(ImfMessageId.Parse(InlineId), Png, PngBytes);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Build_MessageTypeAttachmentIn7bit_ReadsBackAsAnEmbeddedMessage()
        {
            // arrange
            var eml = Octets.Of("From: c@d.test\r\n\r\nx");
            var subject = CreateBuilder().AddAttachment("a.eml", MimeMediaType.MessageRfc822, eml).Build();

            // act
            var testResult = EntityTrees.Describe(EntityTrees.Read(EntityTrees.Write(subject)));

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/mixed", "1 1 part message/rfc822", "1 2 leaf text/plain");
        }

        [Fact]
        public void Build_MessageTypeAttachmentNotIn7bit_IsRefusedByTheWriterRatherThanEncoded()
        {
            // arrange (ref. RFC 2045 §6.4: such an entity can't be given a base64 or quoted-printable encoding)
            var eml = Octets.Of("From: c@d.test\r\n\r\ncaf\x00E9");
            var subject = CreateBuilder().AddAttachment("a.eml", MimeMediaType.MessageRfc822, eml).Build();

            // act
            Action testAction = () => EntityTrees.Write(subject);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void AddInlineResource_NullIdentifier_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.AddInlineResource(null, Png, PngBytes);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetText_Null_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.SetText(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        private static ImfMessageBuilder CreateBuilder()
        {
            var builder = new ImfMessageBuilder();
            builder.Header.From = ImfAddressList.Parse(SampleFrom);
            builder.Header.Date = ImfDateTime.Parse(SampleDate);
            builder.Header.Subject = "Hi";
            return builder;
        }
    }
}
