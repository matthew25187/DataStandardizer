namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Messages that have to come back from a writer byte for byte.
    /// </summary>
    internal static class SignedMessages
    {
        // ref. RFC 1847 §2.1: a multipart/signed message, in which the signature covers the first part with its line breaks and odd spacing.
        public const string MultipartSigned =
            "From: Alice <alice@example.org>\r\n" +
            "To: Bob <bob@example.org>\r\n" +
            "Subject:   signed,\r\n" +
            "\twith a fold\r\n" +
            "DKIM-Signature: v=1; a=rsa-sha256;\r\n" +
            " d=example.org; b=abcd\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/signed; micalg=pgp-sha256;\r\n" +
            "  protocol=\"application/pgp-signature\";\r\n" +
            "  boundary=\"signed boundary\"\r\n" +
            "\r\n" +
            "A preamble.\r\n" +
            "--signed boundary\r\n" +
            "Content-Type:text/plain;charset=us-ascii\r\n" +
            "Content-Transfer-Encoding : 7bit\r\n" +
            "\r\n" +
            "The signed text, with a trailing space \r\n" +
            "and a line break.\r\n" +
            "\r\n" +
            "--signed boundary\r\n" +
            "Content-Type: application/pgp-signature\r\n" +
            "\r\n" +
            "-----BEGIN PGP SIGNATURE-----\r\n" +
            "abcd\r\n" +
            "-----END PGP SIGNATURE-----\r\n" +
            "--signed boundary--\r\n" +
            "An epilogue.\r\n";

        // ref. RFC 2046 §5.1.1: transport padding after a delimiter, which only a copy of the whole entity keeps.
        public const string MultipartSignedWithPadding =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/signed; micalg=pgp-sha256;\r\n" +
            "  protocol=\"application/pgp-signature\"; boundary=b\r\n" +
            "\r\n" +
            "--b   \r\n" +
            "\r\n" +
            "text\r\n" +
            "--b\t\r\n" +
            "Content-Type: application/pgp-signature\r\n" +
            "\r\n" +
            "abcd\r\n" +
            "--b-- \r\n";

        // ref. RFC 5322 Appendix A.1.1: a message from one person to another with simple addressing.
        public const string Rfc5322AppendixA11Fields =
            "From: John Doe <jdoe@machine.example>\r\n" +
            "To: Mary Smith <mary@example.net>\r\n" +
            "Subject: Saying Hello\r\n" +
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n" +
            "Message-ID: <1234@local.machine.example>\r\n";

        public const string Rfc5322AppendixA11 =
            Rfc5322AppendixA11Fields +
            "\r\n" +
            "This is a message just to say hello.\r\n" +
            "So, \"Hello\".\r\n";

        // ref. RFC 5322 Appendix A.1.2: different types of mailboxes.
        public const string Rfc5322AppendixA12 =
            "From: \"Joe Q. Public\" <john.q.public@example.com>\r\n" +
            "To: Mary Smith <mary@x.test>, jdoe@example.org, Who? <one@y.test>\r\n" +
            "Cc: <boss@nil.test>, \"Giant; \\\"Big\\\" Box\" <sysservices@example.net>\r\n" +
            "Date: Tue, 1 Jul 2003 10:52:37 +0200\r\n" +
            "Message-ID: <5678.21-Nov-1997@example.com>\r\n" +
            "\r\n" +
            "Hi everyone.\r\n";

        // ref. RFC 5322 Appendix A.1.3: group addresses.
        public const string Rfc5322AppendixA13 =
            "From: Pete <pete@silly.example>\r\n" +
            "To: A Group:Ed Jones <c@a.test>,joe@where.test,John <jdoe@one.test>;\r\n" +
            "Cc: Undisclosed recipients:;\r\n" +
            "Date: Thu, 13 Feb 1969 23:32:54 -0330\r\n" +
            "Message-ID: <testabcd.1234@silly.example>\r\n" +
            "\r\n" +
            "Testing.\r\n";

        // ref. RFC 5322 Appendix A.5: white space, comments and other oddities.
        public const string Rfc5322AppendixA51 =
            "From: Pete(A nice \\) chap) <pete(his account)@silly.test(his host)>\r\n" +
            "To:A Group(Some people)\r\n" +
            "     :Chris Jones <c@(Chris's host.)public.example>,\r\n" +
            "         joe@example.org,\r\n" +
            "  John <jdoe@one.test> (my dear friend); (the end of the group)\r\n" +
            "Cc:(Empty list)(start)Hidden recipients  :(nobody(that I know))  ;\r\n" +
            "Date: Thu,\r\n" +
            "      13\r\n" +
            "        Feb\r\n" +
            "          1969\r\n" +
            "      23:32\r\n" +
            "               -0330 (Newfoundland Time)\r\n" +
            "Message-ID:              <testabcd.1234@silly.test>\r\n" +
            "\r\n" +
            "Testing.\r\n";

        // ref. RFC 5322 Appendix A.6.1: obsolete forms, which are read, and copied as they are.
        public const string Rfc5322AppendixA6 =
            "From: Joe Q. Public <john.q.public@example.com>\r\n" +
            "To: Mary Smith <@node.test:mary@example.net>, , jdoe@test  . example\r\n" +
            "Date: Tue, 1 Jul 2003 10:52:37 +0200\r\n" +
            "Message-ID: <5678.21-Nov-1997@example.com>\r\n" +
            "\r\n" +
            "Hi everyone.\r\n";
    }
}
