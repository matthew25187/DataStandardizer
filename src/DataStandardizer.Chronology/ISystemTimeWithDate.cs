namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a system time abstraction that includes date components such as year, month, and day.
    /// </summary>
    /// <remarks>
    /// This interface extends <see cref="DataStandardizer.Chronology.ISystemTime"/> by adding properties 
    /// for accessing the year, month, and day components of the system time.
    /// </remarks>
    public interface ISystemTimeWithDate : ISystemTime
    {
        /// <summary>
        /// Gets the day component of the system time.
        /// </summary>
        /// <value>
        /// An integer representing the day of the month, ranging from 1 to the maximum number of days in the specified month.
        /// </value>
        /// <remarks>
        /// This property provides the day component of the date represented by the system time.
        /// It is part of the <see cref="DataStandardizer.Chronology.ISystemTimeWithDate"/> interface, 
        /// which extends <see cref="DataStandardizer.Chronology.ISystemTime"/> to include date-specific properties.
        /// </remarks>
        int Day { get; }

        /// <summary>
        /// Gets the month component of the system time.
        /// </summary>
        /// <value>
        /// An integer representing the month of the year, where 1 corresponds to January and 12 corresponds to December.
        /// </value>
        /// <remarks>
        /// This property provides the month component of the system time, as determined by the underlying implementation.
        /// For example, in the <see cref="SystemTimeWithGregorianCalendar"/> implementation, the month is derived from
        /// the Julian Day Number using the Gregorian calendar.
        /// </remarks>
        int Month { get; }

        /// <summary>
        /// Gets the year component of the system time.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> representing the year component of the system time.
        /// </value>
        /// <remarks>
        /// This property provides the year component of the system time, derived from the Julian Day Number.
        /// It is part of the <see cref="DataStandardizer.Chronology.ISystemTimeWithDate"/> interface, which extends
        /// <see cref="DataStandardizer.Chronology.ISystemTime"/> by including date-specific components.
        /// </remarks>
        int Year { get; }
    }
}