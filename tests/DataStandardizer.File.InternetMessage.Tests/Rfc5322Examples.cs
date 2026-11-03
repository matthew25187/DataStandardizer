namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// The field bodies of the examples in RFC 5322 Appendix A, and what each one is in the modern form.
    /// </summary>
    internal static class Rfc5322Examples
    {
        // ref. RFC 5322 §A.1.1
        public const string A11From = "John Doe <jdoe@machine.example>";
        public const string A11MessageId = "<1234@local.machine.example>";

        // ref. RFC 5322 §A.1.2
        public const string A12From = "\"Joe Q. Public\" <john.q.public@example.com>";
        public const string A12To = "Mary Smith <mary@x.test>, jdoe@example.org, Who? <one@y.test>";
        public const string A12Cc = "<boss@nil.test>, \"Giant; \\\"Big\\\" Box\" <sysservices@example.net>";
        public const string A12CcModern = "boss@nil.test, \"Giant; \\\"Big\\\" Box\" <sysservices@example.net>";
        public const string A12MessageId = "<5678.21-Nov-1997@example.com>";

        // ref. RFC 5322 §A.1.3
        public const string A13To = "A Group:Ed Jones <c@a.test>,joe@where.test,John <jdoe@one.test>;";
        public const string A13ToModern = "A Group: Ed Jones <c@a.test>, joe@where.test, John <jdoe@one.test>;";
        public const string A13Cc = "Undisclosed recipients:;";

        // ref. RFC 5322 §A.2
        public const string A2References = "<1234@local.machine.example> <3456@example.net>";

        // ref. RFC 5322 §A.5
        public const string A5From = "Pete(A nice \\) chap) <pete(his account)@silly.test(his host)>";
        public const string A5FromModern = "Pete <pete@silly.test>";
        public const string A5To = "A Group(Some people)\r\n     :Chris Jones <c@(Chris's host.)public.example>,\r\n         joe@example.org,\r\n  John <jdoe@one.test> (my dear friend); (the end of the group)";
        public const string A5ToModern = "A Group: Chris Jones <c@public.example>, joe@example.org, John <jdoe@one.test>;";
        public const string A5Cc = "(Empty list)(start)Hidden recipients  :(nobody(that I know))  ;";
        public const string A5CcModern = "Hidden recipients:;";

        // ref. RFC 5322 §A.6.1
        public const string A61From = "Joe Q. Public <john.q.public@example.com>";
        public const string A61FromModern = A12From;
        public const string A61To = "Mary Smith <@node.test:mary@example.net>, , jdoe@test  . example";
        public const string A61ToModern = "Mary Smith <mary@example.net>, jdoe@test.example";

        // ref. RFC 5322 §A.6.3
        public const string A63From = "John Doe <jdoe@machine(comment).  example>";
        public const string A63FromModern = A11From;
        public const string A63To = "Mary Smith\r\n  \r\n <mary@example.net>";
        public const string A63ToModern = "Mary Smith <mary@example.net>";
    }
}
