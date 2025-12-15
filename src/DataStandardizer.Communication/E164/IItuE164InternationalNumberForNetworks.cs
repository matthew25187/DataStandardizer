namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number specifically assigned for networks.
    /// </summary>
    /// <remarks>
    /// This interface extends the base <see cref="IItuE164InternationalNumber"/> interface to include
    /// additional properties specific to network-based E.164 numbers, such as the identification code
    /// and subscriber number. These numbers are used for identifying and routing within telecommunication
    /// networks.
    /// </remarks>
    public interface IItuE164InternationalNumberForNetworks : IItuE164InternationalNumber
    {
        /// <summary>
        /// Gets the identification code associated with the ITU-T E.164 international telephone number for networks.
        /// </summary>
        /// <remarks>
        /// The identification code is a network-specific component of the E.164 number, used to identify
        /// and route calls within telecommunication networks. This property is essential for distinguishing
        /// network-based numbers from other types of E.164 numbers.
        /// </remarks>
        ItuE164AssignedIdentificationCodesForNetworks IdentificationCode { get; }

        /// <summary>
        /// Gets the subscriber number associated with the ITU-T E.164 international telephone number for networks.
        /// </summary>
        /// <remarks>
        /// The subscriber number is a unique identifier within the context of the network's identification code.
        /// It is used for routing and identifying specific subscribers in telecommunication networks.
        /// </remarks>
        ItuE164SubscriberNumber SubscriberNumber { get; }
    }
}