namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a system time abstraction that provides a Julian Day Number.
    /// </summary>
    /// <remarks>
    /// This interface serves as a base for defining various system time representations, 
    /// enabling conversions and calculations based on the Julian Day Number.
    /// </remarks>
    public interface ISystemTime
    {
        /// <summary>
        /// Gets the Julian Day Number, which is a continuous count of days since the beginning
        /// of the Julian Period (January 1, 4713 BCE, in the proleptic Julian calendar).
        /// </summary>
        /// <value>
        /// A <see cref="System.Decimal"/> representing the Julian Day Number.
        /// </value>
        /// <remarks>
        /// The Julian Day Number is commonly used in astronomical calculations and serves as a
        /// standard for representing dates and times in a continuous format.
        /// </remarks>
        decimal JulianDayNumber { get; }
    }
}