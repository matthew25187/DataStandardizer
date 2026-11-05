namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Values taken from the vCard specifications, for the tests of the value types.
    /// </summary>
    internal static class VcardValueExamples
    {
        // ref. RFC 6350 §4.3.1: examples for "date".
        public const string DateComplete = "19850412";
        public const string DateYearAndMonth = "1985-04";
        public const string DateYear = "1985";
        public const string DateMonthAndDay = "--0412";
        public const string DateDay = "---12";

        // ref. RFC 6350 §4.3.1: "Note also that YYYY-MM-DD is disallowed".
        public const string DateExtended = "1985-04-12";

        // ref. RFC 6350 §4.3.2: examples for "time".
        public const string TimeComplete = "102200";
        public const string TimeHourAndMinute = "1022";
        public const string TimeHour = "10";
        public const string TimeMinuteAndSecond = "-2200";
        public const string TimeSecond = "--00";
        public const string TimeUtc = "102200Z";
        public const string TimeWithOffset = "102200-0800";

        // ref. RFC 6350 §4.3.3: examples for "date-time".
        public const string DateTimeComplete = "19961022T140000";
        public const string DateTimeMonthAndDay = "--1022T1400";
        public const string DateTimeDay = "---22T14";

        // ref. RFC 6350 §4.3.4: the examples for "date-and-or-time" that are times.
        public const string StandaloneTimeComplete = "T102200";
        public const string StandaloneTimeHourAndMinute = "T1022";
        public const string StandaloneTimeHour = "T10";
        public const string StandaloneTimeMinuteAndSecond = "T-2200";
        public const string StandaloneTimeSecond = "T--00";
        public const string StandaloneTimeUtc = "T102200Z";
        public const string StandaloneTimeWithOffset = "T102200-0800";

        // ref. RFC 6350 §4.3.5: examples for "timestamp".
        public const string TimestampLocal = "19961022T140000";
        public const string TimestampUtc = "19961022T140000Z";
        public const string TimestampOffsetHour = "19961022T140000-05";
        public const string TimestampOffsetHourAndMinute = "19961022T140000-0500";

        // ref. RFC 6350 §4.5: the examples, and the limits.
        public const string IntegerPositive = "1234567890";
        public const string IntegerNegative = "-1234556790";
        public const string IntegerSigned = "+1234556790";
        public const string IntegerMaximum = "9223372036854775807";
        public const string IntegerMinimum = "-9223372036854775808";

        // ref. RFC 6350 §4.6: examples.
        public const string FloatSimple = "20.30";
        public const string FloatPrecise = "1000000.0000001";

        // ref. RFC 6350 §4.2: examples for "uri".
        public const string UriHttp = "http://www.example.com/my/picture.jpg";
        public const string UriLdap = "ldap://ldap.example.com/cn=babs%20jensen";

        // ref. RFC 6350 §4.1: examples for "text".
        public const string TextEscapedComma = "this is a single value\\, with a comma encoded";
        public const string TextUnescapedComma = "this is a single value, with a comma encoded";
        public const string TextList = "this is one value,this is another";

        // ref. RFC 6350 §6.2.2 and RFC 9554 §2.2: examples for N.
        public const string NameSimple = "Public;John;Quinlan;Mr.;Esq.";
        public const string NameLists = "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.";
        public const string NameRfc9554 = "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr.";

        // ref. RFC 6350 §6.3.1 and RFC 9554 §2.1: examples for ADR.
        public const string AddressSimple = ";;123 Main Street;Any Town;CA;91921-1234;U.S.A.";
        public const string AddressRfc9554 = ";;123 Main Street;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;";

        // ref. RFC 6350 §6.6.4: example for ORG.
        public const string Organization = "ABC\\, Inc.;North American Division;Marketing";

        // ref. RFC 6350 §6.2.7: examples for GENDER.
        public const string GenderMale = "M";
        public const string GenderFemale = "F";
        public const string GenderMaleFellow = "M;Fellow";
        public const string GenderFemaleGrrrl = "F;grrrl";
        public const string GenderOtherIntersex = "O;intersex";
        public const string GenderIdentityOnly = ";it's complicated";

        // ref. RFC 6350 §6.7.7: examples for CLIENTPIDMAP and PID.
        public const string ClientPidMap = "1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b";
        public const string ClientPidMapUri = "urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b";
        public const string PidLocalAndSource = "3.1";

        // ref. RFC 6350 §6.5.2 and its erratum 3846: example for GEO, as the URI and as the value in a content line.
        public const string GeoUri = "geo:37.386013,-122.082932";
        public const string GeoValue = "geo:37.386013\\,-122.082932";

        // ref. RFC 2426 §3.4.2: example for GEO.
        public const string GeoVersion30 = "37.386013;-122.082932";

        // ref. vCard 2.1 §2.4.6: example for GEO.
        public const string GeoVersion21 = "37.24,-17.87";
        public const string GeoVersion21AsUri = "geo:37.24,-17.87";

        // ref. RFC 5870 §1: example of a geo URI with an uncertainty, and with a coordinate reference system.
        public const string GeoValueWithUncertainty = "geo:48.2010\\,16.3695\\,183;u=40";

        // ref. RFC 2426 §3.1.5: examples for BDAY.
        public const string Version30DateExtended = "1996-04-15";
        public const string Version30DateTimeUtc = "1953-10-15T23:10:00Z";
        public const string Version30DateTimeWithOffset = "1987-09-27T08:30:00-06:00";

        // ref. RFC 2426 §3.4.1: example for TZ.
        public const string Version30Offset = "-05:00";

        // ref. vCard 2.1 §2.2.4: examples for BDAY.
        public const string Version21DateBasic = "19950415";
        public const string Version21DateExtended = "1995-04-15";

        // ref. vCard 2.1 §2.6.2: examples for REV.
        public const string Version21DateTimeBasic = "19951031T222710";
        public const string Version21DateTimeExtended = "1995-10-31T22:27:10Z";

        // ref. vCard 2.1 §2.4.5: examples for TZ.
        public const string Version21OffsetBasic = "-0500";
        public const string Version21OffsetExtended = "-08:00";

        // ref. vCard 2.1 §2.9: example for nameparts.
        public const string Version21Name = "Public;John;Q.;Reverend Dr.;III, Esq.";

        // ref. vCard 2.1 §2.2.2: example of a name with fewer than five components, and a comma in the first.
        public const string Version21NameTwoComponents = "Veni, Vidi, Vici;The Restaurant.";
    }
}
