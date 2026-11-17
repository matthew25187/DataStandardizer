using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Converts properties between the content lines of the component object model and the elements of xCal (RFC 6321).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A content line holds its value in the text form of RFC 5545, so converting from xCal turns each value element into that form, and
    /// converting to xCal turns the text back into value elements.  The text of a value is kept where both forms allow it, such as the digits
    /// of a FLOAT, so that a value that goes to xCal and back is unchanged.
    /// </para>
    /// <para>
    /// A value that cannot be converted, which <see cref="IcalOptions.AllowInvalidValues"/> lets a calendar hold, is kept as it is: from xCal
    /// as the element's text, which the conformance check then reports; and to xCal in an <c>unknown</c> element, keeping any <c>VALUE</c>
    /// parameter so that nothing is lost.
    /// </para>
    /// </remarks>
    internal static class XcalConverter
    {
        /// <summary>
        /// The names of the value elements (RFC 6321 §3.6), indexed by <see cref="IcalValueType"/>; <see cref="IcalValueType.Unknown"/> is the
        /// <c>unknown</c> element of RFC 6321 §5.
        /// </summary>
        private static readonly string[] ValueElementNames =
        {
            XcalNames.Unknown, "binary", "boolean", "cal-address", "date", "date-time", "duration", "float", "integer", "period", "recur", "text",
            "time", "uri", "utc-offset"
        };

        private const string FrequencyPartName = "freq";
        private const string ByDayPartName = "byday";
        private const string WeekStartPartName = "wkst";

        /// <summary>
        /// The child elements of a <c>recur</c> element, in the order that <c>value-recur</c> in RFC 6321 Appendix A (3.3.10) gives them.
        /// </summary>
        private static readonly string[] RecurPartNames =
        {
            FrequencyPartName, XcalNames.Until, CountPartName, IntervalPartName, "bysecond", "byminute", "byhour", ByDayPartName, "bymonthday", "byyearday",
            "byweekno", "bymonth", "bysetpos", WeekStartPartName
        };

        private const string CountPartName = "count";
        private const string IntervalPartName = "interval";

        /// <summary>
        /// The parameters whose value is a URI (RFC 6321 Appendix A §3.2).
        /// </summary>
        private static readonly string[] UriParameters = { IcalParameterNames.AlternateRepresentation, IcalParameterNames.Directory };

        /// <summary>
        /// The parameters whose values are calendar user addresses (RFC 6321 Appendix A §3.2).
        /// </summary>
        private static readonly string[] CalAddressParameters =
        {
            IcalParameterNames.DelegatedFrom, IcalParameterNames.DelegatedTo, IcalParameterNames.Member, IcalParameterNames.SentBy
        };

        /// <summary>
        /// The other parameters RFC 5545 and RFC 7986 define, whose values are TEXT.  Any other parameter is unrecognised, and its values are
        /// written in <c>unknown</c> elements (RFC 6321 §5).
        /// </summary>
        private static readonly string[] TextParameters =
        {
            IcalParameterNames.CommonName, IcalParameterNames.CalendarUserType, IcalParameterNames.Encoding, IcalParameterNames.FormatType,
            IcalParameterNames.FreeBusyType, IcalParameterNames.Language, IcalParameterNames.ParticipationStatus, IcalParameterNames.Range,
            IcalParameterNames.Related, IcalParameterNames.RelationshipType, IcalParameterNames.Role, IcalParameterNames.TimeZoneId,
            IcalParameterNames.Value, IcalParameterNames.Display, IcalParameterNames.Email, IcalParameterNames.Feature, IcalParameterNames.Label
        };

        /// <summary>
        /// Gets the name of the xCal element for a component, property or parameter name: the name in lower case (RFC 6321 §3.3 to §3.5).
        /// </summary>
        /// <exception cref="IcalFileException">The name cannot be the name of an XML element, because it does not start with a letter.</exception>
        internal static string ToElementName(string name, long lineNumber)
        {
            // ref. XML Namespaces §3: an NCName starts with a letter or "_"; iCalendar names are letters, digits and hyphens.
            var first = name.Length == 0 ? '\0' : name[0];
            if (!(first >= 'A' && first <= 'Z' || first >= 'a' && first <= 'z'))
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture, "The name {0} cannot be written in xCal, since an XML element name must start with a letter.",
                        name), lineNumber, 0, null);

            return name.ToLowerInvariant();
        }

        #region xCal to iCalendar

        /// <summary>
        /// Converts a property element into a content line.
        /// </summary>
        /// <param name="property">The property element, which is a child of a <c>properties</c> element.</param>
        /// <exception cref="IcalFileException">The element is not a valid xCal property.</exception>
        internal static IcalContentLine ToContentLine(XcalElement property)
        {
            var name = ToIcalName(property);

            // ref. RFC 6321 §3.3: the component element takes the place of the BEGIN and END lines that delimit a component, so a property with either name would start or end one.
            if (IsName(name, IcalCalendarBuilder.BeginName) || IsName(name, IcalCalendarBuilder.EndName))
                throw BuildException(property, name,
                    string.Format(CultureInfo.InvariantCulture,
                        "The {0} property can't be in xCal, whose component element takes the place of BEGIN and END: RFC 6321 §3.3.", name));

            CheckNoText(property, name);

            var parameters = new List<IcalContentLineParameter>();
            var values = new List<XcalElement>();
            var hasParameters = false;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? parameterProblem = null;
#else
            string parameterProblem = null;
#endif
            foreach (var child in property.Children)
            {
                if (child.Name == XcalNames.Parameters)
                {
                    if (hasParameters)
                        throw BuildException(child, name, "A property can have only one parameters element.");

                    // ref. RFC 6321 Appendix A: each property is an optional parameters element, then its value.
                    if (values.Count != 0)
                        throw BuildException(child, name,
                            string.Format(CultureInfo.InvariantCulture,
                                "The parameters element of the {0} property must come before its value: RFC 6321 Appendix A.", name));

                    hasParameters = true;
                    parameterProblem = ReadParameters(child, name, parameters);
                }
                else
                {
                    values.Add(child);
                }
            }

            if (values.Count == 0)
                throw BuildException(property, name, string.Format(CultureInfo.InvariantCulture, "The {0} property has no value element.", name));

            string value;
            IcalValueType valueType;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? conversionProblem = null;
#else
            string conversionProblem = null;
#endif
            if (IsName(name, IcalPropertyNames.Geo) && IsStructuredValue(values, XcalNames.Latitude, XcalNames.Longitude))
            {
                value = ReadGeo(values, name);
                valueType = IcalValueType.Float;
            }
            else if (IsName(name, IcalPropertyNames.RequestStatus) && IsStructuredValue(values, XcalNames.Code, XcalNames.Description, XcalNames.Data))
            {
                value = ReadRequestStatus(values, name);
                valueType = IcalValueType.Text;
            }
            else
            {
                value = ReadValues(values, name, out valueType);
                conversionProblem = GetConversionProblem(values, valueType, name);
            }

            conversionProblem = conversionProblem ?? parameterProblem;

            // ref. RFC 6321§3.5.1: the VALUE parameter MUST be included if the value type is not the property's default, and §5: never for
            // an unknown value.  RFC 7986 properties with no default always need one.
            if (valueType != IcalValueType.Unknown &&
                (valueType != IcalValueTypeRegistry.GetDefaultValueType(name.AsSpan()) ||
                 IcalConformance.ValueParameterRequiredProperties.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
                !parameters.Any(parameter => IsName(parameter.Name, IcalParameterNames.Value)))
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value, IcalValueTypeRegistry.GetValueTypeName(valueType)));
            }

            // ref. RFC 6321§3.6.1: a binary element is base64 with no ENCODING parameter, which RFC 5545§3.3.1 requires in iCalendar.
            if (valueType == IcalValueType.Binary && !parameters.Any(parameter => IsName(parameter.Name, IcalParameterNames.Encoding)))
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Encoding, "BASE64"));
            }

            return IcalContentLine.FromConversion(name, parameters.ToArray(), value, property.LineNumber, conversionProblem);
        }

        /// <summary>
        /// Checks the values of a property, including the parts of its periods and the UNTIL of its recurrence rules, for a value in the text
        /// form of a content line that xCal does not allow, such as a DATE-TIME of <c>20240101T120000Z</c> or a DURATION with a lower-case
        /// designator.  The converter keeps such a value as it is, and the conformance check would see a valid value.
        /// </summary>
        /// <returns>The problem with the first such value; or <c>null</c> if there is none.  A value that is not valid in either form is left to
        /// the conformance check, which reports it as not valid for its type.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? GetConversionProblem(List<XcalElement> values, IcalValueType valueType, string propertyName)
