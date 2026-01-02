namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number specifically designed for groups of countries.
    /// </summary>
    /// <remarks>
    /// This interface extends the base <see cref="IItuE164InternationalNumber"/> to include additional 
    /// properties that are specific to numbers assigned to groups of countries. It provides access to the 
    /// group identification code and the subscriber number, enabling detailed representation and processing 
    /// of such numbers.
    /// </remarks>
    public interface IItuE164InternationalNumberForGroupsOfCountries : IItuE164InternationalNumber
    {
        /// <summary>
        /// Gets the group identification code associated with the ITU-T E.164 international telephone number
        /// for groups of countries.
        /// </summary>
        /// <remarks>
        /// The group identification code uniquely identifies a specific group of countries within the context
        /// of ITU-T E.164 numbering. This property is essential for distinguishing numbers assigned to such groups.
        /// </remarks>
        ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries GroupIdentificationCode { get; }

        /// <summary>
        /// Gets the subscriber number associated with the ITU-T E.164 international telephone number 
        /// for groups of countries.
        /// </summary>
        /// <remarks>
        /// The subscriber number uniquely identifies a subscriber within the context of a group of countries.
        /// This property is specific to numbers assigned to groups of countries and complements the 
        /// <see cref="GroupIdentificationCode"/> property.
        /// </remarks>
        ItuE164SubscriberNumber SubscriberNumber { get; }
    }
}