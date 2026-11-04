using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MhtmlDocument"/>: the root, the resources, resolving URLs, and the rules of RFC 2387 §3 and RFC 2557 §7.
    /// </summary>
    public class MhtmlDocumentTests
    {
        private const string Logo = "http://www.ietf.cnri.reston.va.us/images/ietflogo.gif";
        private const string MoreInfo = "http://www.ietf.cnri.reston.va.us/more-info";
        private const string Logo2d = "http://www.ietf.cnri.reston.va.us/images/ietflogo2d.gif";
        private const string Logo2e = "http://www.ietf.cnri.reston.va.us/images/ietflogo2e.gif";
        private const string RelativeLogo2d = "images/ietflogo2d.gif";
        private const string RelativeLogo2e = "images/ietflogo2e.gif";
        private const string TypeHtml = "; type=\"text/html\"";
        private const string SecondPart = "Content-Type: image/png\r\nContent-Location: http://example.org/logo.png\r\n";
        private const string SameLocation = "http://example.org/same";
        private const string RelatedWithNoParts = "MIME-Version: 1.0\r\nContent-Type: multipart/related; boundary=rel; type=\"text/html\"\r\n\r\nno delimiter\r\n";

        private const string MixedWithSameLocation =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-Location: http://example.org/same\r\n" +
            "\r\n" +
            "FIRST\r\n" +
            "--mix\r\n" +
            "Content-Location: http://example.org/same\r\n" +
            "\r\n" +
            "SECOND\r\n" +
            "--mix--\r\n";

        private const string MixedWithSameContentId =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-ID: <same@b.test>\r\n" +
            "\r\n" +
            "FIRST\r\n" +
            "--mix\r\n" +
            "Content-ID: <same@b.test>\r\n" +
            "\r\n" +
            "SECOND\r\n" +
            "--mix--\r\n";

        // The positions of the parts of MhtmlShapes.Rfc2557Section96 in Resources.
        private const int MoreInfoRoot = 3;
        private const int EvenMoreInfoRoot = 6;

        public static TheoryData<string> InvalidStructures => new TheoryData<string>
        {
            // ref. RFC 2387 §3.1: "The type parameter must be specified".
            MhtmlShapes.Related(string.Empty, SecondPart),
            // ref. RFC 2387 §3.1: its value is the media type of the root.
            MhtmlShapes.Related("; type=\"text/plain\"", SecondPart),
            MhtmlShapes.Related("; type=\"not a type\"", SecondPart),
            // ref. RFC 2387 §3.2: start is the Content-ID of the root.
            MhtmlShapes.Related(TypeHtml + "; start=\"<none@b.test>\"", SecondPart),
            MhtmlShapes.Related(TypeHtml + "; start=\"not an id\"", SecondPart),
            // ref. RFC 2557 §7: each part has a different Content-ID and Content-Location.
            MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-ID: <root@b.test>\r\n"),
            MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: http://example.org/page.html\r\n"),
        };

        [Theory]
        [InlineData(MhtmlShapes.Rfc2557Section92, "text", "html")]
        [InlineData(MhtmlShapes.Rfc2557Section93, "text", "html")]
        [InlineData(MhtmlShapes.Rfc2557Section94, "text", "html")]
        [InlineData(MhtmlShapes.Rfc2557Section95, "text", "html")]
        [InlineData(MhtmlShapes.Rfc2557Section96, "text", "html")]
        public void RootMediaType_Rfc2557Example_IsThatOfTheRoot(string document, string type, string subtype)
        {
            // arrange
            var subject = Read(document);

            // act
            var testResult = subject.RootMediaType;

            // assert
            testResult.Type.Should().Be(type);
            testResult.Subtype.Should().Be(subtype);
        }

        [Fact]
        public void Root_NotMultipart_IsTheBody()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section91);

            // act
            var testResult = subject.Root;

            // assert
            testResult.Should().BeSameAs(subject.Message.Body);
        }

        [Fact]
        public void Root_StartNamesALaterPart_IsThatPart()
        {
            // arrange
            var subject = Read(MimeShapes.HtmlRootNamedByStart);

            // act
            var testResult = subject.Root;

            // assert
            testResult.ContentId.Should().Be(ImfMessageId.Parse("<root@b.test>"));
        }

        [Fact]
        public void Root_MultipartAlternativeStart_IsItsHtml()
        {
            // arrange
            var subject = Read(MhtmlShapes.AlternativeRoot);

            // act
            var testResult = subject.Root;

            // assert
            EntityTrees.ContentOf(testResult).Should().Be("<p>html</p>");
            subject.RootMediaType.Subtype.Should().Be("html");
        }

        [Fact]
        public void Root_MultipartMixedBody_IsTheBody()
        {
            // arrange
            var subject = Read(MimeShapes.MixedWithAttachments);

            // act
            var testResult = subject.Root;

            // assert
            testResult.Should().BeSameAs(subject.Message.Body);
        }

        [Fact]
        public void Resources_MultipartBody_GivesEachPartInOrder()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section93);

            // act
            var testResult = subject.Resources;

            // assert
            testResult.Should().HaveCount(4);
            testResult[0].Uri.Should().BeNull();
            testResult[1].Uri.OriginalString.Should().Be("http://www.ietf.cnri.reston.va.us/images/ietflogo1.gif");
            testResult[2].Uri.OriginalString.Should().Be("http://www.ietf.cnri.reston.va.us/images/ietflogo2.gif");
            testResult[3].Uri.OriginalString.Should().Be("http://www.ietf.cnri.reston.va.us/images/ietflogo3.gif");
        }

        [Fact]
        public void Resources_NoBase_ResolveAgainstThisMessage()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section94);

            // act
            var testResult = subject.Resources;

            // assert
            testResult[1].Uri.OriginalString.Should().Be("thismessage:/ietflogo.gif");
        }

        [Fact]
        public void Resources_BodyIsNotMultipart_IsTheBody()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section91);

            // act
            var testResult = subject.Resources;

            // assert
            testResult.Should().ContainSingle().Which.Entity.Should().BeSameAs(subject.Message.Body);
        }

        [Fact]
        public void Resources_CidLocation_HasNoUri()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section95);

            // act
            var testResult = subject.Resources;

            // assert
            testResult[1].Uri.Should().BeNull();
        }

        [Fact]
        public void Resources_EmbeddedMessage_IsNotLookedInto()
        {
            // arrange
            var subject = Read(MimeShapes.Forwarded);

            // act
            var testResult = subject.Resources;

            // assert
            EntityTrees.TypesOf(new[] { testResult[0].Entity, testResult[1].Entity }).Should().Equal("text/plain", "message/rfc822");
            testResult.Should().HaveCount(2);
        }

        [Fact]
        public void TryResolve_AbsoluteLocation_FindsThePart()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section92);

            // act
            var testResult = subject.TryResolve(new Uri(Logo), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Should().BeSameAs(subject.Resources[1]);
        }

        [Theory]
        [InlineData("images/ietflogo1.gif", 1)]
        [InlineData("images/ietflogo2.gif", 2)]
        [InlineData("http://www.ietf.cnri.reston.va.us/images/ietflogo3.gif", 3)]
        public void TryResolve_Rfc2557Section93_FindsThePart(string reference, int expected)
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section93);

            // act
            var testResult = subject.TryResolve(new Uri(reference, UriKind.RelativeOrAbsolute), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Should().BeSameAs(subject.Resources[expected]);
        }

        [Fact]
        public void TryResolve_Rfc2557Section94_FindsThePartWithNoBase()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section94);

            // act
            var testResult = subject.TryResolve(new Uri("ietflogo.gif", UriKind.Relative), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Should().BeSameAs(subject.Resources[1]);
        }

        [Fact]
        public void TryResolve_CidUri_FindsTheContentId()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section95);

            // act
            var testResult = subject.TryResolve(new Uri("cid:foo4@foo1.bar.net"), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Should().BeSameAs(subject.Resources[1]);
        }

        [Fact]
        public void TryResolve_CidUriWithAFragment_FindsTheContentId()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section95);

            // act
            var testResult = subject.TryResolve(new Uri("cid:foo4@foo1.bar.net#section"), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Should().BeSameAs(subject.Resources[1]);
        }

        [Fact]
        public void TryResolve_SameLocationOutsideMultipartRelated_FindsTheFirstWithNoDiagnostic()
        {
            // arrange
            var subject = Read(MixedWithSameLocation);

            // act
            var testResult = subject.TryResolve(new Uri(SameLocation), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be("FIRST");
            subject.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_SameContentIdOutsideMultipartRelated_Throws()
        {
            // arrange
            var message = EntityTrees.Read(MixedWithSameContentId, new ImfOptions { AllowMissingRequiredFields = true });

            // act
            Action testAction = () => new MhtmlDocument(message);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
        }

        [Fact]
        public void Constructor_MultipartRelatedWithNoParts_Throws()
        {
            // arrange
            var message = EntityTrees.Read(RelatedWithNoParts, new ImfOptions { AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true });

            // act
            Action testAction = () => new MhtmlDocument(message);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
        }

        [Fact]
        public void Constructor_MultipartRelatedWithNoPartsAllowed_IsItsOwnRoot()
        {
            // arrange
            var message = EntityTrees.Read(RelatedWithNoParts, new ImfOptions { AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true, AllowInvalidRelatedStructure = true });

            // act
            var testResult = new MhtmlDocument(message);

            // assert
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
            testResult.Root.Should().BeSameAs(message.Body);
        }

        [Theory]
        // ref. RFC 2557 §8.3: a cid: URI is not matched against a Content-Location.
        [InlineData("cid:something@else")]
        [InlineData("cid:none@foo1.bar.net")]
        [InlineData("cid:not valid")]
        public void TryResolve_OtherCidUri_FindsNothing(string reference)
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section95);

            // act
            var testResult = subject.TryResolve(new Uri(reference), out var resource);

            // assert
            testResult.Should().BeFalse();
            resource.Should().BeNull();
        }

        [Theory]
        [InlineData("page.html", "ROOT")]
        [InlineData("./page.html", "ROOT")]
        [InlineData("../img/x.png", "IMG")]
        [InlineData("x/../../img/x.png", "IMG")]
        [InlineData("/a/img/x.png", "IMG")]
        [InlineData("data?id=1", "DATA")]
        [InlineData("sec.html#top", "SEC")]
        [InlineData("a%2eb", "ESCAPED")]
        public void TryResolve_RelativeReference_FindsThePart(string reference, string expected)
        {
            // arrange
            var subject = Read(MhtmlShapes.RelativeParts);

            // act
            var testResult = subject.TryResolve(new Uri(reference, UriKind.Relative), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be(expected);
        }

        [Theory]
        // ref. RFC 1808 §2.4.4: a query is part of what is identified.
        [InlineData("data?id=2")]
        [InlineData("data")]
        // ref. RFC 2557 §8.2 (d): "identical, octet by octet".
        [InlineData("a.b")]
        [InlineData("PAGE.HTML")]
        [InlineData("img/x.png")]
        // ref. RFC 2557 §5 (b): a relative label is no base, so this is the aggregate, which §4.3 says is not its root.
        [InlineData("#top")]
        public void TryResolve_ReferenceThatDiffers_FindsNothing(string reference)
        {
            // arrange
            var subject = Read(MhtmlShapes.RelativeParts);

            // act
            var testResult = subject.TryResolve(new Uri(reference, UriKind.Relative), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryResolve_AbsoluteReferenceToARelativeLabel_FindsThePart()
        {
            // arrange
            var subject = Read(MhtmlShapes.RelativeParts);

            // act
            var testResult = subject.TryResolve(new Uri("http://example.org/a/img/x.png"), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be("IMG");
        }

        [Fact]
        public void TryResolve_FragmentOnly_FindsARootWithAnAbsoluteLabel()
        {
            // arrange
            var subject = Read(MhtmlShapes.AlternativeRoot);

            // act
            var testResult = subject.TryResolve(new Uri("#top", UriKind.Relative), out var resource);

            // assert
            testResult.Should().BeTrue();
            resource.Entity.Should().BeSameAs(subject.Root);
        }

        [Fact]
        public void TryResolve_CallerBase_ResolvesThePartsAgainstIt()
        {
            // arrange
            var message = EntityTrees.Read(MhtmlShapes.RelativePartsNoBase, new ImfOptions { AllowMissingRequiredFields = true });
            var subject = new MhtmlDocument(message, new Uri("http://caller.example/a/b/"));

            // act
            var testResult = subject.TryResolve(new Uri("http://caller.example/a/img/x.png"), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be("IMG");
        }

        [Fact]
        public void TryResolve_MessageHasABaseAndSoDoesTheCaller_UsesThatOfTheMessage()
        {
            // arrange
            var message = EntityTrees.Read(MhtmlShapes.RelativeParts, new ImfOptions { AllowMissingRequiredFields = true });
            var subject = new MhtmlDocument(message, new Uri("http://caller.example/"));

            // act
            var testResult = subject.TryResolve(new Uri("http://example.org/a/b/page.html"), out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void TryResolve_AggregateLocation_IsNotAPart()
        {
            // arrange
            var subject = Read(MhtmlShapes.RelativeParts);

            // act
            var testResult = subject.TryResolve(new Uri("http://example.org/a/b/"), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        // ref. RFC 2557 §9.6: "will be resolved with the image in the next body part".
        [InlineData(Logo, true)]
        // ref. RFC 2557 §9.6: "will be resolved with the nested text/html body part below".
        [InlineData(MoreInfo, true)]
        // ref. RFC 2557 §9.6: "a reference from an outside body part to an inside body part, which is not supported".
        [InlineData(Logo2e, false)]
        public void TryResolve_Rfc2557Section96FromTheOuterRoot_ReachesOnlyItsOwnStructure(string reference, bool expected)
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section96);

            // act
            var testResult = subject.TryResolve(new Uri(reference), out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        // ref. RFC 2557 §9.6: "resolved with the image in the surrounding multipart/related above".
        [InlineData(MoreInfoRoot, "images/ietflogo.gif", true)]
        // ref. RFC 2557 §9.6: "resolved with the image inside the current nested multipart/related".
        [InlineData(MoreInfoRoot, RelativeLogo2e, true)]
        [InlineData(EvenMoreInfoRoot, RelativeLogo2d, true)]
        // ref. RFC 2557 §9.6: "references between parallel multipart/related structures are not supported".
        [InlineData(EvenMoreInfoRoot, RelativeLogo2e, false)]
        [InlineData(MoreInfoRoot, RelativeLogo2d, false)]
        public void TryResolve_Rfc2557Section96FromANestedPart_ReachesItsOwnAndSurroundingStructures(int referrer, string reference, bool expected)
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section96);

            // act
            var testResult = subject.TryResolve(new Uri(reference, UriKind.Relative), subject.Resources[referrer], out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryResolve_ReferrerFromAnotherDocument_Throws()
        {
            // arrange
            var subject = Read(MhtmlShapes.Rfc2557Section92);
            var referrer = Read(MhtmlShapes.Rfc2557Section92).Resources[0];

            // act
            Action testAction = () => subject.TryResolve(new Uri(Logo), referrer, out _);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(InvalidStructures))]
        public void Constructor_InvalidStructure_Throws(string document)
        {
            // arrange
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true });

            // act
            Action testAction = () => new MhtmlDocument(message);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
        }

        [Theory]
        [MemberData(nameof(InvalidStructures))]
        public void Constructor_InvalidStructureAllowed_RecordsADiagnosticAndUsesTheFirstPart(string document)
        {
            // arrange
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true });

            // act
            var testResult = new MhtmlDocument(message);

            // assert
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidRelatedStructure);
            EntityTrees.ContentOf(testResult.Root).Should().Be("ROOT");
        }

        [Fact]
        public void TryResolve_DuplicateAllowed_FindsTheFirst()
        {
            // arrange
            var document = MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: http://example.org/page.html\r\n");
            var subject = new MhtmlDocument(EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidRelatedStructure = true }));

            // act
            var testResult = subject.TryResolve(new Uri("http://example.org/page.html"), out var resource);

            // assert
            testResult.Should().BeTrue();
            EntityTrees.ContentOf(resource.Entity).Should().Be("ROOT");
        }

        [Fact]
        public void Constructor_SecondContentLocation_Throws()
        {
            // arrange
            var document = MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: a\r\nContent-Location: b\r\n");
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true });

            // act
            Action testAction = () => new MhtmlDocument(message);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.CardinalityViolation);
        }

        [Fact]
        public void Constructor_SecondContentLocationAllowed_UsesTheFirst()
        {
            // arrange
            var document = MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: a\r\nContent-Location: b\r\n");
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true, AllowCardinalityViolations = true });

            // act
            var testResult = new MhtmlDocument(message);

            // assert
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.CardinalityViolation);
            testResult.Resources[1].Uri.OriginalString.Should().Be("thismessage:/a");
        }

        [Fact]
        public void Constructor_InvalidContentLocation_Throws()
        {
            // arrange
            var document = MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: <a>\r\n");
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true });

            // act
            Action testAction = () => new MhtmlDocument(message);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidFieldValue);
        }

        [Fact]
        public void Constructor_InvalidContentLocationAllowed_HasNoUri()
        {
            // arrange
            var document = MhtmlShapes.Related(TypeHtml, "Content-Type: image/png\r\nContent-Location: <a>\r\n");
            var message = EntityTrees.Read(document, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidValues = true });

            // act
            var testResult = new MhtmlDocument(message);

            // assert
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidFieldValue);
            testResult.Resources[1].Uri.Should().BeNull();
        }

        [Fact]
        public void Constructor_RelativeBase_Throws()
        {
            // arrange
            var message = EntityTrees.Read(MimeShapes.TextOnly);

            // act
            Action testAction = () => new MhtmlDocument(message, new Uri("a", UriKind.Relative));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullMessage_Throws()
        {
            // act
            Action testAction = () => new MhtmlDocument(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
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
