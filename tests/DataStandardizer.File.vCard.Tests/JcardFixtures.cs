using System.Collections.Generic;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// The cards that the round-trip tests convert between <c>.vcf</c> and jCard, each with the options to read and write it.
    /// </summary>
    internal static class JcardFixtures
    {
        /// <summary>
        /// The example of RFC 6350 §8, whose <c>GEO</c> has a comma with no backslash, which <see cref="VcardOptions.AcceptUnescapedCommasInUris"/> accepts.
        /// </summary>
        public const string Rfc6350Example = nameof(Rfc6350Example);

        /// <summary>
        /// The vCard form of the example of RFC 7095 Appendix B.1.
        /// </summary>
        public const string Rfc7095Example = nameof(Rfc7095Example);

        /// <summary>
        /// Groups, and extension properties of unknown and known types.
        /// </summary>
        public const string GroupsAndExtensions = nameof(GroupsAndExtensions);

        /// <summary>
        /// Lists, structured values, escaping, dates and times, and list parameters.
        /// </summary>
        public const string Values = nameof(Values);

        /// <summary>
        /// The components and parameters of RFC 9554.
        /// </summary>
        public const string JsContactExtensions = nameof(JsContactExtensions);

        /// <summary>
        /// A card of vCard 3.0, which jCard writes as vCard 4.0.
        /// </summary>
        public const string Version30 = nameof(Version30);

        private static readonly VcardOptions AcceptUnescapedCommas = new VcardOptions { MaxLineLength = null, AcceptUnescapedCommasInUris = true };

        private static readonly Dictionary<string, (string Text, VcardOptions Options)> Cards = new Dictionary<string, (string Text, VcardOptions Options)>
        {
            [Rfc6350Example] = (Rfc6350Examples.Example, AcceptUnescapedCommas),
            [Rfc7095Example] = (JcardTestData.RfcExampleVcard, null),
            [GroupsAndExtensions] = (
                "BEGIN:VCARD\r\n" +
                "VERSION:4.0\r\n" +
                "FN:Jane Doe\r\n" +
                "item1.TEL;TYPE=work:+1-555-0100\r\n" +
                "item1.X-ABLABEL:Work\r\n" +
                "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa\r\n" +
                "X-KARMA-POINTS;VALUE=integer:95\r\n" +
                "X-NON-SMOKING;VALUE=boolean:FALSE\r\n" +
                "X-GRADE;VALUE=float:1.30\r\n" +
                "X-TIME-LOCAL;VALUE=time:123000\r\n" +
                "X-A;VALUE=x-mytype:a\\b\r\n" +
                "GENDER;X-PROBABILITY=0.8:M\r\n" +
                "END:VCARD\r\n",
                null),
            [Values] = (
                "BEGIN:VCARD\r\n" +
                "VERSION:4.0\r\n" +
                "FN:Jane Doe\r\n" +
                "N;SORT-AS=\"Doe,Jane\":Doe;Jane;Q.,R.;Dr.;PhD,MD\r\n" +
                "NICKNAME:Janie,JD\r\n" +
                "BDAY:19850412\r\n" +
                "ANNIVERSARY:--0412T1430Z\r\n" +
                "DEATHDATE;VALUE=text:circa 2080\r\n" +
                "REV:20130214T123000-05\r\n" +
                "TZ;VALUE=utc-offset:-0500\r\n" +
                "CATEGORIES:computers,cameras\r\n" +
                "ORG:ABC\\, Inc.;North American Division;Marketing\r\n" +
                "NOTE:Line one\\nLine two\\, with a comma\\\\ and a backslash\r\n" +
                "LANG;PREF=1:fr-CA\r\n" +
                "CLIENTPIDMAP:1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b\r\n" +
                "TEL;VALUE=uri;PID=1.1;TYPE=work,voice:tel:+1-555-555-5555;ext=5555\r\n" +
                "END:VCARD\r\n",
                null),
            [JsContactExtensions] = (
                "BEGIN:VCARD\r\n" +
                "VERSION:4.0\r\n" +
                "FN:Jane Doe III\r\n" +
                "N:Doe;Jane;;;III;;III\r\n" +
                "CREATED:20220705T093412Z\r\n" +
                "PRONOUNS;PREF=1:she/her\r\n" +
                "SOCIALPROFILE;SERVICE-TYPE=Mastodon;USERNAME=JaneDoe:https://example.com/@JaneDoe\r\n" +
                "END:VCARD\r\n",
                null),
            [Version30] = (
                "BEGIN:VCARD\r\n" +
                "VERSION:3.0\r\n" +
                "FN:Jane Doe\r\n" +
                "N:Doe;Jane;;;\r\n" +
                "TEL;TYPE=WORK,VOICE:+1-555-0100\r\n" +
                "END:VCARD\r\n",
                null),
        };

        public static IEnumerable<string> Names => Cards.Keys;

        public static (string Text, VcardOptions Options) Get(string name) => Cards[name];
    }
}
