using System;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Reads typed values from properties, taking the value type and time zone from their parameters.
    /// </summary>
    internal static class IcalPropertyValue
    {
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
            return parameter?.Values[0];
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
            return IcalContentLineScanner.BuildException(message, property.LineNumber, property.PhysicalLineNumber, property.Name);
        }
    }
}