#else
        private static string GetConversionProblem(List<XcalElement> values, IcalValueType valueType, string propertyName)
#endif
        {
            // ref. RFC 6321§3.6: each value type has its own XML form.  Its patterns are XML Schema patterns, which are case-sensitive, so the
            // designators of DATE-TIME, TIME and DURATION are upper case only (§3.6.5, §3.6.6, §3.6.12).
            foreach (var value in values)
            {
                switch (valueType)
                {
                    case IcalValueType.Period:
                        foreach (var part in value.Children)
                        {
                            var partType = part.Name == XcalNames.Duration ? IcalValueType.Duration : IcalValueType.DateTime;
                            if (IcalConversionCheck.IsTextFormOnly(part.Text.AsSpan(), partType, IcalValueFormat.Xml))
                                return IcalConversionCheck.BuildProblem(propertyName, partType, IcalValueFormat.Xml);
                        }

                        break;
                    case IcalValueType.Recur:
                        foreach (var part in value.Children)
                        {
                            if (part.Name != XcalNames.Until) continue;

                            // ref. RFC 6321§3.6.10: UNTIL is a DATE or a DATE-TIME.
                            var until = IcalValueFormatting.TrimXmlWhitespace(part.Text.AsSpan());
                            if (IcalConversionCheck.IsTextFormOnly(until, IcalValueType.Date, IcalValueFormat.Xml))
                                return IcalConversionCheck.BuildProblem(propertyName, IcalValueType.Date, IcalValueFormat.Xml);
                            if (IcalConversionCheck.IsTextFormOnly(until, IcalValueType.DateTime, IcalValueFormat.Xml))
                                return IcalConversionCheck.BuildProblem(propertyName, IcalValueType.DateTime, IcalValueFormat.Xml);
                        }

                        // ref. RFC 6321 Appendix A (3.3.10): FREQ and WKST, and the weekday of BYDAY, are upper case.  A rule that is not valid
                        // in the text form either is left to the conformance check.
                        if (HasLowerCaseKeyword(value) && IcalRecurrenceRule.TryParse(ReadRecur(value, propertyName).AsSpan(), IcalValueFormat.Text, out _))
                            return string.Format(CultureInfo.InvariantCulture,
                                "The {0} property has a RECUR value with a lower-case FREQ, WKST or BYDAY weekday, which xCal does not allow " +
                                "(RFC 6321 Appendix A (3.3.10)).", propertyName);

                        break;
                    default:
                        if (IcalConversionCheck.IsTextFormOnly(value.Text.AsSpan(), valueType, IcalValueFormat.Xml))
                            return IcalConversionCheck.BuildProblem(propertyName, valueType, IcalValueFormat.Xml);

                        break;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the iCalendar name for an element: its name in upper case (RFC 6321 §4).
        /// </summary>
        /// <remarks>
        /// ref. RFC 6321 §3.3, §3.4 and §3.5: a component, property or parameter element has the iCalendar name "but in lowercase", and XML
        /// names are case-sensitive.  The rule parts of a recur element are named in lower case too (RFC 6321 Appendix A).
        /// </remarks>
        /// <exception cref="IcalFileException">The name is not an iCalendar name in lower case.</exception>
        internal static string ToIcalName(XcalElement element)
        {
            if (!IsLowerCaseName(element.Name))
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture,
                        "The element name {0} is not an xCal name, which is one or more lower-case letters, digits and hyphens: RFC 6321 §3.3 to §3.5.",
                        element.Name),
                    element.LineNumber, element.LinePosition, null);

            return element.Name.ToUpperInvariant();
        }

        /// <summary>
        /// Checks that text is an iCalendar name in lower case: one or more lower-case letters, digits and hyphens.  It is the form xCal and
        /// jCal give names in (RFC 6321 §3.3 to §3.5, RFC 7265 §3.3 to §3.5).
        /// </summary>
        internal static bool IsLowerCaseName(string text)
        {
            if (text.Length == 0) return false;

            foreach (var character in text)
            {
                if (!(character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '-')) return false;
            }

            return true;
        }

        /// <summary>
        /// Converts the parameter elements of a property.
        /// </summary>
        /// <returns>The problem with the first parameter value in the text form of a content line that xCal does not allow, such as a BOOLEAN of
        /// <c>TRUE</c>; or <c>null</c> if there is none.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? ReadParameters(XcalElement parametersElement, string propertyName, List<IcalContentLineParameter> parameters)
#else
        private static string ReadParameters(XcalElement parametersElement, string propertyName, List<IcalContentLineParameter> parameters)
#endif
        {
            CheckNoText(parametersElement, propertyName);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? problem = null;
#else
            string problem = null;
#endif
            foreach (var parameter in parametersElement.Children)
            {
                var name = ToIcalName(parameter);
                CheckNoText(parameter, propertyName);
                if (parameter.Children.Count == 0)
                    throw BuildException(parameter, propertyName, string.Format(CultureInfo.InvariantCulture, "The {0} parameter has no value element.", name));

                var values = new string[parameter.Children.Count];
                for (var index = 0; index < values.Length; index++)
                {
                    var valueElement = parameter.Children[index];
                    IcalValueType valueType;
                    if (!TryGetValueType(valueElement.Name, out valueType))
                        throw BuildException(valueElement, propertyName,
                            string.Format(CultureInfo.InvariantCulture, "The {0} element in the {1} parameter is not an xCal value element.", valueElement.Name,
                                name));

                    CheckNoChildren(valueElement, propertyName);

                    // ref. RFC 6321§5: an unknown parameter value is converted as if it were TEXT.  A BOOLEAN, such as RSVP, is upper case in
                    // iCalendar.
                    var text = valueElement.Text;
                    bool flag;
                    if (valueType == IcalValueType.Boolean && IcalValue.TryParseBoolean(text.AsSpan(), IcalValueFormat.Xml, out flag))
                    {
                        text = FormatBoolean(flag, IcalValueFormat.Text);
                    }
                    else if (problem is null && IcalConversionCheck.IsTextFormOnly(text.AsSpan(), valueType, IcalValueFormat.Xml))
                    {
                        // ref. RFC 6321§3.6.2: a BOOLEAN is true or false, so the TRUE of the text form, which the conformance check would
                        // accept, is not allowed.
                        problem = IcalConversionCheck.BuildParameterProblem(propertyName, name, valueType, IcalValueFormat.Xml);
                    }

                    values[index] = text;
                }

                parameters.Add(new IcalContentLineParameter(name, values));
            }

            return problem;
        }

        /// <summary>
        /// Converts the value elements of a property, all of one type, into the text of a content line.
        /// </summary>
        private static string ReadValues(List<XcalElement> values, string propertyName, out IcalValueType valueType)
        {
            var first = values[0];
            if (!TryGetValueType(first.Name, out valueType))
                throw BuildException(first, propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} element in the {1} property is not an xCal value element.", first.Name, propertyName));

            foreach (var value in values)
            {
                if (value.Name != first.Name)
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The values of the {0} property must all be of one type, but it has both {1} and {2} elements.",
                            propertyName, first.Name, value.Name));
            }

            // ref. RFC 6321§3.4.1.1: only the multi-valued properties have more than one value element.  The values of a property whose type
            // is not known may be a list too, as long as their type is.
            if (values.Count > 1 && !IsListProperty(propertyName) &&
                (IcalValueTypeRegistry.IsKnownProperty(propertyName.AsSpan()) || valueType == IcalValueType.Unknown))
                throw BuildException(values[1], propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} property can have only one value element.", propertyName));

            var builder = new StringBuilder();
            for (var index = 0; index < values.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                builder.Append(ReadValue(values[index], valueType, propertyName));
            }

            return builder.ToString();
        }

        private static string ReadValue(XcalElement value, IcalValueType valueType, string propertyName)
        {
            switch (valueType)
            {
                case IcalValueType.Period:
                    return ReadPeriod(value, propertyName);
                case IcalValueType.Recur:
                    return ReadRecur(value, propertyName);
                default:
                    CheckNoChildren(value, propertyName);
                    return ToText(valueType, value.Text, value, propertyName);
            }
        }

        private static string ReadPeriod(XcalElement period, string propertyName)
        {
            // ref. RFC 6321§3.6.9 and Appendix A (3.3.9): a start, then an end or a duration.
            CheckNoText(period, propertyName);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            XcalElement? start = null, end = null;
#else
            XcalElement start = null, end = null;
#endif
            foreach (var part in period.Children)
            {
                CheckNoChildren(part, propertyName);
                if (part.Name == XcalNames.Start && start is null && end is null)
                {
                    start = part;
                }
                else if ((part.Name == XcalNames.End || part.Name == XcalNames.Duration) && start != null && end is null)
                {
                    end = part;
                }
                else
                {
                    throw BuildException(part, propertyName,
                        string.Format(CultureInfo.InvariantCulture,
                            "The {0} element is not expected here in a period, which has a start element, then an end or a duration element: " +
                            "RFC 6321 Appendix A (3.3.9).", part.Name));
                }
            }

            if (start is null || end is null)
                throw BuildException(period, propertyName, "A period must have a start element, and an end or a duration element.");

            var endType = end.Name == XcalNames.Duration ? IcalValueType.Duration : IcalValueType.DateTime;
            return ToText(IcalValueType.DateTime, start.Text, start, propertyName) + "/" + ToText(endType, end.Text, end, propertyName);
        }

        private static string ReadRecur(XcalElement recur, string propertyName)
        {
            // ref. RFC 6321§3.6.10: each rule part is an element, and the values of a BYxxx rule part are repeated elements.  The parts are
            // written in the order of their first elements.
            CheckNoText(recur, propertyName);
            if (recur.Children.Count == 0)
                throw BuildException(recur, propertyName, "A recur element must have a child element for each rule part.");

            var parts = new List<KeyValuePair<string, StringBuilder>>();
            var lastRank = 0;
            var seenRankedPart = false;
            foreach (var part in recur.Children)
            {
                CheckNoChildren(part, propertyName);

                // ref. RFC 6321 Appendix A (3.3.10): value-recur is a sequence, so the rule parts are in its order, with UNTIL and COUNT in
                // one place, and only the BYxxx parts repeat.  An element it does not name, such as an upper-case FREQ (#1015), is not
                // checked here.
                var rank = GetRecurPartRank(part.Name);
                if (rank >= 0 && rank < lastRank)
                    throw BuildException(part, propertyName,
                        string.Format(CultureInfo.InvariantCulture,
                            "The {0} element is out of order in a recur element, whose rule parts are in the order {1}: RFC 6321 Appendix A (3.3.10).",
                            part.Name, string.Join(", ", RecurPartNames)));

                if (rank >= 0 && rank == lastRank && seenRankedPart && !IsRepeatableRecurPart(part.Name))
                    throw BuildException(part, propertyName,
                        string.Format(CultureInfo.InvariantCulture,
                            "The {0} element is repeated in a recur element, which can have only one freq, one until or count, one interval and one " +
                            "wkst: RFC 6321 Appendix A (3.3.10).", part.Name));

                if (rank >= 0)
                {
                    lastRank = rank;
                    seenRankedPart = true;
                }
                var name = ToIcalName(part);
                var value = IcalValueFormatting.TrimXmlWhitespace(part.Text.AsSpan()).ToString();
                if (part.Name == XcalNames.Until)
                {
                    value = ToText(value.Length == IcalDate.XmlLength ? IcalValueType.Date : IcalValueType.DateTime, value, part, propertyName);
                }

                var index = parts.FindIndex(pair => pair.Key == name);
                if (index < 0)
                {
                    parts.Add(new KeyValuePair<string, StringBuilder>(name, new StringBuilder(value)));
                }
                else
                {
                    parts[index].Value.Append(',').Append(value);
                }
            }

            return string.Join(";", parts.Select(pair => pair.Key + "=" + pair.Value));
        }

        /// <summary>
        /// Gets the place of a child element of a <c>recur</c> element in the order of RFC 6321 Appendix A (3.3.10), where <c>until</c> and
        /// <c>count</c> share a place; or -1 if the element is not one of its rule parts.
        /// </summary>
        private static int GetRecurPartRank(string name)
        {
            // ref. RFC 6321 Appendix A (3.3.10): (type-until | type-count)? is one item of the sequence.  The ranks need only increase
            // along the sequence, so COUNT takes the rank of UNTIL and its own index is left unused.
            return Array.IndexOf(RecurPartNames, name == CountPartName ? XcalNames.Until : name);
        }

        /// <summary>
        /// Determines whether a child element of a <c>recur</c> element may repeat: the BYxxx rule parts, one element for each value
        /// (RFC 6321 Appendix A (3.3.10)).
        /// </summary>
        private static bool IsRepeatableRecurPart(string name)
        {
            return name != FrequencyPartName && name != XcalNames.Until && name != CountPartName && name != IntervalPartName &&
                   name != WeekStartPartName;
        }

        /// <summary>
        /// Determines whether a <c>recur</c> element has a <c>freq</c> or <c>wkst</c> value, or the weekday of a <c>byday</c> value, with a
        /// lower-case letter.  RFC 5545 §3.1 makes the enumerated values of a rule case-insensitive, but in xCal they are the upper-case
        /// strings of <c>type-freq</c> and <c>type-weekday</c> (RFC 6321 Appendix A (3.3.10)).
        /// </summary>
        private static bool HasLowerCaseKeyword(XcalElement recur)
        {
            foreach (var part in recur.Children)
            {
                if (part.Name != FrequencyPartName && part.Name != WeekStartPartName && part.Name != ByDayPartName) continue;

                foreach (var character in IcalValueFormatting.TrimXmlWhitespace(part.Text.AsSpan()))
                {
                    if (character >= 'a' && character <= 'z') return true;
                }
            }

            return false;
        }

        private static string ReadGeo(List<XcalElement> values, string propertyName)
        {
            // ref. RFC 6321§3.4.1.2: a latitude element and a longitude element, each holding a FLOAT.
            var latitude = GetRequiredPart(values, XcalNames.Latitude, propertyName);
            var longitude = GetRequiredPart(values, XcalNames.Longitude, propertyName);

            // ref. RFC 6321 Appendix A (3.8.1.6): latitude, then longitude.
            CheckPartOrder(values, propertyName, "3.8.1.6", XcalNames.Latitude, XcalNames.Longitude);
            return ToText(IcalValueType.Float, latitude.Text, latitude, propertyName) + ";" +
                   ToText(IcalValueType.Float, longitude.Text, longitude, propertyName);
        }

        private static string ReadRequestStatus(List<XcalElement> values, string propertyName)
        {
            // ref. RFC 6321§3.4.1.3: code, description and optional data elements, each holding TEXT.
            var code = GetRequiredPart(values, XcalNames.Code, propertyName);
            var description = GetRequiredPart(values, XcalNames.Description, propertyName);
            var data = GetPart(values, XcalNames.Data, propertyName);

            // ref. RFC 6321 Appendix A (3.8.8.3): code, description, then any data.
            CheckPartOrder(values, propertyName, "3.8.8.3", XcalNames.Code, XcalNames.Description, XcalNames.Data);
            var text = ToText(IcalValueType.Text, code.Text, code, propertyName) + ";" +
                       ToText(IcalValueType.Text, description.Text, description, propertyName);
            return data is null ? text : text + ";" + ToText(IcalValueType.Text, data.Text, data, propertyName);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static XcalElement? GetPart(List<XcalElement> values, string name, string propertyName)
#else
        private static XcalElement GetPart(List<XcalElement> values, string name, string propertyName)
#endif
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            XcalElement? found = null;
#else
            XcalElement found = null;
#endif
            foreach (var value in values)
            {
                if (value.Name != name) continue;
                if (found != null)
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The {0} property can have only one {1} element.", propertyName, name));

                CheckNoChildren(value, propertyName);
                found = value;
            }

            return found;
        }

        /// <summary>
        /// Checks that the elements of a structured value are in the order of its sequence in RFC 6321 Appendix A.
        /// </summary>
        /// <param name="values">The elements, each of which is one of <paramref name="partNames"/>, once.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="schemaSection">The number of the comment in Appendix A that defines the property.</param>
        /// <param name="partNames">The names of the elements, in order.</param>
        /// <exception cref="IcalFileException">An element comes before one that the sequence puts ahead of it.</exception>
        private static void CheckPartOrder(List<XcalElement> values, string propertyName, string schemaSection, params string[] partNames)
        {
            var lastRank = 0;
            foreach (var value in values)
            {
                var rank = Array.IndexOf(partNames, value.Name);
                if (rank < lastRank)
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The {0} element is out of order in the {1} property, whose elements are in the order {2}: RFC 6321 Appendix A ({3}).",
                            value.Name, propertyName, string.Join(", ", partNames), schemaSection));

                lastRank = rank;
            }
        }

        private static XcalElement GetRequiredPart(List<XcalElement> values, string name, string propertyName)
        {
            var found = GetPart(values, name, propertyName);
            if (found is null)
                throw BuildException(values[0], propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} property has no {1} element.", propertyName, name));

            return found;
        }

        /// <summary>
        /// Converts the text of a value element to the text form of a content line; or keeps it as it is if it is not valid.
        /// </summary>
        private static string ToText(IcalValueType valueType, string xml, XcalElement element, string propertyName)
        {
            var value = xml.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Text:
                    // ref. RFC 6321§4: BACKSLASH character encoding MUST be applied, except to the semicolon of a VERSION range (RFC 5545§3.7.4).
                    try
                    {
                        return IsName(propertyName, IcalPropertyNames.Version)
                            ? IcalConformance.FormatVersion(xml)
                            : IcalValue.FormatText(xml, IcalValueFormat.Text);
                    }
                    catch (ArgumentException)
                    {
                        throw BuildException(element, propertyName,
                            string.Format(CultureInfo.InvariantCulture, "The text of the {0} property holds a character that iCalendar cannot hold.",
                                propertyName));
                    }
                case IcalValueType.Boolean:
                    bool flag;
                    return IcalValue.TryParseBoolean(value, IcalValueFormat.Xml, out flag) ? FormatBoolean(flag, IcalValueFormat.Text) : xml;
                case IcalValueType.Integer:
                    int integer;
                    if (IcalValue.TryParseInteger(value, IcalValueFormat.Text, out integer)) return xml;

                    return IcalValue.TryParseInteger(value, IcalValueFormat.Xml, out integer) ? IcalPropertyValue.FormatInteger(integer) : xml;
                case IcalValueType.Float:
                    double number;
                    if (IcalValue.TryParseFloat(value, IcalValueFormat.Text, out number)) return xml;

                    return IcalValue.TryParseFloat(value, IcalValueFormat.Xml, out number) ? IcalValue.FormatFloat(number, IcalValueFormat.Text) : xml;
                case IcalValueType.Binary:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    byte[]? bytes;
#else
                    byte[] bytes;
#endif
                    if (IcalValue.TryParseBinary(value, IcalValueFormat.Text, out bytes)) return xml;

                    return IcalValue.TryParseBinary(value, IcalValueFormat.Xml, out bytes) ? IcalValue.FormatBinary(bytes, IcalValueFormat.Text) : xml;
                case IcalValueType.Uri:
                case IcalValueType.CalAddress:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    Uri? uri;
#else
                    Uri uri;
#endif
                    return IcalValue.TryParseUri(value, IcalValueFormat.Xml, out uri)
                        ? IcalValueFormatting.TrimXmlWhitespace(value).ToString()
                        : xml;
                case IcalValueType.Date:
                    IcalDate date;
                    return IcalDate.TryParse(value, IcalValueFormat.Xml, out date) ? date.ToString(IcalValueFormat.Text) : xml;
                case IcalValueType.DateTime:
                    IcalDateTime dateTime;
                    return IcalDateTime.TryParse(value, IcalValueFormat.Xml, out dateTime) ? dateTime.ToString(IcalValueFormat.Text) : xml;
                case IcalValueType.Time:
                    IcalTime time;
                    return IcalTime.TryParse(value, IcalValueFormat.Xml, out time) ? time.ToString(IcalValueFormat.Text) : xml;
                case IcalValueType.UtcOffset:
                    IcalUtcOffset offset;
                    return IcalUtcOffset.TryParse(value, IcalValueFormat.Xml, out offset) ? offset.ToString(IcalValueFormat.Text) : xml;
                default:
                    // DURATION is the same in both forms but for the case of its designators, which GetConversionProblem checks, and an unknown
                    // value is kept as it is (RFC 6321§5).
                    return xml;
            }
        }

        #endregion

        #region iCalendar to xCal

        /// <summary>
        /// Converts a content line into a property element.
        /// </summary>
        /// <param name="property">The content line.</param>
        /// <exception cref="IcalFileException">The name of the property or a parameter cannot be written in xCal, or a value holds a character
        /// that XML cannot hold.</exception>
        internal static XcalElement ToElement(IcalContentLine property)
        {
            var name = property.Name;
            var element = new XcalElement(ToElementName(name, property.LineNumber));

            // ref. RFC 6321§3.5.1 and §5: the value type gives the value element, so the VALUE parameter is not written.  A property whose
            // value type is not known has an unknown element.
            var valueType = IcalValueTypeRegistry.GetValueTypeOfAnyProperty(name,
                IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Value));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            List<XcalElement>? values = null;
