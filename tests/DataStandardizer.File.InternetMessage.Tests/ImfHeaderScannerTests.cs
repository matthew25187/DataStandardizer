using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of what <see cref="ImfHeaderScanner"/> asks of the driver that wraps it, which the readers don't show.
    /// </summary>
    public class ImfHeaderScannerTests
    {
        private const int BlockSize = 64;

        // The names of the values of ImfHeaderScanner.ScanStatus, which is internal, so it can't be a parameter of a public test.
        private const string FieldRead = nameof(ImfHeaderScanner.ScanStatus.FieldRead);
        private const string NeedMoreInput = nameof(ImfHeaderScanner.ScanStatus.NeedMoreInput);
        private const string EndOfHeader = nameof(ImfHeaderScanner.ScanStatus.EndOfHeader);
        private const string EndOfInput = nameof(ImfHeaderScanner.ScanStatus.EndOfInput);

        public static TheoryData<string[], string[]> Scans => new TheoryData<string[], string[]>
        {
            {
                new string[0],
                new[] { NeedMoreInput, EndOfInput }
            },
            {
                new[] { "\r\n" },
                new[] { NeedMoreInput, EndOfHeader }
            },
            {
                // The field is not complete until the next byte shows that it isn't folded.
                new[] { "A:1\r\n", "\r\n" },
                new[] { NeedMoreInput, NeedMoreInput, FieldRead, EndOfHeader }
            },
            {
                new[] { "A:1\r\n" },
                new[] { NeedMoreInput, NeedMoreInput, FieldRead, EndOfInput }
            },
            {
                new[] { "A:1\r", "\nB:2\r\n\r\n" },
                new[] { NeedMoreInput, NeedMoreInput, FieldRead, FieldRead, EndOfHeader }
            },
            {
                // Nothing after the empty line is looked at.
                new[] { "\r\n body" },
                new[] { NeedMoreInput, EndOfHeader }
            },
        };

        [Theory]
        [MemberData(nameof(Scans))]
        public void Scan_InputInBlocks_GivesEachStatus(string[] blocks, string[] expected)
        {
            // arrange
            var scanner = new ImfHeaderScanner(BlockSize, new ImfOptions(), Encoding.UTF8, false);

            // act
            var testResult = ScanThrough(scanner, blocks);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void Scan_SectionEnded_KeepsGivingTheSameStatus()
        {
            // arrange
            var scanner = new ImfHeaderScanner(BlockSize, new ImfOptions(), Encoding.UTF8, false);
            ScanThrough(scanner, new[] { "\r\n" });

            // act
            var testResult = scanner.Scan();

            // assert
            testResult.Should().Be(ImfHeaderScanner.ScanStatus.EndOfHeader);
        }

        /// <summary>
        /// Feeds the blocks to the scanner as a driver would, up to the status that ends the section.
        /// </summary>
        private static List<string> ScanThrough(ImfHeaderScanner scanner, string[] blocks)
        {
            var statuses = new List<string>();
            var nextBlock = 0;
            while (true)
            {
                var status = scanner.Scan();
                statuses.Add(status.ToString());
                if (status == ImfHeaderScanner.ScanStatus.EndOfHeader || status == ImfHeaderScanner.ScanStatus.EndOfInput) return statuses;
                if (status != ImfHeaderScanner.ScanStatus.NeedMoreInput) continue;

                scanner.PrepareRefill(out var offset, out _);
                if (nextBlock == blocks.Length)
                {
                    scanner.CompleteRefill(0);
                    continue;
                }

                var block = Octets.Of(blocks[nextBlock++]);
                Array.Copy(block, 0, scanner.Buffer, offset, block.Length);
                scanner.CompleteRefill(block.Length);
            }
        }
    }
}
