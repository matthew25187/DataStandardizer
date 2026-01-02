using System;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an attribute that specifies a shared ITU E.164 country code for a field.
    /// </summary>
    /// <remarks>
    /// This attribute is used to annotate fields with a specific ITU E.164 country code, 
    /// which is a standardized numbering plan for the international public telecommunication system.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ItuE164SharedCodeAttribute : Attribute
    {
        public ItuE164SharedCodeAttribute(ushort countryCode)
        {
            CountryCode = countryCode;
        }

        /// <summary>
        /// Gets the ITU E.164 country code associated with this attribute.
        /// </summary>
        /// <value>
        /// A <see cref="ushort"/> representing the standardized country code 
        /// as per the ITU E.164 numbering plan.
        /// </value>
        /// <remarks>
        /// The ITU E.164 country code is used to identify a specific country or region
        /// in the international public telecommunication system.
        /// </remarks>
        public ushort CountryCode { get; }
    }
}