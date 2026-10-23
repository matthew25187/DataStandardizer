#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Converts properties between the content lines of the component object model and the property arrays of jCal (RFC 7265).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A content line holds its value in the text form of RFC 5545, so converting from jCal turns each JSON value into that form, and converting
    /// to jCal turns the text back into JSON values.  The text of a value is kept where both forms allow it, such as the digits of a FLOAT, so
    /// that a value that goes to jCal and back is unchanged.
    /// </para>
    /// <para>
    /// A value that cannot be converted, which <see cref="IcalOptions.AllowInvalidValues"/> lets a calendar hold, is kept as it is: from jCal as
    /// the string's text, which the conformance check then reports; and to jCal with the <c>unknown</c> type, keeping any <c>VALUE</c> parameter
    /// so that nothing is lost.
    /// </para>
    /// </remarks>
    internal static class JcalConverter
    {
        /// <summary>
        /// The type identifier of a value whose type is not known (RFC 7265 §5).
        /// </summary>
        internal const string UnknownTypeName = "unknown";

        /// <summary>
        /// The name of the component at the top of a jCal object (RFC 7265 §3.2).
        /// </summary>
        internal const string CalendarName = "vcalendar";

        /// <summary>
        /// The rule part of a RECUR value whose value is a DATE or DATE-TIME (RFC 7265 §3.6.10).
        /// </summary>
        private const string UntilPartName = "UNTIL";

        /// <summary>
        /// The rule parts of a RECUR value whose value is a single JSON number (RFC 7265 §3.6.10).
        /// </summary>
        private static readonly string[] NumberPartNames = { "COUNT", "INTERVAL" };

        /// <summary>
        /// The rule parts of a RECUR value whose values are strings, not numbers (RFC 7265 §3.6.10).  The other BYxxx rule parts hold numbers.
        /// </summary>
        private static readonly string[] StringListPartNames = { "BYDAY" };

        /// <summary>
        /// The properties whose value is a list of TEXT values.
        /// </summary>
        private static readonly string[] TextListProperties = { IcalPropertyNames.Categories, IcalPropertyNames.Resources };

        #region jCal to iCalendar

        /// <summary>
        /// Converts a property array into a content line.
        /// </summary>
        /// <param name="property">The property array: a name, a parameters object, a type identifier and one or more values.</param>
        /// <exception cref="IcalFileException">The array is not a valid jCal property.</exception>
        internal static IcalContentLine ToContentLine(JcalNode property)
        {
            var items = property.Items;
            if (items.Count == 0 || items[0].Kind != JcalNodeKind.String)
                throw BuildException(property, null, "A property must be an array whose first element is the name of the property, as a string.");

            var name = ToIcalName(items[0], null, "property");
            if (items.Count < 4)
                throw BuildException(property, name,
                    string.Format(CultureInfo.InvariantCulture,
                        "The {0} property must be an array of its name, a parameters object, a type identifier and at least one value.", name));

            if (items[1].Kind != JcalNodeKind.Object)
                throw BuildException(items[1], name,
                    string.Format(CultureInfo.InvariantCulture, "The second element of the {0} property must be an object holding its parameters.", name));

            var parameters = ReadParameters(items[1], name);
            if (items[2].Kind != JcalNodeKind.String)
                throw BuildException(items[2], name,
                    string.Format(CultureInfo.InvariantCulture, "The third element of the {0} property must be the type identifier, as a string.", name));

            // ref. RFC 7265§5.2: an unknown value is taken over without processing, and has no VALUE parameter.  A type identifier that is not
            // one of RFC 5545's is an extension value type, whose value is kept as it is.
            var typeName = items[2].Text;
            IcalValueType valueType;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? extensionTypeName = null;
#else
            string extensionTypeName = null;
#endif
            if (!IcalValueTypeRegistry.TryParseValueTypeName(typeName.AsSpan(), out valueType) &&
                !IcalValueFormatting.EqualsKeyword(typeName.AsSpan(), "UNKNOWN"))
            {
                extensionTypeName = typeName.ToUpperInvariant();
                if (!IcalComponent.IsValidName(extensionTypeName))
                    throw BuildException(items[2], name,
                        string.Format(CultureInfo.InvariantCulture,
                            "The type identifier {0} of the {1} property is not a valid value type name, which is one or more letters, digits and hyphens.",
                            typeName, name));
            }

            var values = new List<JcalNode>();
            for (var index = 3; index < items.Count; index++)
            {
                values.Add(items[index]);
            }

            string value;
            if (IsName(name, IcalPropertyNames.Geo) && valueType == IcalValueType.Float && values.Count == 1 && values[0].Kind == JcalNodeKind.Array)
            {
                value = ReadGeo(values[0], name);
            }
            else if (IsName(name, IcalPropertyNames.RequestStatus) && valueType == IcalValueType.Text && values.Count == 1 &&
                     values[0].Kind == JcalNodeKind.Array)
            {
                value = ReadRequestStatus(values[0], name);
            }
            else
            {
                value = ReadValues(values, name, valueType);
            }

            // ref. RFC 7265§3.5.1 and §4: a VALUE parameter is added if the type is not the property's default, but never for an unknown value.
            // RFC 7986 properties with no default always need one.
            if (!parameters.Any(parameter => IsName(parameter.Name, IcalParameterNames.Value)))
            {
                if (extensionTypeName != null)
                {
                    parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value, extensionTypeName));
                }
                else if (valueType != IcalValueType.Unknown &&
                         (valueType != IcalValueTypeRegistry.GetDefaultValueType(name.AsSpan()) ||
                          IcalConformance.ValueParameterRequiredProperties.Contains(name, StringComparer.OrdinalIgnoreCase)))
                {
                    parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value, IcalValueTypeRegistry.GetValueTypeName(valueType)));
                }
            }

            // ref. RFC 5545§3.3.1: a BINARY value needs an ENCODING parameter, which a jCal property may leave out.
            if (valueType == IcalValueType.Binary && !parameters.Any(parameter => IsName(parameter.Name, IcalParameterNames.Encoding)))
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Encoding, "BASE64"));
            }

            return IcalContentLine.FromConversion(name, parameters.ToArray(), value, 0);
        }

        /// <summary>
        /// Gets the iCalendar name for the name of a component or property: the name in upper case (RFC 7265 §4).
        /// </summary>
        /// <exception cref="IcalFileException">The name is not a valid iCalendar name.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string ToIcalName(JcalNode node, string? propertyName, string kind)
