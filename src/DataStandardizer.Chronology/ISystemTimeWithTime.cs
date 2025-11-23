namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a system time abstraction that includes time components such as hour, minute, and second.
    /// </summary>
    /// <remarks>
    /// This interface extends <see cref="DataStandardizer.Chronology.ISystemTime"/> by adding properties 
    /// for accessing the hour, minute, and second components of the system time.
    /// </remarks>
    public interface ISystemTimeWithTime : ISystemTime
    {
        /// <summary>
        /// Gets the hour component of the system time.
        /// </summary>
        /// <value>
        /// An integer representing the hour component of the time, ranging from 0 to 23.
        /// </value>
        /// <remarks>
        /// This property provides the hour component of the system time, which is derived 
        /// from the Julian Day Number representation.
        /// </remarks>
        int Hour { get; }

        /// <summary>
        /// Gets the minute component of the system time.
        /// </summary>
        /// <value>
        /// An integer representing the minute component of the time, ranging from 0 to 59.
        /// </value>
        /// <remarks>
        /// This property provides access to the minute portion of the system time, as defined by the implementation.
        /// </remarks>
        int Minute { get; }

        /// <summary>
        /// Gets the second component of the system time.
        /// </summary>
        /// <value>
        /// An integer representing the second component of the time, ranging from 0 to 59.
        /// </value>
        /// <remarks>
        /// This property provides access to the second component of the system time, 
        /// as part of the time abstraction defined by the <see cref="DataStandardizer.Chronology.ISystemTimeWithTime"/> interface.
        /// </remarks>
        int Second { get; }
    }
}