using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Messages that are valid under the default options, each with what an <see cref="ImfEntityReader"/> gives for it.  See <see cref="EntityEvents"/>
    /// for the form of the expected lines.
    /// </summary>
    internal static class EntityMessages
    {
        // ref. RFC 2046 §5.1.1: the example of a multipart message, with its text as the RFC gives it.
        public const string Rfc2046SimpleBoundaryExample =
            "From: Nathaniel Borenstein <nsb@bellcore.com>\r\n" +
            "To: Ned Freed <ned@innosoft.com>\r\n" +
            "Date: Sun, 21 Mar 1993 23:56:48 -0800 (PST)\r\n" +
            "Subject: Sample message\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-type: multipart/mixed; boundary=\"simple boundary\"\r\n" +
            "\r\n" +
            "This is the preamble.  It is to be ignored, though it\r\n" +
            "is a handy place for composition agents to include an\r\n" +
            "explanatory note to non-MIME conformant readers.\r\n" +
            "\r\n" +
            "--simple boundary\r\n" +
            "\r\n" +
            "This is implicitly typed plain US-ASCII text.\r\n" +
            "It does NOT end with a linebreak.\r\n" +
            "--simple boundary\r\n" +
            "Content-type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "This is explicitly typed plain US-ASCII text.\r\n" +
            "It DOES end with a linebreak.\r\n" +
            "\r\n" +
            "--simple boundary--\r\n" +
            "\r\n" +
            "This is the epilogue.  It is also to be ignored.\r\n";

        private static readonly string[] Rfc2046SimpleBoundaryEvents =
        {
            "start /",
            "field / From",
            "field / To",
            "field / Date",
            "field / Subject",
            "field / MIME-Version",
            "field / Content-type",
            "type / multipart/mixed",
            "preamble / [This is the preamble.  It is to be ignored, though it\r\n" +
            "is a handy place for composition agents to include an\r\n" +
            "explanatory note to non-MIME conformant readers.\r\n]",
            "start 1",
            "type 1 text/plain",
            "body 1 [This is implicitly typed plain US-ASCII text.\r\nIt does NOT end with a linebreak.]",
            "end 1",
            "start 2",
            "field 2 Content-type",
            "type 2 text/plain",
            "body 2 [This is explicitly typed plain US-ASCII text.\r\nIt DOES end with a linebreak.\r\n]",
            "end 2",
            "epilogue / [\r\nThis is the epilogue.  It is also to be ignored.\r\n]",
            "end /",
        };

        // ref. RFC 2049 Appendix A: the outline of a complex multipart message, with its text as the RFC gives it.
        public const string Rfc2049ComplexExample =
            "MIME-Version: 1.0\r\n" +
            "From: Nathaniel Borenstein <nsb@nsb.fv.com>\r\n" +
            "To: Ned Freed <ned@innosoft.com>\r\n" +
            "Date: Fri, 07 Oct 1994 16:15:05 -0700 (PDT)\r\n" +
            "Subject: A multipart example\r\n" +
            "Content-Type: multipart/mixed;\r\n" +
            "              boundary=unique-boundary-1\r\n" +
            "\r\n" +
            "This is the preamble area of a multipart message.\r\n" +
            "Mail readers that understand multipart format\r\n" +
            "should ignore this preamble.\r\n" +
            "\r\n" +
            "If you are reading this text, you might want to\r\n" +
            "consider changing to a mail reader that understands\r\n" +
            "how to properly display multipart messages.\r\n" +
            "\r\n" +
            "--unique-boundary-1\r\n" +
            "\r\n" +
            "  ... Some text appears here ...\r\n" +
            "\r\n" +
            "[Note that the blank between the boundary and the start\r\n" +
            " of the text in this part means no header fields were\r\n" +
            " given and this is text in the US-ASCII character set.\r\n" +
            " It could have been done with explicit typing as in the\r\n" +
            " next part.]\r\n" +
            "\r\n" +
            "--unique-boundary-1\r\n" +
            "Content-type: text/plain; charset=US-ASCII\r\n" +
            "\r\n" +
            "This could have been part of the previous part, but\r\n" +
            "illustrates explicit versus implicit typing of body\r\n" +
            "parts.\r\n" +
            "\r\n" +
            "--unique-boundary-1\r\n" +
            "Content-Type: multipart/parallel; boundary=unique-boundary-2\r\n" +
            "\r\n" +
            "--unique-boundary-2\r\n" +
            "Content-Type: audio/basic\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "  ... base64-encoded 8000 Hz single-channel\r\n" +
            "      mu-law-format audio data goes here ...\r\n" +
            "\r\n" +
            "--unique-boundary-2\r\n" +
            "Content-Type: image/jpeg\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "  ... base64-encoded image data goes here ...\r\n" +
            "\r\n" +
            "--unique-boundary-2--\r\n" +
            "\r\n" +
            "--unique-boundary-1\r\n" +
            "Content-type: text/enriched\r\n" +
            "\r\n" +
            "This is <bold><italic>enriched.</italic></bold>\r\n" +
            "<smaller>as defined in RFC 1896</smaller>\r\n" +
            "\r\n" +
            "Isn't it\r\n" +
            "<bigger><bigger>cool?</bigger></bigger>\r\n" +
            "\r\n" +
            "--unique-boundary-1\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "\r\n" +
            "From: (mailbox in US-ASCII)\r\n" +
            "To: (address in US-ASCII)\r\n" +
            "Subject: (subject in US-ASCII)\r\n" +
            "Content-Type: Text/plain; charset=ISO-8859-1\r\n" +
            "Content-Transfer-Encoding: Quoted-printable\r\n" +
            "\r\n" +
            "  ... Additional text in ISO-8859-1 goes here ...\r\n" +
            "\r\n" +
            "--unique-boundary-1--\r\n";

        private static readonly string[] Rfc2049ComplexEvents =
        {
            "start /",
            "field / MIME-Version",
            "field / From",
            "field / To",
            "field / Date",
            "field / Subject",
            "field / Content-Type",
            "type / multipart/mixed",
            "preamble / [This is the preamble area of a multipart message.\r\n" +
            "Mail readers that understand multipart format\r\n" +
            "should ignore this preamble.\r\n" +
            "\r\n" +
            "If you are reading this text, you might want to\r\n" +
            "consider changing to a mail reader that understands\r\n" +
            "how to properly display multipart messages.\r\n]",
            "start 1",
            "type 1 text/plain",
            "body 1 [  ... Some text appears here ...\r\n" +
            "\r\n" +
            "[Note that the blank between the boundary and the start\r\n" +
            " of the text in this part means no header fields were\r\n" +
            " given and this is text in the US-ASCII character set.\r\n" +
            " It could have been done with explicit typing as in the\r\n" +
            " next part.]\r\n]",
            "end 1",
            "start 2",
            "field 2 Content-type",
            "type 2 text/plain",
            "body 2 [This could have been part of the previous part, but\r\n" +
            "illustrates explicit versus implicit typing of body\r\n" +
            "parts.\r\n]",
            "end 2",
            "start 3",
            "field 3 Content-Type",
            "type 3 multipart/parallel",
            "start 3.1",
            "field 3.1 Content-Type",
            "field 3.1 Content-Transfer-Encoding",
            "type 3.1 audio/basic",
            "body 3.1 [  ... base64-encoded 8000 Hz single-channel\r\n      mu-law-format audio data goes here ...\r\n]",
            "end 3.1",
            "start 3.2",
            "field 3.2 Content-Type",
            "field 3.2 Content-Transfer-Encoding",
            "type 3.2 image/jpeg",
            "body 3.2 [  ... base64-encoded image data goes here ...\r\n]",
            "end 3.2",
            "end 3",
            "start 4",
            "field 4 Content-type",
            "type 4 text/enriched",
            "body 4 [This is <bold><italic>enriched.</italic></bold>\r\n" +
            "<smaller>as defined in RFC 1896</smaller>\r\n" +
            "\r\n" +
            "Isn't it\r\n" +
            "<bigger><bigger>cool?</bigger></bigger>\r\n]",
            "end 4",
            "start 5",
            "field 5 Content-Type",
            "type 5 message/rfc822",
            "start 5",
            "field 5 From",
            "field 5 To",
            "field 5 Subject",
            "field 5 Content-Type",
            "field 5 Content-Transfer-Encoding",
            "type 5 Text/plain",
            "body 5 [  ... Additional text in ISO-8859-1 goes here ...\r\n]",
            "end 5",
            "end 5",
            "end /",
        };

        public static TheoryData<string, string[]> Valid => new TheoryData<string, string[]>
        {
            { Rfc2046SimpleBoundaryExample, Rfc2046SimpleBoundaryEvents },
            { Rfc2049ComplexExample, Rfc2049ComplexEvents },
            {
                // ref. RFC 5322 §3.5: a message may have no body, and no empty line.
                "A:1\r\n",
                new[] { "start /", "field / A", "type / text/plain", "end /" }
            },
            {
                string.Empty,
                new[] { "start /", "type / text/plain", "end /" }
            },
            {
                "A:1\r\n\r\nhello\r\nworld",
                new[] { "start /", "field / A", "type / text/plain", "body / [hello\r\nworld]", "end /" }
            },
            {
                // Without a multipart entity, nothing in a body is a delimiter.
                "A:1\r\n\r\n--b\r\n\r\n--b--\r\n",
                new[] { "start /", "field / A", "type / text/plain", "body / [--b\r\n\r\n--b--\r\n]", "end /" }
            },
            {
                // ref. RFC 2045 §5: the names of the fields are not case-sensitive, nor are the type and subtype.
                "CONTENT-TYPE: MULTIPART/MIXED; BOUNDARY=b\r\n\r\n--b\r\n\r\nx\r\n--b--",
                new[] { "start /", "field / CONTENT-TYPE", "type / MULTIPART/MIXED", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // ref. RFC 2045 §5.1: comments, as in its example, and white space around a delimiter.
                "Content-Type: multipart/mixed (a (nested) comment) ; (c) boundary = b (Plain \\) text)\r\n\r\n--b\r\n\r\nx\r\n--b--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // A comment and white space around the slash, which RFC 822 allows between any two tokens.
                "Content-Type: multipart (c) / (d) mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // A parenthesis in a quoted string is not a comment.
                "Content-Type: multipart/mixed; boundary=\"(b)\"\r\n\r\n--(b)\r\n\r\nx\r\n--(b)--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // ref. RFC 2046 §5.1.1: receivers must handle transport padding, on either kind of delimiter.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b \t\r\n\r\nx\r\n--b  \r\n\r\ny\r\n--b-- \t \r\nz",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x]", "end 1",
                    "start 2", "type 2 text/plain", "body 2 [y]", "end 2",
                    "epilogue / [z]", "end /"
                }
            },
            {
                // A boundary with no line break before it is content, as is a line that only starts like a delimiter.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx --b\r\n-b\r\n--c\r\n--bc\r\n--b-\r\n--b--x\r\n-\r\n--\r\n\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x --b\r\n-b\r\n--c\r\n--bc\r\n--b-\r\n--b--x\r\n-\r\n--\r\n]", "end 1",
                    "end /"
                }
            },
            {
                // A boundary that ends in a hyphen, which is not its own close delimiter.
                "Content-Type: multipart/mixed; boundary=b--\r\n\r\n--b--\r\n\r\nx\r\n--b----",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // ref. RFC 2046 §5.1.1: boundary := 0*69<bchars> bcharsnospace, with every character that isn't a letter or digit.
                "Content-Type: multipart/mixed; boundary=\"'()+_,-./:=? z\"\r\n\r\n--'()+_,-./:=? z\r\n\r\nx\r\n--'()+_,-./:=? z--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // ref. RFC 2046 §5.1.1: the longest boundary, of 70 characters.
                "Content-Type: multipart/mixed; boundary=1234567890123456789012345678901234567890123456789012345678901234567890\r\n\r\n" +
                "--1234567890123456789012345678901234567890123456789012345678901234567890\r\n\r\nx\r\n" +
                "--1234567890123456789012345678901234567890123456789012345678901234567890--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // The empty line before a delimiter shares its line break with the delimiter, so the parts have no body.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\n--b\r\n\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 A", "type 1 text/plain", "end 1",
                    "start 2", "type 2 text/plain", "end 2",
                    "end /"
                }
            },
            {
                // The line break before the delimiter is not part of a body that is otherwise empty, so there is nothing to give.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\n\r\n--b--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "end 1", "end /" }
            },
            {
                // ref. RFC 2046 §5.1.1: multipart-body := [preamble CRLF] dash-boundary, so an empty preamble, which gives no event, can have its line break.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n\r\n--b\r\n\r\nx\r\n--b--",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /" }
            },
            {
                // An empty line after the close delimiter's line break is an empty line of the epilogue.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\n\r\n",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "epilogue / [\r\n]", "end /" }
            },
            {
                // In an epilogue, the boundary of the entity that has closed is content.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\n--b\r\n",
                new[] { "start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "epilogue / [--b\r\n]", "end /" }
            },
            {
                // Nested, the outer boundary a prefix of the inner one.
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: multipart/mixed; boundary=b1\r\n\r\n--b1\r\n\r\nx\r\n--b1\r\n\r\ny\r\n--b1--\r\n--b\r\n\r\nz\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "start 1.2", "type 1.2 text/plain", "body 1.2 [y]", "end 1.2",
                    "end 1",
                    "start 2", "type 2 text/plain", "body 2 [z]", "end 2",
                    "end /"
                }
            },
            {
                // Nested, the inner boundary a prefix of the outer one.
                "Content-Type: multipart/mixed; boundary=b1\r\n\r\n--b1\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b\r\n\r\ny\r\n--b--\r\n--b1\r\n\r\nz\r\n--b1--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "start 1.2", "type 1.2 text/plain", "body 1.2 [y]", "end 1.2",
                    "end 1",
                    "start 2", "type 2 text/plain", "body 2 [z]", "end 2",
                    "end /"
                }
            },
            {
                // An epilogue of an inner entity, which an outer delimiter ends.
                "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\ne\r\n\r\n--a--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "epilogue 1 [e\r\n]", "end 1",
                    "end /"
                }
            },
            {
                // A message/rfc822 part in a multipart/mixed entity, whose message is a multipart entity with the same boundary rules.
                "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: message/rfc822\r\n\r\nSubject: s\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--\r\n--a--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 message/rfc822",
                    "start 1", "field 1 Subject", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "end 1",
                    "end 1",
                    "end /"
                }
            },
            {
                // An embedded message that ends at the empty line, because of a delimiter.
                "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: message/rfc822\r\n\r\n--a--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 message/rfc822", "end 1",
                    "end /"
                }
            },
            {
                // ref. RFC 3501 §6.4.5: the body of a message that is not multipart is its part 1.
                "Content-Type: message/rfc822\r\n\r\nContent-Type: message/rfc822\r\n\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / message/rfc822",
                    "start 1", "field 1 Content-Type", "type 1 message/rfc822",
                    "start 1.1", "field 1.1 Content-Type", "type 1.1 multipart/mixed",
                    "start 1.1.1", "type 1.1.1 text/plain", "body 1.1.1 [x]", "end 1.1.1",
                    "end 1.1",
                    "end 1",
                    "end /"
                }
            },
            {
                "Content-Type: message/rfc822\r\n",
                new[] { "start /", "field / Content-Type", "type / message/rfc822", "end /" }
            },
            {
                // ref. RFC 6532 §3.7: message/global holds a message.
                "Content-Type: message/global\r\nContent-Transfer-Encoding: 8bit\r\n\r\nA:1\r\n\r\nx",
                new[]
                {
                    "start /", "field / Content-Type", "field / Content-Transfer-Encoding", "type / message/global",
                    "start 1", "field 1 A", "type 1 text/plain", "body 1 [x]", "end 1",
                    "end /"
                }
            },
            {
                // ref. RFC 6532 §3.7: message/global may have any transfer encoding, and then can't be read as a nested entity.
                "Content-Type: message/global\r\nContent-Transfer-Encoding: base64\r\n\r\nQTox",
                new[] { "start /", "field / Content-Type", "field / Content-Transfer-Encoding", "type / message/global", "body / [QTox]", "end /" }
            },
            {
                // ref. RFC 2046 §5.2.2: a fragment after the first has no header.
                "Content-Type: message/partial; id=\"x\"; number=2; total=2\r\n\r\nnot a header\r\n",
                new[] { "start /", "field / Content-Type", "type / message/partial", "body / [not a header\r\n]", "end /" }
            },
            {
                // ref. RFC 2046 §5.2.3: the body of an external reference is not followed.
                "Content-Type: message/external-body; access-type=local-file; name=f\r\n\r\nA:1\r\n\r\n",
                new[] { "start /", "field / Content-Type", "type / message/external-body", "body / [A:1\r\n\r\n]", "end /" }
            },
            {
                // ref. RFC 2046 §5.2.4: an unrecognized subtype of message is treated as application/octet-stream.
                "Content-Type: message/x-other\r\n\r\nnot a header\r\n",
                new[] { "start /", "field / Content-Type", "type / message/x-other", "body / [not a header\r\n]", "end /" }
            },
            {
                // ref. RFC 2046 §5.1.5: in a digest, a part is a message unless it says otherwise.
                "Content-Type: multipart/digest; boundary=b\r\n\r\n--b\r\n\r\nA:1\r\n\r\nx\r\n--b\r\nContent-Type: text/plain\r\n\r\ny\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/digest",
                    "start 1", "type 1 message/rfc822",
                    "start 1", "field 1 A", "type 1 text/plain", "body 1 [x]", "end 1",
                    "end 1",
                    "start 2", "field 2 Content-Type", "type 2 text/plain", "body 2 [y]", "end 2",
                    "end /"
                }
            },
            {
                // ref. RFC 2046 §5.1.5: the default applies to the parts of the digest only, not to what they hold.
                "Content-Type: multipart/digest; boundary=b\r\n\r\n--b\r\nContent-Type: multipart/mixed; boundary=c\r\n\r\n--c\r\n\r\nx\r\n--c--\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/digest",
                    "start 1", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "end 1",
                    "end /"
                }
            },
            {
                // ref. RFC 2045 §6.4: the identity encodings are allowed on a composite entity, and comments are allowed in the field.
                "Content-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: 8BIT (raw)\r\n\r\n--b\r\nContent-Transfer-Encoding: binary\r\n\r\n\xFF\r\n--b--",
                new[]
                {
                    "start /", "field / Content-Type", "field / Content-Transfer-Encoding", "type / multipart/mixed",
                    "start 1", "field 1 Content-Transfer-Encoding", "type 1 text/plain", "body 1 [\xFF]", "end 1",
                    "end /"
                }
            },
        };
    }

    /// <summary>
    /// A message with nesting, a delimiter with padding and multi-byte body content, and the ways of splitting it across refills: a first read that ends
    /// after each of its bytes, with a block of several sizes.  Each case holds the length of the first read and the size of the block.
    /// </summary>
    internal sealed class SplitMessages : IEnumerable<object[]>
    {
        private const string MessageText =
            "Content-Type: multipart/mixed; boundary=b\r\n\r\npre\r\n--b \r\nA:1\r\n 2\r\n\r\nx\r\r\n--c\r\n" +
            "--b\r\nContent-Type: message/rfc822\r\n\r\nContent-Type: multipart/mixed; boundary=b1\r\n\r\n--b1\r\n\r\ny\r\n--b1--\r\ne\r\n--b--\r\npost";

        private static readonly int[] BufferSizes = { 1, 2, 3, 5, 8, 64, 4096 };

        public static readonly byte[] Message = Octets.Of(MessageText);

        public static readonly string[] ExpectedEvents =
        {
            "start /", "field / Content-Type", "type / multipart/mixed",
            "preamble / [pre]",
            "start 1", "field 1 A", "type 1 text/plain", "body 1 [x\r\r\n--c]", "end 1",
            "start 2", "field 2 Content-Type", "type 2 message/rfc822",
            "start 2", "field 2 Content-Type", "type 2 multipart/mixed",
            "start 2.1", "type 2.1 text/plain", "body 2.1 [y]", "end 2.1",
            "epilogue 2 [e]", "end 2",
            "end 2",
            "epilogue / [post]", "end /",
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var bufferSize in BufferSizes)
            {
                for (var firstReadLength = 1; firstReadLength <= Message.Length; firstReadLength++)
                {
                    yield return new object[] { firstReadLength, bufferSize };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
