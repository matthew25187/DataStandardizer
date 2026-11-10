using System;
using System.Globalization;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Reads typed values from properties, taking the value type and time zone from their parameters.
    /// </summary>
    internal static class IcalPropertyValue
    {
        private const int MaxRegNameLength = 127;

        /// <summary>
        /// Gets the value type of a property: the type its <c>VALUE</c> parameter names, or its default type.
        /// </summary>
        /// <returns>The value type; or <see cref="IcalValueType.Unknown"/> for a property RFC 5545 and RFC 7986 do not define, or one whose
        /// <c>VALUE</c> parameter names no value type.</returns>
        internal static IcalValueType GetValueType(IcalContentLine property)
        {
            return IcalValueTypeRegistry.GetValueType(property);
        }

        /// <summary>
        /// Gets the first value of a parameter; or <c>null</c> if the property does not have it.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string? GetParameterValue(IcalContentLine property, string name)
#else
        internal static string GetParameterValue(IcalContentLine property, string name)
#endif
        {
            var parameter = property.GetParameter(name);
            return parameter?.ValueArray[0];
        }

        /// <summary>
        /// Parses one DATE or DATE-TIME value of a property, as its <c>VALUE</c> and <c>TZID</c> parameters say.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value, or one item of a list of values.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the property is a DATE or DATE-TIME and the value is valid for it; otherwise <c>false</c>.  A DATE with a
        /// <c>TZID</c>, and a DATE-TIME in UTC with a <c>TZID</c>, are not valid.</returns>
        internal static bool TryParseDateOrDateTime(IcalContentLine property, ReadOnlySpan<char> value, out IcalDateOrDateTime result)
        {
            var timeZoneId = GetParameterValue(property, IcalParameterNames.TimeZoneId);
            switch (GetValueType(property))
            {
                case IcalValueType.Date:
                    IcalDate date;
                    if (timeZoneId is null && IcalDate.TryParse(value, IcalValueFormat.Text, out date))
                    {
                        result = new IcalDateOrDateTime(date);
                        return true;
                    }

                    break;
                case IcalValueType.DateTime:
                    IcalDateTime dateTime;
                    if (timeZoneId?.Length != 0 && IcalDateTime.TryParse(value, timeZoneId, IcalValueFormat.Text, out dateTime))
                    {
                        result = new IcalDateOrDateTime(dateTime);
                        return true;
                    }

                    break;
            }

            result = default(IcalDateOrDateTime);
            return false;
        }

        /// <summary>
        /// Parses the value of a <c>FMTTYPE</c> parameter (RFC 5545 §3.2.8).
        /// </summary>
        /// <param name="value">The parameter value, without any enclosing double quotes and with RFC 6868 caret encoding decoded.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a type and subtype; otherwise <c>false</c>.  Media type parameters are not valid.</returns>
        internal static bool TryParseFormatType(ReadOnlySpan<char> value, out MimeMediaType result)
        {
            // ref. RFC 5545§3.2.8: fmttypeparam = "FMTTYPE" "=" type-name "/" subtype-name
            var separator = value.IndexOf('/');
            if (separator < 0 || !IsRegName(value.Slice(0, separator)) || !IsRegName(value.Slice(separator + 1)))
            {
                result = default(MimeMediaType);
                return false;
            }

            // A reg-name may start with a symbol, which the RFC 6838 §4.2 names that MimeMediaType requires by default may not.
            return MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens, out result);
        }

        /// <summary>
        /// Builds the exception a typed accessor throws for a <c>FMTTYPE</c> parameter that is not valid.
        /// </summary>
        internal static IcalFileException BuildInvalidFormatTypeException(long lineNumber, long physicalLineNumber, string propertyName)
        {
            return IcalContentLineErrorFactory.BuildException(BuildInvalidFormatTypeMessage(propertyName), lineNumber, physicalLineNumber, propertyName);
        }

        /// <summary>
        /// Builds the message for a <c>FMTTYPE</c> parameter that is not valid.
        /// </summary>
        internal static string BuildInvalidFormatTypeMessage(string propertyName)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "The {0} property must have one FMTTYPE parameter with one value, which is a type and subtype.", propertyName);
        }

        // TODO: RFC 5545 cites RFC 4288, which RFC 6838 has obsoleted.  RFC 6838 §4.2 requires a name to start with a letter or digit.
        // ref. RFC 4288§4.2: reg-name = 1*127reg-name-chars
        private static bool IsRegName(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty || value.Length > MaxRegNameLength) return false;

            foreach (var character in value)
            {
                if (!IsRegNameCharacter(character)) return false;
            }

            return true;
        }

        // ref. RFC 4288§4.2: reg-name-chars = ALPHA / DIGIT / "!" / "#" / "$" / "&" / "." / "+" / "-" / "^" / "_"
        private static bool IsRegNameCharacter(char character)
        {
            if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9')) return true;

            switch (character)
            {
                case '!': case '#': case '$': case '&': case '.': case '+': case '-': case '^': case '_':
                    return true;
                default:
                    return false;
            }
        }

        internal static string FormatInteger(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Builds the exception a typed accessor throws for a value that is not valid.
        /// </summary>
        internal static IcalFileException BuildInvalidValueException(IcalContentLine property, IcalValueType valueType)
        {
            var message = valueType == IcalValueType.Unknown
                ? string.Format(CultureInfo.InvariantCulture, "The value of the {0} property is not of a value type it allows.", property.Name)
                : string.Format(CultureInfo.InvariantCulture, "The value of the {0} property is not a valid {1}.", property.Name,
                    IcalValueTypeRegistry.GetValueTypeName(valueType));
            return IcalContentLineErrorFactory.BuildException(message, property.LineNumber, property.PhysicalLineNumber, property.Name);
        }
    }
}