#else
            List<XcalElement> values = null;
#endif
            if (valueType != IcalValueType.Unknown)
            {
                values = TryWriteValues(property, valueType);
            }

            // A value that cannot be converted is kept in an unknown element, with its VALUE parameter.
            var isValueParameterKept = values is null;
            if (values is null)
            {
                values = new List<XcalElement> { CreateTextElement(XcalNames.Unknown, property.Value, property) };
            }

            var parameters = new XcalElement(XcalNames.Parameters);
            foreach (var parameter in property.ParameterArray)
            {
                if (!isValueParameterKept && IsName(parameter.Name, IcalParameterNames.Value)) continue;

                var parameterElement = parameters.Add(new XcalElement(ToElementName(parameter.Name, property.LineNumber)));
                foreach (var value in parameter.ValueArray)
                {
                    WriteParameterValue(parameterElement, parameter.Name, value, property);
                }
            }

            // ref. RFC 6321§3.5: the parameters element SHOULD NOT be present if there are no parameters.
            if (parameters.Children.Count != 0)
            {
                element.Add(parameters);
            }

            foreach (var value in values)
            {
                element.Add(value);
            }

            return element;
        }

        private static void WriteParameterValue(XcalElement parameterElement, string parameterName, string value, IcalContentLine property)
        {
            // ref. RFC 6321 Appendix A §3.2 and §5: the value element of each parameter, and unknown for one that is not recognised.
            string valueName;
            bool flag;
            if (UriParameters.Contains(parameterName, StringComparer.OrdinalIgnoreCase))
            {
                valueName = ValueElementNames[(int)IcalValueType.Uri];
            }
            else if (CalAddressParameters.Contains(parameterName, StringComparer.OrdinalIgnoreCase))
            {
                valueName = ValueElementNames[(int)IcalValueType.CalAddress];
            }
            else if (IsName(parameterName, IcalParameterNames.Rsvp) && IcalValue.TryParseBoolean(value.AsSpan(), IcalValueFormat.Text, out flag))
            {
                valueName = ValueElementNames[(int)IcalValueType.Boolean];
                value = FormatBoolean(flag, IcalValueFormat.Xml);
            }
            else if (TextParameters.Contains(parameterName, StringComparer.OrdinalIgnoreCase))
            {
                valueName = ValueElementNames[(int)IcalValueType.Text];
            }
            else
            {
                valueName = XcalNames.Unknown;
            }

            parameterElement.Add(CreateTextElement(valueName, value, property));
        }

        /// <summary>
        /// Converts the value of a property into value elements.
        /// </summary>
        /// <returns>The elements; or <c>null</c> if the value is not a valid value of its type.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcalElement>? TryWriteValues(IcalContentLine property, IcalValueType valueType)
