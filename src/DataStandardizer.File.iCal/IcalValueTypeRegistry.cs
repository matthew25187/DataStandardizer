using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Gives the value type of each property defined by RFC 5545 and RFC 7986, and honours a <c>VALUE</c> parameter that overrides it.
    /// </summary>
    /// <remarks>
    /// Unknown, experimental (<c>X-</c>) and IANA-registered properties have the value type <see cref="IcalValueType.Unknown"/> whatever their
    /// <c>VALUE</c> parameter says, so that their values are kept as raw text and round-trip without loss.  Property names and value type
    /// names are compared case-insensitively.
    /// </remarks>
    public static class IcalValueTypeRegistry
    {
        private const string ValueParameterName = "VALUE";

        private sealed class Entry
        {
            internal Entry(string name, IcalValueType defaultValueType, params IcalValueType[] otherValueTypes)
            {
                Name = name;
                DefaultValueType = defaultValueType;
                OtherValueTypes = otherValueTypes;
            }

            internal string Name { get; }

            internal IcalValueType DefaultValueType { get; }

            internal IcalValueType[] OtherValueTypes { get; }
        }

        // Indexed by IcalValueType.
        private static readonly string[] ValueTypeNames =
        {
            string.Empty, "BINARY", "BOOLEAN", "CAL-ADDRESS", "DATE", "DATE-TIME", "DURATION", "FLOAT", "INTEGER", "PERIOD", "RECUR", "TEXT", "TIME", "URI",
            "UTC-OFFSET"
        };

        private static readonly Entry[] Entries =
        {
            // ref. RFC 5545§3.7: calendar properties.
            new Entry("CALSCALE", IcalValueType.Text),
            new Entry("METHOD", IcalValueType.Text),
            new Entry("PRODID", IcalValueType.Text),
            new Entry("VERSION", IcalValueType.Text),

            // ref. RFC 5545§3.8.1: descriptive component properties.
            new Entry("ATTACH", IcalValueType.Uri, IcalValueType.Binary),
            new Entry("CATEGORIES", IcalValueType.Text),
            new Entry("CLASS", IcalValueType.Text),
            new Entry("COMMENT", IcalValueType.Text),
            new Entry("DESCRIPTION", IcalValueType.Text),
            new Entry("GEO", IcalValueType.Float),
            new Entry("LOCATION", IcalValueType.Text),
            new Entry("PERCENT-COMPLETE", IcalValueType.Integer),
            new Entry("PRIORITY", IcalValueType.Integer),
            new Entry("RESOURCES", IcalValueType.Text),
            new Entry("STATUS", IcalValueType.Text),
            new Entry("SUMMARY", IcalValueType.Text),

            // ref. RFC 5545§3.8.2: date and time component properties.
            new Entry("COMPLETED", IcalValueType.DateTime),
            new Entry("DTEND", IcalValueType.DateTime, IcalValueType.Date),
            new Entry("DUE", IcalValueType.DateTime, IcalValueType.Date),
            new Entry("DTSTART", IcalValueType.DateTime, IcalValueType.Date),
            new Entry("DURATION", IcalValueType.Duration),
            new Entry("FREEBUSY", IcalValueType.Period),
            new Entry("TRANSP", IcalValueType.Text),

            // ref. RFC 5545§3.8.3: time zone component properties.
            new Entry("TZID", IcalValueType.Text),
            new Entry("TZNAME", IcalValueType.Text),
            new Entry("TZOFFSETFROM", IcalValueType.UtcOffset),
            new Entry("TZOFFSETTO", IcalValueType.UtcOffset),
            new Entry("TZURL", IcalValueType.Uri),

            // ref. RFC 5545§3.8.4: relationship component properties.
            new Entry("ATTENDEE", IcalValueType.CalAddress),
            new Entry("CONTACT", IcalValueType.Text),
            new Entry("ORGANIZER", IcalValueType.CalAddress),
            new Entry("RECURRENCE-ID", IcalValueType.DateTime, IcalValueType.Date),
            new Entry("RELATED-TO", IcalValueType.Text),
            new Entry("URL", IcalValueType.Uri),
            new Entry("UID", IcalValueType.Text),

            // ref. RFC 5545§3.8.5: recurrence component properties.
            new Entry("EXDATE", IcalValueType.DateTime, IcalValueType.Date),
            new Entry("RDATE", IcalValueType.DateTime, IcalValueType.Date, IcalValueType.Period),
            new Entry("RRULE", IcalValueType.Recur),

            // ref. RFC 5545§3.8.6: alarm component properties.
            new Entry("ACTION", IcalValueType.Text),
            new Entry("REPEAT", IcalValueType.Integer),
            new Entry("TRIGGER", IcalValueType.Duration, IcalValueType.DateTime),

            // ref. RFC 5545§3.8.7: change management component properties.
            new Entry("CREATED", IcalValueType.DateTime),
            new Entry("DTSTAMP", IcalValueType.DateTime),
            new Entry("LAST-MODIFIED", IcalValueType.DateTime),
            new Entry("SEQUENCE", IcalValueType.Integer),

            // ref. RFC 5545§3.8.8.3: miscellaneous component properties.
            new Entry("REQUEST-STATUS", IcalValueType.Text),

            // ref. RFC 7986§5: new properties.  REFRESH-INTERVAL, SOURCE, IMAGE and CONFERENCE have no default and need a VALUE parameter; their
            // first allowed type is given as the default.
            new Entry("NAME", IcalValueType.Text),
            new Entry("REFRESH-INTERVAL", IcalValueType.Duration),
            new Entry("SOURCE", IcalValueType.Uri),
            new Entry("COLOR", IcalValueType.Text),
            new Entry("IMAGE", IcalValueType.Uri, IcalValueType.Binary),
            new Entry("CONFERENCE", IcalValueType.Uri)
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
        /// <returns>The value type; or <see cref="IcalValueType.Unknown"/> for a property whose value type is not known.</returns>
        public static IcalValueType GetDefaultValueType(ReadOnlySpan<char> propertyName)
        {
            var entry = FindEntry(propertyName);
            return entry is null ? IcalValueType.Unknown : entry.DefaultValueType;
        }

        /// <summary>
        /// Gets the value type of a property that has a <c>VALUE</c> parameter.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="valueParameter">The value of the <c>VALUE</c> parameter, such as <c>DATE</c>.</param>
        /// <returns>The value type the parameter names; or <see cref="IcalValueType.Unknown"/> for a property whose value type is not known,
        /// or a parameter that names no value type of RFC 5545.  A known type is returned even if the property does not allow it; see
        /// <see cref="IsAllowedValueType"/>.</returns>
        public static IcalValueType GetValueType(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> valueParameter)
        {
            IcalValueType valueType;
            return FindEntry(propertyName) != null && TryParseValueTypeName(valueParameter, out valueType) ? valueType : IcalValueType.Unknown;
        }

        /// <summary>
        /// Gets the value type of a content line, from its property name and any <c>VALUE</c> parameter.
        /// </summary>
        /// <param name="line">The content line.</param>
        /// <returns>The value type; or <see cref="IcalValueType.Unknown"/> for a property whose value type is not known, or a <c>VALUE</c>
        /// parameter that names no value type of RFC 5545.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
        public static IcalValueType GetValueType(IcalContentLine line)
        {
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            var valueParameter = line.GetParameter(ValueParameterName);
            return valueParameter is null
                ? GetDefaultValueType(line.Name.AsSpan())
                : GetValueType(line.Name.AsSpan(), valueParameter.Values[0].AsSpan());
        }

        /// <summary>
        /// Gets the value type of the line a reader has just read, from its property name and any <c>VALUE</c> parameter, without
        /// allocation.
        /// </summary>
        /// <param name="line">The view of the line.</param>
        /// <returns>The value type; or <see cref="IcalValueType.Unknown"/> for a property whose value type is not known, or a <c>VALUE</c>
        /// parameter that names no value type of RFC 5545.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public static IcalValueType GetValueType(IcalContentLineView line)
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
        public static bool IsAllowedValueType(ReadOnlySpan<char> propertyName, IcalValueType valueType)
        {
            var entry = FindEntry(propertyName);
            if (entry is null) return false;
            if (entry.DefaultValueType == valueType) return true;

            return Array.IndexOf(entry.OtherValueTypes, valueType) >= 0;
        }

        /// <summary>
        /// Parses the name of a value type, as given by a <c>VALUE</c> parameter, such as <c>DATE-TIME</c>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="valueType">The value type, if the name is one of RFC 5545; otherwise <see cref="IcalValueType.Unknown"/>.</param>
        /// <returns><c>true</c> if the name is that of a value type of RFC 5545; otherwise <c>false</c>.</returns>
        public static bool TryParseValueTypeName(ReadOnlySpan<char> name, out IcalValueType valueType)
        {
            for (var index = 1; index < ValueTypeNames.Length; index++)
            {
                if (IcalValueFormatting.EqualsKeyword(name, ValueTypeNames[index]))
                {
                    valueType = (IcalValueType)index;
                    return true;
                }
            }

            valueType = IcalValueType.Unknown;
            return false;
        }

        /// <summary>
        /// Gets the name of a value type, as written in a <c>VALUE</c> parameter, such as <c>DATE-TIME</c>.
        /// </summary>
        /// <param name="valueType">The value type.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="valueType"/> is <see cref="IcalValueType.Unknown"/>, which has no
        /// name, or is not an <see cref="IcalValueType"/> member.</exception>
        public static string GetValueTypeName(IcalValueType valueType)
        {
            if (valueType <= IcalValueType.Unknown || valueType > IcalValueType.UtcOffset)
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
                if (IcalValueFormatting.EqualsKeyword(propertyName, entry.Name)) return entry;
            }

            return null;
        }
    }
}
