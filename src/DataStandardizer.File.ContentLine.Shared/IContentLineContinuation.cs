using System;

namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// What happens at the end of a physical line, as an <see cref="IContentLineContinuation"/> decides.
    /// </summary>
    internal enum ContentLineContinuationKind
    {
        /// <summary>The usual rules apply: the next physical line continues this one only if it is a fold.</summary>
        None,

        /// <summary>The next physical line continues this one, whatever it starts with, and all of it is kept.  The data may end here.</summary>
        Join,

        /// <summary>As <see cref="Join"/>, but the logical line isn't complete until a later physical line gives <see cref="End"/>.  If the line ends any
        /// other way first, including at the end of the data, it is reported as <see cref="ContentLineErrorKind.MissingContinuationEnd"/>.</summary>
        JoinUntilEnd,

        /// <summary>The logical line ends here.  The next physical line starts a new one, even if it starts with white space.</summary>
        End,

        /// <summary>This physical line can't be part of the logical line.  The scanner reports
        /// <see cref="ContentLineErrorKind.InvalidContinuationLine"/> and is then faulted, since where the line ends is unknown.</summary>
        Invalid
    }

    /// <summary>
    /// Decides whether a logical line continues past the end of a physical line, for a format whose lines can continue other than by folding.
    /// </summary>
    /// <remarks>
    /// A format that doesn't need this, such as iCalendar, doesn't supply one.  The method may be called more than once for one physical line, when the
    /// scanner has to wait for more input, so it must give the same answer each time.
    /// </remarks>
    internal interface IContentLineContinuation
    {
        /// <summary>
        /// Gets what happens at the end of a physical line.
        /// </summary>
        /// <param name="line">The logical line so far, unfolded, with the physical line just ended at its end and no line break.</param>
        /// <param name="physicalLineStart">The offset in <paramref name="line"/> at which the physical line starts; 0 for the first physical line.</param>
        /// <param name="dataLength">The length of <paramref name="line"/> to keep, which is how a soft line break's <c>=</c> is removed.  It is used only for
        /// <see cref="ContentLineContinuationKind.Join"/>, and is never less than <paramref name="physicalLineStart"/>.</param>
        /// <returns>What happens.</returns>
        ContentLineContinuationKind GetContinuation(ReadOnlySpan<char> line, int physicalLineStart, out int dataLength);
    }
}
