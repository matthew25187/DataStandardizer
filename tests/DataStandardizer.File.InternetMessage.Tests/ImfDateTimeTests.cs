using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfDateTimeTests
    {
        private const string SimpleDate = "1 Jan 2000 00:00:00 +0000";
        private const string OneSecondLaterDate = "1 Jan 2000 00:00:01 +0000";

        // ref. RFC 5322 §3.3: the time-of-day range ends at 23:59:60.
        private const string LeapSecondDate = "31 Dec 2016 23:59:60 +0000";

        // ref. RFC 5322 §3.3: "-0000" means the local offset is unknown, unlike "+0000".
        private const string UniversalTimeDate = "1 Jan 2000 00:00:00 +0000";
        private const string UnknownOffsetDate = "1 Jan 2000 00:00:00 -0000";

        // 1 January 2000 was a Saturday, so this day of the week breaks RFC 5322 §3.3.
        private const string WrongDayOfWeekDate = "Sun, 1 Jan 2000 00:00:00 +0000";
        private const string CorrectedDayOfWeekDate = "Sat, 1 Jan 2000 00:00:00 +0000";

        private static readonly ImfOptions AllowInvalidValues = new ImfOptions { AllowInvalidValues = true };

        public static TheoryData<string, int, int, int, int, int, int, int, bool, bool> Parts =>
            new TheoryData<string, int, int, int, int, int, int, int, bool, bool>
            {
                // value, year, month, day, hour, minute, second, offset in minutes, offset unknown, day of the week given

                // ref. RFC 5322 Appendix A
                { Rfc5322Examples.A11Date, 1997, 11, 21, 9, 55, 6, -360, false, true },
                { Rfc5322Examples.A12Date, 2003, 7, 1, 10, 52, 37, 120, false, true },
                { Rfc5322Examples.A2ReplyDate, 1997, 11, 21, 10, 1, 10, -360, false, true },
                { Rfc5322Examples.A2ReplyToReplyDate, 1997, 11, 21, 11, 0, 0, -360, false, true },
                { Rfc5322Examples.A3ResentDate, 1997, 11, 24, 14, 22, 1, -480, false, true },
                { Rfc5322Examples.A4ReceivedDate, 1997, 11, 21, 10, 5, 43, -360, false, false },
                { Rfc5322Examples.A5Date, 1969, 2, 13, 23, 32, 0, -210, false, true },
                { Rfc5322Examples.A62Date, 1997, 11, 21, 9, 55, 6, 0, false, false },
                { Rfc5322Examples.A63Date, 1997, 11, 21, 9, 55, 6, -360, false, true },

                // ref. RFC 5322 §3.3: the seconds are optional, and zones go as far as the digits do.
                { "1 Jan 2000 12:30 +0000", 2000, 1, 1, 12, 30, 0, 0, false, false },
                { "1 Jan 2000 00:00:00 +9959", 2000, 1, 1, 0, 0, 0, 5999, false, false },
                { "1 Jan 2000 00:00:00 -9959", 2000, 1, 1, 0, 0, 0, -5999, false, false },
                { "1 Jan 2000 00:00:00 -0001", 2000, 1, 1, 0, 0, 0, -1, false, false },
                { UnknownOffsetDate, 2000, 1, 1, 0, 0, 0, 0, true, false },

                // ref. RFC 5322 §3.3: the last day of each month, the leap year rules, and the first and any later year.
                { "31 Jan 2000 00:00 +0000", 2000, 1, 31, 0, 0, 0, 0, false, false },
                { "29 Feb 2000 00:00 +0000", 2000, 2, 29, 0, 0, 0, 0, false, false },
                { "29 Feb 1996 00:00 +0000", 1996, 2, 29, 0, 0, 0, 0, false, false },
                { "28 Feb 1900 00:00 +0000", 1900, 2, 28, 0, 0, 0, 0, false, false },
                { "31 Mar 2000 00:00 +0000", 2000, 3, 31, 0, 0, 0, 0, false, false },
                { "30 Apr 2000 00:00 +0000", 2000, 4, 30, 0, 0, 0, 0, false, false },
                { "31 May 2000 00:00 +0000", 2000, 5, 31, 0, 0, 0, 0, false, false },
                { "30 Jun 2000 00:00 +0000", 2000, 6, 30, 0, 0, 0, 0, false, false },
                { "31 Jul 2000 00:00 +0000", 2000, 7, 31, 0, 0, 0, 0, false, false },
                { "31 Aug 2000 00:00 +0000", 2000, 8, 31, 0, 0, 0, 0, false, false },
                { "30 Sep 2000 00:00 +0000", 2000, 9, 30, 0, 0, 0, 0, false, false },
                { "31 Oct 2000 00:00 +0000", 2000, 10, 31, 0, 0, 0, 0, false, false },
                { "30 Nov 2000 00:00 +0000", 2000, 11, 30, 0, 0, 0, 0, false, false },
                { "31 Dec 2000 00:00 +0000", 2000, 12, 31, 0, 0, 0, 0, false, false },
                { "1 Jan 1900 00:00 +0000", 1900, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 10000 00:00 +0000", 10000, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 2147483647 00:00 +0000", 2147483647, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 00002000 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },

                // ref. RFC 5322 §3.3: the time-of-day range ends at 23:59:60, and a leap second falls at another minute in another zone.
                { LeapSecondDate, 2016, 12, 31, 23, 59, 60, 0, false, false },
                { "1 Jan 2017 05:29:60 +0530", 2017, 1, 1, 5, 29, 60, 330, false, false },

                // ref. RFC 5234 §2.3: ABNF string literals are case-insensitive.
                { "SAT, 1 JAN 2000 00:00 GMT", 2000, 1, 1, 0, 0, 0, 0, false, true },
                { "sat, 1 jan 2000 00:00 gmt", 2000, 1, 1, 0, 0, 0, 0, false, true },

                // ref. RFC 5322 §4.3: obs-year
                { "1 Jan 00 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 49 00:00 +0000", 2049, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 50 00:00 +0000", 1950, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 99 00:00 +0000", 1999, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 000 00:00 +0000", 1900, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 100 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 999 00:00 +0000", 2899, 1, 1, 0, 0, 0, 0, false, false },

                // ref. RFC 5322 §4.3: CFWS at each place the obsolete rules allow it.
                { "(a) Sat (b) , (c) 1 (d) Jan (e) 2000 (f) 00 (g) : (h) 00 (i) : (j) 00 (k) +0000 (l)", 2000, 1, 1, 0, 0, 0, 0, false, true },
                { "(a(b)\\)) 1 Jan 2000 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { "(\x00C3\x00A9) 1 Jan 2000 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { " 1\t Jan\r\n 2000 \r\n \r\n 00:00 +0000 ", 2000, 1, 1, 0, 0, 0, 0, false, false },

                // ref. RFC 5322 §4.3: the same rules need no white space between the parts.
                { "Sat,1Jan2000 00:00 +0000", 2000, 1, 1, 0, 0, 0, 0, false, true },
                { "1Jan97(a)00:00 +0000", 1997, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 2000 00:00GMT", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 2000 00:00(a)GMT", 2000, 1, 1, 0, 0, 0, 0, false, false },
                { "1 Jan 2000 00:00(a) +0000", 2000, 1, 1, 0, 0, 0, 0, false, false },

                // ref. RFC 5322 §4.3: with nothing between them, the last two digits are obs-hour and the others obs-year.
                { "1 Jan 200012:30 +0000", 2000, 1, 1, 12, 30, 0, 0, false, false },
                { "1 Jan 9712:30 +0000", 1997, 1, 1, 12, 30, 0, 0, false, false },
                { "1 Jan 10012:30 +0000", 2000, 1, 1, 12, 30, 0, 0, false, false },
                { "1 Jan 200012 :30 +0000", 2000, 1, 1, 12, 30, 0, 0, false, false },
                { "1 Jan 2000 :30 +0000", 2020, 1, 1, 0, 30, 0, 0, false, false },
            };

        public static TheoryData<string, int, bool> ObsoleteZones
        {
            get
            {
                var data = new TheoryData<string, int, bool>
                {
                    // value, offset in minutes, offset unknown

                    // ref. RFC 5322 §4.3: UT and GMT are "+0000".
                    { "1 Jan 2000 00:00 UT", 0, false },
                    { "1 Jan 2000 00:00 GMT", 0, false },

                    // ref. RFC 5322 §4.3: the US zones
                    { "1 Jan 2000 00:00 EDT", -240, false },
                    { "1 Jan 2000 00:00 EST", -300, false },
                    { "1 Jan 2000 00:00 CDT", -300, false },
                    { "1 Jan 2000 00:00 CST", -360, false },
                    { "1 Jan 2000 00:00 MDT", -360, false },
                    { "1 Jan 2000 00:00 MST", -420, false },
                    { "1 Jan 2000 00:00 PDT", -420, false },
                    { "1 Jan 2000 00:00 PST", -480, false },
                    { "1 Jan 2000 00:00 ut", 0, false },
                    { "1 Jan 2000 00:00 pst", -480, false },
                };

                // ref. RFC 5322 §4.3: %d65-73 / %d75-90 / %d97-105 / %d107-122, which "SHOULD all be considered equivalent to '-0000'".
                foreach (var zone in "ABCDEFGHIKLMNOPQRSTUVWXYZabcdefghiklmnopqrstuvwxyz")
                {
                    data.Add("1 Jan 2000 00:00 " + zone, 0, true);
                }

                return data;
            }
        }

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            // ref. RFC 5322 §3.3: required parts
            "",
            " ",
            "(a)",
            "Sat,",
            "1",
            "1 Jan",
            "1 Jan 2000",
            "1 Jan 2000 00",
            "1 Jan 2000 00:",
            "1 Jan 2000 00:00",
            "1 Jan 2000 00:00:00",
            "1 Jan 2000 00:00:",
            "1 Jan 2000 00:00:00 +",
            "Jan 2000 00:00 +0000",
            "1 2000 00:00 +0000",
            "1 Jan 00:00 +0000",

            // ref. RFC 5322 §3.3: the shape of each part
            "Sat 1 Jan 2000 00:00 +0000",
            ", 1 Jan 2000 00:00 +0000",
            "Saturday, 1 Jan 2000 00:00 +0000",
            "Xyz, 1 Jan 2000 00:00 +0000",
            "001 Jan 2000 00:00 +0000",
            "1 January 2000 00:00 +0000",
            "1 Xyz 2000 00:00 +0000",
            "1 Jan 2 00:00 +0000",
            "1 Jan 2000 0:00 +0000",
            "1 Jan 2000 000:00 +0000",
            "1 Jan 2000 00:0 +0000",
            "1 Jan 2000 00:000 +0000",
            "1 Jan 2000 00:00:0 +0000",
            "1 Jan 2000 00:00:000 +0000",
            "1 Jan 2000 00 00 +0000",
            "1 Jan 2000 00:00.5 +0000",
            "1 Jan 2000 00:00:00.5 +0000",

            // ref. RFC 5322 §3.3: zone = (FWS ( "+" / "-" ) 4DIGIT) / obs-zone
            "1 Jan 2000 00:00 +000",
            "1 Jan 2000 00:00 +00000",
            "1 Jan 2000 00:00 + 0000",
            "1 Jan 2000 00:00 0000",
            "1 Jan 2000 00:00 +00:00",
            "1 Jan 2000 00:00+0000",
            "1 Jan 2000 00:00:00+0000",
            "1 Jan 2000 00:00 (a)+0000",

            // ref. RFC 5322 §4.3: obs-zone leaves out J, and no other name.
            "1 Jan 2000 00:00 J",
            "1 Jan 2000 00:00 j",
            "1 Jan 2000 00:00 UTC",
            "1 Jan 2000 00:00 CET",
            "1 Jan 2000 00:00 AB",
            "1 Jan 2000 00:00 GMT+1",
            "1 Jan 2000 00:00 GMT +0000",

            // ref. RFC 5322 §3.3: "A date-time specification MUST be semantically valid."
            "0 Jan 2000 00:00 +0000",
            "32 Jan 2000 00:00 +0000",
            "29 Feb 1900 00:00 +0000",
            "29 Feb 1999 00:00 +0000",
            "30 Feb 2000 00:00 +0000",
            "31 Apr 2000 00:00 +0000",
            "31 Jun 2000 00:00 +0000",
            "31 Sep 2000 00:00 +0000",
            "31 Nov 2000 00:00 +0000",
            "1 Jan 2000 24:00 +0000",
            "1 Jan 2000 00:60 +0000",
            "1 Jan 2000 00:00:61 +0000",
            "1 Jan 2000 00:00 +0060",
            "1 Jan 2000 00:00 -0099",

            // ref. RFC 5322 §3.3: "The year is any numeric year 1900 or later."
            "1 Jan 1899 00:00 +0000",
            "1 Jan 0099 00:00 +0000",
            "1 Jan 0000 00:00 +0000",

            // A year too large to hold.
            "1 Jan 2147483648 00:00 +0000",
            "1 Jan 99999999999999999999 00:00 +0000",

            // Outside CFWS, and not a line break that folds.
            "1 Jan 2000 00:00 +0000\r\n",
            "1 Jan\r\n2000 00:00 +0000",
            "1 Jan 2000 00:00 +0000 x",
            "1 Jan 2000 00:00 +0000 +0000",
            "1 Jan 2000 00:00 +0000 (a",
            "(a 1 Jan 2000 00:00 +0000",
            "\"a\" 1 Jan 2000 00:00 +0000",
            "1 Jan 2000 00:00 \xFF10\xFF10",
            "\xD800 1 Jan 2000 00:00 +0000",
        };

        public static TheoryData<string, string> WrongDaysOfWeek => new TheoryData<string, string>
        {
            // value, the date-time as written

            { WrongDayOfWeekDate, CorrectedDayOfWeekDate },
            { "Thu, 21 Nov 1997 09:55:06 -0600", Rfc5322Examples.A11Date },
        };

        public static TheoryData<string> WrongDayOfWeekValues => new TheoryData<string>
        {
            WrongDayOfWeekDate,
            "Thu, 21 Nov 1997 09:55:06 -0600",
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            // ref. RFC 5322 Appendix A: the modern form is written as it was read.
            { Rfc5322Examples.A11Date, Rfc5322Examples.A11Date },
            { Rfc5322Examples.A12Date, Rfc5322Examples.A12Date },
            { Rfc5322Examples.A3ResentDate, Rfc5322Examples.A3ResentDate },
            { Rfc5322Examples.A4ReceivedDate, Rfc5322Examples.A4ReceivedDate },
            { UnknownOffsetDate, UnknownOffsetDate },
            { LeapSecondDate, LeapSecondDate },
            { "1 Jan 10000 00:00:00 +0000", "1 Jan 10000 00:00:00 +0000" },
            { "1 Jan 2000 00:00:00 -0001", "1 Jan 2000 00:00:00 -0001" },
            { "1 Jan 2000 00:00:00 +9959", "1 Jan 2000 00:00:00 +9959" },

            // ref. RFC 5322 §4.3: each obsolete form is written in the modern form.
            { Rfc5322Examples.A5Date, Rfc5322Examples.A5DateModern },
            { Rfc5322Examples.A62Date, Rfc5322Examples.A62DateModern },
            { Rfc5322Examples.A63Date, Rfc5322Examples.A11Date },
            { "1 Jan 49 00:00 PST", "1 Jan 2049 00:00:00 -0800" },
            { "1 Jan 100 00:00 EDT", "1 Jan 2000 00:00:00 -0400" },
            { "1 Jan 2000 00:00 UT", SimpleDate },
            { "1 Jan 2000 00:00 Z", "1 Jan 2000 00:00:00 -0000" },
            { "sat, 1 jan 2000 00:00 +0000", "Sat, 1 Jan 2000 00:00:00 +0000" },
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { SimpleDate, SimpleDate, true },

            // The same value in another form.
            { "1 Jan 2000 00:00 UT", "1 Jan 00 00:00:00 +0000", true },
            { "1 Jan 2000 00:00 Z", UnknownOffsetDate, true },
            { "1 Jan 2000 00:00 EST", "(a) 1 Jan 2000 00:00 -0500", true },

            // ref. RFC 5322 §3.3: "-0000" differs from "+0000".
            { UnknownOffsetDate, UniversalTimeDate, false },

            // The same moment, written otherwise.
            { "1 Jan 2000 12:00 +0100", "1 Jan 2000 11:00 +0000", false },
            { "Sat, 1 Jan 2000 00:00 +0000", SimpleDate, false },

            // Each part.
            { SimpleDate, "1 Jan 2001 00:00:00 +0000", false },
            { SimpleDate, "1 Feb 2000 00:00:00 +0000", false },
            { SimpleDate, "2 Jan 2000 00:00:00 +0000", false },
            { SimpleDate, "1 Jan 2000 01:00:00 +0000", false },
            { SimpleDate, "1 Jan 2000 00:01:00 +0000", false },
            { SimpleDate, OneSecondLaterDate, false },
            { SimpleDate, "1 Jan 2000 00:00:00 +0001", false },
        };

        public static TheoryData<string, string, bool> InequalityCases => new TheoryData<string, string, bool>
        {
            { SimpleDate, SimpleDate, false },

            // The same value in another form.
            { "1 Jan 2000 00:00 UT", "1 Jan 00 00:00:00 +0000", false },
            { "1 Jan 2000 00:00 Z", UnknownOffsetDate, false },
            { "1 Jan 2000 00:00 EST", "(a) 1 Jan 2000 00:00 -0500", false },

            // ref. RFC 5322 §3.3: "-0000" differs from "+0000".
            { UnknownOffsetDate, UniversalTimeDate, true },

            // The same moment, written otherwise.
            { "1 Jan 2000 12:00 +0100", "1 Jan 2000 11:00 +0000", true },
            { "Sat, 1 Jan 2000 00:00 +0000", SimpleDate, true },

            // Each part.
            { SimpleDate, "1 Jan 2001 00:00:00 +0000", true },
            { SimpleDate, "1 Feb 2000 00:00:00 +0000", true },
            { SimpleDate, "2 Jan 2000 00:00:00 +0000", true },
            { SimpleDate, "1 Jan 2000 01:00:00 +0000", true },
            { SimpleDate, "1 Jan 2000 00:01:00 +0000", true },
            { SimpleDate, OneSecondLaterDate, true },
            { SimpleDate, "1 Jan 2000 00:00:00 +0001", true },
        };

        public static TheoryData<string, string, int> OrderingCases => new TheoryData<string, string, int>
        {
            // left, right, the sign of the result

            { SimpleDate, SimpleDate, 0 },
            { SimpleDate, OneSecondLaterDate, -1 },
            { OneSecondLaterDate, SimpleDate, 1 },
            { "31 Dec 1999 23:59:59 +0000", SimpleDate, -1 },
            { SimpleDate, "1 Jan 2001 00:00:00 +0000", -1 },
            { "29 Feb 2000 00:00:00 +0000", "1 Mar 2000 00:00:00 +0000", -1 },

            // By moment, not by what was written.
            { "1 Jan 2000 12:00:00 +0100", "1 Jan 2000 11:30:00 +0000", -1 },
            { "1 Jan 2000 01:00:00 +0200", "1 Jan 2000 00:00:00 -0100", -1 },

            // A leap second is after the second before it, and before the one after it.
            { "31 Dec 2016 23:59:59 +0000", LeapSecondDate, -1 },
            { LeapSecondDate, "1 Jan 2017 00:00:00 +0000", -1 },
            { LeapSecondDate, "1 Jan 2017 00:59:59 +0100", 1 },

            // Then the differences that make the same moment unequal.
            { "1 Jan 2000 11:00:00 +0000", "1 Jan 2000 12:00:00 +0100", -1 },
            { UniversalTimeDate, UnknownOffsetDate, -1 },
            { UnknownOffsetDate, UniversalTimeDate, 1 },
            { SimpleDate, "Sat, 1 Jan 2000 00:00:00 +0000", -1 },

            // Years a DateTimeOffset can't hold.
            { "31 Dec 9999 23:59:59 +0000", "1 Jan 10000 00:00:00 +0000", -1 },
            { "1 Jan 10000 00:00:00 +0000", "1 Jan 2147483647 00:00:00 +0000", -1 },
        };

        public static TheoryData<string, DayOfWeek> DaysOfWeek => new TheoryData<string, DayOfWeek>
        {
            { "Sun, 2 Jan 2000 00:00 +0000", DayOfWeek.Sunday },
            { "Mon, 3 Jan 2000 00:00 +0000", DayOfWeek.Monday },
            { "Tue, 4 Jan 2000 00:00 +0000", DayOfWeek.Tuesday },
            { "Wed, 5 Jan 2000 00:00 +0000", DayOfWeek.Wednesday },
            { "Thu, 6 Jan 2000 00:00 +0000", DayOfWeek.Thursday },
            { "Fri, 7 Jan 2000 00:00 +0000", DayOfWeek.Friday },
            { "Sat, 8 Jan 2000 00:00 +0000", DayOfWeek.Saturday },

            // ref. RFC 5322 §A.5, §A.1.1, §A.1.2; and on each side of a leap day; and with none given.
            { Rfc5322Examples.A5Date, DayOfWeek.Thursday },
            { Rfc5322Examples.A11Date, DayOfWeek.Friday },
            { Rfc5322Examples.A12Date, DayOfWeek.Tuesday },
            { "Mon, 28 Feb 2000 00:00 +0000", DayOfWeek.Monday },
            { "Tue, 29 Feb 2000 00:00 +0000", DayOfWeek.Tuesday },
            { "Wed, 1 Mar 2000 00:00 +0000", DayOfWeek.Wednesday },
            { "Mon, 1 Jan 1900 00:00 +0000", DayOfWeek.Monday },
            { "1 Mar 2000 00:00 +0000", DayOfWeek.Wednesday },
        };

        public static TheoryData<string, int, int, int, int, int, int, int> Conversions =>
            new TheoryData<string, int, int, int, int, int, int, int>
            {
                // value, year, month, day, hour, minute, second, offset in minutes

                { Rfc5322Examples.A11Date, 1997, 11, 21, 9, 55, 6, -360 },
                { Rfc5322Examples.A5Date, 1969, 2, 13, 23, 32, 0, -210 },
                { UniversalTimeDate, 2000, 1, 1, 0, 0, 0, 0 },
                { "1 Jan 2000 00:00 +1400", 2000, 1, 1, 0, 0, 0, 840 },
                { "1 Jan 2000 00:00 -1400", 2000, 1, 1, 0, 0, 0, -840 },
                { "31 Dec 9999 23:59:59 +0000", 9999, 12, 31, 23, 59, 59, 0 },

                // A leap second becomes second 59.
                { LeapSecondDate, 2016, 12, 31, 23, 59, 59, 0 },
            };

        public static TheoryData<string> NotRepresentableValues => new TheoryData<string>
        {
            "1 Jan 10000 00:00 +0000",
            "1 Jan 2000 00:00 +1401",
            "1 Jan 2000 00:00 -1401",
            "31 Dec 9999 23:59 -0100",
        };

        public static TheoryData<Action> NullOptionsCalls => new TheoryData<Action>
        {
            () => ImfDateTime.Parse(SimpleDate, null),
            () => ImfDateTime.Parse(Octets.Of(SimpleDate), null),
            () => ImfDateTime.TryParse(SimpleDate, null, out _),
            () => ImfDateTime.TryParse(Octets.Of(SimpleDate), null, out _),
        };

        public static TheoryData<Func<ImfDateTime, object>> UninitialisedMembers => new TheoryData<Func<ImfDateTime, object>>
        {
            value => value.Year,
            value => value.Month,
            value => value.Day,
            value => value.Hour,
            value => value.Minute,
            value => value.Second,
            value => value.Offset,
            value => value.IsOffsetUnknown,
            value => value.HasDayOfWeek,
            value => value.DayOfWeek,
            value => value.ToDateTimeOffset(),
            value => value.ToDateTimeOffset(true),
        };

        [Theory]
        [MemberData(nameof(Parts))]
        public void Parse_String_GivesItsParts(string value, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond, int expectedOffsetMinutes, bool expectedIsOffsetUnknown, bool expectedHasDayOfWeek)
        {
            // act
            var testResult = ImfDateTime.Parse(value);

            // assert
            testResult.Year.Should().Be(expectedYear);
            testResult.Month.Should().Be(expectedMonth);
            testResult.Day.Should().Be(expectedDay);
            testResult.Hour.Should().Be(expectedHour);
            testResult.Minute.Should().Be(expectedMinute);
            testResult.Second.Should().Be(expectedSecond);
            testResult.Offset.Should().Be(TimeSpan.FromMinutes(expectedOffsetMinutes));
            testResult.IsOffsetUnknown.Should().Be(expectedIsOffsetUnknown);
            testResult.HasDayOfWeek.Should().Be(expectedHasDayOfWeek);
        }

        [Theory]
        [MemberData(nameof(Parts))]
        public void TryParse_String_GivesItsParts(string value, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond, int expectedOffsetMinutes, bool expectedIsOffsetUnknown, bool expectedHasDayOfWeek)
        {
            // act
            var testResult = ImfDateTime.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Year.Should().Be(expectedYear);
            result.Month.Should().Be(expectedMonth);
            result.Day.Should().Be(expectedDay);
            result.Hour.Should().Be(expectedHour);
            result.Minute.Should().Be(expectedMinute);
            result.Second.Should().Be(expectedSecond);
            result.Offset.Should().Be(TimeSpan.FromMinutes(expectedOffsetMinutes));
            result.IsOffsetUnknown.Should().Be(expectedIsOffsetUnknown);
            result.HasDayOfWeek.Should().Be(expectedHasDayOfWeek);
        }

        [Theory]
        [MemberData(nameof(ObsoleteZones))]
        public void Parse_ObsoleteZone_GivesItsOffset(string value, int expectedOffsetMinutes, bool expectedIsOffsetUnknown)
        {
            // act
            var testResult = ImfDateTime.Parse(value);

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromMinutes(expectedOffsetMinutes));
            testResult.IsOffsetUnknown.Should().Be(expectedIsOffsetUnknown);
        }

        [Fact]
        public void Parse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of(Rfc5322Examples.A11Date);

            // act
            var testResult = ImfDateTime.Parse(value);

            // assert
            testResult.Year.Should().Be(1997);
            testResult.Second.Should().Be(6);
        }

        [Fact]
        public void TryParse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of(Rfc5322Examples.A11Date);

            // act
            var testResult = ImfDateTime.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Year.Should().Be(1997);
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfDateTime.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfDateTime.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(ImfDateTime));
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidStringAllowingInvalidValues_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfDateTime.TryParse(value, AllowInvalidValues, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("(\x00FF) 1 Jan 2000 00:00 +0000");

            // act
            Action testAction = () => ImfDateTime.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfDateTime.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfDateTime.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_NullStringWithOptions_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfDateTime.TryParse(value, AllowInvalidValues, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_NullStringWithOptions_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfDateTime.Parse(value, AllowInvalidValues);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Theory]
        [MemberData(nameof(NullOptionsCalls))]
        public void Parse_NullOptions_ThrowsArgumentNullException(Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("options");
        }

        [Theory]
        [MemberData(nameof(WrongDayOfWeekValues))]
        public void Parse_WrongDayOfWeek_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfDateTime.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(WrongDayOfWeekValues))]
        public void Parse_WrongDayOfWeekWithDefaultOptions_ThrowsFormatException(string value)
        {
            // arrange
            var options = new ImfOptions();

            // act
            Action testAction = () => ImfDateTime.Parse(value, options);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(WrongDaysOfWeek))]
        public void Parse_WrongDayOfWeekAllowingInvalidValues_WritesTheDayOfTheDate(string value, string expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(value, AllowInvalidValues);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Parse_BytesWithWrongDayOfWeekAllowingInvalidValues_GivesTheDayOfTheDate()
        {
            // arrange
            var value = Octets.Of(WrongDayOfWeekDate);

            // act
            var testResult = ImfDateTime.Parse(value, AllowInvalidValues);

            // assert
            testResult.HasDayOfWeek.Should().BeTrue();
            testResult.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        }

        [Fact]
        public void TryParse_BytesWithWrongDayOfWeek_ReturnsFalse()
        {
            // arrange
            var value = Octets.Of(WrongDayOfWeekDate);

            // act
            var testResult = ImfDateTime.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_StringWithWrongDayOfWeekAllowingInvalidValues_ReturnsTrue()
        {
            // act
            var testResult = ImfDateTime.TryParse(WrongDayOfWeekDate, AllowInvalidValues, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        }

        [Fact]
        public void AllowInvalidValues_Default_IsFalse()
        {
            // act
            var testResult = new ImfOptions();

            // assert
            testResult.AllowInvalidValues.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DaysOfWeek))]
        public void DayOfWeek_Parsed_IsTheDayOfTheDate(string value, DayOfWeek expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(value);

            // act
            var testResult = subject.DayOfWeek;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(Rfc5322Examples.A4ReceivedDate, true, "Fri, 21 Nov 1997 10:05:43 -0600")]
        [InlineData(Rfc5322Examples.A4ReceivedDate, false, Rfc5322Examples.A4ReceivedDate)]
        [InlineData(Rfc5322Examples.A11Date, true, Rfc5322Examples.A11Date)]
        [InlineData(Rfc5322Examples.A11Date, false, Rfc5322Examples.A11Date)]
        public void ToString_AskedForTheDayOfWeek_AddsItToOneNotGiven(string value, bool includeDayOfWeek, string expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(value);

            // act
            var testResult = subject.ToString(includeDayOfWeek);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Conversions))]
        public void ToDateTimeOffset_KnownOffset_GivesTheDateTime(string value, int year, int month, int day, int hour, int minute, int second, int offsetMinutes)
        {
            // arrange
            var subject = ImfDateTime.Parse(value);
            var expected = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.FromMinutes(offsetMinutes));

            // act
            var testResult = subject.ToDateTimeOffset();

            // assert
            testResult.Should().Be(expected);
            testResult.Offset.Should().Be(expected.Offset);
        }

        [Fact]
        public void ToDateTimeOffset_UnknownOffset_ThrowsInvalidOperationException()
        {
            // arrange
            var subject = ImfDateTime.Parse(UnknownOffsetDate);

            // act
            Action testAction = () => subject.ToDateTimeOffset();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToDateTimeOffset_UnknownOffsetNotTreatedAsUniversalTime_ThrowsInvalidOperationException()
        {
            // arrange
            var subject = ImfDateTime.Parse(UnknownOffsetDate);

            // act
            Action testAction = () => subject.ToDateTimeOffset(false);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToDateTimeOffset_UnknownOffsetTreatedAsUniversalTime_GivesUniversalTime()
        {
            // arrange
            var subject = ImfDateTime.Parse(UnknownOffsetDate);
            var expected = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            // act
            var testResult = subject.ToDateTimeOffset(true);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToDateTimeOffset_UniversalTime_NeedsNoPermission()
        {
            // arrange
            var subject = ImfDateTime.Parse(UniversalTimeDate);
            var expected = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            // act
            var testResult = subject.ToDateTimeOffset(false);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToDateTimeOffset_MilitaryZone_ThrowsInvalidOperationException()
        {
            // arrange
            var subject = ImfDateTime.Parse("1 Jan 2000 00:00 A");

            // act
            Action testAction = () => subject.ToDateTimeOffset();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(NotRepresentableValues))]
        public void ToDateTimeOffset_NotRepresentable_ThrowsInvalidOperationException(string value)
        {
            // arrange
            var subject = ImfDateTime.Parse(value);

            // act
            Action testAction = () => subject.ToDateTimeOffset();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_DateTimeOffset_TakesItsPartsAndOffset()
        {
            // arrange
            var value = new DateTimeOffset(1997, 11, 21, 9, 55, 6, 789, TimeSpan.FromMinutes(-210));

            // act
            var testResult = new ImfDateTime(value);

            // assert
            testResult.Year.Should().Be(1997);
            testResult.Month.Should().Be(11);
            testResult.Day.Should().Be(21);
            testResult.Hour.Should().Be(9);
            testResult.Minute.Should().Be(55);
            testResult.Second.Should().Be(6);
            testResult.Offset.Should().Be(TimeSpan.FromMinutes(-210));
            testResult.HasDayOfWeek.Should().BeFalse();
            testResult.IsOffsetUnknown.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ZeroOffset_IsAKnownOffset()
        {
            // arrange
            var value = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var expected = ImfDateTime.Parse(UniversalTimeDate);

            // act
            var testResult = new ImfDateTime(value);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Constructor_EarliestYear_IsAccepted()
        {
            // arrange
            var value = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

            // act
            var testResult = new ImfDateTime(value);

            // assert
            testResult.Year.Should().Be(1900);
        }

        [Fact]
        public void Constructor_YearBefore1900_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var value = new DateTimeOffset(1899, 12, 31, 0, 0, 0, TimeSpan.Zero);

            // act
            Action testAction = () => new ImfDateTime(value);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_DateTime_ComparesEveryPart(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void EqualityOperator_DateTime_ComparesEveryPart(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(InequalityCases))]
        public void InequalityOperator_DateTime_ComparesEveryPart(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Equals_ObjectOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfDateTime.Parse(SimpleDate);
            object other = ImfDateTime.Parse(SimpleDate);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_ObjectOfAnotherType_ReturnsFalse()
        {
            // arrange
            var subject = ImfDateTime.Parse(SimpleDate);
            object other = SimpleDate;

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_EqualValues_AreTheSame()
        {
            // arrange
            var subject = ImfDateTime.Parse("1 Jan 2000 00:00 UT");
            var expected = ImfDateTime.Parse("1 Jan 00 00:00:00 +0000").GetHashCode();

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(OrderingCases))]
        public void CompareTo_DateTime_OrdersByMoment(string left, string right, int expectedSign)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject.CompareTo(other);

            // assert
            Math.Sign(testResult).Should().Be(expectedSign);
        }

        [Theory]
        [InlineData(SimpleDate, SimpleDate, false)]
        [InlineData(SimpleDate, OneSecondLaterDate, true)]
        [InlineData(OneSecondLaterDate, SimpleDate, false)]
        public void LessThanOperator_DateTime_FollowsTheOrdering(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject < other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(SimpleDate, SimpleDate, true)]
        [InlineData(SimpleDate, OneSecondLaterDate, true)]
        [InlineData(OneSecondLaterDate, SimpleDate, false)]
        public void LessThanOrEqualOperator_DateTime_FollowsTheOrdering(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject <= other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(SimpleDate, SimpleDate, false)]
        [InlineData(SimpleDate, OneSecondLaterDate, false)]
        [InlineData(OneSecondLaterDate, SimpleDate, true)]
        public void GreaterThanOperator_DateTime_FollowsTheOrdering(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject > other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(SimpleDate, SimpleDate, true)]
        [InlineData(SimpleDate, OneSecondLaterDate, false)]
        [InlineData(OneSecondLaterDate, SimpleDate, true)]
        public void GreaterThanOrEqualOperator_DateTime_FollowsTheOrdering(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfDateTime.Parse(left);
            var other = ImfDateTime.Parse(right);

            // act
            var testResult = subject >= other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(UninitialisedMembers))]
        public void Member_Uninitialised_ThrowsInvalidOperationException(Func<ImfDateTime, object> member)
        {
            // arrange
            var subject = default(ImfDateTime);

            // act
            Action testAction = () => member(subject);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Equals_TwoUninitialised_ReturnsTrue()
        {
            // arrange
            var subject = default(ImfDateTime);

            // act
            var testResult = subject.Equals(new ImfDateTime());

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_UninitialisedAndInitialised_ReturnsFalse()
        {
            // arrange
            var subject = default(ImfDateTime);
            var other = ImfDateTime.Parse(SimpleDate);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_Uninitialised_IsZero()
        {
            // arrange
            var subject = default(ImfDateTime);

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void ToString_Uninitialised_GivesTheNameOfTheType()
        {
            // arrange
            var subject = default(ImfDateTime);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be("DataStandardizer.File.InternetMessage.ImfDateTime");
        }

        [Fact]
        public void CompareTo_TwoUninitialised_IsZero()
        {
            // arrange
            var subject = default(ImfDateTime);

            // act
            var testResult = subject.CompareTo(new ImfDateTime());

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void CompareTo_UninitialisedAndInitialised_PutsUninitialisedFirst()
        {
            // arrange
            var subject = default(ImfDateTime);
            var other = ImfDateTime.Parse(SimpleDate);

            // act
            var testResult = subject.CompareTo(other);

            // assert
            Math.Sign(testResult).Should().Be(-1);
        }

        [Fact]
        public void CompareTo_InitialisedAndUninitialised_PutsUninitialisedFirst()
        {
            // arrange
            var subject = ImfDateTime.Parse(SimpleDate);

            // act
            var testResult = subject.CompareTo(default(ImfDateTime));

            // assert
            Math.Sign(testResult).Should().Be(1);
        }
    }
}
