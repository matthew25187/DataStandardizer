using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// The whole messages of RFC 5322 Appendix A, with CRLF line breaks.
    /// </summary>
    internal static class Rfc5322Messages
    {
        // ref. RFC 5322 §A.1.1
        public const string A11 = "From: John Doe <jdoe@machine.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMessage-ID: <1234@local.machine.example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";
        public const string A11Sender = "From: John Doe <jdoe@machine.example>\r\nSender: Michael Jones <mjones@machine.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMessage-ID: <1234@local.machine.example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        // ref. RFC 5322 §A.1.2
        public const string A12 = "From: \"Joe Q. Public\" <john.q.public@example.com>\r\nTo: Mary Smith <mary@x.test>, jdoe@example.org, Who? <one@y.test>\r\nCc: <boss@nil.test>, \"Giant; \\\"Big\\\" Box\" <sysservices@example.net>\r\nDate: Tue, 1 Jul 2003 10:52:37 +0200\r\nMessage-ID: <5678.21-Nov-1997@example.com>\r\n\r\nHi everyone.\r\n";

        // ref. RFC 5322 §A.1.3
        public const string A13 = "From: Pete <pete@silly.example>\r\nTo: A Group:Ed Jones <c@a.test>,joe@where.test,John <jdoe@one.test>;\r\nCc: Undisclosed recipients:;\r\nDate: Thu, 13 Feb 1969 23:32:54 -0330\r\nMessage-ID: <testabcd.1234@silly.example>\r\n\r\nTesting.\r\n";

        // ref. RFC 5322 §A.2
        public const string A2Reply = "From: Mary Smith <mary@example.net>\r\nTo: John Doe <jdoe@machine.example>\r\nReply-To: \"Mary Smith: Personal Account\" <smith@home.example>\r\nSubject: Re: Saying Hello\r\nDate: Fri, 21 Nov 1997 10:01:10 -0600\r\nMessage-ID: <3456@example.net>\r\nIn-Reply-To: <1234@local.machine.example>\r\nReferences: <1234@local.machine.example>\r\n\r\nThis is a reply to your hello.\r\n";
        public const string A2ReplyToReply = "To: \"Mary Smith: Personal Account\" <smith@home.example>\r\nFrom: John Doe <jdoe@machine.example>\r\nSubject: Re: Saying Hello\r\nDate: Fri, 21 Nov 1997 11:00:00 -0600\r\nMessage-ID: <abcd.1234@local.machine.test>\r\nIn-Reply-To: <3456@example.net>\r\nReferences: <1234@local.machine.example> <3456@example.net>\r\n\r\nThis is a reply to your reply.\r\n";

        // ref. RFC 5322 §A.3
        public const string A3 = "Resent-From: Mary Smith <mary@example.net>\r\nResent-To: Jane Brown <j-brown@other.example>\r\nResent-Date: Mon, 24 Nov 1997 14:22:01 -0800\r\nResent-Message-ID: <78910@example.net>\r\nFrom: John Doe <jdoe@machine.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMessage-ID: <1234@local.machine.example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        // ref. RFC 5322 §A.4
        public const string A4 = "Received: from x.y.test\r\n   by example.net\r\n   via TCP\r\n   with ESMTP\r\n   id ABC12345\r\n   for <mary@example.net>;  21 Nov 1997 10:05:43 -0600\r\nReceived: from node.example by x.y.test; 21 Nov 1997 10:01:22 -0600\r\nFrom: John Doe <jdoe@node.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMessage-ID: <1234@local.node.example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        // ref. RFC 5322 §A.5
        public const string A5 = "From: Pete(A nice \\) chap) <pete(his account)@silly.test(his host)>\r\nTo:A Group(Some people)\r\n     :Chris Jones <c@(Chris's host.)public.example>,\r\n         joe@example.org,\r\n  John <jdoe@one.test> (my dear friend); (the end of the group)\r\nCc:(Empty list)(start)Hidden recipients  :(nobody(that I know))  ;\r\nDate: Thu,\r\n      13\r\n        Feb\r\n          1969\r\n      23:32\r\n               -0330 (Newfoundland Time)\r\nMessage-ID:              <testabcd.1234@silly.test>\r\n\r\nTesting.\r\n";

        // ref. RFC 5322 §A.6.1
        public const string A61 = "From: Joe Q. Public <john.q.public@example.com>\r\nTo: Mary Smith <@node.test:mary@example.net>, , jdoe@test  . example\r\nDate: Tue, 1 Jul 2003 10:52:37 +0200\r\nMessage-ID: <5678.21-Nov-1997@example.com>\r\n\r\nHi everyone.\r\n";

        // ref. RFC 5322 §A.6.2
        public const string A62 = "From: John Doe <jdoe@machine.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: 21 Nov 97 09:55:06 GMT\r\nMessage-ID: <1234@local.machine.example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        // ref. RFC 5322 §A.6.3
        public const string A63 = "From  : John Doe <jdoe@machine(comment).  example>\r\nTo    : Mary Smith\r\n  \r\n          <mary@example.net>\r\nSubject     : Saying Hello\r\nDate  : Fri, 21 Nov 1997 09(comment):   55  :  06 -0600\r\nMessage-ID  : <1234   @   local(blah)  .machine .example>\r\n\r\nThis is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        public const string A11Body = "This is a message just to say hello.\r\nSo, \"Hello\".\r\n";

        /// <summary>
        /// Gets every message of the appendix.
        /// </summary>
        public static TheoryData<string> All => new TheoryData<string>
        {
            A11, A11Sender, A12, A13, A2Reply, A2ReplyToReply, A3, A4, A5, A61, A62, A63,
        };
    }

    /// <summary>
    /// Reads and writes messages held as strings of one byte per character, for tests whose act is something else.
    /// </summary>
    internal static class ImfMessages
    {
        public static ImfMessage Read(string message)
        {
            return Read(message, new ImfOptions());
        }

        public static ImfMessage Read(string message, ImfOptions options)
        {
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), options))
            {
                return reader.ReadMessage();
            }
        }

        public static string Write(ImfMessage message)
        {
            return Write(message, new ImfOptions());
        }

        public static string Write(ImfMessage message, ImfOptions options)
        {
            var stream = new MemoryStream();
            using (var writer = new ImfWriter(stream, options))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Options that accept every conformance problem, for arranging a message that has one.
        /// </summary>
        public static ImfOptions AcceptAll()
        {
            return new ImfOptions { AllowCardinalityViolations = true, AllowInvalidValues = true, AllowMissingRequiredFields = true };
        }
    }
}
