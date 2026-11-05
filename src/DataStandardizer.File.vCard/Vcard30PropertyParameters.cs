using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The group and parameters of a property being added through <see cref="IVcard30PropertyList{TValue}"/>.  Each one left at its default is left out.
    /// </summary>
    public sealed class Vcard30PropertyParameters
    {
        /// <summary>
        /// Gets parameters that no version of vCard defines, to go after the others.  It can be changed.
        /// </summary>
        public IList<VcardContentLineParameter> AdditionalParameters { get; } = new List<VcardContentLineParameter>();

        /// <summary>
        /// Gets the <c>TYPE</c> values, other than <c>pref</c>.  It can be changed.
        /// </summary>
        public IList<string> Types { get; } = new List<string>();

        /// <summary>
        /// Gets or sets a flag indicating if the property is preferred, which is <c>TYPE=pref</c> in vCard 3.0.
        /// </summary>
        public bool IsPreferred { get; set; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets or sets the group, without its '.'.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LANGUAGE</c>, a tag of RFC 1766, which RFC 2426 §4 cites.
        /// </summary>
        public string? Language { get; set; }
#else
        /// <summary>
        /// Gets or sets the group, without its '.'.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LANGUAGE</c>, a tag of RFC 1766, which RFC 2426 §4 cites.
        /// </summary>
        public string Language { get; set; }
#endif

        /// <summary>
        /// Gets the group and parameters in canonical form.
        /// </summary>
        internal VcardPropertyParameters ToCanonical()
        {
            var parameters = new VcardPropertyParameters { Group = Group };
            foreach (var type in Types)
            {
                parameters.Types.Add(type);
            }

            if (IsPreferred)
            {
                parameters.Pref = VcardPropertyParameterReader.MinPreference;
            }

            // Added as it is, since a tag of RFC 1766 isn't always one that Bcp47LanguageTag holds.
            if (Language != null)
            {
                parameters.AdditionalParameters.Add(new VcardContentLineParameter(VcardParameterNames.Language, Language));
            }

            foreach (var parameter in AdditionalParameters)
            {
                parameters.AdditionalParameters.Add(parameter);
            }

            return parameters;
        }
    }
}
