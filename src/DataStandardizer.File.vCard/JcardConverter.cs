#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Converts properties between the content lines of the card object model and the property arrays of jCard (RFC 7095).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A content line holds its value in the text form of RFC 6350, so converting from jCard turns each JSON value into that form, and converting to jCard turns the text back into
    /// JSON values.  The text of a value is kept where both forms allow it, such as the digits of a FLOAT, so that a value that goes to jCard and back is unchanged.
    /// </para>
    /// <para>
    /// A value that doesn't follow its type, which <see cref="VcardOptions.AllowInvalidValues"/> lets a card hold, is kept as it is.  From jCard, a string is taken for a value of any
    /// type, and its text is kept if it can't be converted, so that the conformance check reports it.  To jCard, a value that can't be converted is written as a string with its own
    /// type identifier, which reads back the same way; TEXT, URI and structured values, whose escaping would change, are written with the <c>unknown</c> type instead.
    /// </para>
    /// </remarks>
    internal static class JcardConverter
    {
        /// <summary>
        /// The type identifier of a value whose type is not known (RFC 7095 §5).
        /// </summary>
        internal const string UnknownTypeName = "unknown";

        /// <summary>
        /// The first element of a jCard object (RFC 7095 §3.2).
        /// </summary>
        internal const string CardName = "vcard";

        /// <summary>
        /// The parameter that holds the group of a property in jCard (RFC 7095 §3.3.1.2).
        /// </summary>
        private const string GroupParameterName = "group";

        /// <summary>
        /// The key of the <c>VALUE</c> parameter, which "MUST NOT be added to the parameters object" (RFC 7095 §3.4.1).
        /// </summary>
        private const string ValueParameterName = "value";

        /// <summary>
        /// The properties of RFC 6350 whose value is a list of TEXT values, each of which is an element of the property array (RFC 7095 §3.3).
        /// </summary>
        private static readonly string[] ListProperties = { VcardPropertyNames.Nickname, VcardPropertyNames.Categories };

        /// <summary>
        /// The parameters whose value is a list (RFC 6350 §5.5, §5.6 and §5.9), which jCard writes as an array of the separated values (RFC 7095 §3.4.2).
        /// </summary>
        private static readonly string[] ListParameters = { VcardParameterNames.Type, VcardParameterNames.Pid, VcardParameterNames.SortAs };

        #region jCard to vCard

        /// <summary>
        /// Converts a property array into a content line.
        /// </summary>
        /// <param name="property">The property array: a name, a parameters object, a type identifier and one or more values.</param>
        /// <param name="cardIndex">The 0-based index of the card the property is in.</param>
        /// <exception cref="VcardFileException">The array is not a valid jCard property.</exception>
        internal static VcardContentLine ToContentLine(JcardNode property, int cardIndex)
        {
            var items = property.Items;
            if (items.Count == 0 || items[0].Kind != JcardNodeKind.String)
                throw BuildException(property, null, "A property must be an array whose first element is the name of the property, as a string.");

            var name = ToVcardName(items[0], null, "property");

            // ref. RFC 7095 §3.2: BEGIN and END "MUST NOT appear as properties of the jCard".
            if (IsName(name, VcardPropertyNames.Begin) || IsName(name, VcardPropertyNames.End))
                throw BuildException(items[0], name,
                    Format("The {0} property can't be in a jCard object, which takes the place of BEGIN and END: RFC 7095 §3.2.", name));

            if (items.Count < 4)
                throw BuildException(property, name, Format("The {0} property must be an array of its name, a parameters object, a type identifier and at least one value.", name));

            if (items[1].Kind != JcardNodeKind.Object)
                throw BuildException(items[1], name, Format("The second element of the {0} property must be an object holding its parameters.", name));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? group;
#else
            string group;
#endif
            var parameters = ReadParameters(items[1], name, out group);
            if (items[2].Kind != JcardNodeKind.String)
                throw BuildException(items[2], name, Format("The third element of the {0} property must be the type identifier, as a string.", name));

            // ref. RFC 7095 §3.3: the type identifier is in lower case.  ref. §5.2: an unknown value is taken over without processing, and has no VALUE parameter.  A type
            // identifier that is not one of RFC 6350's is an extension value type, whose value is kept as it is too.
            var typeName = items[2].Text;
            CheckJcardName(typeName, items[2], name, "type identifier");
            VcardValueType valueType;
            var isExtensionType = !VcardValueTypeRegistry.TryParseValueTypeName(typeName.AsSpan(), out valueType) && typeName != UnknownTypeName;

            var values = new List<JcardNode>();
            for (var index = 3; index < items.Count; index++)
            {
                values.Add(items[index]);
            }

            var value = ReadValues(values, name, valueType, isExtensionType);

            // ref. RFC 7095 §3.4.1 and §4: a VALUE parameter is added if the type is not the property's default, and never for an unknown value.
            if (isExtensionType)
            {
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, typeName));
            }
            else if (valueType != VcardValueType.Unknown && valueType != VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan()))
            {
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, VcardValueTypeRegistry.GetValueTypeName(valueType)));
            }

            return VcardContentLine.FromConversion(group, name, parameters.ToArray(), value, cardIndex);
        }

        /// <summary>
        /// Gets the vCard name for the name of a property: the name in upper case (RFC 7095 §4).
        /// </summary>
        /// <exception cref="VcardFileException">The name is not a name of jCard: one or more lower-case letters, digits and hyphens.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string ToVcardName(JcardNode node, string? propertyName, string kind)