#else
        private static List<XcalElement> TryWriteValues(IcalContentLine property, IcalValueType valueType)
#endif
        {
            var name = property.Name;
            var value = property.Value;

            // ref. RFC 5545§3.2.20: a VALUE parameter may name the default type, so the structure follows the type, not the parameter.
            if (valueType == IcalValueType.Float && IsName(name, IcalPropertyNames.Geo)) return TryWriteGeo(value, property);
            if (valueType == IcalValueType.Text && IsName(name, IcalPropertyNames.RequestStatus)) return TryWriteRequestStatus(value, property);
            // ref. RFC 5545§3.7.4: the semicolon between minver and maxver is a separator, not TEXT.
            if (valueType == IcalValueType.Text && IsName(name, IcalPropertyNames.Version)) return TryWriteVersion(value, property);

            // ref. RFC 6321§3.4.1.1: each value of a multi-valued property is an element.  The values of a property whose type is not known
            // may be a list too, so they are split as well.
            var items = new List<string>();
            if (!IsListProperty(name) && IcalValueTypeRegistry.IsKnownProperty(name.AsSpan()))
            {
                items.Add(value);
            }
            else if (valueType == IcalValueType.Text)
            {
                foreach (var item in IcalValue.EnumerateTextList(value.AsSpan(), IcalValueFormat.Text))
                {
                    items.Add(item.ToString());
                }
            }
            else
            {
                items.AddRange(value.Split(','));
            }

            var elementName = ValueElementNames[(int)valueType];
            var elements = new List<XcalElement>();
            foreach (var item in items)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                XcalElement? element;
#else
                XcalElement element;
#endif
                switch (valueType)
                {
                    case IcalValueType.Period:
                        element = TryWritePeriod(item, property);
                        break;
                    case IcalValueType.Recur:
                        element = TryWriteRecur(item, property);
                        break;
                    default:
                        var xml = TryToXml(valueType, item);
                        element = xml is null ? null : CreateTextElement(elementName, xml, property);
                        break;
                }

                if (element is null) return null;

                elements.Add(element);
            }

            return elements;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static XcalElement? TryWritePeriod(string value, IcalContentLine property)
