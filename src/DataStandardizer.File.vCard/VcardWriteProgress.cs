using System;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The progress of a writer.  <see cref="VcardContentLineWriter"/> reports it after each line a bulk write hands to the underlying
    /// <see cref="System.IO.TextWriter"/>.
    /// </summary>
    /// <remarks>
    /// Counts are cumulative for the writer, not for the call that reports them.
    /// </remarks>
    public readonly struct VcardWriteProgress : IEquatable<VcardWriteProgress>
    {
        /// <summary>
        /// Creates a progress value.
        /// </summary>
        /// <param name="linesWritten">The number of logical (unfolded) content lines written.</param>
        public VcardWriteProgress(long linesWritten)
        {
            LinesWritten = linesWritten;
        }

        /// <summary>
        /// Gets the number of logical (unfolded) content lines the writer has written so far.
        /// </summary>
        public long LinesWritten { get; }

        /// <summary>
        /// Indicates whether this progress value equals another.
        /// </summary>
        /// <param name="other">The other progress value.</param>
        /// <returns><c>true</c> if the counts are equal; otherwise <c>false</c>.</returns>
        public bool Equals(VcardWriteProgress other)
        {
            return LinesWritten == other.LinesWritten;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardWriteProgress other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return LinesWritten.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "LinesWritten = {0}", LinesWritten);
        }

        /// <summary>
        /// Indicates whether two progress values are equal.
        /// </summary>
        public static bool operator ==(VcardWriteProgress left, VcardWriteProgress right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two progress values differ.
        /// </summary>
        public static bool operator !=(VcardWriteProgress left, VcardWriteProgress right)
        {
            return !left.Equals(right);
        }
    }
}