#else
        internal static string ToVcardName(JcardNode node, string propertyName, string kind)
#endif
        {
            CheckJcardName(node.Text, node, propertyName, kind);
            return node.Text.ToUpperInvariant();
        }

        private static List<VcardContentLineParameter> ReadParameters(JcardNode parametersNode, string propertyName,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            out string? group)
#else
            out string group)
#endif
        {
            group = null;
            var parameters = new List<VcardContentLineParameter>();
            var names = new List<string>();
            var items = parametersNode.Items;
            for (var index = 0; index < items.Count; index++)
            {
                var valueNode = items[index];
                var name = parametersNode.Names[index];
                CheckJcardName(name, valueNode, propertyName, "parameter");
                if (names.Contains(name))
                    throw BuildException(valueNode, propertyName, Format("The {0} parameter appears more than once in the {1} property.", name.ToUpperInvariant(), propertyName));

                names.Add(name);

                // ref. RFC 7095 §3.4.1: the VALUE parameter "MUST NOT be added to the parameters object".
                if (name == ValueParameterName)
                    throw BuildException(valueNode, propertyName,
                        Format("The {0} property has a value parameter, which RFC 7095 §3.4.1 doesn't allow: the type identifier gives the value type.", propertyName));

                if (name == GroupParameterName)
                {
                    group = ReadGroup(valueNode, propertyName);
                    continue;
                }

                // ref. RFC 7095 §3.4: a parameter value is a string, or an array of strings for a multi-valued parameter (§3.4.2).  "the original case of the parameter value MUST
                // be preserved".
                string[] values;
                if (valueNode.Kind == JcardNodeKind.String)
                {
                    values = new[] { valueNode.Text };
                }
                else if (valueNode.Kind == JcardNodeKind.Array && valueNode.Items.Count != 0 && AreAllStrings(valueNode.Items))
                {
                    values = new string[valueNode.Items.Count];
                    for (var itemIndex = 0; itemIndex < values.Length; itemIndex++)
                    {
                        values[itemIndex] = valueNode.Items[itemIndex].Text;
                    }
                }
                else
                {
                    throw BuildException(valueNode, propertyName, Format("The value of the {0} parameter must be a string, or a non-empty array of strings.", name.ToUpperInvariant()));
                }

                parameters.Add(new VcardContentLineParameter(name.ToUpperInvariant(), values));
            }

            return parameters;
        }

        private static string ReadGroup(JcardNode valueNode, string propertyName)
        {
            // ref. RFC 7095 §3.3.1.2.1: "the parameter's value is a single opaque string".  ref. §7.1: it "is restricted to characters allowed in property names, namely ALPHA,
            // DIGIT, and "-" characters", and "MUST NOT be empty".  Its case "SHOULD be converted into uppercase".
            if (valueNode.Kind != JcardNodeKind.String || !IsVcardName(valueNode.Text))
                throw BuildException(valueNode, propertyName,
                    Format("The group parameter of the {0} property must be a string of one or more letters, digits and hyphens: RFC 7095 §7.1.", propertyName));

            return valueNode.Text.ToUpperInvariant();
        }

        /// <summary>
        /// Converts the values of a property into the text of a content line.
        /// </summary>
        private static string ReadValues(List<JcardNode> values, string propertyName, VcardValueType valueType, bool isExtensionType)
        {
            // ref. RFC 7095 §3.3: only a multi-valued property has more than one value.  The values of a property whose type isn't known may be a list too, as long as their type is.
            // ref. §5.2: an unknown value "MUST be taken over in vCard without processing", so it is one string.
            var isUnknownType = valueType == VcardValueType.Unknown && !isExtensionType;
            if (values.Count > 1 && (isUnknownType || (VcardValueTypeRegistry.IsKnownProperty(propertyName.AsSpan()) && !IsListProperty(propertyName))))
                throw BuildException(values[1], propertyName, Format("The {0} property can have only one value.", propertyName));

            var builder = new StringBuilder();
            for (var index = 0; index < values.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                builder.Append(ReadValue(values[index], propertyName, valueType, isExtensionType));
            }

            return builder.ToString();
        }

        private static string ReadValue(JcardNode value, string propertyName, VcardValueType valueType, bool isExtensionType)
        {
            var isUnknownType = valueType == VcardValueType.Unknown && !isExtensionType;
            switch (value.Kind)
            {
                case JcardNodeKind.String:
                    if (isUnknownType || isExtensionType) return value.Text;

                    // A string is taken for any type, so that a value that doesn't follow its type is kept, and the conformance check reports it.
                    return ToText(valueType, value, propertyName, IsStructuredProperty(propertyName, valueType));
                case JcardNodeKind.Number when isExtensionType || valueType == VcardValueType.Integer || valueType == VcardValueType.Float:
                    return isExtensionType ? value.Text : ToText(valueType, value, propertyName, false);
                case JcardNodeKind.True when isExtensionType || valueType == VcardValueType.Boolean:
                case JcardNodeKind.False when isExtensionType || valueType == VcardValueType.Boolean:
                    return isExtensionType ? DescribeLiteral(value.Kind) : FormatBoolean(value.Kind == JcardNodeKind.True);
                case JcardNodeKind.Array when !isUnknownType && !isExtensionType:
                    return ReadStructuredValue(value, propertyName, valueType);
                default:
                    throw BuildException(value, propertyName,
                        Format("A value of the {0} property with the type {1} cannot be {2}.", propertyName, GetTypeIdentifier(valueType, isExtensionType), DescribeKind(value.Kind)));
            }
        }

        private static string ReadStructuredValue(JcardNode structure, string propertyName, VcardValueType valueType)
        {
            // ref. RFC 7095 §3.3.1.3: an array with an element for each component, and an array for a component that has more than one value.  "the array element values MUST
            // have the primitive type that matches the jCard type identifier".
            var builder = new StringBuilder();
            for (var index = 0; index < structure.Items.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(';');
                }

                var component = structure.Items[index];
                if (component.Kind != JcardNodeKind.Array)
                {
                    builder.Append(ReadComponentItem(component, propertyName, valueType));
                    continue;
                }

                for (var itemIndex = 0; itemIndex < component.Items.Count; itemIndex++)
                {
                    if (itemIndex != 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(ReadComponentItem(component.Items[itemIndex], propertyName, valueType));
                }
            }

            return builder.ToString();
        }

        private static string ReadComponentItem(JcardNode item, string propertyName, VcardValueType valueType)
        {
            switch (item.Kind)
            {
                case JcardNodeKind.String:
                    return ToText(valueType, item, propertyName, true);
                case JcardNodeKind.Number when valueType == VcardValueType.Integer || valueType == VcardValueType.Float:
                    return ToText(valueType, item, propertyName, true);
                case JcardNodeKind.True when valueType == VcardValueType.Boolean:
                case JcardNodeKind.False when valueType == VcardValueType.Boolean:
                    return FormatBoolean(item.Kind == JcardNodeKind.True);
                default:
                    throw BuildException(item, propertyName,
                        Format("A component of the {0} property with the type {1} cannot be {2}.", propertyName, GetTypeIdentifier(valueType, false), DescribeKind(item.Kind)));
            }
        }

        /// <summary>
        /// Converts the text of a JSON string or number to the text form of a content line; or keeps it as it is if it is not valid.
        /// </summary>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="node">The string or number.</param>
        /// <param name="propertyName">The name of the property, for a message.</param>
        /// <param name="isComponent"><c>true</c> if the text is a component of a structured value, where a semicolon has to be escaped.</param>
        private static string ToText(VcardValueType valueType, JcardNode node, string propertyName, bool isComponent)
        {
            var json = node.Text;
            var value = json.AsSpan();
            switch (valueType)
            {
                case VcardValueType.Text:
                    // ref. RFC 7095 §3.1: "vCard escaping MUST be applied".
                    if (!VcardText.IsValidUnescapedText(value))
                        throw BuildException(node, propertyName, Format("The text of the {0} property holds a character that a vCard value cannot hold.", propertyName));

                    var buffer = new char[VcardText.GetMaxEscapedLength(json.Length)];
                    var writer = new VcardSpanWriter(buffer);
                    VcardText.AppendEscaped(ref writer, value, VcardVersion.V4_0, isComponent);
                    int charsWritten;
                    return VcardValueFormatting.ToString(buffer, writer.TryComplete(out charsWritten), charsWritten);
                case VcardValueType.Uri:
                    // ref. RFC 6350 §3.4 and its errata 3845 and 3846: a comma in a URI value "MUST be escaped".
                    return VcardValue.EscapeUri(json);
                case VcardValueType.Boolean:
                    bool flag;
                    return VcardValue.TryParseBoolean(value, VcardValueFormat.Json, out flag) ? FormatBoolean(flag) : json;
                case VcardValueType.Integer:
                    // ref. RFC 7095 §3.5.9: "any decimals and exponents MUST be eliminated".  The digits are kept where vCard allows them.
                    long integer;
                    if (VcardValue.TryParseInteger(value, VcardValueFormat.Text, out integer)) return json;

                    return VcardValue.TryParseJsonInteger(value, out integer) ? integer.ToString(CultureInfo.InvariantCulture) : json;
                case VcardValueType.Float:
                    // ref. RFC 7095 §3.5.10: "any exponents MUST be eliminated".  The digits are kept where vCard allows them.
                    double number;
                    if (VcardValue.TryParseFloat(value, VcardValueFormat.Text, out number)) return json;

                    return VcardValue.TryParseJsonFloat(value, out number) ? VcardValue.FormatFloatText(number) : json;
                case VcardValueType.Date:
                    VcardDate date;
                    return VcardDate.TryParse(value, VcardValueFormat.Json, out date) ? date.ToString(VcardValueFormat.Text) : json;
                case VcardValueType.Time:
                    VcardTime time;
                    return VcardTime.TryParse(value, VcardValueFormat.Json, out time) ? time.ToString(VcardValueFormat.Text) : json;
                case VcardValueType.DateTime:
                    VcardDateTime dateTime;
                    return VcardDateTime.TryParse(value, VcardValueFormat.Json, out dateTime) ? dateTime.ToString(VcardValueFormat.Text) : json;
                case VcardValueType.DateAndOrTime:
                    VcardDateAndOrTime dateAndOrTime;
                    return VcardDateAndOrTime.TryParse(value, VcardValueFormat.Json, out dateAndOrTime) ? dateAndOrTime.ToString(VcardValueFormat.Text) : json;
                case VcardValueType.Timestamp:
                    VcardTimestamp timestamp;
                    return VcardTimestamp.TryParse(value, VcardValueFormat.Json, out timestamp) ? timestamp.ToString(VcardValueFormat.Text) : json;
                case VcardValueType.UtcOffset:
                    VcardUtcOffset offset;
                    return VcardUtcOffset.TryParse(value, VcardValueFormat.Json, out offset) ? offset.ToString(VcardValueFormat.Text) : json;
                default:
                    // A LANGUAGE-TAG is the same in both forms.
                    return json;
            }
        }

        #endregion

        #region vCard to jCard

        /// <summary>
        /// Writes a content line as a property array.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="property">The content line.</param>
        /// <exception cref="VcardFileException">The line can't be written in jCard: a name isn't valid, it is a BEGIN or END, a parameter appears more than once, it has a GROUP
        /// parameter, or a value holds a lone surrogate.</exception>
        internal static void WriteProperty(Utf8JsonWriter writer, VcardContentLine property)
        {
            var name = property.Name;
            if (!IsVcardName(name))
                throw BuildWriteException(Format("The name {0} cannot be written in jCard, since it is not one or more letters, digits and hyphens.", name), property);

            // ref. RFC 7095 §3.2: BEGIN and END "MUST NOT appear as properties of the jCard".
            if (IsName(name, VcardPropertyNames.Begin) || IsName(name, VcardPropertyNames.End))
                throw BuildWriteException(Format("A {0} property can't be written in jCard, whose jCard object takes its place: RFC 7095 §3.2.", name.ToUpperInvariant()), property);

            var group = property.Group;
            if (group != null && !IsVcardName(group))
                throw BuildWriteException(Format("The group {0} cannot be written in jCard, since it is not one or more letters, digits and hyphens: RFC 7095 §7.1.", group), property);

            // ref. RFC 7095 §3.4.1: the type is the VALUE parameter's, or the property's default, or unknown.  ref. §5.1: a value whose type isn't known is written as it is.
            var valueParameter = VcardPropertyParameterReader.GetValue(property, VcardParameterNames.Value);
            VcardValueType valueType;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? extensionTypeName = null;
#else
            string extensionTypeName = null;
#endif
            if (valueParameter is null)
            {
                valueType = VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan());
            }
            else if (!VcardValueTypeRegistry.TryParseValueTypeName(valueParameter.AsSpan(), out valueType) && IsVcardName(valueParameter) &&
                     !VcardValueFormatting.EqualsKeyword(valueParameter.AsSpan(), "UNKNOWN"))
            {
                extensionTypeName = valueParameter.ToLowerInvariant();
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            List<JcardNode>? values = null;
#else
            List<JcardNode> values = null;
#endif
            if (extensionTypeName is null && valueType != VcardValueType.Unknown)
            {
                values = TryConvertValues(property, valueType);
            }

            string typeName;
            if (values != null)
            {
                typeName = VcardValueTypeRegistry.GetValueTypeName(valueType);
            }
            else
            {
                values = new List<JcardNode> { new JcardNode(JcardNodeKind.String, property.Value) };
                typeName = extensionTypeName ?? UnknownTypeName;
            }

            // The JSON writer would escape a lone surrogate, but a JSON reader can't read it back as text, so it is refused.
            if (HasLoneSurrogate(group ?? string.Empty) || HasLoneSurrogateInParameters(property) || values.Exists(HasLoneSurrogate))
                throw BuildWriteException(
                    Format("The {0} property holds a lone surrogate, which can't be written in jCard, since it is not valid Unicode text.", name.ToUpperInvariant()), property);

            writer.WriteStartArray();
            writer.WriteStringValue(name.ToLowerInvariant());
            WriteParameters(writer, property);
            writer.WriteStringValue(typeName);
            foreach (var value in values)
            {
                WriteNode(writer, value);
            }

            writer.WriteEndArray();
        }

        private static void WriteParameters(Utf8JsonWriter writer, VcardContentLine property)
        {
            // ref. RFC 7095 §3.4: an object whose keys are the parameter names in lower case.  ref. §3.3.1.2.1: the group is the group parameter, in lower case.
            var names = new List<string>();
            writer.WriteStartObject();
            if (property.Group != null)
            {
                writer.WritePropertyName(GroupParameterName);
                writer.WriteStringValue(property.Group.ToLowerInvariant());
            }

            foreach (var parameter in property.ParameterArray)
            {
                // ref. RFC 7095 §3.4.1: the type identifier takes the place of the VALUE parameter.
                if (IsName(parameter.Name, VcardParameterNames.Value)) continue;

                if (!IsVcardName(parameter.Name))
                    throw BuildWriteException(
                        Format("The parameter name {0} cannot be written in jCard, since it is not one or more letters, digits and hyphens.", parameter.Name), property);

                var name = parameter.Name.ToLowerInvariant();

                // ref. RFC 7095 §7.1: GROUP "MUST NOT be used in plain vCard", and it would clash with the group of the property.
                if (name == GroupParameterName)
                    throw BuildWriteException(
                        Format("The {0} property has a GROUP parameter, which is reserved for jCard (RFC 7095 §7.1) and can't be written.", property.Name.ToUpperInvariant()), property);

                var isList = IsListParameter(name);
                if (names.Contains(name))
                {
                    // The values of a list parameter that is repeated, as in TYPE=work;TYPE=voice (RFC 6350 §6.4.1), were written with the first.
                    if (isList) continue;

                    throw BuildWriteException(
                        Format("The {0} parameter appears more than once in the {1} property, which jCard can't hold, since it is a key of an object.", parameter.Name.ToUpperInvariant(),
                            property.Name.ToUpperInvariant()), property);
                }

                names.Add(name);
                writer.WritePropertyName(name);

                // ref. RFC 7095 §3.4.2: "the value for such parameters MUST be represented in an array containing the separated values", and "Single-value parameters SHOULD be
                // represented using a single string value".
                IReadOnlyList<string> values = isList ? VcardPropertyParameterReader.GetListValues(property, parameter.Name) : (IReadOnlyList<string>)parameter.ValueArray;
                if (values.Count == 1)
                {
                    writer.WriteStringValue(values[0]);
                    continue;
                }

                writer.WriteStartArray();
                foreach (var value in values)
                {
                    writer.WriteStringValue(value);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        private static void WriteNode(Utf8JsonWriter writer, JcardNode node)
        {
            switch (node.Kind)
            {
                case JcardNodeKind.String:
                    writer.WriteStringValue(node.Text);
                    break;
                case JcardNodeKind.Number:
                    // The text has been checked to be a JSON number, so that its digits are kept.
                    writer.WriteRawValue(node.Text, true);
                    break;
                case JcardNodeKind.True:
                case JcardNodeKind.False:
                    writer.WriteBooleanValue(node.Kind == JcardNodeKind.True);
                    break;
                default:
                    writer.WriteStartArray();
                    foreach (var item in node.Items)
                    {
                        WriteNode(writer, item);
                    }

                    writer.WriteEndArray();
                    break;
            }
        }

        /// <summary>
        /// Converts the value of a property into JSON values.
        /// </summary>
        /// <returns>The values; or <c>null</c> if the value can't be converted without changing it.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<JcardNode>? TryConvertValues(VcardContentLine property, VcardValueType valueType)
#else
        private static List<JcardNode> TryConvertValues(VcardContentLine property, VcardValueType valueType)
#endif
        {
            var name = property.Name;
            var value = property.Value;
            if (IsStructuredProperty(name, valueType))
            {
                var structure = TryConvertStructuredValue(value, AllowsLists(name));
                return structure is null ? null : new List<JcardNode> { structure };
            }

            // ref. RFC 7095 §3.3: each value of a multi-valued property is an element of the array.  The values of a property whose type isn't known may be a list too, so they are
            // split as well.
            var items = new List<string>();
            if (IsListProperty(name) || !VcardValueTypeRegistry.IsKnownProperty(name.AsSpan()))
            {
                foreach (var item in VcardValue.EnumerateList(value.AsSpan(), VcardValueFormat.Text))
                {
                    items.Add(item.ToString());
                }
            }
            else
            {
                items.Add(value);
            }

            var nodes = new List<JcardNode>();
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
        /// <returns>The value; or <c>null</c> if it can't be converted without changing it.  A value of another type that isn't valid is a string of its text, which reads back the
        /// same.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcardNode? TryConvertValue(VcardValueType valueType, string text)
#else
        private static JcardNode TryConvertValue(VcardValueType valueType, string text)
#endif
        {
            var value = text.AsSpan();
            switch (valueType)
            {
                case VcardValueType.Text:
                    // ref. RFC 7095 §3.1: "any vCard escaping MUST be unescaped".
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? unescaped;
#else
                    string unescaped;
#endif
                    return VcardValue.TryParseText(value, VcardValueFormat.Text, out unescaped) ? new JcardNode(JcardNodeKind.String, unescaped) : null;
                case VcardValueType.Uri:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? uri;
#else
                    string uri;
#endif
                    return VcardValue.TryGetUriText(text, out uri) ? new JcardNode(JcardNodeKind.String, uri) : null;
                case VcardValueType.Boolean:
                    bool flag;
                    if (!VcardValue.TryParseBoolean(value, VcardValueFormat.Text, out flag)) break;

                    return new JcardNode(flag ? JcardNodeKind.True : JcardNodeKind.False, string.Empty);
                case VcardValueType.Integer:
                    long integer;
                    if (!VcardValue.TryParseInteger(value, VcardValueFormat.Text, out integer)) break;

                    // ref. RFC 7095 §3.5.9: a JSON number, which has no plus sign or leading zeros.
                    return new JcardNode(JcardNodeKind.Number, VcardValueFormatting.IsJsonNumber(value) ? text : integer.ToString(CultureInfo.InvariantCulture));
                case VcardValueType.Float:
                    double number;
                    if (!VcardValue.TryParseFloat(value, VcardValueFormat.Text, out number)) break;

                    // ref. RFC 7095 §3.5.10: a JSON number.  The text is kept where JSON allows it, so that its digits are not changed.
                    return new JcardNode(JcardNodeKind.Number, VcardValueFormatting.IsJsonNumber(value) ? text : VcardValue.FormatFloatText(number));
                case VcardValueType.Date:
                    VcardDate date;
                    if (!VcardDate.TryParse(value, VcardValueFormat.Text, out date)) break;

                    return new JcardNode(JcardNodeKind.String, date.ToString(VcardValueFormat.Json));
                case VcardValueType.Time:
                    VcardTime time;
                    if (!VcardTime.TryParse(value, VcardValueFormat.Text, out time)) break;

                    return new JcardNode(JcardNodeKind.String, time.ToString(VcardValueFormat.Json));
                case VcardValueType.DateTime:
                    VcardDateTime dateTime;
                    if (!VcardDateTime.TryParse(value, VcardValueFormat.Text, out dateTime)) break;

                    return new JcardNode(JcardNodeKind.String, dateTime.ToString(VcardValueFormat.Json));
                case VcardValueType.DateAndOrTime:
                    VcardDateAndOrTime dateAndOrTime;
                    if (!VcardDateAndOrTime.TryParse(value, VcardValueFormat.Text, out dateAndOrTime)) break;

                    return new JcardNode(JcardNodeKind.String, dateAndOrTime.ToString(VcardValueFormat.Json));
                case VcardValueType.Timestamp:
                    VcardTimestamp timestamp;
                    if (!VcardTimestamp.TryParse(value, VcardValueFormat.Text, out timestamp)) break;

                    return new JcardNode(JcardNodeKind.String, timestamp.ToString(VcardValueFormat.Json));
                case VcardValueType.UtcOffset:
                    VcardUtcOffset offset;
                    if (!VcardUtcOffset.TryParse(value, VcardValueFormat.Text, out offset)) break;

                    return new JcardNode(JcardNodeKind.String, offset.ToString(VcardValueFormat.Json));
            }

            // A LANGUAGE-TAG is the same in both forms, and a value of these types that isn't valid is kept as a string, which ToText keeps as it is.
            return new JcardNode(JcardNodeKind.String, text);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static JcardNode? TryConvertStructuredValue(string value, bool allowsLists)
#else
        private static JcardNode TryConvertStructuredValue(string value, bool allowsLists)
#endif
        {
            // ref. RFC 7095 §3.3.1.3: an array with an element for each text component, "with empty/missing text components represented by zero-length strings", and an array for a
            // component with more than one value.  "it is RECOMMENDED to represent" a value of one component "as a single text value".
            int componentCount;
            if (!VcardText.TryCountComponents(value.AsSpan(), VcardVersion.V4_0, allowsLists, out componentCount)) return null;

            var components = VcardComponentList.Parse(value.AsSpan(), VcardVersion.V4_0, allowsLists, componentCount);
            if (componentCount == 1 && components[0].Count <= 1) return ToComponentNode(components[0]);

            var node = new JcardNode(JcardNodeKind.Array, string.Empty);
            for (var index = 0; index < componentCount; index++)
            {
                node.Add(ToComponentNode(components[index]));
            }

            return node;
        }

        private static JcardNode ToComponentNode(IReadOnlyList<string> items)
        {
            if (items.Count <= 1) return new JcardNode(JcardNodeKind.String, items.Count == 0 ? string.Empty : items[0]);

            var node = new JcardNode(JcardNodeKind.Array, string.Empty);
            foreach (var item in items)
            {
                node.Add(new JcardNode(JcardNodeKind.String, item));
            }

            return node;
        }

        private static bool HasLoneSurrogate(JcardNode node)
        {
            if (HasLoneSurrogate(node.Text)) return true;

            foreach (var item in node.Items)
            {
                if (HasLoneSurrogate(item)) return true;
            }

            return false;
        }

        private static bool HasLoneSurrogateInParameters(VcardContentLine property)
        {
            foreach (var parameter in property.ParameterArray)
            {
                foreach (var value in parameter.ValueArray)
                {
                    if (HasLoneSurrogate(value)) return true;
                }
            }

            return false;
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

        #endregion

        /// <summary>
        /// Gets whether a property's value is structured: N, ADR, ORG, GENDER and CLIENTPIDMAP (RFC 6350 §6.2.2, §6.3.1, §6.6.4, §6.2.7 and §6.7.7) when their value is TEXT.
        /// </summary>
        private static bool IsStructuredProperty(string name, VcardValueType valueType)
        {
            return valueType == VcardValueType.Text &&
                   (IsName(name, VcardPropertyNames.Name) || IsName(name, VcardPropertyNames.Address) || IsName(name, VcardPropertyNames.Organization) ||
                    IsName(name, VcardPropertyNames.Gender) || IsName(name, VcardPropertyNames.ClientPidMap));
        }

        /// <summary>
        /// Gets whether a component of a structured value may have more than one value: those of N and ADR (RFC 6350 §6.2.2 and §6.3.1).
        /// </summary>
        private static bool AllowsLists(string name)
        {
            return IsName(name, VcardPropertyNames.Name) || IsName(name, VcardPropertyNames.Address);
        }

        private static bool IsListProperty(string name)
        {
            foreach (var listProperty in ListProperties)
            {
                if (IsName(name, listProperty)) return true;
            }

            return false;
        }

        private static bool IsListParameter(string name)
        {
            foreach (var listParameter in ListParameters)
            {
                if (IsName(name, listParameter)) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether text is a name of vCard: one or more letters, digits and hyphens (RFC 6350 §3.3).
        /// </summary>
        internal static bool IsVcardName(string text)
        {
            if (text.Length == 0) return false;

            foreach (var character in text)
            {
                if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') && !VcardValueFormatting.IsDigit(character) && character != '-') return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that text is a name of jCard: one or more lower-case letters, digits and hyphens.  ref. RFC 7095 §3.3: property names "MUST be in lowercase", and the type
        /// identifier is "in lowercase".  ref. §3.4: "The name of the parameter MUST be in lowercase".
        /// </summary>
        /// <exception cref="VcardFileException">The text is not such a name.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void CheckJcardName(string text, JcardNode node, string? propertyName, string kind)
#else
        private static void CheckJcardName(string text, JcardNode node, string propertyName, string kind)
#endif
        {
            var isValid = text.Length != 0;
            foreach (var character in text)
            {
                isValid &= (character >= 'a' && character <= 'z') || VcardValueFormatting.IsDigit(character) || character == '-';
            }

            if (!isValid)
                throw BuildException(node, propertyName,
                    Format("The {0} name \"{1}\" is not a name of jCard, which is one or more lower-case letters, digits and hyphens: RFC 7095 §3.3 and §3.4.", kind, text));
        }

        private static bool AreAllStrings(IReadOnlyList<JcardNode> items)
        {
            foreach (var item in items)
            {
                if (item.Kind != JcardNodeKind.String) return false;
            }

            return true;
        }

        private static string FormatBoolean(bool value)
        {
            var buffer = new char[5];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, VcardValue.TryFormatBoolean(value, buffer, out charsWritten, VcardValueFormat.Text), charsWritten);
        }

        private static string DescribeLiteral(JcardNodeKind kind)
        {
            return kind == JcardNodeKind.True ? "true" : "false";
        }

        private static string GetTypeIdentifier(VcardValueType valueType, bool isExtensionType)
        {
            if (isExtensionType) return "of an extension";

            return valueType == VcardValueType.Unknown ? UnknownTypeName : VcardValueTypeRegistry.GetValueTypeName(valueType);
        }

        /// <summary>
        /// Describes the kind of a JSON value, for a message.
        /// </summary>
        internal static string DescribeKind(JcardNodeKind kind)
        {
            switch (kind)
            {
                case JcardNodeKind.String:
                    return "a string";
                case JcardNodeKind.Number:
                    return "a number";
                case JcardNodeKind.True:
                case JcardNodeKind.False:
                    return "a Boolean";
                case JcardNodeKind.Null:
                    return "null";
                case JcardNodeKind.Array:
                    return "an array";
                default:
                    return "an object";
            }
        }

        private static bool IsName(string name, string expected)
        {
            return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        /// <summary>
        /// Builds the exception for a problem in the structure of a jCard document, at a JSON value.
        /// </summary>
        /// <param name="node">The value at which the problem was found.</param>
        /// <param name="propertyName">The name of the property; or <c>null</c>.</param>
        /// <param name="message">The message.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardFileException BuildException(JcardNode node, string? propertyName, string message)
#else
        internal static VcardFileException BuildException(JcardNode node, string propertyName, string message)
#endif
        {
            var exception = new VcardFileException(message);
            exception.Data[VcardFileException.DataItemName.DiagnosticKind] = VcardDiagnosticKind.InvalidStructure;
            AddLocation(exception, node);
            if (propertyName != null)
            {
                exception.Data[VcardFileException.DataItemName.PropertyName] = propertyName;
            }

            return exception;
        }

        /// <summary>
        /// Adds the JSON path and byte position of a value to the data of an exception.
        /// </summary>
        internal static void AddLocation(VcardFileException exception, JcardNode node)
        {
            exception.Data[VcardFileException.DataItemName.JsonPath] = node.GetPath();
            exception.Data[VcardFileException.DataItemName.BytePosition] = node.BytePosition;
        }

        private static VcardFileException BuildWriteException(string message, VcardContentLine property)
        {
            var exception = new VcardFileException(message);
            if (property.LineNumber != 0)
            {
                exception.Data[VcardFileException.DataItemName.LineNumber] = property.LineNumber;
            }

            exception.Data[VcardFileException.DataItemName.PropertyName] = property.Name;
            if (property.Group != null)
            {
                exception.Data[VcardFileException.DataItemName.Group] = property.Group;
            }

            return exception;
        }
    }
}
#endif
