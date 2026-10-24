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

        /// <summary>
        /// The properties whose value is a list of TEXT values.
        /// </summary>
        private static readonly string[] TextListProperties = { IcalPropertyNames.Categories, IcalPropertyNames.Resources };

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
            CheckNoText(property, name);

            var parameters = new List<IcalContentLineParameter>();
            var values = new List<XcalElement>();
            var hasParameters = false;
            foreach (var child in property.Children)
            {
                if (child.Name == XcalNames.Parameters)
                {
                    if (hasParameters)
                        throw BuildException(child, name, "A property can have only one parameters element.");

                    hasParameters = true;
                    ReadParameters(child, name, parameters);
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
            }

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

            return IcalContentLine.FromConversion(name, parameters.ToArray(), value, property.LineNumber);
        }

        /// <summary>
        /// Gets the iCalendar name for an element: its name in upper case (RFC 6321 §4).
        /// </summary>
        /// <exception cref="IcalFileException">The name is not a valid iCalendar name.</exception>
        internal static string ToIcalName(XcalElement element)
        {
            var name = element.Name.ToUpperInvariant();
            if (!IcalComponent.IsValidName(name))
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture,
                        "The element name {0} is not a valid iCalendar name, which is one or more letters, digits and hyphens.", element.Name),
                    element.LineNumber, element.LinePosition, null);

            return name;
        }

        private static void ReadParameters(XcalElement parametersElement, string propertyName, List<IcalContentLineParameter> parameters)
        {
            CheckNoText(parametersElement, propertyName);
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

                    values[index] = text;
                }

                parameters.Add(new IcalContentLineParameter(name, values));
            }
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
            // ref. RFC 6321§3.6.9: a start, and an end or a duration.
            CheckNoText(period, propertyName);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            XcalElement? start = null, end = null;
#else
            XcalElement start = null, end = null;
#endif
            foreach (var part in period.Children)
            {
                CheckNoChildren(part, propertyName);
                if (part.Name == XcalNames.Start && start is null)
                {
                    start = part;
                }
                else if ((part.Name == XcalNames.End || part.Name == XcalNames.Duration) && end is null)
                {
                    end = part;
                }
                else
                {
                    throw BuildException(part, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The {0} element is not expected in a period, which has a start and an end or a duration.",
                            part.Name));
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
            foreach (var part in recur.Children)
            {
                CheckNoChildren(part, propertyName);
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

        private static string ReadGeo(List<XcalElement> values, string propertyName)
        {
            // ref. RFC 6321§3.4.1.2: a latitude element and a longitude element, each holding a FLOAT.
            var latitude = GetRequiredPart(values, XcalNames.Latitude, propertyName);
            var longitude = GetRequiredPart(values, XcalNames.Longitude, propertyName);
            return ToText(IcalValueType.Float, latitude.Text, latitude, propertyName) + ";" +
                   ToText(IcalValueType.Float, longitude.Text, longitude, propertyName);
        }

        private static string ReadRequestStatus(List<XcalElement> values, string propertyName)
        {
            // ref. RFC 6321§3.4.1.3: code, description and optional data elements, each holding TEXT.
            var code = GetRequiredPart(values, XcalNames.Code, propertyName);
            var description = GetRequiredPart(values, XcalNames.Description, propertyName);
            var data = GetPart(values, XcalNames.Data, propertyName);
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
                    // ref. RFC 6321§4: BACKSLASH character encoding MUST be applied.
                    try
                    {
                        return IcalValue.FormatText(xml, IcalValueFormat.Text);
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
                    // DURATION is the same in both forms, and an unknown value is kept as it is (RFC 6321§5).
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
            var valueParameter = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Value);
            IcalValueType valueType;
            if (valueParameter is null)
            {
                valueType = IcalValueTypeRegistry.GetDefaultValueType(name.AsSpan());
            }
            else if (!IcalValueTypeRegistry.TryParseValueTypeName(valueParameter.AsSpan(), out valueType))
            {
                valueType = IcalValueType.Unknown;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            List<XcalElement>? values = null;
#else
            List<XcalElement> values = null;
#endif
            if (valueType != IcalValueType.Unknown)
            {
                values = TryWriteValues(property, valueType, valueParameter is null);
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
        private static List<XcalElement>? TryWriteValues(IcalContentLine property, IcalValueType valueType, bool isDefaultValueType)
#else
        private static List<XcalElement> TryWriteValues(IcalContentLine property, IcalValueType valueType, bool isDefaultValueType)
#endif
        {
            var name = property.Name;
            var value = property.Value;
            if (isDefaultValueType && IsName(name, IcalPropertyNames.Geo)) return TryWriteGeo(value, property);
            if (isDefaultValueType && IsName(name, IcalPropertyNames.RequestStatus)) return TryWriteRequestStatus(value, property);

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
                // The duration is the same in both forms, so its text is kept.
                element.Add(XcalNames.Duration, value.Substring(value.IndexOf('/') + 1));
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
            // ref. RFC 6321§3.6.10: a recur element with an element for each rule part, and one for each value of a BYxxx rule part.  The
            // rule parts keep their order.
            IcalRecurrenceRule rule;
            if (!IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out rule)) return null;

            var element = new XcalElement(ValueElementNames[(int)IcalValueType.Recur]);
            foreach (var part in value.Split(';'))
            {
                var equalsIndex = part.IndexOf('=');
                var partName = ToElementName(part.Substring(0, equalsIndex), property.LineNumber);
                var partValue = part.Substring(equalsIndex + 1);
                if (partName == XcalNames.Until)
                {
                    var until = TryToXml(partValue.Length == IcalDate.TextLength ? IcalValueType.Date : IcalValueType.DateTime, partValue);
                    if (until is null) return null;

                    element.Add(partName, until);
                }
                else if (partName.StartsWith("by", StringComparison.Ordinal))
                {
                    foreach (var item in partValue.Split(','))
                    {
                        element.Add(partName, item);
                    }
                }
                else
                {
                    element.Add(partName, partValue);
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
                    return IcalDuration.TryParse(value, IcalValueFormat.Text, out _) ? text : null;
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
                   TextListProperties.Contains(name, StringComparer.OrdinalIgnoreCase);
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
