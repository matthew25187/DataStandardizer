using System;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A conformance problem found by <see cref="VcardCard.Validate"/>, or by a reader or writer.
    /// </summary>
    public sealed class VcardDiagnostic
    {
        /// <summary>
        /// Creates a diagnostic.
        /// </summary>
        /// <param name="kind">The kind of problem.</param>
        /// <param name="message">A description of the problem.</param>
        /// <param name="propertyName">The name of the property at fault; or <c>null</c> if the problem is not with one property.</param>
        /// <param name="group">The group of the property at fault; or <c>null</c> if it has none.</param>
        /// <param name="lineNumber">The 1-based number of the logical line the problem was found on; or 0 if the card was not read from a source.</param>
        /// <param name="cardIndex">The 0-based index of the card in its source; or -1 if the card was not read from a source.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardDiagnostic(VcardDiagnosticKind kind, string message, string? propertyName, string? group, long lineNumber, int cardIndex)
#else
        public VcardDiagnostic(VcardDiagnosticKind kind, string message, string propertyName, string group, long lineNumber, int cardIndex)
#endif
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Kind = kind;
            Message = message;
            PropertyName = propertyName;
            Group = group;
            LineNumber = lineNumber;
            CardIndex = cardIndex;
        }

        /// <summary>
        /// Gets the 0-based index of the card in its source; or -1 if the card was not read from a source.
        /// </summary>
        public int CardIndex { get; }

        /// <summary>
        /// Gets the group of the property at fault; or <c>null</c> if it has none.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group { get; }
#else
        public string Group { get; }
#endif

        /// <summary>
        /// Gets the kind of problem.
        /// </summary>
        public VcardDiagnosticKind Kind { get; }

        /// <summary>
        /// Gets the 1-based number of the logical line the problem was found on: the line of the property at fault, or the <c>BEGIN</c> line of the card.  0 if the card was not
        /// read from a source.
        /// </summary>
        public long LineNumber { get; }

        /// <summary>
        /// Gets a description of the problem.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the name of the property at fault; or <c>null</c> if the problem is not with one property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? PropertyName { get; }
#else
        public string PropertyName { get; }
#endif

        /// <summary>
        /// Gets the problem, with where it was found.
        /// </summary>
        public override string ToString()
        {
            return LineNumber == 0
                ? Message
                : string.Format(CultureInfo.InvariantCulture, "Line {0}: {1}", LineNumber, Message);
        }
    }
}
