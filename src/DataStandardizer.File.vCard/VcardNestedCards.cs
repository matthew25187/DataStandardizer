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
    /// RFC 2426 §4 says the value "MUST be a valid vCard object", so a nested card in a vCard 3.0 card is held to the rules of a card, under the options of the reader or writer of the card around
    /// it.  Its example in §3.5.4 has no <c>VERSION</c> or <c>N</c> and has a parameter with no name, so reading that takes <see cref="VcardOptions.AllowMissingRequiredProperties"/> and
    /// <see cref="VcardOptions.AcceptLegacySyntax"/>.  A line break is a line feed whatever the options, since §5 says that <c>\n</c> stands for the CRLF of a line.  It must be exactly one card, and
    /// the depth is limited, since each level of nesting is read again.
    /// </remarks>
    internal static class VcardNestedCards
    {
        // Nothing sets a limit.  Each level doubles the backslashes of the one inside it, so a real card never gets near this.
        private const int MaxDepth = 8;

        private const int NoFolding = int.MaxValue / 2;

        /// <summary>
        /// Gets the options for a card that the model already holds, which were judged as it was read or added.
        /// </summary>
        internal static readonly VcardOptions HeldCardOptions = new VcardOptions
        {
            MaxLineLength = null,
            AcceptBareLineFeed = true,
            AcceptLegacySyntax = true,
            AcceptMissingFinalLineBreak = true,
            AllowCardinalityViolations = true,
            AllowInvalidValues = true,
            AllowMissingRequiredProperties = true,
            AllowVersionNotFirst = true
        };

        /// <summary>
        /// Reads a card that the model already holds, in the text of a <c>RELATED;TYPE=agent</c>.
        /// </summary>
        /// <param name="text">The text, with its escaping as a value removed.</param>
        /// <param name="card">The card, if the text is one.</param>
        /// <returns><c>true</c> if the text is exactly one card that can be read.</returns>
        internal static bool TryReadHeld(string text, out VcardCard card)
        {
            return TryReadCore(text, HeldCardOptions, VcardVersionMapping.Canonical, 0, out card);
        }

        /// <summary>
        /// Reads a card held in the value of an <c>AGENT</c> of vCard 3.0, under the options of the card around it.
        /// </summary>
        /// <param name="text">The text, with its escaping as a value removed.</param>
        /// <param name="options">The options of the card around it.</param>
        /// <param name="depth">How many cards the card around it is nested in.</param>
        /// <param name="card">The card, if the text is one.</param>
        /// <returns><c>true</c> if the text is exactly one card that the options accept.</returns>
        internal static bool TryRead(string text, VcardOptions options, int depth, out VcardCard card)
        {
            var nestedOptions = new VcardOptions
            {
                // The text is inside a line that the reader has already limited, so none of its lines can be longer, and a writer, which ignores the limit,
                // reads it too, to check it.  Either way a limit here could only reject a card that the outer line's limit accepted.
                MaxLineLength = null,
                AcceptBareLineFeed = true,
                AcceptLegacySyntax = options.AcceptLegacySyntax,
                AcceptMissingFinalLineBreak = options.AcceptMissingFinalLineBreak,
                AllowCardinalityViolations = options.AllowCardinalityViolations,
                AllowInvalidValues = options.AllowInvalidValues,
                AllowMissingRequiredProperties = options.AllowMissingRequiredProperties,
                AllowVersionNotFirst = options.AllowVersionNotFirst
            };

            return TryReadCore(text, nestedOptions, VcardVersionMapping.Version30, depth, out card);
        }

        private static bool TryReadCore(string text, VcardOptions options, VcardVersionMapping defaultMapping, int depth, out VcardCard card)
        {
            card = null;
            if (depth >= MaxDepth) return false;

            try
            {
                using (var reader = new VcardReader(new StringReader(text), options, defaultMapping, depth + 1))
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
                MaxLineLength = null,
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
