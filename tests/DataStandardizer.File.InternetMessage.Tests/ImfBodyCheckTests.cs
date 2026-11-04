using System.Globalization;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfBodyCheckTests
    {
        // ref. RFC 5322 §2.1.1: a line is at most 998 octets, not counting its line break.
        private const int MaxLineLength = 998;

        // ref. RFC 2045 §6.7 and §6.8: a line of an encoded body is at most 76 characters.
        private const int MaxEncodedLineLength = 76;

        private const string CrLf = "\r\n";

        // Each side of the widths of a word and a vector, where a fast search could treat an octet differently.
        private static readonly int[] Positions = { 0, 1, 2, 7, 8, 9, 15, 16, 17, 31, 32, 33, 63, 64, 65, 127, 128, 129 };

        public static TheoryData<string, MimeDataDomain, bool, string> OctetsAtEachPosition
        {
            get
            {
                var data = new TheoryData<string, MimeDataDomain, bool, string>();
                foreach (var position in Positions)
                {
                    var before = new string('a', position);
                    var invalid = string.Format(CultureInfo.InvariantCulture, "invalid:0:{0}", position);

                    // ref. RFC 2045 §2.7: 7bit data has no octet above 127, and no NUL.
                    data.Add(before + "\x0080" + "aaa", MimeDataDomain.SevenBit, true, invalid);
                    data.Add(before + "\x00FF" + "aaa", MimeDataDomain.SevenBit, true, invalid);
                    data.Add(before + "\0" + "aaa", MimeDataDomain.SevenBit, true, invalid);
                    data.Add(before + "\x007F" + "aaa", MimeDataDomain.SevenBit, true, string.Empty);
                    data.Add(before + "\x0001" + "aaa", MimeDataDomain.SevenBit, true, string.Empty);

                    // ref. RFC 2045 §2.8: 8bit data may have octets above 127, and no NUL.
                    data.Add(before + "\x00FF" + "aaa", MimeDataDomain.EightBit, true, string.Empty);
                    data.Add(before + "\0" + "aaa", MimeDataDomain.EightBit, true, invalid);

                    // ref. RFC 5322 §2.3: no octet above 127 in a message without MIME.  §4.1: it may have a NUL.
                    data.Add(before + "\x0080" + "aaa", MimeDataDomain.SevenBit, false, invalid);
                    data.Add(before + "\0" + "aaa", MimeDataDomain.SevenBit, false, string.Empty);
                    data.Add(before + "\x00FF" + "aaa", MimeDataDomain.EightBit, false, string.Empty);
                    data.Add(before + "\0" + "aaa", MimeDataDomain.EightBit, false, string.Empty);

                    // ref. RFC 2045 §2.9: binary data has no rules.
                    data.Add(before + "\0" + "aaa", MimeDataDomain.Binary, true, string.Empty);
                    data.Add(before + "\x00FF" + "aaa", MimeDataDomain.Binary, true, string.Empty);
                }

                return data;
            }
        }

        public static TheoryData<string, string> OctetValues
        {
            get
            {
                var data = new TheoryData<string, string>();
                for (var octet = 0; octet < 256; octet++)
                {
                    if (octet == '\r' || octet == '\n') continue;

                    var expected = octet == 0 || octet > 127 ? "invalid:0:9" : string.Empty;
                    data.Add("aaaaaaaaa" + (char)octet + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", expected);
                }

                return data;
            }
        }

        public static TheoryData<string, bool, bool, string> LineBreaksAtEachPosition
        {
            get
            {
                var data = new TheoryData<string, bool, bool, string>();
                foreach (var position in Positions)
                {
                    var before = new string('a', position);
                    var atPosition = string.Format(CultureInfo.InvariantCulture, "invalid:0:{0}", position);
                    var afterPosition = string.Format(CultureInfo.InvariantCulture, "invalid:0:{0}", position + 1);

                    // ref. RFC 2045 §2.7: CR and LF only as a CRLF.
                    data.Add(before + CrLf + "aaa", true, false, string.Empty);
                    data.Add(before + "\raaa", true, false, afterPosition);
                    data.Add(before + "\naaa", true, false, atPosition);
                    data.Add(before + "\r", true, false, "end");
                    data.Add(before + "\r\r", true, false, afterPosition);

                    // ref. RFC 5322 §4.1: bare CR and LF are obsolete, and a receiver must accept them, without MIME.
                    data.Add(before + "\raaa", false, false, string.Empty);
                    data.Add(before + "\naaa", false, false, string.Empty);
                    data.Add(before + "\r", false, false, string.Empty);

                    // ImfOptions.AcceptBareLineFeed takes an LF for a line break, and nothing else.
                    data.Add(before + "\naaa", true, true, string.Empty);
                    data.Add(before + "\raaa", true, true, afterPosition);
                }

                return data;
            }
        }

        public static TheoryData<string[], int, string> Lines => new TheoryData<string[], int, string>
        {
            { new[] { new string('a', MaxLineLength) }, MaxLineLength, string.Empty },
            { new[] { new string('a', MaxLineLength + 1) }, MaxLineLength, "long:0:998" },
            { new[] { new string('a', MaxLineLength + 100) }, MaxLineLength, "long:0:998" },
            { new[] { new string('a', MaxLineLength) + CrLf + new string('a', MaxLineLength) }, MaxLineLength, string.Empty },
            { new[] { "a" + CrLf + new string('a', MaxLineLength + 1) }, MaxLineLength, "long:0:1001" },
            { new[] { new string('a', MaxEncodedLineLength) + CrLf + new string('a', MaxEncodedLineLength) + CrLf }, MaxEncodedLineLength, string.Empty },
            { new[] { new string('a', MaxEncodedLineLength + 1) }, MaxEncodedLineLength, "long:0:76" },

            // A line goes on across blocks.
            { new[] { new string('a', 500), new string('a', 498) }, MaxLineLength, string.Empty },
            { new[] { new string('a', 500), new string('a', 499) }, MaxLineLength, "long:1:498" },
            { new[] { new string('a', MaxLineLength), "a" }, MaxLineLength, "long:1:0" },

            // A line break ends a line, wherever the blocks are split.
            { new[] { new string('a', MaxLineLength), CrLf + new string('a', MaxLineLength) }, MaxLineLength, string.Empty },
            { new[] { new string('a', MaxLineLength) + "\r", "\n" + new string('a', MaxLineLength) }, MaxLineLength, string.Empty },

            // A bare CR isn't a line break, and doesn't count towards the length.
            { new[] { new string('a', MaxLineLength) + "\ra" }, MaxLineLength, "long:0:999 invalid:0:999" },
            { new[] { new string('a', 500) + "\r" + new string('a', 498) }, MaxLineLength, "invalid:0:501" },

            // Each problem is given once for each body, and the first of a kind is the one given.
            { new[] { new string('a', MaxLineLength + 1) + CrLf + new string('a', MaxLineLength + 1) }, MaxLineLength, "long:0:998" },
            { new[] { new string('a', MaxLineLength + 1), new string('a', MaxLineLength + 1) }, MaxLineLength, "long:0:998" },
            { new[] { "a\x0080\x0080" }, MaxLineLength, "invalid:0:1" },
            { new[] { "a\x0080", "\x0080" }, MaxLineLength, "invalid:0:1" },
            { new[] { "\x0080" + CrLf + "\0" }, MaxLineLength, "invalid:0:0" },
            { new[] { "\x0080\r" }, MaxLineLength, "invalid:0:0" },

            // Lines are still measured after a problem of the other kind, and the reverse.
            { new[] { "\x0080" + new string('a', MaxLineLength - 1) }, MaxLineLength, "invalid:0:0" },
            { new[] { "\x0080" + new string('a', MaxLineLength) }, MaxLineLength, "long:0:998 invalid:0:0" },
            { new[] { "\x0080" + new string('a', MaxLineLength - 1) + CrLf + new string('a', MaxLineLength) }, MaxLineLength, "invalid:0:0" },
            { new[] { new string('a', MaxLineLength + 1) + "\x0080" }, MaxLineLength, "long:0:998 invalid:0:999" },
            { new[] { new string('a', MaxLineLength + 1), "\r" }, MaxLineLength, "long:0:998 end" },
        };

        public static TheoryData<string[], bool, string> LineFeedsAsLineBreaks => new TheoryData<string[], bool, string>
        {
            { new[] { new string('a', MaxLineLength) + "\n" + new string('a', MaxLineLength) }, true, string.Empty },
            { new[] { new string('a', MaxLineLength), "\n" + new string('a', MaxLineLength) }, true, string.Empty },
            { new[] { new string('a', MaxLineLength + 1) + "\n" }, true, "long:0:998" },

            // A CRLF still ends a line, and a bare CR is still not a line break.
            { new[] { "a" + CrLf + "a" }, true, string.Empty },
            { new[] { "a\r", "\na" }, true, string.Empty },
            { new[] { "a\ra" }, true, "invalid:0:2" },
            { new[] { "a\r" }, true, "end" },
            { new[] { "a\ra" }, false, string.Empty },

            // Without MIME, each LF ends a line as well.
            { new[] { new string('a', MaxLineLength) + "\n" + new string('a', MaxLineLength) }, false, string.Empty },
        };

        public static TheoryData<string[], string> SplitBlocks
        {
            get
            {
                var data = new TheoryData<string[], string>();
                const string Plain = "abc\r\ndefghijk\r\nlmn";
                for (var split = 0; split <= Plain.Length; split++)
                {
                    data.Add(new[] { Plain.Substring(0, split), Plain.Substring(split) }, string.Empty);
                }

                // The problem is at index 12, so it's in the first block or the second by where the split is.
                const string WithOctet = "abc\r\ndefghij\x0080klm\r\nn";
                for (var split = 0; split <= WithOctet.Length; split++)
                {
                    var expected = split > 12 ? "invalid:0:12" : string.Format(CultureInfo.InvariantCulture, "invalid:1:{0}", 12 - split);
                    data.Add(new[] { WithOctet.Substring(0, split), WithOctet.Substring(split) }, expected);
                }

                // A bare CR is found at the octet after it, at index 4.
                const string WithBareCarriageReturn = "abc\rdefgh\r\ni";
                for (var split = 0; split <= WithBareCarriageReturn.Length; split++)
                {
                    var expected = split > 4 ? "invalid:0:4" : string.Format(CultureInfo.InvariantCulture, "invalid:1:{0}", 4 - split);
                    data.Add(new[] { WithBareCarriageReturn.Substring(0, split), WithBareCarriageReturn.Substring(split) }, expected);
                }

                // A bare LF is found where it is, at index 3.
                const string WithBareLineFeed = "abc\ndefgh\r\ni";
                for (var split = 0; split <= WithBareLineFeed.Length; split++)
                {
                    var expected = split > 3 ? "invalid:0:3" : string.Format(CultureInfo.InvariantCulture, "invalid:1:{0}", 3 - split);
                    data.Add(new[] { WithBareLineFeed.Substring(0, split), WithBareLineFeed.Substring(split) }, expected);
                }

                data.Add(new[] { "a\r", "\r\n" }, "invalid:1:0");
                data.Add(new[] { "a\r", string.Empty, "\nb" }, string.Empty);
                data.Add(new[] { "a\r", string.Empty }, "end");
                data.Add(new[] { "\r", "\n", "\r", "\n" }, string.Empty);
                data.Add(new[] { string.Empty }, string.Empty);
                data.Add(new string[0], string.Empty);
                return data;
            }
        }

        public static TheoryData<string, MimeDataDomain, bool> HeaderOctets
        {
            get
            {
                var data = new TheoryData<string, MimeDataDomain, bool>();
                foreach (var position in Positions)
                {
                    var before = new string('a', position);
                    data.Add(before + "\x0080" + "a", MimeDataDomain.SevenBit, true);
                    data.Add(before + "\x0080" + "a", MimeDataDomain.EightBit, false);
                    data.Add(before + "\x0080" + "a", MimeDataDomain.Binary, false);

                    // Only the octets above 127 are looked for here.
                    data.Add(before + "\0" + "a", MimeDataDomain.SevenBit, false);
                    data.Add(before + "\x007F" + "a", MimeDataDomain.SevenBit, false);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(OctetsAtEachPosition))]
        public void Check_OctetAtEachPosition_IsReportedAtItsOffset(string body, MimeDataDomain domain, bool isMime, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), false);
            check.Start(domain, isMime, MaxLineLength);

            // act
            var found = BodyChecks.Run(check, new[] { body });

            // assert
            found.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(OctetValues))]
        public void Check_EachOctetValue_FollowsTheRuleForSevenBitData(string body, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), false);
            check.Start(MimeDataDomain.SevenBit, true, MaxLineLength);

            // act
            var found = BodyChecks.Run(check, new[] { body });

            // assert
            found.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LineBreaksAtEachPosition))]
        public void Check_LineBreakAtEachPosition_IsReportedAtItsOffset(string body, bool isMime, bool acceptBareLineFeed, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions { AcceptBareLineFeed = acceptBareLineFeed }, false);
            check.Start(MimeDataDomain.SevenBit, isMime, MaxLineLength);

            // act
            var found = BodyChecks.Run(check, new[] { body });

            // assert
            found.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Lines))]
        public void Check_Lines_AreMeasuredAcrossBlocksAndProblems(string[] blocks, int maxLineLength, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), false);
            check.Start(MimeDataDomain.SevenBit, true, maxLineLength);

            // act
            var found = BodyChecks.Run(check, blocks);

            // assert
            found.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LineFeedsAsLineBreaks))]
        public void Check_LineFeedThatIsALineBreak_EndsTheLine(string[] blocks, bool isMime, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), true);
            check.Start(MimeDataDomain.SevenBit, isMime, MaxLineLength);

            // act
            var found = BodyChecks.Run(check, blocks);

            // assert
            found.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SplitBlocks))]
        public void Check_BodySplitAtAnyPoint_GivesTheSameProblems(string[] blocks, string expected)
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), false);
            check.Start(MimeDataDomain.SevenBit, true, MaxLineLength);

            // act
            var found = BodyChecks.Run(check, blocks);

            // assert
            found.Should().Be(expected);
        }

        [Fact]
        public void Start_AfterAProblem_ForgetsIt()
        {
            // arrange
            var check = new ImfBodyCheck(new ImfOptions(), false);
            check.Start(MimeDataDomain.SevenBit, true, MaxEncodedLineLength);
            BodyChecks.Run(check, new[] { new string('a', MaxEncodedLineLength + 1) + "\x0080" });
            check.Start(MimeDataDomain.SevenBit, true, MaxEncodedLineLength);

            // act
            var found = BodyChecks.Run(check, new[] { new string('a', MaxEncodedLineLength) });

            // assert
            found.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(HeaderOctets))]
        public void HasInvalidHeaderOctet_OctetAtEachPosition_FollowsTheEnclosingDomain(string header, MimeDataDomain enclosing, bool expected)
        {
            // act
            var found = ImfBodyCheck.HasInvalidHeaderOctet(Octets.Of(header), enclosing);

            // assert
            found.Should().Be(expected);
        }
    }
}
