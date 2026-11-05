namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Examples from RFC 6350 at the lexical level, with the lines the reader should give for them, worked out by hand.
    /// </summary>
    internal static class Rfc6350Examples
    {
        /// <summary>
        /// ref. RFC 6350 §8: the example vCard, with the folds and quoted TYPE values as printed in the RFC.
        /// </summary>
        public const string Example =
            "BEGIN:VCARD\r\n" +
            "VERSION:4.0\r\n" +
            "FN:Simon Perreault\r\n" +
            "N:Perreault;Simon;;;ing. jr,M.Sc.\r\n" +
            "BDAY:--0203\r\n" +
            "ANNIVERSARY:20090808T1430-0500\r\n" +
            "GENDER:M\r\n" +
            "LANG;PREF=1:fr\r\n" +
            "LANG;PREF=2:en\r\n" +
            "ORG;TYPE=work:Viagenie\r\n" +
            "ADR;TYPE=work:;Suite D2-630;2875 Laurier;\r\n" +
            " Quebec;QC;G1V 2M2;Canada\r\n" +
            "TEL;VALUE=uri;TYPE=\"work,voice\";PREF=1:tel:+1-418-656-9254;ext=102\r\n" +
            "TEL;VALUE=uri;TYPE=\"work,cell,voice,video,text\":tel:+1-418-262-6501\r\n" +
            "EMAIL;TYPE=work:simon.perreault@viagenie.ca\r\n" +
            "GEO;TYPE=work:geo:46.772673,-71.282945\r\n" +
            "KEY;TYPE=work;VALUE=uri:\r\n" +
            " http://www.viagenie.ca/simon.perreault/simon.asc\r\n" +
            "TZ:-0500\r\n" +
            "URL;TYPE=home:http://nomis80.org\r\n" +
            "END:VCARD\r\n";

        /// <summary>
        /// The number of physical lines in <see cref="Example"/>.
        /// </summary>
        public const long ExamplePhysicalLineCount = 21;

        /// <summary>
        /// The lines of <see cref="Example"/>, as <see cref="LineDescription"/> gives them.
        /// </summary>
        public static readonly string[] ExampleLines =
        {
            "1/1 BEGIN:VCARD",
            "2/2 VERSION:4.0",
            "3/3 FN:Simon Perreault",
            "4/4 N:Perreault;Simon;;;ing. jr,M.Sc.",
            "5/5 BDAY:--0203",
            "6/6 ANNIVERSARY:20090808T1430-0500",
            "7/7 GENDER:M",
            "8/8 LANG;PREF=1:fr",
            "9/9 LANG;PREF=2:en",
            "10/10 ORG;TYPE=work:Viagenie",
            "11/11 ADR;TYPE=work:;Suite D2-630;2875 Laurier;Quebec;QC;G1V 2M2;Canada",
            "12/13 TEL;VALUE=uri;TYPE=work,voice;PREF=1:tel:+1-418-656-9254;ext=102",
            "13/14 TEL;VALUE=uri;TYPE=work,cell,voice,video,text:tel:+1-418-262-6501",
            "14/15 EMAIL;TYPE=work:simon.perreault@viagenie.ca",
            "15/16 GEO;TYPE=work:geo:46.772673,-71.282945",
            "16/17 KEY;TYPE=work;VALUE=uri:http://www.viagenie.ca/simon.perreault/simon.asc",
            "17/19 TZ:-0500",
            "18/20 URL;TYPE=home:http://nomis80.org",
            "19/21 END:VCARD"
        };

        /// <summary>
        /// ref. RFC 6350 §3.2: the same NOTE, unfolded and in the first of its folded forms.  The fold's white space is the one after the line break.
        /// </summary>
        public const string NoteFoldedOnce = "NOTE:This is a long description\r\n  that exists on a long line.\r\n";

        /// <summary>
        /// ref. RFC 6350 §3.2: the second folded form of the NOTE, which folds inside words.
        /// </summary>
        public const string NoteFoldedTwice = "NOTE:This is a long descrip\r\n tion that exists o\r\n n a long line.\r\n";

        /// <summary>
        /// ref. RFC 6350 §3.2: the value of the NOTE once unfolded.
        /// </summary>
        public const string NoteValue = "This is a long description that exists on a long line.";

        /// <summary>
        /// ref. RFC 6350 §6.2.1: an FN value with an escaped comma.
        /// </summary>
        public const string EscapedFormattedName = "FN:Mr. John Q. Public\\, Esq.\r\n";

        /// <summary>
        /// ref. RFC 6350 §6.2.1: the value of <see cref="EscapedFormattedName"/> without its escaping.
        /// </summary>
        public const string FormattedName = "Mr. John Q. Public, Esq.";

        /// <summary>
        /// ref. RFC 6350 §6.3.1: an ADR with quoted GEO and LABEL parameters, whose values hold ':', ',' and a TEXT escape.
        /// </summary>
        public const string AddressWithQuotedParameters =
            "ADR;GEO=\"geo:12.3457,78.910\";LABEL=\"Mr. John Q. Public, Esq.\\nMail Drop: TNE QB\\n123 Main Street\\nAny Town, CA  91921-1234\\nU.S.A.\":" +
            ";;123 Main Street;Any Town;CA;91921-1234;U.S.A.\r\n";
    }
}
