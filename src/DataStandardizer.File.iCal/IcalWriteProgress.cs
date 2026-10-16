using System;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The progress of a writer.  <see cref="IcalContentLineWriter"/> reports it after each line a bulk write hands to the underlying
    /// <see cref="System.IO.TextWriter"/>, and <see cref="IcalWriter"/>, <see cref="XcalWriter"/> and <c>JcalWriter</c> after each
    /// calendar.  For <see cref="XcalWriter"/> and <c>JcalWriter</c>, the lines counted are those the calendars would have as <c>.ics</c>
    /// data.
    /// </summary>
    /// <remarks>
    /// Counts are cumulative for the writer, not for the call that reports them.
    /// </remarks>
    public readonly struct IcalWriteProgress : IEquatable<IcalWriteProgress>
    {
        /// <summary>
        /// Creates a progress value.
        /// </summary>
        /// <param name="linesWritten">The number of logical (unfolded) content lines written.</param>
        public IcalWriteProgress(long linesWritten) : this(linesWritten, 0, 0)
        {
        }

        /// <summary>
        /// Creates a progress value.
        /// </summary>
        /// <param name="linesWritten">The number of logical (unfolded) content lines written.</param>
        /// <param name="componentsWritten">The number of components written, not counting the VCALENDAR objects.</param>
        /// <param name="calendarsWritten">The number of VCALENDAR objects written.</param>
        public IcalWriteProgress(long linesWritten, long componentsWritten, long calendarsWritten)
        {
            LinesWritten = linesWritten;
            ComponentsWritten = componentsWritten;
            CalendarsWritten = calendarsWritten;
        }

        /// <summary>
        /// Gets the number of VCALENDAR objects the writer has written so far; always 0 for <see cref="IcalContentLineWriter"/>.
        /// </summary>
        public long CalendarsWritten { get; }

        /// <summary>
        /// Gets the number of components the writer has written so far, at any depth and not counting the VCALENDAR objects; always 0 for
        /// <see cref="IcalContentLineWriter"/>.
        /// </summary>
        public long ComponentsWritten { get; }

        /// <summary>
        /// Gets the number of logical (unfolded) content lines the writer has written so far.
        /// </summary>
        public long LinesWritten { get; }

        /// <summary>
        /// Indicates whether this progress value equals another.
        /// </summary>
        /// <param name="other">The other progress value.</param>
        /// <returns><c>true</c> if all the counts are equal; otherwise <c>false</c>.</returns>
        public bool Equals(IcalWriteProgress other)
        {
            return LinesWritten == other.LinesWritten && ComponentsWritten == other.ComponentsWritten && CalendarsWritten == other.CalendarsWritten;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalWriteProgress other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (LinesWritten.GetHashCode() * 397 ^ ComponentsWritten.GetHashCode()) * 397 ^ CalendarsWritten.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "LinesWritten = {0}, ComponentsWritten = {1}, CalendarsWritten = {2}", LinesWritten,
                ComponentsWritten, CalendarsWritten);
        }

        /// <summary>
        /// Indicates whether two progress values are equal.
        /// </summary>
        public static bool operator ==(IcalWriteProgress left, IcalWriteProgress right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two progress values differ.
        /// </summary>
        public static bool operator !=(IcalWriteProgress left, IcalWriteProgress right)
        {
            return !left.Equals(right);
        }
    }
}
