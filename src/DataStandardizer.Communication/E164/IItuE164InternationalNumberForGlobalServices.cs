namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number specifically designed for global services.
    /// </summary>
    /// <remarks>
    /// This interface extends the base <see cref="IItuE164InternationalNumber"/> interface to include
    /// additional functionality for global services. It provides access to the global subscriber number,
    /// which uniquely identifies a subscriber within the context of global telecommunication services.
    /// </remarks>
    public interface IItuE164InternationalNumberForGlobalServices : IItuE164InternationalNumber
    {
        /// <summary>
        /// Gets the global subscriber number, which uniquely identifies a subscriber
        /// within the context of global telecommunication services.
        /// </summary>
        /// <value>
        /// A 64-bit unsigned integer representing the global subscriber number.
        /// </value>
        /// <remarks>
        /// The global subscriber number is a key component of ITU-T E.164 international
        /// telephone numbers for global services. It is used to uniquely identify
        /// subscribers in global telecommunication systems.
        /// </remarks>
        ItuE164GlobalSubscriberNumber GlobalSubscriberNumber { get; }
    }
}