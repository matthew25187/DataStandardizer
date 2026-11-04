namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Aggregate documents for the tests of MHTML: the examples of RFC 2557 §9, and hand-written files in the shapes that browsers and Word produce.
    /// </summary>
    /// <remarks>
    /// The examples give identifiers such as <c>foo3@foo1@bar.net</c>, which RFC 5322 §3.6.4 doesn't allow, and §9 says that they are only illustrative.  So they use
    /// <c>foo3@foo1.bar.net</c> here, except in <see cref="Rfc2557Section92Verbatim"/>.
    /// </remarks>
    internal static class MhtmlShapes
    {
        /// <summary>The fields that start each example of RFC 2557 §9, which have no Date.</summary>
        public const string ExampleHeader = "From: foo1@bar.net\r\nTo: foo2@bar.net\r\nSubject: A simple example\r\nMime-Version: 1.0\r\n";

        public const string Gif = "R0lGODlh\r\n";

        /// <summary>RFC 2557 §9.1: one HTML body, with no linked objects.</summary>
        public const string Rfc2557Section91 =
            ExampleHeader +
            "Content-Type: text/html; charset=\"iso-8859-1\"\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n" +
            "\r\n" +
            "<HTML><body>E with acute accent becomes \x00C9.</body></HTML>\r\n";

        /// <summary>RFC 2557 §9.2: an absolute URI to an embedded picture.</summary>
        public const string Rfc2557Section92 =
            ExampleHeader +
            "Content-Type: multipart/related; boundary=\"boundary-example\";\r\n" +
            "        type=\"text/html\"; start=\"<foo3@foo1.bar.net>\"\r\n" +
            "\r\n" +
            "--boundary-example\r\n" +
            "Content-Type: text/html;charset=\"US-ASCII\"\r\n" +
            "Content-ID: <foo3@foo1.bar.net>\r\n" +
            "\r\n" +
            "<IMG SRC=\"http://www.ietf.cnri.reston.va.us/images/ietflogo.gif\" ALT=\"IETF logo\">\r\n" +
            "--boundary-example\r\n" +
            "Content-Location:\r\n" +
            "   http://www.ietf.cnri.reston.va.us/images/ietflogo.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example--\r\n";

        /// <summary>RFC 2557 §9.2 with its identifier as printed, which is not a valid msg-id, so <c>start</c> names no part.</summary>
        public const string Rfc2557Section92Verbatim =
            ExampleHeader +
            "Content-Type: multipart/related; boundary=\"boundary-example\";\r\n" +
            "        type=\"text/html\"; start=\"<foo3@foo1@bar.net>\"\r\n" +
            "\r\n" +
            "--boundary-example\r\n" +
            "Content-Type: text/html;charset=\"US-ASCII\"\r\n" +
            "Content-ID: <foo3@foo1@bar.net>\r\n" +
            "\r\n" +
            "<p>root</p>\r\n" +
            "--boundary-example\r\n" +
            "Content-Location: http://www.ietf.cnri.reston.va.us/images/ietflogo.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example--\r\n";

        /// <summary>RFC 2557 §9.3: a Content-Location in the outermost heading is the base of relative URIs.</summary>
        public const string Rfc2557Section93 =
            ExampleHeader +
            "Content-Location: http://www.ietf.cnri.reston.va.us/\r\n" +
            "Content-Type: multipart/related; boundary=\"boundary-example\";\r\n" +
            "        type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example\r\n" +
            "Content-Type: text/html; charset=\"ISO-8859-1\"\r\n" +
            "Content-Transfer-Encoding: QUOTED-PRINTABLE\r\n" +
            "\r\n" +
            "<IMG SRC=\"images/ietflogo1.gif\" ALT=\"IETF logo1\">\r\n" +
            "--boundary-example\r\n" +
            "Content-Location:\r\n" +
            "         http://www.ietf.cnri.reston.va.us/images/ietflogo1.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            "logo1\r\n" +
            "--boundary-example\r\n" +
            "Content-Location: images/ietflogo2.gif\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            "logo2\r\n" +
            "--boundary-example\r\n" +
            "Content-Location:\r\n" +
            "         http://www.ietf.cnri.reston.va.us/images/ietflogo3.gif\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            "logo3\r\n" +
            "--boundary-example--\r\n";

        /// <summary>RFC 2557 §9.4: a relative URI with no base available.</summary>
        public const string Rfc2557Section94 =
            ExampleHeader +
            "Content-Type: multipart/related; boundary=\"boundary-example\";\r\n" +
            "        type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example\r\n" +
            "Content-Type: text/html; charset=\"iso-8859-1\"\r\n" +
            "Content-Transfer-Encoding: QUOTED-PRINTABLE\r\n" +
            "\r\n" +
            "<IMG SRC=\"ietflogo.gif\" ALT=\"IETF logo\">\r\n" +
            "--boundary-example\r\n" +
            "Content-Location: ietflogo.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example--\r\n";

        /// <summary>RFC 2557 §9.5: a CID URL, with a Content-Location that is disregarded.</summary>
        public const string Rfc2557Section95 =
            ExampleHeader +
            "Content-Type: multipart/related; boundary=\"boundary-example\";\r\n" +
            "        type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example\r\n" +
            "Content-Type: text/html; charset=\"US-ASCII\"\r\n" +
            "\r\n" +
            "<IMG SRC=\"cid:foo4@foo1.bar.net\" ALT=\"IETF logo\">\r\n" +
            "--boundary-example\r\n" +
            "Content-Location: CID:something@else ; this header is disregarded\r\n" +
            "Content-ID: <foo4@foo1.bar.net>\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example--\r\n";

        /// <summary>
        /// RFC 2557 §9.6: references between nested body parts.  The example labels the nested pictures <c>http:images/ietflogo2.gif</c> and <c>http:images/ietflogo2d.gif</c> while its
        /// text refers to <c>images/ietflogo2e.gif</c> and <c>images/ietflogo2d.gif</c>, and which isn't an HTTP URL (RFC 1738 §3.3), so absolute labels are used here to show what the text describes.
        /// </summary>
        public const string Rfc2557Section96 =
            ExampleHeader +
            "Content-Type: multipart/related; boundary=\"boundary-example-1\";\r\n" +
            "          type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example-1\r\n" +
            "Content-Type: text/html;charset=\"US-ASCII\"\r\n" +
            "Content-ID: <foo3@foo1.bar.net>\r\n" +
            "\r\n" +
            "outer root\r\n" +
            "--boundary-example-1\r\n" +
            "Content-Location:\r\n" +
            "         http://www.ietf.cnri.reston.va.us/images/ietflogo.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example-1\r\n" +
            "Content-Location:\r\n" +
            "     http://www.ietf.cnri.reston.va.us/more-info\r\n" +
            "Content-Type: multipart/related; boundary=\"boundary-example-2\";\r\n" +
            "           type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example-2\r\n" +
            "Content-Type: text/html;charset=\"US-ASCII\"\r\n" +
            "Content-ID: <foo4@foo1.bar.net>\r\n" +
            "\r\n" +
            "more info\r\n" +
            "--boundary-example-2\r\n" +
            "Content-Location: http://www.ietf.cnri.reston.va.us/images/ietflogo2e.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example-2--\r\n" +
            "\r\n" +
            "--boundary-example-1\r\n" +
            "Content-Location:\r\n" +
            "           http://www.ietf.cnri.reston.va.us/even-more-info\r\n" +
            "Content-Type: multipart/related; boundary=\"boundary-example-3\";\r\n" +
            "           type=\"text/html\"\r\n" +
            "\r\n" +
            "--boundary-example-3\r\n" +
            "Content-Type: text/html;charset=\"US-ASCII\"\r\n" +
            "Content-ID: <4@foo.bar.net>\r\n" +
            "\r\n" +
            "even more info\r\n" +
            "--boundary-example-3\r\n" +
            "Content-Location: http://www.ietf.cnri.reston.va.us/images/ietflogo2d.gif\r\n" +
            "Content-Type: IMAGE/GIF\r\n" +
            "Content-Transfer-Encoding: BASE64\r\n" +
            "\r\n" +
            Gif +
            "--boundary-example-3--\r\n" +
            "\r\n" +
            "--boundary-example-1--\r\n";

        /// <summary>What Chromium saves: a From field that is not an address, Snapshot-Content-Location, and absolute locations.</summary>
        public const string Chromium =
            "From: <Saved by Blink>\r\n" +
            "Snapshot-Content-Location: https://example.org/page.html\r\n" +
            "Subject: Example\r\n" +
            "Date: Wed, 4 Nov 2026 10:00:00 +1300\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/related;\r\n" +
            "\ttype=\"text/html\";\r\n" +
            "\tboundary=\"----MultipartBoundary--abc----\"\r\n" +
            "\r\n" +
            "\r\n" +
            "------MultipartBoundary--abc----\r\n" +
            "Content-Type: text/html\r\n" +
            "Content-ID: <frame-1@mhtml.blink>\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "Content-Location: https://example.org/page.html\r\n" +
            "\r\n" +
            "<html>page</html>\r\n" +
            "------MultipartBoundary--abc----\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "Content-Location: https://example.org/logo.png\r\n" +
            "\r\n" +
            "iVBORw==\r\n" +
            "------MultipartBoundary--abc------\r\n";

        /// <summary>
        /// What Word saves as a web page, with the structure of a file saved from Word: no Date or From, no <c>type</c> parameter, a preamble, <c>file:</c> locations, HTML in
        /// <c>windows-1252</c>, and supporting files as parts.
        /// </summary>
        public const string Word =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/related; boundary=\"----=_NextPart_01C1BFB8.D3BA6F40\"\r\n" +
            "\r\n" +
            "This document is a Single File Web Page, also known as a Web Archive file.\r\n" +
            "\r\n" +
            "------=_NextPart_01C1BFB8.D3BA6F40\r\n" +
            "Content-Location: file:///C:/page.htm\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "Content-Type: text/html; charset=\"windows-1252\"\r\n" +
            "\r\n" +
            "<html>caf=E9</html>\r\n" +
            "------=_NextPart_01C1BFB8.D3BA6F40\r\n" +
            "Content-Location: file:///C:/page_files/themedata.thmx\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "Content-Type: application/vnd.ms-officetheme\r\n" +
            "\r\n" +
            "UEsDBA==\r\n" +
            "------=_NextPart_01C1BFB8.D3BA6F40\r\n" +
            "Content-Location: file:///C:/page_files/filelist.xml\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "Content-Type: text/xml; charset=\"utf-8\"\r\n" +
            "\r\n" +
            "<xml/>\r\n" +
            "------=_NextPart_01C1BFB8.D3BA6F40--\r\n";

        /// <summary>A root that is <c>multipart/alternative</c>, for which <c>type</c> gives that type (RFC 2557 §7).</summary>
        public const string AlternativeRoot =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/related; boundary=rel; type=\"multipart/alternative\"\r\n" +
            "\r\n" +
            "--rel\r\n" +
            "Content-Type: multipart/alternative; boundary=alt\r\n" +
            "\r\n" +
            "--alt\r\n" +
            "Content-Type: text/plain\r\n" +
            "\r\n" +
            "plain\r\n" +
            "--alt\r\n" +
            "Content-Type: text/html\r\n" +
            "Content-Location: http://example.org/page.html\r\n" +
            "\r\n" +
            "<p>html</p>\r\n" +
            "--alt--\r\n" +
            "\r\n" +
            "--rel\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-Location: http://example.org/logo.png\r\n" +
            "\r\n" +
            "png\r\n" +
            "--rel--\r\n";

        private const string RelativePartsBody =
            "Content-Type: multipart/related; boundary=rel; type=\"text/html\"\r\n" +
            "\r\n" +
            "--rel\r\n" +
            "Content-Type: text/html\r\n" +
            "Content-Location: page.html\r\n" +
            "\r\n" +
            "ROOT\r\n" +
            "--rel\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-Location: ../img/x.png\r\n" +
            "\r\n" +
            "IMG\r\n" +
            "--rel\r\n" +
            "Content-Type: text/plain\r\n" +
            "Content-Location: data?id=1\r\n" +
            "\r\n" +
            "DATA\r\n" +
            "--rel\r\n" +
            "Content-Type: text/html\r\n" +
            "Content-Location: sec.html\r\n" +
            "\r\n" +
            "SEC\r\n" +
            "--rel\r\n" +
            "Content-Type: text/plain\r\n" +
            "Content-Location: a%2eb\r\n" +
            "\r\n" +
            "ESCAPED\r\n" +
            "--rel--\r\n";

        /// <summary>Parts with relative locations, and an aggregate with a Content-Location that is their base.</summary>
        public const string RelativeParts = "MIME-Version: 1.0\r\nContent-Location: http://example.org/a/b/\r\n" + RelativePartsBody;

        /// <summary>Parts with relative locations and nothing to resolve them against but what the caller gives.</summary>
        public const string RelativePartsNoBase = "MIME-Version: 1.0\r\n" + RelativePartsBody;

        /// <summary>A multipart/related entity with a list of fields for its top level, and two parts, of which the second has fields of its own choosing.</summary>
        public static string Related(string contentTypeParameters, string secondPartFields)
        {
            return "MIME-Version: 1.0\r\n" +
                "Content-Type: multipart/related; boundary=rel" + contentTypeParameters + "\r\n" +
                "\r\n" +
                "--rel\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-ID: <root@b.test>\r\n" +
                "Content-Location: http://example.org/page.html\r\n" +
                "\r\n" +
                "ROOT\r\n" +
                "--rel\r\n" +
                secondPartFields +
                "\r\n" +
                "SECOND\r\n" +
                "--rel--\r\n";
        }
    }
}
