namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents a field that adheres to the ITU E.164 standard for international telephone numbers.
    /// </summary>
    public interface IItuE164Field
    {
        /// <summary>
        /// Gets the total number of digits in the field value.
        /// </summary>
        /// <value>
        /// The number of digits in the field value, as defined by the ITU E.164 standard.
        /// </value>
        int DigitCount { get; }
    }
}