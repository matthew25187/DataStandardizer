using System;
using System.IO;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MhtmlDocumentBuilder"/>: what it writes, and that it reads back with the same root and resources.
    /// </summary>
    public class MhtmlDocumentBuilderTests
    {
        private const string Html = "<p>page</p>";
        private const string PageUrl = "http://example.org/page.html";
        private const string LogoUrl = "http://example.org/logo.png";
        private const string AggregateUrl = "http://example.org/aggregate";
        private const string RelativeLogo = "images/logo.png";
        private const string ResolvedRelativeLogo = "http://example.org/images/logo.png";
        private const string CidLogo = "cid:logo@b.test";
        private const string RootId = "<root@b.test>";

        private const string BuiltDocument =
            "MIME-Version: 1.0\r\n" +
            "Content-Location: http://example.org/aggregate\r\n" +
            "Content-Type: multipart/related; type=\"text/html\"; start=\"<root@b.test>\";\r\n boundary=\"=_1\"\r\n" +
            "\r\n" +
            "--=_1\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "Content-Location: http://example.org/page.html\r\n" +
            "Content-ID: <root@b.test>\r\n" +
            "\r\n" +
            "<p>page</p>\r\n" +
            "--=_1\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-Location: http://example.org/logo.png\r\n" +
            "\r\n" +
            "PNG\r\n" +
            "--=_1--";

        private static readonly MimeMediaType Png = new MimeMediaType("image", "png");
        private static readonly ReadOnlyMemory<byte> PngBytes = Octets.Of("PNG");

        [Fact]
        public void Build_RootAndResource_WritesTheStructureOfRfc2557()
        {
            // arrange
            var subject = CreateBuilder().SetLocation(new Uri(AggregateUrl)).SetHtml(new Uri(PageUrl), Html).AddResource(new Uri(LogoUrl), Png, PngBytes);

            // act
            var testResult = Write(subject.Build());

            // assert
            testResult.Should().Be(BuiltDocument);
        }

        [Fact]
        public void Build_WhatItWrites_ReadsBackWithTheSameRootAndResources()
        {
            // arrange
            var built = CreateBuilder().SetLocation(new Uri("http://example.org/")).SetHtml(new Uri(PageUrl), Html).AddResource(new Uri(RelativeLogo, UriKind.Relative), Png, PngBytes).Build();

            // act
            var testResult = Read(Write(built));

            // assert
            testResult.Root.ContentId.Should().Be(built.Root.ContentId);
            testResult.RootMediaType.Subtype.Should().Be("html");
            testResult.Diagnostics.Should().BeEmpty();
            testResult.Resources.Should().HaveCount(2);
            testResult.Resources[0].Uri.OriginalString.Should().Be(PageUrl);
            testResult.Resources[1].Uri.OriginalString.Should().Be(ResolvedRelativeLogo);
        }

        [Fact]
        public void TryResolve_WhatTheBuilderWrote_FindsTheResource()
        {
            // arrange
            var built = CreateBuilder().SetLocation(new Uri("http://example.org/")).SetHtml(new Uri(PageUrl), Html).AddResource(new Uri(RelativeLogo, UriKind.Relative), Png, PngBytes).Build();
            var subject = Read(Write(built));

            // act
            var testResult = subject.TryResolve(new Uri(RelativeLogo, UriKind.Relative), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be("PNG");
        }

        [Fact]
        public void Build_ResourceWithACidUrl_IsFoundByItsContentId()
        {
            // arrange
            var subject = CreateBuilder().SetHtml(new Uri(PageUrl), Html).AddResource(new Uri(CidLogo), Png, PngBytes).Build();

            // act
            var testResult = subject.TryResolve(new Uri(CidLogo), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Entity.ContentLocation.Should().BeNull();
            resource.Entity.ContentId.Should().Be(ImfMessageId.Parse("<logo@b.test>"));
        }

        [Fact]
        public void Build_RootWithACidUrl_UsesItForStart()
        {
            // arrange
            var subject = CreateBuilder().SetHtml(new Uri("cid:page@b.test"), Html);

            // act
            var testResult = subject.Build();

            // assert
            testResult.Root.ContentId.Should().Be(ImfMessageId.Parse("<page@b.test>"));
            testResult.Message.Body.ContentType.TryGetParameter("start", out var start).Should().BeTrue();
            start.Value.Should().Be("<page@b.test>");
        }

        [Fact]
        public void Build_RootOfAnotherType_GivesItsTypeInTheParameter()
        {
            // arrange
            var subject = CreateBuilder().SetRoot(new Uri(PageUrl), Png, PngBytes);

            // act
            var testResult = subject.Build();

            // assert
            testResult.Message.Body.ContentType.TryGetParameter("type", out var type).Should().BeTrue();
            type.Value.Should().Be("image/png");
            EntityTrees.ContentOf(testResult.Root).Should().Be("PNG");
        }

        [Fact]
        public void Build_NoGenerator_GivesTheRootAContentId()
        {
            // arrange
            var subject = new MhtmlDocumentBuilder().SetHtml(new Uri(PageUrl), Html);

            // act
            var testResult = subject.Build();

            // assert
            testResult.Root.ContentId.IdRight.Should().Be("mhtml.invalid");
        }

        [Fact]
        public void Build_HeaderFields_AreCopied()
        {
            // arrange
            var subject = CreateBuilder().SetHtml(new Uri(PageUrl), Html);
            subject.Header.Subject = "Saved";

            // act
            var testResult = subject.Build();

            // assert
            testResult.Message.Subject.Should().Be("Saved");
        }

        [Fact]
        public void Build_NoRoot_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Build_TwoResourcesWithTheSameResolvedUrl_Throws()
        {
            // arrange
            var subject = CreateBuilder()
                .SetLocation(new Uri("http://example.org/"))
                .SetHtml(new Uri(PageUrl), Html)
                .AddResource(new Uri(RelativeLogo, UriKind.Relative), Png, PngBytes)
                .AddResource(new Uri(ResolvedRelativeLogo), Png, PngBytes);

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
        }

        [Fact]
        public void Build_TwoResourcesWithTheSameUrlAndOptionsThatAllowIt_Throws()
        {
            // arrange
            var subject = new MhtmlDocumentBuilder(new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true })
                .SetHtml(new Uri(PageUrl), Html)
                .AddResource(new Uri(LogoUrl), Png, PngBytes)
                .AddResource(new Uri(LogoUrl), Png, PngBytes);

            // act
            Action testAction = () => subject.Build();

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void Build_Twice_SharesNothing()
        {
            // arrange
            var subject = CreateBuilder().SetHtml(new Uri(PageUrl), Html).AddResource(new Uri(LogoUrl), Png, PngBytes);

            // act
            var testResult = new[] { subject.Build(), subject.Build() };

            // assert
            testResult[0].Root.Should().NotBeSameAs(testResult[1].Root);
            testResult[0].Resources[1].Entity.Should().NotBeSameAs(testResult[1].Resources[1].Entity);
        }

        [Fact]
        public void SetLocation_Relative_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.SetLocation(new Uri("a", UriKind.Relative));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddResource_MultipartType_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.AddResource(new Uri(LogoUrl), MimeMediaType.MultipartMixed, PngBytes);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddResource_InvalidCidUrl_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.AddResource(new Uri("cid:not valid"), Png, PngBytes);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetRoot_NullLocation_Throws()
        {
            // arrange
            var subject = CreateBuilder();

            // act
            Action testAction = () => subject.SetRoot(null, Png, PngBytes);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        private static MhtmlDocumentBuilder CreateBuilder()
        {
            return new MhtmlDocumentBuilder(new ImfOptions { AllowMissingRequiredFields = true }, () => ImfMessageId.Parse(RootId));
        }

        private static string Write(MhtmlDocument document)
        {
            var output = new MemoryStream();
            using (var writer = new MhtmlWriter(output, new ImfOptions { AllowMissingRequiredFields = true }, MessageWrites.NextBoundary()))
            {
                writer.WriteDocument(document);
            }

            return Octets.ToText(output.ToArray());
        }

        private static MhtmlDocument Read(string document)
        {
            using (var reader = new MhtmlReader(new MemoryStream(Octets.Of(document)), EntityEvents.DocumentOptions))
            {
                return reader.ReadDocument();
            }
        }
    }
}
