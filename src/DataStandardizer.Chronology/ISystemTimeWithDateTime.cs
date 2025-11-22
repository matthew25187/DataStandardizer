namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a system time abstraction that combines both date and time components.
    /// </summary>
    /// <remarks>
    /// This interface extends <see cref="DataStandardizer.Chronology.ISystemTimeWithDate"/> and 
    /// <see cref="DataStandardizer.Chronology.ISystemTimeWithTime"/> to provide a unified representation 
    /// of system time, including both date and time details.
    /// </remarks>
    public interface ISystemTimeWithDateTime : ISystemTimeWithDate, ISystemTimeWithTime
    {

    }
}