namespace DataStandardizer.Communication
{
    /// <summary>
    /// Represents a telephony number abstraction, providing a base for various telecommunication number types.
    /// </summary>
    public interface ITelephonyNumber
    {
        /// <summary>
        /// Gets the numeric representation of the telephony number.
        /// </summary>
        /// <remarks>
        /// This property provides the raw numeric value of the telephony number, 
        /// which can be used for processing or validation purposes. The format 
        /// and interpretation of the number may vary depending on the specific 
        /// telephony number implementation.
        /// </remarks>
        ulong Number { get; }
    }
}