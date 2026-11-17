using System;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Finds values read from jCal or xCal that are in the text form of a content line instead of the form of their serialization.  The
    /// converters keep a value that is not in its serialization's form as it is, so that the conformance check reports it; but the check sees
    /// the text form, where such a value is valid.
    /// </summary>
    internal static class IcalConversionCheck
    {
        /// <summary>
        /// Determines whether a value is valid in the text form of a content line, but not in the form of another serialization.
        /// </summary>
        /// <param name="value">The value, as it was read.</param>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="format">The form of the serialization it was read from: <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>.</param>
        /// <returns><c>true</c> if the value is valid only in the text form; <c>false</c> if it is valid in the serialization's form, or not valid
        /// in either, so the conformance check reports it, or its type has the same form in both, or is not one this check covers.</returns>
        internal static bool IsTextFormOnly(ReadOnlySpan<char> value, IcalValueType valueType, IcalValueFormat format)
        {
            return IsCovered(valueType) && !TryParse(value, valueType, format) && TryParse(value, valueType, IcalValueFormat.Text);
        }

        /// <summary>
        /// Builds the problem reported for a value that is valid only in the text form.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="valueType">The type of the value; one that <see cref="IsTextFormOnly"/> covers.</param>
        /// <param name="format">The form of the serialization it was read from: <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>.</param>
        internal static string BuildProblem(string propertyName, IcalValueType valueType, IcalValueFormat format)
        {
            // ref. RFC 7265§3.6 and RFC 6321§3.6: the two RFCs number the sections of the value types alike.
            return string.Format(CultureInfo.InvariantCulture,
                "The {0} property has a {1} value in the iCalendar text form, which {2} does not allow (RFC {3} §3.6.{4}).", propertyName,
                IcalValueTypeRegistry.GetValueTypeName(valueType), format == IcalValueFormat.Json ? "jCal" : "xCal",
                format == IcalValueFormat.Json ? "7265" : "6321", GetSectionNumber(valueType));
        }

        /// <summary>
        /// Builds the problem reported for a parameter value that is valid only in the text form.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="valueType">The type of the value; one that <see cref="IsTextFormOnly"/> covers.</param>
        /// <param name="format">The form of the serialization it was read from: <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>.</param>
        internal static string BuildParameterProblem(string propertyName, string parameterName, IcalValueType valueType, IcalValueFormat format)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "The {0} parameter of the {1} property has a {2} value in the iCalendar text form, which {3} does not allow (RFC {4} §3.6.{5}).",
                parameterName, propertyName, IcalValueTypeRegistry.GetValueTypeName(valueType), format == IcalValueFormat.Json ? "jCal" : "xCal",
                format == IcalValueFormat.Json ? "7265" : "6321", GetSectionNumber(valueType));
        }

        private static bool IsCovered(IcalValueType valueType)
        {
            return GetSectionNumber(valueType) != 0;
        }

        /// <summary>
        /// Gets the number of the subsection of RFC 7265 §3.6 and RFC 6321 §3.6 that gives the form of a value type whose text form differs;
        /// or 0 for any other type.
        /// </summary>
        private static int GetSectionNumber(IcalValueType valueType)
        {
            // INTEGER, FLOAT and BINARY in the text form are valid in both serializations, URI and CAL-ADDRESS have the same form in all three,
            // and the forms of PERIOD and RECUR are built from their parts, which are checked as values of their own types.  XcalConverter
            // checks the case of the enumerated rule parts of an xCal RECUR itself.
            switch (valueType)
            {
                case IcalValueType.Boolean:
                    return 2;
                case IcalValueType.Date:
                    return 4;
                case IcalValueType.DateTime:
                    return 5;
                case IcalValueType.Duration:
                    return 6;
                case IcalValueType.Time:
                    return 12;
                case IcalValueType.UtcOffset:
                    return 14;
                default:
                    return 0;
            }
        }

        private static bool TryParse(ReadOnlySpan<char> value, IcalValueType valueType, IcalValueFormat format)
        {
            switch (valueType)
            {
                case IcalValueType.Boolean:
                    return IcalValue.TryParseBoolean(value, format, out _);
                case IcalValueType.Date:
                    return IcalDate.TryParse(value, format, out _);
                case IcalValueType.DateTime:
                    return IcalDateTime.TryParse(value, format, out _);
                case IcalValueType.Duration:
                    return IcalDuration.TryParse(value, format, out _);
                case IcalValueType.Time:
                    return IcalTime.TryParse(value, format, out _);
                default:
                    return IcalUtcOffset.TryParse(value, format, out _);
            }
        }
    }
}
