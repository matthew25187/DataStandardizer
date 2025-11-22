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
        /// An unsigned 16-bit integer representing the hour component of the system time, 
        /// ranging from 0 to 23.
        /// </value>
        /// <remarks>
        /// This property provides the hour component of the system time as defined by the 
        /// Julian Day Number. It is useful for time-based calculations and representations.
        /// </remarks>
        ushort Hour { get; }

        /// <summary>
        /// Gets the minute component of the system time.
        /// </summary>
        /// <value>
        /// An unsigned 16-bit integer representing the minute component, ranging from 0 to 59.
        /// </value>
        /// <remarks>
        /// This property provides access to the minute component of the system time, 
        /// allowing for precise time representation in conjunction with the hour and second components.
        /// </remarks>
        ushort Minute { get; }

        /// <summary>
        /// Gets the second component of the system time.
        /// </summary>
        /// <value>
        /// An unsigned 16-bit integer representing the second component of the time, ranging from 0 to 59.
        /// </value>
        /// <remarks>
        /// This property provides access to the second component of the system time, 
        /// complementing the <see cref="Hour"/> and <see cref="Minute"/> properties to represent the full time.
        /// </remarks>
        ushort Second { get; }
    }
}