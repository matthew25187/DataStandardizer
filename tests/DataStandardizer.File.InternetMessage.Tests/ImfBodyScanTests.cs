using System;
using System.IO;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Takes blocks of a body through an <see cref="ImfBodyScan"/>, and gives what it found as text, so that a test can compare it with one literal.
    /// </summary>
    internal static class BodyScans
    {
        /// <summary>
        /// Scans the blocks.
        /// </summary>
        /// <returns>The domain, then the size in quoted-printable with line breaks kept, the size with them encoded, the size in base64, and <c>bare</c>, <c>long</c> and <c>invalid</c> for each flag that is set.</returns>
        public static string Describe(string[] blocks, bool lineFeedIsLineBreak)
        {
            var scan = Scan(blocks, new ImfBoundary[0], lineFeedIsLineBreak);
            return scan.Domain + " " + scan.GetQuotedPrintableLength(false) + " " + scan.GetQuotedPrintableLength(true) + " " + scan.Base64Length
                + (scan.HasBareLineBreak ? " bare" : string.Empty) + (scan.HasLongLine ? " long" : string.Empty) + (scan.HasInvalidOctets ? " invalid" : string.Empty);
        }

        /// <summary>
        /// Scans a body that fits on one line of quoted-printable.
        /// </summary>
        /// <returns>The domain and the size in quoted-printable, and <c>invalid</c> if the flag is set.</returns>
        public static string DescribeOneLine(string body)
        {
            var scan = Scan(new[] { body }, new ImfBoundary[0], false);
            return scan.Domain + " " + scan.GetQuotedPrintableLength(false) + (scan.HasInvalidOctets ? " invalid" : string.Empty);
        }

        /// <summary>
        /// Scans the blocks, with some boundaries around them.
        /// </summary>
        /// <returns>The boundaries that a line of the blocks collides with, in the order they were found, separated by spaces.</returns>
        public static string FindCollisions(string[] blocks, string[] boundaries)
        {
            var enclosing = new ImfBoundary[boundaries.Length];
            for (var index = 0; index < boundaries.Length; index++)
            {
                enclosing[index] = new ImfBoundary(boundaries[index], false, new ImfBoundary[0]);
            }

            var collisions = Scan(blocks, enclosing, false).Collisions;
            var found = new string[collisions.Count];
            for (var index = 0; index < found.Length; index++)
            {
                found[index] = collisions[index].Value;
            }

            return string.Join(" ", found);
        }

        private static ImfBodyScan Scan(string[] blocks, ImfBoundary[] enclosing, bool lineFeedIsLineBreak)
        {
            var scan = new ImfBodyScan(ReadOnlyMemory<byte>.Empty, Stream.Null, enclosing, lineFeedIsLineBreak);
            scan.Begin(false);
            foreach (var block in blocks)
            {
                scan.Add(Octets.Of(block));
            }

            scan.Complete();
            return scan;
        }
    }

    public class ImfBodyScanTests
    {
        // ref. RFC 5322 §2.1.1: a line is at most 998 octets, not counting its line break.
        private const int MaxLineLength = 998;

        // Each side of the widths of a vector, where counting could treat an octet differently.
        private static readonly int[] Positions = { 0, 1, 15, 16, 17, 31, 32, 33, 63, 64, 65 };

        public static TheoryData<string[], bool, string> Bodies => new TheoryData<string[], bool, string>
        {
            { new string[0], false, "SevenBit 0 0 0" },
            { new[] { string.Empty }, false, "SevenBit 0 0 0" },

            // ref. RFC 2045 §6.7 rules 2 and 3: written as themselves.  ref. RFC 2045 §6.8: 4 characters for each 3 octets, or part of 3.
            { new[] { "a" }, false, "SevenBit 1 1 4" },
            { new[] { "abc" }, false, "SevenBit 3 3 4" },
            { new[] { "a b\tc" }, false, "SevenBit 5 5 8" },
            { new[] { "!~" }, false, "SevenBit 2 2 4" },

            // ref. RFC 2045 §6.7 rule 1: written as three characters.
            { new[] { "=" }, false, "SevenBit 3 3 4" },
            { new[] { "\x0001" }, false, "SevenBit 3 3 4" },
            { new[] { "\x001F" }, false, "SevenBit 3 3 4" },
            { new[] { "\x007F" }, false, "SevenBit 3 3 4" },
            { new[] { "\0" }, false, "Binary 3 3 4 invalid" },
            { new[] { "\x0080" }, false, "EightBit 3 3 4" },
            { new[] { "\x00FF" }, false, "EightBit 3 3 4" },

            // A line break counts as itself, or as three characters for each octet if it's encoded.
            { new[] { "a\r\nb" }, false, "SevenBit 4 8 8" },
            { new[] { "a\r\n" }, false, "SevenBit 3 7 4" },
            { new[] { "\r\n\r\n" }, false, "SevenBit 4 12 8" },

            // ref. RFC 2045 §2.7: CR and LF only as a CRLF.
            { new[] { "a\rb" }, false, "Binary 3 5 4 bare invalid" },
            { new[] { "a\nb" }, false, "Binary 3 5 4 bare invalid" },
            { new[] { "a\r" }, false, "Binary 2 4 4 bare invalid" },
            { new[] { "\r\r\n" }, false, "Binary 3 9 4 bare invalid" },
            { new[] { "\n" }, false, "Binary 1 3 4 bare invalid" },

            // A file whose lines end with LF.
            { new[] { "a\nb" }, true, "SevenBit 3 5 4" },
            { new[] { "a\r\nb" }, true, "Binary 4 8 8 bare invalid" },
            { new[] { "a\rb" }, true, "Binary 3 5 4 bare invalid" },

            // A line, and how much it takes to fold it (RFC 2045 §6.7 rule 5).
            { new[] { new string('a', 76) }, false, "SevenBit 76 76 106" },
            { new[] { new string('a', 77) }, false, "SevenBit 80 80 106" },
            { new[] { new string('a', 150) }, false, "SevenBit 153 153 204" },
            { new[] { new string('a', 151) }, false, "SevenBit 157 157 208" },
            { new[] { new string('a', 76) + "\r\n" + new string('a', 77) }, false, "SevenBit 158 165 212" },
            { new[] { new string('=', 26) }, false, "SevenBit 81 81 36" },

            // ref. RFC 5322 §2.1.1: the longest line.
            { new[] { new string('a', MaxLineLength) }, false, "SevenBit 1037 1037 1366" },
            { new[] { new string('a', MaxLineLength + 1) }, false, "Binary 1038 1038 1366 long" },
            { new[] { new string('a', 500), new string('a', 499) }, false, "Binary 1038 1038 1366 long" },
            { new[] { new string('a', MaxLineLength) + "\r\n" + new string('a', MaxLineLength) }, false, "SevenBit 2076 2080 2734" },

            // A bare CR doesn't end a line for any of this.
            { new[] { new string('a', 500) + "\r" + new string('a', 499) }, false, "Binary 1039 1041 1370 bare long invalid" },
        };

        public static TheoryData<string, string> OctetsAtEachPosition
        {
            get
            {
                var data = new TheoryData<string, string>();
                foreach (var position in Positions)
                {
                    var before = new string('a', position);

                    // ref. RFC 2045 §6.7 rule 1: three characters.
                    var encoded = position + 3;
                    data.Add(before + "=", "SevenBit " + encoded);
                    data.Add(before + "\x0001", "SevenBit " + encoded);
                    data.Add(before + "\x001F", "SevenBit " + encoded);
                    data.Add(before + "\x007F", "SevenBit " + encoded);
                    data.Add(before + "\x0080", "EightBit " + encoded);
                    data.Add(before + "\x00FF", "EightBit " + encoded);
                    data.Add(before + "\0", "Binary " + encoded + " invalid");

                    // ref. RFC 2045 §6.7 rules 2 and 3: written as themselves.
                    var literal = position + 1;
                    data.Add(before + " ", "SevenBit " + literal);
                    data.Add(before + "\t", "SevenBit " + literal);
                    data.Add(before + "~", "SevenBit " + literal);
                    data.Add(before + "!", "SevenBit " + literal);
                }

                return data;
            }
        }

        public static TheoryData<string, string> EachOctetValue
        {
            get
            {
                // 70 of an octet, across the width of a vector and past it.  70 characters need no folding, and 210 need two soft line breaks of 3 octets (RFC 2045 §6.7 rule 5).  In base64,
                // 70 octets are 24 quanta, and 96 characters need one line break (RFC 2045 §6.8).
                const string Literal = "SevenBit 70 70 98";
                const string Control = "SevenBit 216 216 98";
                const string EightBit = "EightBit 216 216 98";
                const string Nul = "Binary 216 216 98 invalid";

                var data = new TheoryData<string, string>();
                for (var octet = 0; octet < 256; octet++)
                {
                    if (octet == '\r' || octet == '\n') continue;

                    var isLiteral = (octet >= 33 && octet <= 126 && octet != '=') || octet == ' ' || octet == '\t';
                    data.Add(new string((char)octet, 70), octet == 0 ? Nul : octet >= 0x80 ? EightBit : isLiteral ? Literal : Control);
                }

                return data;
            }
        }

        public static TheoryData<string[], string> SplitBlocks
        {
            get
            {
                var data = new TheoryData<string[], string>();
                const string Lines = "abc=\r\n\x0080" + "de\r\nf";
                for (var split = 0; split <= Lines.Length; split++)
                {
                    data.Add(new[] { Lines.Substring(0, split), Lines.Substring(split) }, "EightBit 16 24 16");
                }

                const string BareCarriageReturn = "abc\rdef";
                for (var split = 0; split <= BareCarriageReturn.Length; split++)
                {
                    data.Add(new[] { BareCarriageReturn.Substring(0, split), BareCarriageReturn.Substring(split) }, "Binary 7 9 12 bare invalid");
                }

                data.Add(new[] { "a\r", "\nb" }, "SevenBit 4 8 8");
                data.Add(new[] { "a\r", string.Empty, "\nb" }, "SevenBit 4 8 8");
                data.Add(new[] { "\r", "\n", "\r", "\n" }, "SevenBit 4 12 8");
                data.Add(new[] { "a\r", "\r\n" }, "Binary 4 10 8 bare invalid");
                return data;
            }
        }

        public static TheoryData<string[], string> Collisions => new TheoryData<string[], string>
        {
            { new[] { "--b" }, "b" },
            { new[] { "--b\r\n" }, "b" },
            { new[] { "a\r\n--b\r\nc" }, "b" },
            { new[] { "--bc" }, "b" },
            { new[] { "--a" }, string.Empty },
            { new[] { "a--b" }, string.Empty },
            { new[] { "-a--b" }, string.Empty },
            { new[] { "a\r--b" }, string.Empty },
            { new[] { "a\r\n" }, string.Empty },
            { new[] { "-" }, string.Empty },
            { new[] { "--" }, string.Empty },

            // A line may end in another block than it started in, and a delimiter may be split, and the rest of a line that isn't one is passed over.
            { new[] { "-", "-", "b" }, "b" },
            { new[] { "--", "b\r\n" }, "b" },
            { new[] { "a\r", "\n--b" }, "b" },
            { new[] { "a\r\n", "--b" }, "b" },
            { new[] { "a", "b", "\r\n--b" }, "b" },
            { new[] { "a", "--b" }, string.Empty },
            { new[] { "xxxx", "xxxx\r\n", "xxxx\r\n", "--b" }, "b" },
            { new[] { "xxxx\r\nxxxx\r\n--b\r\nxxxx" }, "b" },
        };

        [Theory]
        [MemberData(nameof(Bodies))]
        public void Add_Body_GivesItsDomainAndSizes(string[] blocks, bool lineFeedIsLineBreak, string expected)
        {
            // act
            var description = BodyScans.Describe(blocks, lineFeedIsLineBreak);

            // assert
            description.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(OctetsAtEachPosition))]
        public void Add_OctetAtEachPosition_IsCountedOnce(string body, string expected)
        {
            // act
            var description = BodyScans.DescribeOneLine(body);

            // assert
            description.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EachOctetValue))]
        public void Add_EachOctetValue_FollowsRfc2045Section6Point7(string body, string expected)
        {
            // act
            var description = BodyScans.Describe(new[] { body }, false);

            // assert
            description.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SplitBlocks))]
        public void Add_BodySplitAtAnyPoint_GivesTheSameResult(string[] blocks, string expected)
        {
            // act
            var description = BodyScans.Describe(blocks, false);

            // assert
            description.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Collisions))]
        public void Add_LineThatStartsWithADelimiter_IsACollision(string[] blocks, string expected)
        {
            // act
            var found = BodyScans.FindCollisions(blocks, new[] { "b" });

            // assert
            found.Should().Be(expected);
        }

        [Fact]
        public void Add_SecondPassOverTheBytes_CountsThemOnce()
        {
            // arrange
            var scan = new ImfBodyScan(ReadOnlyMemory<byte>.Empty, Stream.Null, new ImfBoundary[0], false);
            scan.Begin(false);
            scan.Add(Octets.Of("abc"));
            scan.Complete();
            scan.Begin(false);

            // act
            scan.Add(Octets.Of("abc"));
            scan.Complete();

            // assert
            scan.Base64Length.Should().Be(4);
        }

        [Fact]
        public void Describe_AllFlagsSet_NamesThem()
        {
            // act
            var description = BodyScans.Describe(new[] { new string('a', MaxLineLength + 1) + "\r" }, false);

            // assert
            description.Should().Be("Binary 1039 1041 1370 bare long invalid");
        }

        [Fact]
        public void DescribeOneLine_InvalidOctet_NamesIt()
        {
            // act
            var description = BodyScans.DescribeOneLine("\0");

            // assert
            description.Should().Be("Binary 3 invalid");
        }

        [Fact]
        public void FindCollisions_TwoBoundaries_GivesEachThatCollides()
        {
            // act
            var found = BodyScans.FindCollisions(new[] { "--c\r\n--b\r\n--d" }, new[] { "b", "c" });

            // assert
            found.Should().Be("c b");
        }
    }
}
