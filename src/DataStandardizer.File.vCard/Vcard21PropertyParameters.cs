using System;
using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The group and parameters of a property being added through <see cref="IVcard21PropertyList{TValue}"/>.  Each one left at its default is left out.
    /// </summary>
    /// <remarks>
    /// <c>ENCODING</c> and <c>CHARSET</c> aren't here, since a writer chooses them for the value, and neither is <c>VALUE</c>, which the value says.
    /// </remarks>
    public sealed class Vcard21PropertyParameters
    {
        /// <summary>
        /// Gets parameters whose names start with <c>X-</c> (vCard 2.1 §2.9), to go after the others.  It can be changed.
        /// </summary>
        public IList<VcardContentLineParameter> AdditionalParameters { get; } = new List<VcardContentLineParameter>();

        /// <summary>
        /// Gets the <c>TYPE</c> values, other than <c>PREF</c>: each a <c>knowntype</c> of vCard 2.1 §2.9, such as <c>WORK</c>, in any case, or a value that starts with <c>X-</c>.
        /// It can be changed.
        /// </summary>
        public IList<string> Types { get; } = new List<string>();

        /// <summary>
        /// Gets or sets a flag indicating if the property is preferred, which is <c>PREF</c> in vCard 2.1.
        /// </summary>
        public bool IsPreferred { get; set; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets or sets the group, without its '.'.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LANGUAGE</c>, a tag of RFC 1766, which vCard 2.1 §2.1.7 cites.
        /// </summary>
        public string? Language { get; set; }
#else
        /// <summary>
        /// Gets or sets the group, without its '.'.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LANGUAGE</c>, a tag of RFC 1766, which vCard 2.1 §2.1.7 cites.
        /// </summary>
        public string Language { get; set; }
#endif

        /// <summary>
        /// Gets the parameters in canonical form.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that gave this object, for an exception.</param>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold a parameter.</exception>
        internal VcardPropertyParameters ToCanonical(string parameterName)
        {
            var parameters = new VcardPropertyParameters { Group = Group };
            foreach (var type in Types)
            {
                if (type is null)
                    throw new ArgumentException("A TYPE value can't be null.", parameterName);
                if (Vcard21Views.IsPreferredType(type))
                    throw new ArgumentException("PREF isn't one of the Types; set IsPreferred instead.", parameterName);

                // ref. vCard 2.1 §2.9: ptypeval = knowntype / "X-" word.  A knowntype is held as RFC 6350 writes its values, in lower case.
                if (VcardLegacyTokens.IsKnownType(type.AsSpan()))
                {
                    parameters.Types.Add(type.ToLowerInvariant());
                }
                else if (VcardLegacyTokens.IsExtensionWord(type))
                {
                    parameters.Types.Add(type);
                }
                else
                {
                    throw new ArgumentException("vCard 2.1 has no TYPE " + type + ".", parameterName);
                }
            }

            if (IsPreferred)
            {
                parameters.Pref = VcardPropertyParameterReader.MinPreference;
            }

            // Added as it is, since a tag of RFC 1766 isn't always one that Bcp47LanguageTag holds.
            if (Language != null)
            {
                if (!Vcard30Conformance.IsRfc1766Tag(Language))
                    throw new ArgumentException("\"" + Language + "\" is not a language tag of RFC 1766.", parameterName);

                parameters.AdditionalParameters.Add(new VcardContentLineParameter(VcardParameterNames.Language, Language));
            }

            foreach (var parameter in AdditionalParameters)
            {
                if (parameter is null || !VcardLegacyTokens.IsExtensionWord(parameter.Name) || parameter.ValueArray.Length != 1 || !VcardLegacyTokens.IsWord(parameter.ValueArray[0]))
                    throw new ArgumentException("vCard 2.1 has only parameters named X- with one value that is a word (§2.9).", parameterName);

                parameters.AdditionalParameters.Add(parameter);
            }

            return parameters;
        }
    }
}