#else
        private static XcalElement TryWritePeriod(string value, IcalContentLine property)
#endif
        {
            // ref. RFC 6321§3.6.9: a period element with start, and end or duration, elements.
            IcalPeriod period;
            if (!IcalPeriod.TryParse(value.AsSpan(), IcalValueFormat.Text, out period)) return null;

            var element = new XcalElement(ValueElementNames[(int)IcalValueType.Period]);
            element.Add(XcalNames.Start, period.Start.ToString(IcalValueFormat.Xml));
            if (period.Duration.HasValue)
            {
                element.Add(XcalNames.Duration, ToXmlDuration(value.Substring(value.IndexOf('/') + 1)));
            }
            else
            {
                element.Add(XcalNames.End, period.End.GetValueOrDefault().ToString(IcalValueFormat.Xml));
            }

            return element;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static XcalElement? TryWriteRecur(string value, IcalContentLine property)
#else
        private static XcalElement TryWriteRecur(string value, IcalContentLine property)
#endif
        {
            // ref. RFC 6321§3.6.10: a recur element with an element for each rule part, and one for each value of a BYxxx rule part.  ref. RFC
            // 6321 Appendix A (3.3.10): the elements follow the order of value-recur, whatever the order of the rule parts, and the values of
            // freq and wkst, and the weekday of a byday value, are upper case.
            IcalRecurrenceRule rule;
            if (!IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out rule)) return null;

            // The rule parsed, so each rule part is one of RecurPartNames, and appears once.
            var partValues = new string[RecurPartNames.Length];
            foreach (var part in value.Split(';'))
            {
                var equalsIndex = part.IndexOf('=');
                var partName = ToElementName(part.Substring(0, equalsIndex), property.LineNumber);
                var partValue = part.Substring(equalsIndex + 1);
                if (partName == XcalNames.Until)
                {
                    var until = TryToXml(partValue.Length == IcalDate.TextLength ? IcalValueType.Date : IcalValueType.DateTime, partValue);
                    if (until is null) return null;

                    partValue = until;
                }

                partValues[Array.IndexOf(RecurPartNames, partName)] = partValue;
            }

            var element = new XcalElement(ValueElementNames[(int)IcalValueType.Recur]);
            for (var index = 0; index < RecurPartNames.Length; index++)
            {
                var partName = RecurPartNames[index];
                var partValue = partValues[index];
                if (partValue is null) continue;

                if (partName.StartsWith("by", StringComparison.Ordinal))
                {
                    foreach (var item in partValue.Split(','))
                    {
                        element.Add(partName, partName == ByDayPartName ? item.ToUpperInvariant() : item);
                    }
                }
                else
                {
                    element.Add(partName, partName == FrequencyPartName || partName == WeekStartPartName ? partValue.ToUpperInvariant() : partValue);
                }
            }

            return element;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcalElement>? TryWriteGeo(string value, IcalContentLine property)
#else
        private static List<XcalElement> TryWriteGeo(string value, IcalContentLine property)
#endif
        {
            // ref. RFC 6321§3.4.1.2: latitude and longitude elements, each holding a FLOAT.
            var parts = value.Split(';');
            double number;
            if (parts.Length != 2 || !IcalValue.TryParseFloat(parts[0].AsSpan(), IcalValueFormat.Text, out number) ||
                !IcalValue.TryParseFloat(parts[1].AsSpan(), IcalValueFormat.Text, out number))
            {
                return null;
            }

            return new List<XcalElement>
            {
                CreateTextElement(XcalNames.Latitude, parts[0], property), CreateTextElement(XcalNames.Longitude, parts[1], property)
            };
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcalElement>? TryWriteVersion(string value, IcalContentLine property)
#else
        private static List<XcalElement> TryWriteVersion(string value, IcalContentLine property)
#endif
        {
            // A range is one text element, minver ";" maxver, as XcalReader reads it.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? text;
#else
            string text;
#endif
            if (!IcalConformance.TryParseVersion(value, out text)) return null;

            return new List<XcalElement> { CreateTextElement(ValueElementNames[(int)IcalValueType.Text], text, property) };
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcalElement>? TryWriteRequestStatus(string value, IcalContentLine property)
#else
        private static List<XcalElement> TryWriteRequestStatus(string value, IcalContentLine property)
#endif
        {
            // ref. RFC 5545§3.8.8.3: rstatus = statcode ";" statdesc [";" extdata].  ref. RFC 6321§3.4.1.3: code, description and data
            // elements, each holding TEXT; data SHOULD NOT be present if there is none.
            var parts = SplitOnUnescaped(value, ';', 3);
            if (parts.Count < 2) return null;

            string[] names = { XcalNames.Code, XcalNames.Description, XcalNames.Data };
            var elements = new List<XcalElement>();
            for (var index = 0; index < parts.Count; index++)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                string? text;
#else
                string text;
#endif
                if (!IcalValue.TryParseText(parts[index].AsSpan(), IcalValueFormat.Text, out text)) return null;

                elements.Add(CreateTextElement(names[index], text, property));
            }

            return elements;
        }

        /// <summary>
        /// Converts a valid DURATION from the text form of a content line to the text of a duration element.
        /// </summary>
        private static string ToXmlDuration(string text)
        {
            // ref. RFC 6321§3.6.6: the designators are upper case, as the xCal pattern is case-sensitive; the text form also allows lower case
            // (RFC 5545§3.3.6).  Otherwise the forms are the same, so the digits are kept as written.  A valid duration holds only ASCII
            // characters, so the invariant culture changes only its designators.  Nearly every duration is already upper case, and is kept.
            foreach (var character in text)
            {
                if (character >= 'a' && character <= 'z') return text.ToUpperInvariant();
            }

            return text;
        }

        /// <summary>
        /// Converts a value from the text form of a content line to the text of a value element.
        /// </summary>
        /// <returns>The text; or <c>null</c> if the value is not valid.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? TryToXml(IcalValueType valueType, string text)
#else
        private static string TryToXml(IcalValueType valueType, string text)
#endif
        {
            var value = text.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Text:
                    // ref. RFC 6321§3.1: the escaping MUST be removed.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? unescaped;
#else
                    string unescaped;
#endif
                    return IcalValue.TryParseText(value, IcalValueFormat.Text, out unescaped) ? unescaped : null;
                case IcalValueType.Boolean:
                    bool flag;
                    return IcalValue.TryParseBoolean(value, IcalValueFormat.Text, out flag) ? FormatBoolean(flag, IcalValueFormat.Xml) : null;
                case IcalValueType.Integer:
                    return IcalValue.TryParseInteger(value, IcalValueFormat.Text, out _) ? text : null;
                case IcalValueType.Float:
                    return IcalValue.TryParseFloat(value, IcalValueFormat.Text, out _) ? text : null;
                case IcalValueType.Binary:
                    return IcalValue.TryParseBinary(value, IcalValueFormat.Text, out _) ? text : null;
                case IcalValueType.Uri:
                case IcalValueType.CalAddress:
                    return IcalValue.TryParseUri(value, IcalValueFormat.Text, out _) ? text : null;
                case IcalValueType.Duration:
                    return IcalDuration.TryParse(value, IcalValueFormat.Text, out _) ? ToXmlDuration(text) : null;
                case IcalValueType.Date:
                    IcalDate date;
                    return IcalDate.TryParse(value, IcalValueFormat.Text, out date) ? date.ToString(IcalValueFormat.Xml) : null;
                case IcalValueType.DateTime:
                    IcalDateTime dateTime;
                    return IcalDateTime.TryParse(value, IcalValueFormat.Text, out dateTime) ? dateTime.ToString(IcalValueFormat.Xml) : null;
                case IcalValueType.Time:
                    IcalTime time;
                    return IcalTime.TryParse(value, IcalValueFormat.Text, out time) ? time.ToString(IcalValueFormat.Xml) : null;
                case IcalValueType.UtcOffset:
                    IcalUtcOffset offset;
                    return IcalUtcOffset.TryParse(value, IcalValueFormat.Text, out offset) ? offset.ToString(IcalValueFormat.Xml) : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates an element holding text, checking that XML can hold the text.
        /// </summary>
        /// <exception cref="IcalFileException">The text holds a character that XML cannot hold.</exception>
        private static XcalElement CreateTextElement(string name, string text, IcalContentLine property)
        {
            if (!IcalValueFormatting.IsXmlText(text.AsSpan()))
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture, "The {0} property holds a character that XML cannot hold, so it cannot be written in xCal.",
                        property.Name.ToUpperInvariant()), property.LineNumber, 0, property.Name);

            var element = new XcalElement(name);
            element.AppendText(text);
            return element;
        }

        /// <summary>
        /// Splits text on the separators that are not escaped with a backslash, into at most <paramref name="maxParts"/> parts.
        /// </summary>
        private static List<string> SplitOnUnescaped(string value, char separator, int maxParts)
        {
            var parts = new List<string>();
            var start = 0;
            for (var index = 0; index < value.Length && parts.Count < maxParts - 1; index++)
            {
                if (value[index] == '\\')
                {
                    index++;
                }
                else if (value[index] == separator)
                {
                    parts.Add(value.Substring(start, index - start));
                    start = index + 1;
                }
            }

            parts.Add(value.Substring(start));
            return parts;
        }

        #endregion

        /// <summary>
        /// Gets the value type a value element names.
        /// </summary>
        /// <returns><c>true</c> if the name is that of a value element, including <c>unknown</c>; otherwise <c>false</c>.</returns>
        private static bool TryGetValueType(string elementName, out IcalValueType valueType)
        {
            var index = Array.IndexOf(ValueElementNames, elementName);
            valueType = index < 0 ? IcalValueType.Unknown : (IcalValueType)index;
            return index >= 0;
        }

        private static bool IsStructuredValue(List<XcalElement> values, params string[] partNames)
        {
            foreach (var value in values)
            {
                if (Array.IndexOf(partNames, value.Name) < 0) return false;
            }

            return true;
        }

        private static bool IsListProperty(string name)
        {
            return IcalConformance.ListProperties.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                   IcalConformance.TextListProperties.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private static string FormatBoolean(bool value, IcalValueFormat format)
        {
            var buffer = new char[5];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, IcalValue.TryFormatBoolean(value, buffer, out charsWritten, format), charsWritten);
        }

        private static void CheckNoText(XcalElement element, string propertyName)
        {
            if (element.HasNonWhitespaceText)
                throw BuildException(element, propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} element cannot hold text; its content must be elements.", element.Name));
        }

        private static void CheckNoChildren(XcalElement element, string propertyName)
        {
            if (element.Children.Count != 0)
                throw BuildException(element.Children[0], propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} element cannot hold elements; its content must be text.", element.Name));
        }

        private static bool IsName(string name, string expected)
        {
            return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static IcalFileException BuildException(XcalElement element, string propertyName, string message)
        {
            return BuildException(message, element.LineNumber, element.LinePosition, propertyName);
        }

        /// <summary>
        /// Builds the exception for a problem in an xCal document.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="lineNumber">The 1-based line of the XML document; or 0 if not known.</param>
        /// <param name="linePosition">The 1-based position on the line; or 0 if not known.</param>
        /// <param name="propertyName">The name of the property; or <c>null</c>.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static IcalFileException BuildException(string message, long lineNumber, long linePosition, string? propertyName)
#else
        internal static IcalFileException BuildException(string message, long lineNumber, long linePosition, string propertyName)
#endif
        {
            var exception = new IcalFileException(message);
            AddLocation(exception, lineNumber, linePosition);
            if (propertyName != null)
            {
                exception.Data[IcalFileException.DataItemName.PropertyName] = propertyName;
            }

            return exception;
        }

        /// <summary>
        /// Adds the line and position in an XML document to the data of an exception.
        /// </summary>
        internal static void AddLocation(IcalFileException exception, long lineNumber, long linePosition)
        {
            if (lineNumber != 0)
            {
                exception.Data[IcalFileException.DataItemName.LineNumber] = lineNumber;
            }

            if (linePosition != 0)
            {
                exception.Data[IcalFileException.DataItemName.LinePosition] = linePosition;
            }
        }
    }
}
