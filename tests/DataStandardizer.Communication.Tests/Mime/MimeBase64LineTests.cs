using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// Feeds an encoder blocks of a fixed size, with room for a fixed number of characters at a time, until it has taken them all.
    /// </summary>
    internal static class Base64Runs
    {
        public static string EncodeToUtf8(int dataLength, int blockSize, int destinationLength)
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var data = new byte[dataLength];
            var destination = new byte[destinationLength];
            var output = new StringBuilder();
            var position = 0;
            while (true)
            {
                var count = Math.Min(blockSize, dataLength - position);
                var isFinalBlock = position + count == dataLength;
                var status = encoder.Encode(new ReadOnlySpan<byte>(data, position, count), destination, out var consumed, out var written, isFinalBlock);
                output.Append(Encoding.ASCII.GetString(destination, 0, written));
                position += consumed;
                if (status == OperationStatus.Done && isFinalBlock) return output.ToString();
            }
        }

        public static string EncodeToChars(int dataLength, int blockSize, int destinationLength)
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var data = new byte[dataLength];
            var destination = new char[destinationLength];
            var output = new StringBuilder();
            var position = 0;
            while (true)
            {
                var count = Math.Min(blockSize, dataLength - position);
                var isFinalBlock = position + count == dataLength;
                var status = encoder.Encode(new ReadOnlySpan<byte>(data, position, count), destination, out var consumed, out var written, isFinalBlock);
                output.Append(destination, 0, written);
                position += consumed;
                if (status == OperationStatus.Done && isFinalBlock) return output.ToString();
            }
        }
    }

    public class MimeBase64LineTests
    {
        // ref. RFC 2045 §6.8: at most 76 characters to a line, which is 19 quanta of 57 octets.  A zero octet is "A", and each quantum of zeros is "AAAA".
        private const int OctetsPerLine = 57;
        private const string Line = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string LineBreak = "\r\n";

        // The most room that a quantum takes: its 4 characters, and the line break before it.
        private const int SmallestDestination = 6;

        private static readonly int[] BlockSizes = { 1, 2, 3, 4, 5, 56, 57, 58, 113, 114, 115, 1000 };
        private static readonly int[] DestinationLengths = { SmallestDestination, 7, 8, 9, 76, 77, 78, 79, 80, 81, 82, 1000 };

        public static TheoryData<int, int, int, string> Runs
        {
            get
            {
                var data = new TheoryData<int, int, int, string>();
                foreach (var blockSize in BlockSizes)
                {
                    foreach (var destinationLength in DestinationLengths)
                    {
                        // A line, with no line break after it, as nothing follows it.
                        data.Add(OctetsPerLine, blockSize, destinationLength, Line);

                        // One quantum past a line.
                        data.Add(OctetsPerLine + 3, blockSize, destinationLength, Line + LineBreak + "AAAA");

                        // A partial quantum past a line.
                        data.Add(OctetsPerLine + 1, blockSize, destinationLength, Line + LineBreak + "AA==");
                        data.Add(OctetsPerLine + 2, blockSize, destinationLength, Line + LineBreak + "AAA=");

                        // Several lines, ending on a line and part-way through one.
                        data.Add(OctetsPerLine * 3, blockSize, destinationLength, Line + LineBreak + Line + LineBreak + Line);
                        data.Add(OctetsPerLine * 3 + 4, blockSize, destinationLength, Line + LineBreak + Line + LineBreak + Line + LineBreak + "AAAAAA==");

                        // Less than a line.
                        data.Add(1, blockSize, destinationLength, "AA==");
                        data.Add(3, blockSize, destinationLength, "AAAA");
                    }
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Runs))]
        public void EncodeToUtf8_RunAcrossLines_BreaksLinesAt76CharactersHoweverItIsSplit(int dataLength, int blockSize, int destinationLength, string expected)
        {
            // act
            var encoded = Base64Runs.EncodeToUtf8(dataLength, blockSize, destinationLength);

            // assert
            encoded.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Runs))]
        public void EncodeToChars_RunAcrossLines_BreaksLinesAt76CharactersHoweverItIsSplit(int dataLength, int blockSize, int destinationLength, string expected)
        {
            // act
            var encoded = Base64Runs.EncodeToChars(dataLength, blockSize, destinationLength);

            // assert
            encoded.Should().Be(expected);
        }

        [Fact]
        public void EncodeToUtf8_DestinationEndsAtALineEnd_HoldsTheLineBreakUntilThereIsRoomForAQuantum()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new byte[Line.Length + SmallestDestination - 1];

            // act
            var status = encoder.Encode(new byte[OctetsPerLine + 3], destination, out var consumed, out var written, true);

            // assert
            status.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(OctetsPerLine);
            written.Should().Be(Line.Length);
        }

        [Fact]
        public void EncodeToChars_DestinationEndsAtALineEnd_HoldsTheLineBreakUntilThereIsRoomForAQuantum()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new char[Line.Length + SmallestDestination - 1];

            // act
            var status = encoder.Encode(new byte[OctetsPerLine + 3], destination, out var consumed, out var written, true);

            // assert
            status.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(OctetsPerLine);
            written.Should().Be(Line.Length);
        }

        [Fact]
        public void EncodeToUtf8_DestinationHasRoomForALineBreakAndAQuantum_WritesThem()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new byte[Line.Length + SmallestDestination];

            // act
            var status = encoder.Encode(new byte[OctetsPerLine + 3], destination, out var consumed, out var written, true);

            // assert
            status.Should().Be(OperationStatus.Done);
            consumed.Should().Be(OctetsPerLine + 3);
            written.Should().Be(Line.Length + SmallestDestination);
        }

        [Fact]
        public void EncodeToUtf8_LineEndedInAnEarlierCall_ContinuesOnANewLine()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new byte[Line.Length];
            encoder.Encode(new byte[OctetsPerLine], destination, out _, out _, false);
            var next = new byte[SmallestDestination];

            // act
            encoder.Encode(new byte[3], next, out _, out var written, true);

            // assert
            Encoding.ASCII.GetString(next, 0, written).Should().Be(LineBreak + "AAAA");
        }

        [Fact]
        public void EncodeToUtf8_LineEndedBeforeReset_StartsANewLineWithNoBreak()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new byte[Line.Length];
            encoder.Encode(new byte[OctetsPerLine], destination, out _, out _, false);
            encoder.Reset();
            var next = new byte[SmallestDestination];

            // act
            encoder.Encode(new byte[3], next, out _, out var written, true);

            // assert
            Encoding.ASCII.GetString(next, 0, written).Should().Be("AAAA");
        }

        [Fact]
        public void Base64Runs_ALineAndAQuantumInSmallPieces_GivesTheWholeOutput()
        {
            // act
            var encoded = Base64Runs.EncodeToUtf8(OctetsPerLine + 3, 2, SmallestDestination);

            // assert
            encoded.Should().Be(Line + LineBreak + "AAAA");
        }

        [Fact]
        public void Base64Runs_ALineAndAQuantumAsCharsInSmallPieces_GivesTheWholeOutput()
        {
            // act
            var encoded = Base64Runs.EncodeToChars(OctetsPerLine + 3, 2, SmallestDestination);

            // assert
            encoded.Should().Be(Line + LineBreak + "AAAA");
        }
    }
}