#else
        internal static string ToIcalName(JcalNode node, string propertyName, string kind)
#endif
        {
            return ToIcalName(node.Text, node, propertyName, kind);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string ToIcalName(string text, JcalNode node, string? propertyName, string kind)
#else
        private static string ToIcalName(string text, JcalNode node, string propertyName, string kind)
#endif
        {
            var name = text.ToUpperInvariant();
            if (!IcalComponent.IsValidName(name))
                throw BuildException(node, propertyName,
                    string.Format(CultureInfo.InvariantCulture,
                        "The {0} name \"{1}\" is not a valid iCalendar name, which is one or more letters, digits and hyphens.", kind, text));

            return name;
        }

        private static List<IcalContentLineParameter> ReadParameters(JcalNode parametersNode, string propertyName)
        {
            var parameters = new List<IcalContentLineParameter>();
            var items = parametersNode.Items;
            var names = parametersNode.Names;
            for (var index = 0; index < items.Count; index++)
            {
                var valueNode = items[index];
                var name = ToIcalName(names[index], valueNode, propertyName, "parameter");
                if (parameters.Any(parameter => IsName(parameter.Name, name)))
                    throw BuildException(valueNode, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The {0} parameter appears more than once in the {1} property.", name, propertyName));

                // ref. RFC 7265§3.5.2: a parameter value is a string, or an array of strings for a multi-value parameter.
                string[] values;
                if (valueNode.Kind == JcalNodeKind.String)
                {
                    values = new[] { valueNode.Text };
                }
                else if (valueNode.Kind == JcalNodeKind.Array && valueNode.Items.Count != 0 &&
                         valueNode.Items.All(item => item.Kind == JcalNodeKind.String))
                {
                    values = valueNode.Items.Select(item => item.Text).ToArray();
                }
                else
                {
                    throw BuildException(valueNode, propertyName,
                        string.Format(CultureInfo.InvariantCulture,
                            "The value of the {0} parameter must be a string, or a non-empty array of strings.", name));
                }

                parameters.Add(new IcalContentLineParameter(name, values));
            }

            return parameters;
        }

        /// <summary>
        /// Converts the values of a property into the text of a content line.
        /// </summary>
        private static string ReadValues(List<JcalNode> values, string propertyName, IcalValueType valueType)
        {
            // ref. RFC 7265§3.4: only the multi-valued properties have more than one value.  The values of a property whose type is not known
            // may be a list too, as long as their type is.
            if (values.Count > 1 && !IsListProperty(propertyName) &&
                (IcalValueTypeRegistry.IsKnownProperty(propertyName.AsSpan()) || valueType == IcalValueType.Unknown))
                throw BuildException(values[1], propertyName,
                    string.Format(CultureInfo.InvariantCulture, "The {0} property can have only one value.", propertyName));

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

        private static string ReadValue(JcalNode value, IcalValueType valueType, string propertyName)
        {
            switch (value.Kind)
            {
                case JcalNodeKind.Array when valueType == IcalValueType.Period:
                    return ReadPeriod(value, propertyName);
                case JcalNodeKind.Object when valueType == IcalValueType.Recur:
                    return ReadRecur(value, propertyName);
                case JcalNodeKind.String:
                    // A string is taken for any type, so that a value that does not follow its type is kept, and the conformance check
                    // reports it.  A RECUR value as a string is kept in the text form.
                    return valueType == IcalValueType.Recur ? value.Text : ToText(valueType, value.Text, value, propertyName);
                case JcalNodeKind.Number when valueType == IcalValueType.Integer || valueType == IcalValueType.Float:
                    return ToText(valueType, value.Text, value, propertyName);
                case JcalNodeKind.True when valueType == IcalValueType.Boolean:
                case JcalNodeKind.False when valueType == IcalValueType.Boolean:
                    return FormatBoolean(value.Kind == JcalNodeKind.True, IcalValueFormat.Text);
                default:
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "A value of the {0} property with the type {1} cannot be {2}.", propertyName,
                            GetTypeIdentifier(valueType), DescribeKind(value.Kind)));
            }
        }

        private static string ReadPeriod(JcalNode period, string propertyName)
        {
            // ref. RFC 7265§3.6.9: an array of two strings, the start and the end or duration.
            if (period.Items.Count != 2 || period.Items.Any(item => item.Kind != JcalNodeKind.String))
                throw BuildException(period, propertyName,
                    "A period must be an array of two strings: the start, and the end or the duration.");

            return ToPeriodText(period.Items[0].Text, period.Items[1].Text);
        }

        private static string ToPeriodText(string start, string end)
        {
            // A part that is not valid is kept as it is.
            IcalDateTime dateTime;
            var startText = IcalDateTime.TryParse(start.AsSpan(), IcalValueFormat.Json, out dateTime) ? dateTime.ToString(IcalValueFormat.Text) : start;
            string endText;
            if (IcalDuration.TryParse(end.AsSpan(), IcalValueFormat.Json, out _))
            {
                // The duration is the same in both forms, so its text is kept.
                endText = end;
            }
            else
            {
                endText = IcalDateTime.TryParse(end.AsSpan(), IcalValueFormat.Json, out dateTime) ? dateTime.ToString(IcalValueFormat.Text) : end;
            }

            return startText + "/" + endText;
        }

        private static string ReadRecur(JcalNode recur, string propertyName)
        {
            // ref. RFC 7265§3.6.10: an object with a member for each rule part, whose value is a string, a number, or an array of them for a
            // BYxxx rule part.  The parts are written in the order of the members.
            if (recur.Items.Count == 0)
                throw BuildException(recur, propertyName, "A recurrence rule must be an object with a member for each rule part.");

            var parts = new List<string>();
            var builder = new StringBuilder();
            for (var index = 0; index < recur.Items.Count; index++)
            {
                var value = recur.Items[index];
                var name = ToIcalName(recur.Names[index], value, propertyName, "rule part");
                if (parts.Contains(name))
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The {0} rule part appears more than once in a recurrence rule.", name));

                parts.Add(name);
                if (index != 0)
                {
                    builder.Append(';');
                }

                builder.Append(name).Append('=');
                if (value.Kind == JcalNodeKind.Array)
                {
                    if (value.Items.Count == 0)
                        throw BuildException(value, propertyName,
                            string.Format(CultureInfo.InvariantCulture, "The {0} rule part cannot be an empty array.", name));

                    for (var itemIndex = 0; itemIndex < value.Items.Count; itemIndex++)
                    {
                        if (itemIndex != 0)
                        {
                            builder.Append(',');
                        }

                        builder.Append(ReadRulePartValue(value.Items[itemIndex], name, propertyName));
                    }
                }
                else
                {
                    builder.Append(ReadRulePartValue(value, name, propertyName));
                }
            }

            return builder.ToString();
        }

        private static string ReadRulePartValue(JcalNode value, string partName, string propertyName)
        {
            switch (value.Kind)
            {
                case JcalNodeKind.String:
                    if (partName != UntilPartName) return value.Text;

                    // ref. RFC 7265§3.6.10: UNTIL is a date or date-time in the jCal form.
                    var valueType = value.Text.Length == IcalDate.XmlLength ? IcalValueType.Date : IcalValueType.DateTime;
                    return ToText(valueType, value.Text, value, propertyName);
                case JcalNodeKind.Number:
                    int number;
                    return IcalValue.TryParseInteger(value.Text.AsSpan(), IcalValueFormat.Json, out number) ? IcalPropertyValue.FormatInteger(number) : value.Text;
                default:
                    throw BuildException(value, propertyName,
                        string.Format(CultureInfo.InvariantCulture, "The value of the {0} rule part must be a string or a number, not {1}.", partName,
                            DescribeKind(value.Kind)));
            }
        }

        private static string ReadGeo(JcalNode geo, string propertyName)
        {
            // ref. RFC 7265§3.4.1.1: an array of two FLOAT values, the latitude and the longitude.
            if (geo.Items.Count != 2 || geo.Items.Any(item => item.Kind != JcalNodeKind.Number && item.Kind != JcalNodeKind.String))
                throw BuildException(geo, propertyName, "The value of the GEO property must be an array of two numbers: the latitude and the longitude.");

            return ToText(IcalValueType.Float, geo.Items[0].Text, geo.Items[0], propertyName) + ";" +
                   ToText(IcalValueType.Float, geo.Items[1].Text, geo.Items[1], propertyName);
        }

        private static string ReadRequestStatus(JcalNode status, string propertyName)
        {
            // ref. RFC 7265§3.4.1.2: an array of two or three strings, the code, the description and any additional data.
            if (status.Items.Count < 2 || status.Items.Count > 3 || status.Items.Any(item => item.Kind != JcalNodeKind.String))
                throw BuildException(status, propertyName,
                    "The value of the REQUEST-STATUS property must be an array of two or three strings: the code, the description and any data.");

            return string.Join(";", status.Items.Select(item => ToText(IcalValueType.Text, item.Text, item, propertyName)));
        }

        /// <summary>
        /// Converts the text of a JSON string or number to the text form of a content line; or keeps it as it is if it is not valid.
        /// </summary>
        private static string ToText(IcalValueType valueType, string json, JcalNode node, string propertyName)
        {
            var value = json.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Text:
                    // ref. RFC 7265§4: iCalendar escaping MUST be applied.
                    try
                    {
                        return IcalValue.FormatText(json, IcalValueFormat.Text);
                    }
                    catch (ArgumentException)
                    {
                        throw BuildException(node, propertyName,
                            string.Format(CultureInfo.InvariantCulture, "The text of the {0} property holds a character that iCalendar cannot hold.",
                                propertyName));
                    }
                case IcalValueType.Boolean:
                    bool flag;
                    return IcalValue.TryParseBoolean(value, IcalValueFormat.Json, out flag) ? FormatBoolean(flag, IcalValueFormat.Text) : json;
                case IcalValueType.Integer:
                    int integer;
                    if (IcalValue.TryParseInteger(value, IcalValueFormat.Text, out integer)) return json;

                    return IcalValue.TryParseInteger(value, IcalValueFormat.Json, out integer) ? IcalPropertyValue.FormatInteger(integer) : json;
                case IcalValueType.Float:
                    double number;
                    if (IcalValue.TryParseFloat(value, IcalValueFormat.Text, out number)) return json;

                    return IcalValue.TryParseFloat(value, IcalValueFormat.Json, out number) ? IcalValue.FormatFloat(number, IcalValueFormat.Text) : json;
                case IcalValueType.Date:
                    IcalDate date;
                    return IcalDate.TryParse(value, IcalValueFormat.Json, out date) ? date.ToString(IcalValueFormat.Text) : json;
                case IcalValueType.DateTime:
                    IcalDateTime dateTime;
                    return IcalDateTime.TryParse(value, IcalValueFormat.Json, out dateTime) ? dateTime.ToString(IcalValueFormat.Text) : json;
                case IcalValueType.Time:
                    IcalTime time;
                    return IcalTime.TryParse(value, IcalValueFormat.Json, out time) ? time.ToString(IcalValueFormat.Text) : json;
                case IcalValueType.UtcOffset:
                    IcalUtcOffset offset;
                    return IcalUtcOffset.TryParse(value, IcalValueFormat.Json, out offset) ? offset.ToString(IcalValueFormat.Text) : json;
                case IcalValueType.Period:
                    // A period as one string, with its parts joined by a solidus, as some writers give it.
                    var solidusIndex = json.IndexOf('/');
                    return solidusIndex < 0 ? json : ToPeriodText(json.Substring(0, solidusIndex), json.Substring(solidusIndex + 1));
                default:
                    // DURATION, BINARY, URI and CAL-ADDRESS are the same in both forms, and an unknown value is kept as it is (RFC 7265§5.2).
                    return json;
            }
        }

        #endregion

        #region iCalendar to jCal

        /// <summary>
        /// Writes a content line as a property array.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="property">The content line.</param>
        /// <exception cref="IcalFileException">The name of the property or a parameter is not a valid iCalendar name, a parameter appears more
        /// than once, or a value holds a character that cannot be written.</exception>
        internal static void WriteProperty(Utf8JsonWriter writer, IcalContentLine property)
        {
            var name = property.Name;
            CheckName(name, property);

            // ref. RFC 7265§3.5.1: the type identifier gives the value type, so the VALUE parameter is not written.  ref. RFC 7265§5.1: a
            // property whose value type is not known has the unknown type.
            var valueParameter = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Value);
            IcalValueType valueType;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? extensionTypeName = null;
