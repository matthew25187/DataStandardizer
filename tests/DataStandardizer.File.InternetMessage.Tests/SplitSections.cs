using System.Collections;
using System.Collections.Generic;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// A header section with a fold, CRLFs and multi-byte UTF-8 characters, and the ways of splitting it across refills: a first read that ends
    /// after each of its bytes, with a block of several sizes.  Each case holds the length of the first read and the size of the block.
    /// </summary>
    internal sealed class SplitSections : IEnumerable<object[]>
    {
        private const string SectionText = "Subject: caf\xC3\xA9\r\n \xE2\x82\xAC 1\r\nTo :x\r\nB:\r\n\r\n";

        private static readonly int[] BufferSizes = { 1, 2, 3, 5, 8, 64, 4096 };

        public static readonly byte[] Section = Octets.Of(SectionText);

        public static readonly FieldSnapshot[] ExpectedFields =
        {
            new FieldSnapshot("Subject: caf\xC3\xA9\r\n \xE2\x82\xAC 1\r\n", "Subject", " caf\xC3\xA9 \xE2\x82\xAC 1", " caf\xE9 \x20AC 1", 0, 1),
            new FieldSnapshot("To :x\r\n", "To", "x", "x", 24, 3),
            new FieldSnapshot("B:\r\n", "B", "", "", 31, 4),
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var bufferSize in BufferSizes)
            {
                for (var firstReadLength = 1; firstReadLength <= Section.Length; firstReadLength++)
                {
                    yield return new object[] { firstReadLength, bufferSize };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
