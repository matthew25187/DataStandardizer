namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number specifically for geographic areas.
    /// </summary>
    /// <remarks>
    /// This interface extends the base <see cref="IItuE164InternationalNumber"/> interface to include
    /// additional properties specific to geographic area numbers, such as the national significant number.
    /// It is designed to handle telephone numbers that are assigned to geographic regions.
    /// </remarks>
    public interface IItuE164InternationalNumberForGeographicAreas : IItuE164InternationalNumber
    {
        /// <summary>
        /// Gets the national significant number (NSN) of the ITU-T E.164 international telephone number
        /// for geographic areas.
        /// </summary>
        /// <remarks>
        /// The national significant number is the part of the telephone number that follows the country code
        /// and uniquely identifies a subscriber within a specific geographic region.
        /// </remarks>
        ItuE164NationalSignificantNumber NationalSignificantNumber { get; }
    }
}