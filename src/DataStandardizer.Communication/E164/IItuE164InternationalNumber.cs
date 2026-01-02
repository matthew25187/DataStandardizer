namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Defines the structure of an ITU-T E.164 international telephone number.
    /// </summary>
    /// <remarks>
    /// This interface represents the base abstraction for ITU-T E.164 international telephone numbers,
    /// which are used for global telecommunication. It provides access to the country code and serves
    /// as a foundation for more specific types of E.164 numbers, such as those for geographic areas,
    /// global services, networks, groups of countries, and trials.
    /// </remarks>
    public interface IItuE164InternationalNumber : ITelephonyNumber
    {
        /// <summary>
        /// Gets the ITU-T E.164 country code associated with the international telephone number.
        /// </summary>
        /// <remarks>
        /// The country code is a numeric prefix that identifies the country or region
        /// to which the telephone number belongs. It is an integral part of the E.164
        /// numbering plan, enabling global telecommunication routing.
        /// </remarks>
        /// <value>
        /// A <see cref="ushort"/> representing the country code of the telephone number.
        /// </value>
        /// <exception cref="System.NotSupportedException">
        /// Thrown when the country code is not supported for the specific type of E.164 number.
        /// </exception>
        ushort CountryCode { get; }
    }
}