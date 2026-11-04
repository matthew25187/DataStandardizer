using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Runs blocks of a body through an <see cref="ImfBodyCheck"/>, and gives what it found as text, so that a test can compare it with one literal.
    /// </summary>
    internal static class BodyChecks
    {
        /// <summary>
        /// Checks the blocks, and ends the body.
        /// </summary>
        /// <returns>What was found, in order and separated by spaces: <c>long:1:3</c> and <c>invalid:1:3</c> for a problem at offset 3 of block 1, and <c>end</c> for a bare CR that ended the body.</returns>
        public static string Run(ImfBodyCheck check, string[] blocks)
        {
            var found = new List<string>();
            for (var block = 0; block < blocks.Length; block++)
            {
                check.Check(Octets.Of(blocks[block]), out var foundLongLine, out var foundInvalidOctet, out var longLineOffset, out var invalidOctetOffset);
                if (foundLongLine) found.Add(string.Format(CultureInfo.InvariantCulture, "long:{0}:{1}", block, longLineOffset));
                if (foundInvalidOctet) found.Add(string.Format(CultureInfo.InvariantCulture, "invalid:{0}:{1}", block, invalidOctetOffset));
            }

            if (check.End()) found.Add("end");

            return string.Join(" ", found);
        }
    }
}
