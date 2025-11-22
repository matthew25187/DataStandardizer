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
        /// An unsigned 16-bit integer representing the day of the month.
        /// </value>
        /// <remarks>
        /// The <see cref="Day"/> property provides the day component of the date, 
        /// which is calculated based on the Julian Day Number.
        /// </remarks>
        ushort Day { get; }

        /// <summary>
        /// Gets the month component of the system time.
        /// </summary>
        /// <value>
        /// An unsigned 16-bit integer representing the month, where 1 corresponds to January and 12 corresponds to December.
        /// </value>
        /// <remarks>
        /// The <see cref="Month"/> property provides the month component of the date represented by the system time.
        /// It is derived from the Julian Day Number.
        /// </remarks>
        ushort Month { get; }

        /// <summary>
        /// Gets the year component of the system time.
        /// </summary>
        /// <value>
        /// An unsigned 16-bit integer representing the year.
        /// </value>
        /// <remarks>
        /// The <see cref="Year"/> property provides the year component of the system time, 
        /// which is derived from the Julian Day Number.
        /// </remarks>
        ushort Year { get; }
    }
}