#else
            string extensionTypeName = null;
#endif
            if (valueParameter is null)
            {
                valueType = IcalValueTypeRegistry.GetDefaultValueType(name.AsSpan());
            }
            else if (!IcalValueTypeRegistry.TryParseValueTypeName(valueParameter.AsSpan(), out valueType) &&
                     IcalComponent.IsValidName(valueParameter) && !IcalValueFormatting.EqualsKeyword(valueParameter.AsSpan(), "UNKNOWN"))
            {
                extensionTypeName = valueParameter;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            List<JcalNode>? values = null;
#else
            List<JcalNode> values = null;
#endif
            if (valueType != IcalValueType.Unknown)
            {
                values = TryConvertValues(property, valueType, valueParameter is null);
            }

            // A value that cannot be converted is kept with the unknown type, with its VALUE parameter; and an extension value type is kept as
            // its type identifier.
            string typeName;
            var isValueParameterKept = false;
            if (values != null)
            {
                typeName = IcalValueTypeRegistry.GetValueTypeName(valueType).ToLowerInvariant();
            }
            else
            {
                values = new List<JcalNode> { new JcalNode(JcalNodeKind.String, property.Value) };
                if (extensionTypeName != null)
                {
                    typeName = extensionTypeName.ToLowerInvariant();
                }
                else
                {
                    typeName = UnknownTypeName;
                    isValueParameterKept = valueParameter != null;
                }
            }

            // The JSON writer would escape a lone surrogate, but a JSON reader cannot read it back as text, so it is refused.
            if (property.ParameterArray.Any(parameter => parameter.ValueArray.Any(HasLoneSurrogate)) || values.Any(HasLoneSurrogate))
                throw BuildWriteException(
                    string.Format(CultureInfo.InvariantCulture,
                        "The {0} property holds a lone surrogate, which cannot be written in jCal, since it is not valid Unicode text.",
                        name.ToUpperInvariant()), property);

            writer.WriteStartArray();
            writer.WriteStringValue(name.ToLowerInvariant());
            WriteParameters(writer, property, isValueParameterKept);
            writer.WriteStringValue(typeName);
            foreach (var value in values)
            {
                WriteNode(writer, value);
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Gets the name of a component, in lower case, as jCal writes it (RFC 7265 §3.3).
        /// </summary>
        /// <exception cref="IcalFileException">The name is not a valid iCalendar name.</exception>
        internal static string ToJcalComponentName(IcalComponent component)
        {
            if (!IcalComponent.IsValidName(component.Name))
                throw BuildWriteException(
                    string.Format(CultureInfo.InvariantCulture,
                        "The component name {0} cannot be written in jCal, since it is not one or more letters, digits and hyphens.", component.Name),
                    component.LineNumber, null);

            return component.Name.ToLowerInvariant();
        }

        private static void WriteParameters(Utf8JsonWriter writer, IcalContentLine property, bool isValueParameterKept)
        {
            // ref. RFC 7265§3.5: an object whose keys are the parameter names in lower case.  ref. §3.5.2: a parameter with more than one value
            // is an array of strings.
            var names = new List<string>();
            writer.WriteStartObject();
            foreach (var parameter in property.ParameterArray)
            {
                if (!isValueParameterKept && IsName(parameter.Name, IcalParameterNames.Value)) continue;

                CheckName(parameter.Name, property);
                var name = parameter.Name.ToLowerInvariant();
                if (names.Contains(name))
                    throw BuildWriteException(
                        string.Format(CultureInfo.InvariantCulture,
                            "The {0} parameter appears more than once in the {1} property, which jCal cannot hold, since it is a key of an object.",
                            parameter.Name.ToUpperInvariant(), property.Name.ToUpperInvariant()), property);

                names.Add(name);
                writer.WritePropertyName(name);
                if (parameter.ValueArray.Length == 1)
                {
                    writer.WriteStringValue(parameter.ValueArray[0]);
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var value in parameter.ValueArray)
                    {
                        writer.WriteStringValue(value);
                    }

                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        private static void WriteNode(Utf8JsonWriter writer, JcalNode node)
        {
            switch (node.Kind)
            {
                case JcalNodeKind.String:
                    writer.WriteStringValue(node.Text);
                    break;
                case JcalNodeKind.Number:
                    // The text has been checked to be a JSON number, so that its digits are kept.
                    writer.WriteRawValue(node.Text, true);
                    break;
                case JcalNodeKind.True:
                case JcalNodeKind.False:
                    writer.WriteBooleanValue(node.Kind == JcalNodeKind.True);
                    break;
                case JcalNodeKind.Null:
                    writer.WriteNullValue();
                    break;
                case JcalNodeKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in node.Items)
                    {
                        WriteNode(writer, item);
                    }

                    writer.WriteEndArray();
                    break;
                default:
                    writer.WriteStartObject();
                    for (var index = 0; index < node.Items.Count; index++)
                    {
                        writer.WritePropertyName(node.Names[index]);
                        WriteNode(writer, node.Items[index]);
                    }

                    writer.WriteEndObject();
                    break;
            }
        }

        /// <summary>
        /// Converts the value of a property into JSON values.
        /// </summary>
        /// <returns>The values; or <c>null</c> if the value is not a valid value of its type.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<JcalNode>? TryConvertValues(IcalContentLine property, IcalValueType valueType, bool isDefaultValueType)
#else
        private static List<JcalNode> TryConvertValues(IcalContentLine property, IcalValueType valueType, bool isDefaultValueType)
#endif
        {
            var name = property.Name;
            var value = property.Value;
            if (isDefaultValueType && IsName(name, IcalPropertyNames.Geo)) return ToList(TryConvertGeo(value));
            if (isDefaultValueType && IsName(name, IcalPropertyNames.RequestStatus)) return ToList(TryConvertRequestStatus(value));

            // ref. RFC 7265§3.4: each value of a multi-valued property is an element of the array.  The values of a property whose type is
            // not known may be a list too, so they are split as well.
            // A RECUR value has commas of its own, and is never a list.
            var items = new List<string>();
            if (valueType == IcalValueType.Recur || !IsListProperty(name) && IcalValueTypeRegistry.IsKnownProperty(name.AsSpan()))
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

            var nodes = new List<JcalNode>();
            foreach (var item in items)
            {
                var node = TryConvertValue(valueType, item);
                if (node is null) return null;

                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// Converts a value from the text form of a content line to a JSON value.
        /// </summary>
        /// <returns>The value; or <c>null</c> if the value is not valid.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertValue(IcalValueType valueType, string text)
#else
        private static JcalNode TryConvertValue(IcalValueType valueType, string text)
#endif
        {
            var value = text.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Text:
                    // ref. RFC 7265§3.1: iCalendar escaping MUST be unescaped.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? unescaped;
#else
                    string unescaped;
#endif
                    return IcalValue.TryParseText(value, IcalValueFormat.Text, out unescaped) ? new JcalNode(JcalNodeKind.String, unescaped) : null;
                case IcalValueType.Boolean:
                    bool flag;
                    if (!IcalValue.TryParseBoolean(value, IcalValueFormat.Text, out flag)) return null;

                    return new JcalNode(flag ? JcalNodeKind.True : JcalNodeKind.False, string.Empty);
                case IcalValueType.Integer:
                    int integer;
                    return IcalValue.TryParseInteger(value, IcalValueFormat.Text, out integer)
                        ? new JcalNode(JcalNodeKind.Number, IcalPropertyValue.FormatInteger(integer))
                        : null;
                case IcalValueType.Float:
                    return TryConvertFloat(text);
                case IcalValueType.Binary:
                    return IcalValue.TryParseBinary(value, IcalValueFormat.Text, out _) ? new JcalNode(JcalNodeKind.String, text) : null;
                case IcalValueType.Uri:
                case IcalValueType.CalAddress:
                    return IcalValue.TryParseUri(value, IcalValueFormat.Text, out _) ? new JcalNode(JcalNodeKind.String, text) : null;
                case IcalValueType.Duration:
                    return IcalDuration.TryParse(value, IcalValueFormat.Text, out _) ? new JcalNode(JcalNodeKind.String, text) : null;
                case IcalValueType.Period:
                    return TryConvertPeriod(text);
                case IcalValueType.Recur:
                    return TryConvertRecur(text);
                default:
                    var json = TryToJson(valueType, text);
                    return json is null ? null : new JcalNode(JcalNodeKind.String, json);
            }
        }

        /// <summary>
        /// Converts a DATE, DATE-TIME, TIME or UTC-OFFSET value from the text form to the jCal form.
        /// </summary>
        /// <returns>The text; or <c>null</c> if the value is not valid.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? TryToJson(IcalValueType valueType, string text)
#else
        private static string TryToJson(IcalValueType valueType, string text)
#endif
        {
            var value = text.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Date:
                    IcalDate date;
                    return IcalDate.TryParse(value, IcalValueFormat.Text, out date) ? date.ToString(IcalValueFormat.Json) : null;
                case IcalValueType.DateTime:
                    IcalDateTime dateTime;
                    return IcalDateTime.TryParse(value, IcalValueFormat.Text, out dateTime) ? dateTime.ToString(IcalValueFormat.Json) : null;
                case IcalValueType.Time:
                    IcalTime time;
                    return IcalTime.TryParse(value, IcalValueFormat.Text, out time) ? time.ToString(IcalValueFormat.Json) : null;
                case IcalValueType.UtcOffset:
                    IcalUtcOffset offset;
                    return IcalUtcOffset.TryParse(value, IcalValueFormat.Text, out offset) ? offset.ToString(IcalValueFormat.Json) : null;
                default:
                    return null;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertFloat(string text)
#else
        private static JcalNode TryConvertFloat(string text)
#endif
        {
            // ref. RFC 7265§3.1: a number may need another form in JSON, which has no plus sign or leading zeros.  The text is kept where JSON
            // allows it, so that its digits are not changed.
            double number;
            if (!IcalValue.TryParseFloat(text.AsSpan(), IcalValueFormat.Text, out number)) return null;

            return new JcalNode(JcalNodeKind.Number,
                IcalValueFormatting.IsJsonNumber(text.AsSpan()) ? text : IcalValue.FormatFloat(number, IcalValueFormat.Json));
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertPeriod(string text)
#else
        private static JcalNode TryConvertPeriod(string text)
#endif
        {
            // ref. RFC 7265§3.6.9: an array of two strings, the start and the end or duration.
            IcalPeriod period;
            if (!IcalPeriod.TryParse(text.AsSpan(), IcalValueFormat.Text, out period)) return null;

            var node = new JcalNode(JcalNodeKind.Array, string.Empty);
            node.Add(new JcalNode(JcalNodeKind.String, period.Start.ToString(IcalValueFormat.Json)));

            // The duration is the same in both forms, so its text is kept.
            node.Add(new JcalNode(JcalNodeKind.String,
                period.Duration.HasValue ? text.Substring(text.IndexOf('/') + 1) : period.End.GetValueOrDefault().ToString(IcalValueFormat.Json)));
            return node;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertRecur(string text)
#else
        private static JcalNode TryConvertRecur(string text)
#endif
        {
            // ref. RFC 7265§3.6.10: an object with a member for each rule part, keeping their order.  COUNT and INTERVAL are numbers, and a
            // BYxxx rule part is a number or string, or an array of them if it has more than one value.
            if (!IcalRecurrenceRule.TryParse(text.AsSpan(), IcalValueFormat.Text, out _)) return null;

            var node = new JcalNode(JcalNodeKind.Object, string.Empty);
            foreach (var part in text.Split(';'))
            {
                var equalsIndex = part.IndexOf('=');
                var partName = part.Substring(0, equalsIndex).ToUpperInvariant();
                var partValue = part.Substring(equalsIndex + 1);
                var key = partName.ToLowerInvariant();
                if (partName == UntilPartName)
                {
                    var until = TryToJson(partValue.Length == IcalDate.TextLength ? IcalValueType.Date : IcalValueType.DateTime, partValue);
                    if (until is null) return null;

                    node.Add(key, new JcalNode(JcalNodeKind.String, until));
                }
                else if (NumberPartNames.Contains(partName))
                {
                    node.Add(key, ToRulePartNode(partValue, true));
                }
                else if (partName.StartsWith("BY", StringComparison.Ordinal))
                {
                    var items = partValue.Split(',');
                    var isNumber = !StringListPartNames.Contains(partName);
                    if (items.Length == 1)
                    {
                        node.Add(key, ToRulePartNode(items[0], isNumber));
                    }
                    else
                    {
                        var array = node.Add(key, new JcalNode(JcalNodeKind.Array, string.Empty));
                        foreach (var item in items)
                        {
                            array.Add(ToRulePartNode(item, isNumber));
                        }
                    }
                }
                else
                {
                    node.Add(key, new JcalNode(JcalNodeKind.String, partValue));
                }
            }

            return node;
        }

        private static JcalNode ToRulePartNode(string value, bool isNumber)
        {
            // A value that is not an integer, such as a leap month of RFC 7529, is kept as a string.
            int number;
            return isNumber && IcalValue.TryParseInteger(value.AsSpan(), IcalValueFormat.Text, out number)
                ? new JcalNode(JcalNodeKind.Number, IcalPropertyValue.FormatInteger(number))
                : new JcalNode(JcalNodeKind.String, value);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertGeo(string value)
#else
        private static JcalNode TryConvertGeo(string value)
#endif
        {
            // ref. RFC 7265§3.4.1.1: an array of the latitude and the longitude, each a FLOAT.
            var parts = value.Split(';');
            if (parts.Length != 2) return null;

            var latitude = TryConvertFloat(parts[0]);
            var longitude = TryConvertFloat(parts[1]);
            if (latitude is null || longitude is null) return null;

            var node = new JcalNode(JcalNodeKind.Array, string.Empty);
            node.Add(latitude);
            node.Add(longitude);
            return node;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcalNode? TryConvertRequestStatus(string value)
#else
        private static JcalNode TryConvertRequestStatus(string value)
#endif
        {
            // ref. RFC 5545§3.8.8.3: rstatus = statcode ";" statdesc [";" extdata].  ref. RFC 7265§3.4.1.2: an array of two or three strings;
            // the data SHOULD NOT be present if there is none.
            var parts = SplitOnUnescaped(value, ';', 3);
            if (parts.Count < 2) return null;

            var node = new JcalNode(JcalNodeKind.Array, string.Empty);
            foreach (var part in parts)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                string? text;
#else
                string text;
#endif
                if (!IcalValue.TryParseText(part.AsSpan(), IcalValueFormat.Text, out text)) return null;

                node.Add(new JcalNode(JcalNodeKind.String, text));
            }

            return node;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<JcalNode>? ToList(JcalNode? node)
#else
        private static List<JcalNode> ToList(JcalNode node)
#endif
        {
            return node is null ? null : new List<JcalNode> { node };
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

        private static bool HasLoneSurrogate(JcalNode node)
        {
            return HasLoneSurrogate(node.Text) || node.Names.Any(HasLoneSurrogate) || node.Items.Any(HasLoneSurrogate);
        }

        private static bool HasLoneSurrogate(string text)
        {
            for (var index = 0; index < text.Length; index++)
            {
                if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    index++;
                }
                else if (char.IsSurrogate(text[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckName(string name, IcalContentLine property)
        {
            if (!IcalComponent.IsValidName(name))
                throw BuildWriteException(
                    string.Format(CultureInfo.InvariantCulture,
                        "The name {0} cannot be written in jCal, since it is not one or more letters, digits and hyphens.", name), property);
        }

        #endregion

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

        private static string GetTypeIdentifier(IcalValueType valueType)
        {
            return valueType == IcalValueType.Unknown ? UnknownTypeName : IcalValueTypeRegistry.GetValueTypeName(valueType).ToLowerInvariant();
        }

        /// <summary>
        /// Describes the kind of a JSON value, for a message.
        /// </summary>
        internal static string DescribeKind(JcalNodeKind kind)
        {
            switch (kind)
            {
                case JcalNodeKind.String:
                    return "a string";
                case JcalNodeKind.Number:
                    return "a number";
                case JcalNodeKind.True:
                case JcalNodeKind.False:
                    return "a Boolean";
                case JcalNodeKind.Null:
                    return "null";
                case JcalNodeKind.Array:
                    return "an array";
                default:
                    return "an object";
            }
        }

        private static bool IsName(string name, string expected)
        {
            return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds the exception for a problem in a jCal document, at a JSON value.
        /// </summary>
        /// <param name="node">The value at which the problem was found.</param>
        /// <param name="propertyName">The name of the property; or <c>null</c>.</param>
        /// <param name="message">The message.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static IcalFileException BuildException(JcalNode node, string? propertyName, string message)
#else
        internal static IcalFileException BuildException(JcalNode node, string propertyName, string message)
#endif
        {
            var exception = new IcalFileException(message);
            AddLocation(exception, node);
            if (propertyName != null)
            {
                exception.Data[IcalFileException.DataItemName.PropertyName] = propertyName;
            }

            return exception;
        }

        /// <summary>
        /// Adds the JSON path and byte position of a value to the data of an exception.
        /// </summary>
        internal static void AddLocation(IcalFileException exception, JcalNode node)
        {
            exception.Data[IcalFileException.DataItemName.JsonPath] = node.GetPath();
            exception.Data[IcalFileException.DataItemName.BytePosition] = node.BytePosition;
        }

        private static IcalFileException BuildWriteException(string message, IcalContentLine property)
        {
            return BuildWriteException(message, property.LineNumber, property.Name);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static IcalFileException BuildWriteException(string message, long lineNumber, string? propertyName)
#else
        private static IcalFileException BuildWriteException(string message, long lineNumber, string propertyName)
#endif
        {
            var exception = new IcalFileException(message);
            if (lineNumber != 0)
            {
                exception.Data[IcalFileException.DataItemName.LineNumber] = lineNumber;
            }

            if (propertyName != null)
            {
                exception.Data[IcalFileException.DataItemName.PropertyName] = propertyName;
            }

            return exception;
        }
    }
}
#endif
