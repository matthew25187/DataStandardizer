using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Gives the value type of each property defined by RFC 6350 §6, and honours a <c>VALUE</c> parameter that overrides it.
    /// </summary>
    /// <remarks>
    /// Unknown and experimental (<c>X-</c>) properties have the value type <see cref="VcardValueType.Unknown"/> whatever their <c>VALUE</c> parameter says, so that their values are
    /// kept as raw text and round-trip without loss.  Property names and value type names are compared case-insensitively.  N, ADR, ORG, GENDER and CLIENTPIDMAP are <c>text</c>
    /// here, as their <c>VALUE</c> parameter is in RFC 6350 where they have one; their structure has a type of its own, such as <see cref="VcardName"/>.
    /// </remarks>
    public static class VcardValueTypeRegistry
    {
        private sealed class Entry
        {
            internal Entry(string name, VcardValueType defaultValueType, params VcardValueType[] otherValueTypes)
            {
                Name = name;
                DefaultValueType = defaultValueType;
                OtherValueTypes = otherValueTypes;
            }

            internal string Name { get; }

            internal VcardValueType DefaultValueType { get; }

            internal VcardValueType[] OtherValueTypes { get; }
        }

        private const string ValueParameterName = "VALUE";

        // Indexed by VcardValueType.  ref. RFC 6350 §5.2: value-type.
        private static readonly string[] ValueTypeNames =
        {
            string.Empty, "text", "uri", "date", "time", "date-time", "date-and-or-time", "timestamp", "boolean", "integer", "float", "utc-offset", "language-tag"
        };

        private static readonly Entry[] Entries =
        {
            // ref. RFC 6350 §6.1: general properties.
            new Entry("BEGIN", VcardValueType.Text),
            new Entry("END", VcardValueType.Text),
            new Entry("SOURCE", VcardValueType.Uri),
            new Entry("KIND", VcardValueType.Text),
            new Entry("XML", VcardValueType.Text),

            // ref. RFC 6350 §6.2: identification properties.
            new Entry("FN", VcardValueType.Text),
            new Entry("N", VcardValueType.Text),
            new Entry("NICKNAME", VcardValueType.Text),
            new Entry("PHOTO", VcardValueType.Uri),
            new Entry("BDAY", VcardValueType.DateAndOrTime, VcardValueType.Text),
            new Entry("ANNIVERSARY", VcardValueType.DateAndOrTime, VcardValueType.Text),
            new Entry("GENDER", VcardValueType.Text),

            // ref. RFC 6350 §6.3: delivery addressing properties.
            new Entry("ADR", VcardValueType.Text),

            // ref. RFC 6350 §6.4: communications properties.
            new Entry("TEL", VcardValueType.Text, VcardValueType.Uri),
            new Entry("EMAIL", VcardValueType.Text),
            new Entry("IMPP", VcardValueType.Uri),
            new Entry("LANG", VcardValueType.LanguageTag),

            // ref. RFC 6350 §6.5: geographical properties.
            new Entry("TZ", VcardValueType.Text, VcardValueType.Uri, VcardValueType.UtcOffset),
            new Entry("GEO", VcardValueType.Uri),

            // ref. RFC 6350 §6.6: organizational properties.
            new Entry("TITLE", VcardValueType.Text),
            new Entry("ROLE", VcardValueType.Text),
            new Entry("LOGO", VcardValueType.Uri),
            new Entry("ORG", VcardValueType.Text),
            new Entry("MEMBER", VcardValueType.Uri),
            new Entry("RELATED", VcardValueType.Uri, VcardValueType.Text),

            // ref. RFC 6350 §6.7: explanatory properties.
            new Entry("CATEGORIES", VcardValueType.Text),
            new Entry("NOTE", VcardValueType.Text),
            new Entry("PRODID", VcardValueType.Text),
            new Entry("REV", VcardValueType.Timestamp),
            new Entry("SOUND", VcardValueType.Uri),
            new Entry("UID", VcardValueType.Uri, VcardValueType.Text),
            new Entry("CLIENTPIDMAP", VcardValueType.Text),
            new Entry("URL", VcardValueType.Uri),
            new Entry("VERSION", VcardValueType.Text),

            // ref. RFC 6350 §6.8: security properties.
            new Entry("KEY", VcardValueType.Uri, VcardValueType.Text),

            // ref. RFC 6350 §6.9: calendar properties.
            new Entry("FBURL", VcardValueType.Uri),
            new Entry("CALADRURI", VcardValueType.Uri),
            new Entry("CALURI", VcardValueType.Uri)
        };

        /// <summary>
        /// Gets a flag indicating if a property is one whose value type is known.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        public static bool IsKnownProperty(ReadOnlySpan<char> propertyName)
        {
            return FindEntry(propertyName) != null;
        }

        /// <summary>
        /// Gets the default value type of a property: the type of its value when it has no <c>VALUE</c> parameter.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value type; or <see cref="VcardValueType.Unknown"/> for a property whose value type is not known.</returns>
        public static VcardValueType GetDefaultValueType(ReadOnlySpan<char> propertyName)
        {
            var entry = FindEntry(propertyName);
            return entry is null ? VcardValueType.Unknown : entry.DefaultValueType;
        }

        /// <summary>
        /// Gets the value type of a property that has a <c>VALUE</c> parameter.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="valueParameter">The value of the <c>VALUE</c> parameter, such as <c>uri</c>.</param>
        /// <returns>The value type the parameter names; or <see cref="VcardValueType.Unknown"/> for a property whose value type is not known, or a parameter that names no value type
        /// of RFC 6350.  A known type is returned even if the property does not allow it; see <see cref="IsAllowedValueType"/>.</returns>
        public static VcardValueType GetValueType(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> valueParameter)
        {
            VcardValueType valueType;
            return FindEntry(propertyName) != null && TryParseValueTypeName(valueParameter, out valueType) ? valueType : VcardValueType.Unknown;
        }

        /// <summary>
        /// Gets the value type of a content line, from its property name and any <c>VALUE</c> parameter.
        /// </summary>
        /// <param name="line">The content line.</param>
        /// <returns>The value type; or <see cref="VcardValueType.Unknown"/> for a property whose value type is not known, or a <c>VALUE</c> parameter that names no value type of
        /// RFC 6350.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
        public static VcardValueType GetValueType(VcardContentLine line)
        {
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            var valueParameter = line.GetParameter(ValueParameterName);
            return valueParameter is null
                ? GetDefaultValueType(line.Name.AsSpan())
                : GetValueType(line.Name.AsSpan(), valueParameter.ValueArray[0].AsSpan());
        }

        /// <summary>
        /// Gets the value type of the line a reader has just read, from its property name and any <c>VALUE</c> parameter, without allocation.
        /// </summary>
        /// <param name="line">The view of the line.</param>
        /// <returns>The value type; or <see cref="VcardValueType.Unknown"/> for a property whose value type is not known, or a <c>VALUE</c> parameter that names no value type of
        /// RFC 6350.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public static VcardValueType GetValueType(VcardContentLineView line)
        {
            var parameterIndex = line.IndexOfParameter(ValueParameterName.AsSpan());
            return parameterIndex < 0 ? GetDefaultValueType(line.Name) : GetValueType(line.Name, line.GetParameterValue(parameterIndex, 0));
        }

        /// <summary>
        /// Gets a flag indicating if a property allows a value type: its default type, or one a <c>VALUE</c> parameter may name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="valueType">The value type.</param>
        /// <returns><c>true</c> if the property is known and allows the type; otherwise <c>false</c>.</returns>
        public static bool IsAllowedValueType(ReadOnlySpan<char> propertyName, VcardValueType valueType)
        {
            var entry = FindEntry(propertyName);
            if (entry is null) return false;
            if (entry.DefaultValueType == valueType) return true;

            return Array.IndexOf(entry.OtherValueTypes, valueType) >= 0;
        }

        /// <summary>
        /// Parses the name of a value type, as given by a <c>VALUE</c> parameter, such as <c>date-and-or-time</c>.
        /// </summary>
        /// <param name="name">The name, in any case.</param>
        /// <param name="valueType">The value type, if the name is one of RFC 6350; otherwise <see cref="VcardValueType.Unknown"/>.</param>
        /// <returns><c>true</c> if the name is that of a value type of RFC 6350; otherwise <c>false</c>.</returns>
        public static bool TryParseValueTypeName(ReadOnlySpan<char> name, out VcardValueType valueType)
        {
            for (var index = 1; index < ValueTypeNames.Length; index++)
            {
                if (EqualsIgnoringCase(name, ValueTypeNames[index]))
                {
                    valueType = (VcardValueType)index;
                    return true;
                }
            }

            valueType = VcardValueType.Unknown;
            return false;
        }

        /// <summary>
        /// Gets the name of a value type, as written in a <c>VALUE</c> parameter, such as <c>date-and-or-time</c>.
        /// </summary>
        /// <param name="valueType">The value type.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="valueType"/> is <see cref="VcardValueType.Unknown"/>, which has no name, or is not a
        /// <see cref="VcardValueType"/> member.</exception>
        public static string GetValueTypeName(VcardValueType valueType)
        {
            if (valueType <= VcardValueType.Unknown || valueType > VcardValueType.LanguageTag)
                throw new ArgumentOutOfRangeException(nameof(valueType), valueType, "The value type has no name.");

            return ValueTypeNames[(int)valueType];
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static Entry? FindEntry(ReadOnlySpan<char> propertyName)
#else
        private static Entry FindEntry(ReadOnlySpan<char> propertyName)
#endif
        {
            foreach (var entry in Entries)
            {
                if (VcardValueFormatting.EqualsKeyword(propertyName, entry.Name)) return entry;
            }

            return null;
        }

        private static bool EqualsIgnoringCase(ReadOnlySpan<char> value, string lowerCaseName)
        {
            if (value.Length != lowerCaseName.Length) return false;

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character >= 'A' && character <= 'Z')
                {
                    character = (char)(character + 0x20);
                }

                if (character != lowerCaseName[index]) return false;
            }

            return true;
        }
    }
}
