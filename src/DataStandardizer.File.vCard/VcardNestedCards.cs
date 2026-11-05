#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System.IO;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Reads and writes the card that an <c>AGENT</c> of vCard 3.0 holds as text, which the model holds in the text of a <c>RELATED;TYPE=agent</c>.
    /// </summary>
    /// <remarks>
    /// RFC 2426 §4 says the value "MUST be a valid vCard object", but its example in §3.5.4 has no <c>VERSION</c> or <c>N</c>, has a parameter with no name, and ends its lines with a line
    /// feed, as a <c>\n</c> gives.  So a nested card is read with those accepted, whatever the options of the reader of the card around it.  The other forms stay limited: it must be
    /// exactly one card, and the depth is limited, since each level of nesting is read again.
    /// </remarks>
    internal static class VcardNestedCards
    {
        // Nothing sets a limit.  Each level doubles the backslashes of the one inside it, so a real card never gets near this.
        private const int MaxDepth = 8;

        private const int NoFolding = int.MaxValue / 2;

        private static readonly VcardOptions ReadOptions = new VcardOptions
        {
            AcceptBareLineFeed = true,
            AcceptLegacySyntax = true,
            AcceptMissingFinalLineBreak = true,
            AllowCardinalityViolations = true,
            AllowInvalidValues = true,
            AllowMissingRequiredProperties = true,
            AllowVersionNotFirst = true
        };

        /// <summary>
        /// Reads a nested card.
        /// </summary>
        /// <param name="text">The text, with its escaping as a value removed.</param>
        /// <param name="defaultMapping">The mapping of a card with no <c>VERSION</c>.</param>
        /// <param name="depth">How many cards the card around it is nested in.</param>
        /// <param name="card">The card, if the text is one.</param>
        /// <returns><c>true</c> if the text is exactly one card that can be read.</returns>
        internal static bool TryRead(string text, VcardVersionMapping defaultMapping, int depth, out VcardCard card)
        {
            card = null;
            if (depth >= MaxDepth) return false;

            try
            {
                using (var reader = new VcardReader(new StringReader(text), ReadOptions, defaultMapping, depth + 1))
                {
                    card = reader.ReadCard();
                    if (card is null || reader.ReadCard() != null)
                    {
                        card = null;
                        return false;
                    }
                }
            }
            catch (VcardFileException)
            {
                card = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the text of a nested card, with no escaping as a value yet, which each line break becomes.
        /// </summary>
        /// <param name="card">The card, which is written with whatever it holds, since what it can't hold is reported on the property it is in.</param>
        /// <param name="version">The version to write.</param>
        internal static string Write(VcardCard card, VcardVersion version)
        {
            var options = new VcardOptions
            {
                AllowCardinalityViolations = true,
                AllowInvalidValues = true,
                AllowLossyVersionConversion = true,
                AllowMissingRequiredProperties = true,
                FoldLength = NoFolding,
                LineBreak = "\n",
                WriteVersion = version
            };

            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options))
            {
                writer.WriteCard(card);
            }

            return output.ToString();
        }
    }
}
