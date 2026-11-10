using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Who acts for the entity of a card: a <c>RELATED</c> property with <c>TYPE=agent</c> (RFC 6350 §6.6.6), which is the <c>AGENT</c> of older versions.  Get one from <see cref="VcardCard.Agents"/>.
    /// </summary>
    /// <remarks>
    /// Its value is one of a URI, a card, or other text.
    /// </remarks>
    public sealed class VcardAgent
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardAgent(Uri? uri, VcardCard? card, string? text)
#else
        internal VcardAgent(Uri uri, VcardCard card, string text)
#endif
        {
            Uri = uri;
            Card = card;
            Text = text;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the URI; or <c>null</c> if the value is something else.
        /// </summary>
        public Uri? Uri { get; }

        /// <summary>
        /// Gets the card, read from the text of the property, so a change to it isn't a change to the property; or <c>null</c> if the value is something else.
        /// </summary>
        public VcardCard? Card { get; }

        /// <summary>
        /// Gets the text of a value that isn't a card or a URI; or <c>null</c> if the value is one of those.
        /// </summary>
        public string? Text { get; }
#else
        /// <summary>
        /// Gets the URI; or <c>null</c> if the value is something else.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Gets the card, read from the text of the property, so a change to it isn't a change to the property; or <c>null</c> if the value is something else.
        /// </summary>
        public VcardCard Card { get; }

        /// <summary>
        /// Gets the text of a value that isn't a card or a URI; or <c>null</c> if the value is one of those.
        /// </summary>
        public string Text { get; }
#endif
    }
}
