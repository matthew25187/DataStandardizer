using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Converts properties between the content lines of the card object model and the elements of xCard (RFC 6351).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A content line holds its value in the text form of RFC 6350, so converting from xCard turns each value element into that form, and converting to xCard turns the text back
    /// into value elements.  The text of a value is kept where both forms allow it, such as the digits of a FLOAT, so that a value that goes to xCard and back is unchanged.
    /// </para>
    /// <para>
    /// A value that doesn't follow its type, which <see cref="VcardOptions.AllowInvalidValues"/> lets a card hold, is kept as it is.  From xCard, its text is kept if it can't be
    /// converted, so that the conformance check reports it.  To xCard, a value that can't be converted is written as it is in the element of its type, which reads back the same
    /// way; a TEXT, URI or structured value, whose escaping would change, and a value that xCard has no form for, are written in an <c>unknown</c> element instead (RFC 6351 §6).
    /// </para>
    /// </remarks>
    internal static class XcardConverter
    {
        /// <summary>
        /// The names of the value elements (RFC 6351 Appendix A), indexed by <see cref="VcardValueType"/>; <see cref="VcardValueType.Unknown"/> is the <c>unknown</c> element of
        /// RFC 6351 §6.  DATE-AND-OR-TIME has no element of its own, and is written in a <c>date</c>, <c>date-time</c> or <c>time</c> element.
        /// </summary>
        private static readonly string[] ValueElementNames =
        {
            XcardNames.Unknown, XcardNames.Text, XcardNames.Uri, XcardNames.Date, XcardNames.Time, XcardNames.DateTime, string.Empty, "timestamp", "boolean", "integer", "float",
            "utc-offset", "language-tag"
        };

        /// <summary>
        /// The properties of RFC 6350 whose value is a list of TEXT values, each of which is a value element (RFC 6351 Appendix A).
        /// </summary>
        private static readonly string[] ListProperties = { VcardPropertyNames.Nickname, VcardPropertyNames.Categories };

        /// <summary>
        /// The parameters whose value is a list (RFC 6350 §5.5, §5.6 and §5.9), each of whose values is a value element (RFC 6351 §5.1).
        /// </summary>
        private static readonly string[] ListParameters = { VcardParameterNames.Type, VcardParameterNames.Pid, VcardParameterNames.SortAs };

        // ref. RFC 6351 Appendix A §5, RFC 6715 §3, and RFC 9554 §4: the value element of each parameter that isn't TEXT.  TZ may be TEXT or a URI (RFC 6351 Appendix A §5.11).
        private static readonly string[] UriParameters = { VcardParameterNames.Geo, VcardParameterNames.Author };
        private static readonly string[] IntegerParameters = { VcardParameterNames.Preference, VcardParameterNames.Index };
        private static readonly string[] BooleanParameters = { VcardParameterNames.Derived };
        private static readonly string[] TimestampParameters = { VcardParameterNames.Created };
        private static readonly string[] LanguageTagParameters = { VcardParameterNames.Language };

        /// <summary>
        /// The values of the sex element of GENDER (RFC 6351 Appendix A §6.2.7).
        /// </summary>
        private static readonly string[] SexValues = { string.Empty, "M", "F", "O", "N", "U" };

        /// <summary>
        /// The parameters of RFC 6350, RFC 6351, RFC 6715, RFC 8605 and RFC 9554 whose values are TEXT.  Any parameter in none of these lists is unrecognised, and its values are
        /// written in <c>unknown</c> elements (RFC 6351 §6).
        /// </summary>
        private static readonly string[] TextParameters =
        {
            VcardParameterNames.AlternativeId, VcardParameterNames.Pid, VcardParameterNames.Type, VcardParameterNames.MediaType, VcardParameterNames.CalendarScale,
            VcardParameterNames.SortAs, VcardParameterNames.Label, VcardParameterNames.Level, VcardParameterNames.CountryCode, VcardParameterNames.AuthorName,
            VcardParameterNames.Phonetic, VcardParameterNames.PropertyId, VcardParameterNames.Script, VcardParameterNames.ServiceType, VcardParameterNames.Username
        };

        #region xCard to vCard

        /// <summary>
        /// Converts a property element into a content line.
        /// </summary>
        /// <param name="property">The property element, a child of a <c>vcard</c> or <c>group</c> element.</param>
        /// <param name="group">The group of the property; or <c>null</c>.</param>
        /// <param name="cardIndex">The 0-based index of the card the property is in.</param>
        /// <param name="isValueNotInXcardForm"><c>true</c> if a value isn't in the form xCard gives its type, such as a BOOLEAN of <c>TRUE</c>, or a structured value that lacks a
        /// component.  It is kept as it is, and the caller reports it, since its text may be a valid value of vCard that the conformance check wouldn't report.</param>
        /// <exception cref="VcardFileException">The element is not a valid xCard property.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardContentLine ToContentLine(XcardElement property, string? group, int cardIndex, out bool isValueNotInXcardForm)
#else
        internal static VcardContentLine ToContentLine(XcardElement property, string group, int cardIndex, out bool isValueNotInXcardForm)
#endif
        {
            var conversion = new Conversion();
            var name = ToVcardName(property, null, "property");

            // ref. RFC 6350 §6.1.1 and §6.1.2: BEGIN and END delimit a card, which the vcard element does.  ref. RFC 6351 §5.1: "In XML, this property [VERSION] is absent."
            if (IsName(name, VcardPropertyNames.Begin) || IsName(name, VcardPropertyNames.End) || IsName(name, VcardPropertyNames.Version))
                throw BuildException(property, name,
                    Format("The {0} property can't be in xCard, whose vcard element and namespace take its place: RFC 6351 §5 and §5.1.", name));

            CheckNoText(property, name);
            var parameters = new List<VcardContentLineParameter>();
            var values = new List<XcardElement>();
            for (var index = 0; index < property.Children.Count; index++)
            {
                var child = property.Children[index];
                if (child.Name == XcardNames.Parameters)
                {
                    // ref. RFC 6351 Appendix A: the parameters element, where there is one, comes first.
                    if (index != 0)
                        throw BuildException(child, name, Format("The parameters element of the {0} property must be its first child element, and it can have only one.", name));

                    ReadParameters(child, name, parameters, conversion);
                }
                else
                {
                    values.Add(child);
                }
            }

            string value;
            VcardValueType valueType;
            if (!TryReadStructuredValue(values, name, conversion, out value))
            {
                value = ReadValues(values, property, name, conversion, out valueType);

                // ref. RFC 6350 §5.2: a VALUE parameter names a type that is not the property's default.  ref. RFC 6351 §6: never one for an unknown value.
                if (valueType != VcardValueType.Unknown && valueType != VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan()))
                {
                    parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, VcardValueTypeRegistry.GetValueTypeName(valueType)));
                }
            }

            isValueNotInXcardForm = conversion.IsValueNotInXcardForm;
            return VcardContentLine.FromConversion(group, name, parameters.ToArray(), value, cardIndex);
        }

        /// <summary>
        /// Gets the vCard name for the name of an element: the name in upper case (RFC 6351 §6).
        /// </summary>
        /// <exception cref="VcardFileException">The name is not a name of xCard: one or more lower-case letters, digits and hyphens.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string ToVcardName(XcardElement element, string? propertyName, string kind)
#else
        internal static string ToVcardName(XcardElement element, string propertyName, string kind)
#endif
        {
            // ref. RFC 6351 §5.1: "New XML vCard property and parameter element names MUST be lower-case", as those of the schema are.  ref. RFC 6350 §3.3: names are letters,
            // digits and hyphens.
            var isValid = element.Name.Length != 0;
            foreach (var character in element.Name)
            {
                isValid &= (character >= 'a' && character <= 'z') || VcardValueFormatting.IsDigit(character) || character == '-';
            }

            if (!isValid)
                throw BuildException(element, propertyName,
                    Format("The {0} element name {1} is not a name of xCard, which is one or more lower-case letters, digits and hyphens: RFC 6351 §5.1.", kind, element.Name));

            return element.Name.ToUpperInvariant();
        }

        private static void ReadParameters(XcardElement parametersElement, string propertyName, List<VcardContentLineParameter> parameters, Conversion conversion)
        {
            CheckNoText(parametersElement, propertyName);
            foreach (var parameter in parametersElement.Children)
            {
                var name = ToVcardName(parameter, propertyName, "parameter");

                // The value element gives the type of a value, as the VALUE parameter does in vCard, so a value parameter would contradict it.
                if (IsName(name, VcardParameterNames.Value))
                    throw BuildException(parameter, propertyName,
                        Format("The {0} property has a value parameter, which xCard doesn't have: the value element gives the type of the value.", propertyName));

                CheckNoText(parameter, propertyName);

                // ref. RFC 6351 §5.1: a list of values is "a list of elements".  ref. §6: other elements are dropped.
                var values = new List<string>();
                foreach (var valueElement in parameter.Children)
                {
                    VcardValueType valueType;
                    if (!TryGetValueType(valueElement.Name, out valueType)) continue;

                    CheckNoChildren(valueElement, propertyName);
                    if (!IsExpectedParameterElement(name, valueElement.Name))
                    {
                        conversion.IsValueNotInXcardForm = true;
                    }

                    values.Add(ReadParameterValue(valueElement, valueType, conversion));
                }

                if (values.Count == 0)
                    throw BuildException(parameter, propertyName, Format("The {0} parameter of the {1} property has no value element.", name, propertyName));

                parameters.Add(new VcardContentLineParameter(name, values));
            }
        }

        private static string ReadParameterValue(XcardElement valueElement, VcardValueType valueType, Conversion conversion)
        {
            // ref. RFC 6351 §6: "Any <unknown> parameter value XML elements are converted as if they were <text> value type XML elements."  The types whose white space XML Schema
            // collapses lose it, and a BOOLEAN takes the case of vCard.
            var text = valueElement.Text;
            switch (valueType)
            {
                case VcardValueType.Boolean:
                    bool flag;
                    if (VcardValue.TryParseBoolean(text.AsSpan(), VcardValueFormat.Xml, out flag)) return FormatBoolean(flag);

                    conversion.IsValueNotInXcardForm = true;
                    return text;
                case VcardValueType.Integer:
                case VcardValueType.Uri:
                    return VcardValueFormatting.TrimXmlWhitespace(text.AsSpan()).ToString();
                default:
                    return text;
            }
        }

        /// <summary>
        /// Gets whether a value element is the one RFC 6351 gives a parameter, or an <c>unknown</c> element, which any parameter may have.  A parameter that isn't recognised may
        /// have any.
        /// </summary>
        private static bool IsExpectedParameterElement(string parameterName, string elementName)
        {
            if (elementName == XcardNames.Unknown) return true;

            string expected;
            if (!TryGetParameterElementName(parameterName, out expected)) return true;

            // ref. RFC 6351 Appendix A §5.11: param-tz = element tz { value-text | value-uri }
            return elementName == expected || (IsName(parameterName, VcardParameterNames.TimeZone) && (elementName == XcardNames.Text || elementName == XcardNames.Uri));
        }

        /// <summary>
        /// Converts the value elements of a property, all of one type, into the text of a content line.
        /// </summary>
        private static string ReadValues(List<XcardElement> children, XcardElement property, string propertyName, Conversion conversion, out VcardValueType valueType)
        {
            // ref. RFC 6351 §6: elements that aren't value elements are dropped.
            var values = new List<XcardElement>();
            foreach (var child in children)
            {
                VcardValueType childType;
                if (TryGetValueType(child.Name, out childType))
                {
                    values.Add(child);
                }
            }

            if (values.Count == 0)
                throw BuildException(property, propertyName, Format("The {0} property has no value element.", propertyName));

            var first = values[0];
            TryGetValueType(first.Name, out valueType);
            foreach (var value in values)
            {
                if (value.Name != first.Name)
                    throw BuildException(value, propertyName,
                        Format("The values of the {0} property must all be of one type, but it has both {1} and {2} elements.", propertyName, first.Name, value.Name));
            }

            // ref. RFC 6351 Appendix A: only NICKNAME and CATEGORIES (value-text-list) have more than one value element.  The values of a property whose type isn't known may be a
            // list too, as long as their type is.  ref. RFC 6351 §6: an unknown value is "the unprocessed value text", so it is one.
            if (values.Count > 1 &&
                (valueType == VcardValueType.Unknown || (VcardValueTypeRegistry.IsKnownProperty(propertyName.AsSpan()) && !IsListProperty(propertyName))))
                throw BuildException(values[1], propertyName, Format("The {0} property can have only one value element.", propertyName));

            // ref. RFC 6351 Appendix A §4.3.4: value-date-and-or-time = value-date | value-date-time | value-time.  A property whose type is DATE-AND-OR-TIME holds one of them.
            var isDateAndOrTime = VcardValueTypeRegistry.GetDefaultValueType(propertyName.AsSpan()) == VcardValueType.DateAndOrTime &&
                                  (valueType == VcardValueType.Date || valueType == VcardValueType.DateTime || valueType == VcardValueType.Time);
            var builder = new StringBuilder();
            for (var index = 0; index < values.Count; index++)
            {
                CheckNoChildren(values[index], propertyName);
                if (index != 0)
                {
                    builder.Append(',');
                }

                builder.Append(ToText(valueType, values[index], propertyName, isDateAndOrTime, conversion));
            }

            if (isDateAndOrTime)
            {
                valueType = VcardValueType.DateAndOrTime;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts the text of a value element to the text form of a content line; or keeps it as it is if it isn't in the form xCard gives its type.
        /// </summary>
        private static string ToText(VcardValueType valueType, XcardElement element, string propertyName, bool isDateAndOrTime, Conversion conversion)
        {
            var xml = element.Text;
            var value = xml.AsSpan();
            switch (valueType)
            {
                case VcardValueType.Text:
                    return EscapeText(element, propertyName, false);
                case VcardValueType.Uri:
                    // ref. RFC 6351 §6: "Property value escaping is carried out", and RFC 6350 §3.4 and its errata 3845 and 3846 say a comma in a URI "MUST be escaped".
                    return VcardValue.EscapeUri(VcardValueFormatting.TrimXmlWhitespace(value).ToString());
                case VcardValueType.Boolean:
                    bool flag;
                    if (VcardValue.TryParseBoolean(value, VcardValueFormat.Xml, out flag)) return FormatBoolean(flag);

                    break;
                case VcardValueType.Integer:
                    long integer;
                    if (VcardValue.TryParseInteger(value, VcardValueFormat.Xml, out integer)) return VcardValueFormatting.TrimXmlWhitespace(value).ToString();

                    break;
                case VcardValueType.Float:
                    // The digits are kept, moved by any exponent in the text, not through a double.
                    var trimmed = VcardValueFormatting.TrimXmlWhitespace(value);
                    double number;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? expanded;
#else
                    string expanded;
#endif
                    if (VcardValue.TryParseFloat(trimmed, VcardValueFormat.Text, out number)) return trimmed.ToString();
                    if (VcardValue.TryParseFloat(value, VcardValueFormat.Xml, out number) && VcardValue.TryExpandXmlFloat(value, out expanded)) return expanded;

                    break;
                case VcardValueType.Date:
                    VcardDate date;
                    if (VcardDate.TryParse(value, VcardValueFormat.Xml, out date)) return xml;

                    break;
                case VcardValueType.Time:
                    // ref. RFC 6350 §4: a DATE-AND-OR-TIME that is a time alone has a "T" before it, which the time element of xCard doesn't.
                    VcardTime time;
                    if (VcardTime.TryParse(value, VcardValueFormat.Xml, out time)) return isDateAndOrTime ? "T" + xml : xml;

                    break;
                case VcardValueType.DateTime:
                    VcardDateTime dateTime;
                    if (VcardDateTime.TryParse(value, VcardValueFormat.Xml, out dateTime)) return xml;

                    break;
                case VcardValueType.Timestamp:
                    VcardTimestamp timestamp;
                    if (VcardTimestamp.TryParse(value, VcardValueFormat.Xml, out timestamp)) return xml;

                    break;
                case VcardValueType.UtcOffset:
                    VcardUtcOffset offset;
                    if (VcardUtcOffset.TryParse(value, VcardValueFormat.Xml, out offset)) return xml;

                    break;
                default:
                    // A LANGUAGE-TAG is the same in both forms, and an unknown value "MUST be converted directly into vCard values" (RFC 6351 §6).
                    return xml;
            }

            conversion.IsValueNotInXcardForm = true;
            return xml;
        }

        /// <summary>
        /// Reads the elements of a structured value: the components of N, ADR, ORG, GENDER or CLIENTPIDMAP (RFC 6351 Appendix A).
        /// </summary>
        /// <returns><c>true</c> if the property's value is structured, and <paramref name="value"/> has its text; <c>false</c> if it is in value elements, such as an
        /// <c>unknown</c> one.</returns>
        private static bool TryReadStructuredValue(List<XcardElement> children, string propertyName, Conversion conversion, out string value)
        {
            value = string.Empty;
            string[] componentNames;
            if (IsName(propertyName, VcardPropertyNames.Name))
            {
                componentNames = XcardNames.NameComponents;
            }
            else if (IsName(propertyName, VcardPropertyNames.Address))
            {
                componentNames = XcardNames.AddressComponents;
            }
            else if (IsName(propertyName, VcardPropertyNames.Gender))
            {
                componentNames = new[] { XcardNames.Sex, XcardNames.Identity };
            }
            else if (IsName(propertyName, VcardPropertyNames.ClientPidMap))
            {
                componentNames = new[] { XcardNames.SourceId, XcardNames.Uri };
            }
            else if (IsName(propertyName, VcardPropertyNames.Organization))
            {
                return TryReadOrganization(children, propertyName, out value);
            }
            else
            {
                return false;
            }

            // The value is structured if it has a component element of its own; a CLIENTPIDMAP's uri element is also a value element, so its sourceid decides.
            var isStructured = false;
            foreach (var child in children)
            {
                var index = Array.IndexOf(componentNames, child.Name);
                isStructured |= index >= 0 && child.Name != XcardNames.Uri;
            }

            if (!isStructured) return false;

            value = ReadComponents(children, componentNames, propertyName, conversion);
            return true;
        }

        private static string ReadComponents(List<XcardElement> children, string[] componentNames, string propertyName, Conversion conversion)
        {
            // ref. RFC 6351 Appendix A: an element for each component, in order, repeated for each value of a component that has more than one, "with element { text }+" for each
            // component of RFC 6350.  Elements that aren't components, other than value elements, are dropped (RFC 6351 §6).
            var items = new List<string>[componentNames.Length];
            var position = 0;
            var lastIndex = -1;
            foreach (var child in children)
            {
                var index = Array.IndexOf(componentNames, child.Name);
                if (index < 0)
                {
                    VcardValueType valueType;
                    if (!TryGetValueType(child.Name, out valueType)) continue;

                    throw BuildException(child, propertyName, Format("The {0} element can't be among the components of the {1} property.", child.Name, propertyName));
                }

                if (index < position)
                    throw BuildException(child, propertyName,
                        Format("The {0} element of the {1} property is out of order: its components are {2}, in that order.", child.Name, propertyName,
                            string.Join(", ", componentNames)));

                CheckNoChildren(child, propertyName);
                position = index;
                lastIndex = Math.Max(lastIndex, index);
                if (items[index] is null)
                {
                    items[index] = new List<string>();
                }
                else if (!AllowsLists(propertyName))
                {
                    throw BuildException(child, propertyName, Format("The {0} property can have only one {1} element.", propertyName, child.Name));
                }

                items[index].Add(ReadComponentItem(child, propertyName, conversion));
            }

            // ref. RFC 6350 §6.2.2 and §6.3.1, and RFC 9554 §2: N has 5 or 7 components, and ADR 7 or 18.  GENDER may have its sex alone, and CLIENTPIDMAP has both of its parts.
            // Once a component of RFC 9554 is there, each of its components has an element too, as each of RFC 6350's does, and as the writer writes them.
            int requiredCount, count;
            if (IsName(propertyName, VcardPropertyNames.Name) || IsName(propertyName, VcardPropertyNames.Address))
            {
                var rfc6350Count = IsName(propertyName, VcardPropertyNames.Name) ? 5 : 7;
                count = lastIndex < rfc6350Count ? rfc6350Count : componentNames.Length;
                requiredCount = count;
            }
            else
            {
                requiredCount = IsName(propertyName, VcardPropertyNames.Gender) ? 1 : 2;
                count = Math.Max(requiredCount, lastIndex + 1);
            }

            var builder = new StringBuilder();
            for (var index = 0; index < count; index++)
            {
                if (index != 0)
                {
                    builder.Append(';');
                }

                if (items[index] is null)
                {
                    // ref. RFC 6351 Appendix A: each component has an element, even an empty one.
                    if (index < requiredCount)
                    {
                        conversion.IsValueNotInXcardForm = true;
                    }

                    continue;
                }

                builder.Append(string.Join(",", items[index]));
            }

            return builder.ToString();
        }

        private static string ReadComponentItem(XcardElement component, string propertyName, Conversion conversion)
        {
            switch (component.Name)
            {
                case XcardNames.SourceId:
                    // ref. RFC 6351 Appendix A §6.7.7: element sourceid { xsd:positiveInteger }, whose white space is collapsed.
                    var trimmed = VcardValueFormatting.TrimXmlWhitespace(component.Text.AsSpan()).ToString();
                    long sourceId;
                    if (!VcardValue.TryParseInteger(trimmed.AsSpan(), VcardValueFormat.Text, out sourceId) || sourceId < 1)
                    {
                        conversion.IsValueNotInXcardForm = true;
                        return component.Text;
                    }

                    return trimmed;
                case XcardNames.Uri:
                    return VcardValue.EscapeUri(VcardValueFormatting.TrimXmlWhitespace(component.Text.AsSpan()).ToString());
                case XcardNames.Sex:
                    // ref. RFC 6351 Appendix A §6.2.7: element sex { "" | "M" | "F" | "O" | "N" | "U" }, whose values XML compares by case, unlike the ABNF of RFC 6350.
                    if (Array.IndexOf(SexValues, component.Text) < 0)
                    {
                        conversion.IsValueNotInXcardForm = true;
                    }

                    return EscapeText(component, propertyName, true);
                default:
                    return EscapeText(component, propertyName, true);
            }
        }

        private static bool TryReadOrganization(List<XcardElement> children, string propertyName, out string value)
        {
            // ref. RFC 6351 Appendix A §6.6.4: property-org = element org { ..., value-text-list }, a text element for each component of RFC 6350 §6.6.4.
            value = string.Empty;
            var components = new List<string>();
            foreach (var child in children)
            {
                VcardValueType valueType;
                if (!TryGetValueType(child.Name, out valueType)) continue;
                if (valueType != VcardValueType.Text) return false;

                CheckNoChildren(child, propertyName);
                components.Add(EscapeText(child, propertyName, true));
            }

            if (components.Count == 0) return false;

            value = string.Join(";", components);
            return true;
        }

        /// <summary>
        /// Escapes the text of an element as a TEXT value, or as a component of a structured value, where a semicolon is escaped too (RFC 6351 §6).
        /// </summary>
        /// <exception cref="VcardFileException">The text holds a character that a vCard value can't hold.</exception>
        private static string EscapeText(XcardElement element, string propertyName, bool isComponent)
        {
            var text = element.Text;
            if (!VcardText.IsValidUnescapedText(text.AsSpan()))
                throw BuildException(element, propertyName, Format("The text of the {0} property holds a character that a vCard value cannot hold.", propertyName));

            var buffer = new char[VcardText.GetMaxEscapedLength(text.Length)];
            var writer = new VcardSpanWriter(buffer);
            VcardText.AppendEscaped(ref writer, text.AsSpan(), VcardVersion.V4_0, isComponent);
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, writer.TryComplete(out charsWritten), charsWritten);
        }

        #endregion

        #region vCard to xCard

        /// <summary>
        /// Converts a content line into a property element.
        /// </summary>
        /// <param name="property">The content line.</param>
        /// <exception cref="VcardFileException">The line can't be written in xCard: a name isn't valid, it is a BEGIN, END or GROUP property, it has more than one VALUE parameter, or
        /// it holds a character that XML can't hold.</exception>
        internal static XcardElement ToElement(VcardContentLine property)
        {
            var name = property.Name;
            if (!IsVcardName(name))
                throw BuildWriteException(Format("The name {0} cannot be written in xCard, since it is not one or more letters, digits and hyphens.", name), property);

            // ref. RFC 6351 §5: the vcard element takes the place of BEGIN and END, and the group element is the group construct, so no property can have its name.
            if (IsName(name, VcardPropertyNames.Begin) || IsName(name, VcardPropertyNames.End) || IsName(name, XcardNames.Group))
                throw BuildWriteException(Format("A {0} property can't be written in xCard, which has an element of that name in its place: RFC 6351 §5.", name.ToUpperInvariant()),
                    property);

            if (property.ParameterArray.Length == 0 && IsName(name, VcardPropertyNames.Xml))
            {
                // ref. RFC 6351 §6: "The content of "XML" properties is copied as is to XML."  One that isn't a valid XML property is written as any other.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                string? xml;
#else
                string xml;
#endif
                if (VcardValue.TryParseText(property.Value.AsSpan(), VcardValueFormat.Text, out xml) && VcardConformance.IsValidXmlElement(xml))
                    return new XcardElement(name.ToLowerInvariant()) { RawXml = xml };
            }

            if (CountParameters(property, VcardParameterNames.Value) > 1)
                throw BuildWriteException(
                    Format("The VALUE parameter appears more than once in the {0} property, which xCard can't hold, since the value element gives one type.",
                        name.ToUpperInvariant()), property);

            // ref. RFC 6351 §6: a property whose type isn't known has an unknown element, as does one whose VALUE parameter names a type that has no element.
            var valueParameter = VcardPropertyParameterReader.GetValue(property, VcardParameterNames.Value);
            VcardValueType valueType;
            if (valueParameter is null)
            {
                valueType = VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan());
            }
            else if (!VcardValueTypeRegistry.TryParseValueTypeName(valueParameter.AsSpan(), out valueType))
            {
                valueType = VcardValueType.Unknown;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            List<XcardElement>? values = null;
#else
            List<XcardElement> values = null;
#endif
            if (valueType != VcardValueType.Unknown)
            {
                values = IsStructuredProperty(name, valueType) ? TryWriteStructuredValue(property) : TryWriteValues(property, valueType);
            }

            if (values is null)
            {
                // ref. RFC 6351 Appendix A §4.3.1: value-date has no year alone.  In an unknown element such a value would lose the type its VALUE parameter names (§6), so it is
                // refused; with no VALUE parameter, the type is the property's default, which the unknown element keeps.
                if (valueParameter != null && HasYearAloneDate(valueType, property.Value))
                    throw BuildWriteException(
                        Format("The {0} property has VALUE={1} and a date of a year alone, which xCard has no form for: RFC 6351 Appendix A §4.3.1.", name.ToUpperInvariant(),
                            valueParameter), property);

                values = new List<XcardElement> { CreateTextElement(XcardNames.Unknown, property.Value, property) };
            }

            var element = new XcardElement(name.ToLowerInvariant());
            var parameters = WriteParameters(property);

            // ref. RFC 6351 Appendix A: the parameters element is optional, so it is left out when there are none.
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

        private static XcardElement WriteParameters(VcardContentLine property)
        {
            var parameters = new XcardElement(XcardNames.Parameters);
            var listParameterNames = new List<string>();
            foreach (var parameter in property.ParameterArray)
            {
                // ref. RFC 6351 §6: the value element takes the place of the VALUE parameter.
                if (IsName(parameter.Name, VcardParameterNames.Value)) continue;

                if (!IsVcardName(parameter.Name))
                    throw BuildWriteException(
                        Format("The parameter name {0} cannot be written in xCard, since it is not one or more letters, digits and hyphens.", parameter.Name), property);

                // ref. RFC 6351 §5.1: "Parameters containing a list of values are expressed using a list of elements", so the values of a list parameter that is repeated, as in
                // TYPE=work;TYPE=voice (RFC 6350 §6.4.1), are written with the first.
                var name = parameter.Name.ToLowerInvariant();
                var isList = IsListParameter(name);
                if (isList)
                {
                    if (listParameterNames.Contains(name)) continue;

                    listParameterNames.Add(name);
                }

                var parameterElement = parameters.Add(new XcardElement(name));
                IReadOnlyList<string> values = isList ? VcardPropertyParameterReader.GetListValues(property, parameter.Name) : (IReadOnlyList<string>)parameter.ValueArray;
                foreach (var value in values)
                {
                    WriteParameterValue(parameterElement, parameter.Name, value, property);
                }
            }

            return parameters;
        }

        private static void WriteParameterValue(XcardElement parameterElement, string parameterName, string value, VcardContentLine property)
        {
            // ref. RFC 6351 Appendix A §5 and §6: the value element of each parameter, and unknown for one that isn't recognised.  A value that isn't valid for its type is written in
            // an unknown element too, which reads back as it is.
            string elementName;
            if (!TryGetParameterElementName(parameterName, out elementName))
            {
                elementName = XcardNames.Unknown;
            }
            else if (IsName(parameterName, VcardParameterNames.TimeZone))
            {
                elementName = VcardValue.IsValidUriText(value.AsSpan()) ? XcardNames.Uri : XcardNames.Text;
            }

            var text = value;
            switch (elementName)
            {
                case "boolean":
                    bool flag;
                    if (VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Text, out flag))
                    {
                        text = FormatXmlBoolean(flag);
                    }
                    else
                    {
                        elementName = XcardNames.Unknown;
                    }

                    break;
                case "integer":
                    long integer;
                    if (!VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Text, out integer))
                    {
                        elementName = XcardNames.Unknown;
                    }

                    break;
                case "timestamp":
                    VcardTimestamp timestamp;
                    if (!VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, out timestamp))
                    {
                        elementName = XcardNames.Unknown;
                    }

                    break;
                case XcardNames.Uri:
                    if (!VcardValue.IsValidUriText(value.AsSpan()))
                    {
                        elementName = XcardNames.Unknown;
                    }

                    break;
            }

            parameterElement.Add(CreateTextElement(elementName, text, property));
        }

        /// <summary>
        /// Converts the value of a property into value elements.
        /// </summary>
        /// <returns>The elements; or <c>null</c> if the value has to be written in an unknown element.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcardElement>? TryWriteValues(VcardContentLine property, VcardValueType valueType)
#else
        private static List<XcardElement> TryWriteValues(VcardContentLine property, VcardValueType valueType)
#endif
        {
            // ref. RFC 6351 Appendix A: each value of a multi-valued property is an element.  The values of a property whose type isn't known may be a list too, so they are split
            // as well.
            var name = property.Name;
            var items = new List<string>();
            if (IsListProperty(name) || !VcardValueTypeRegistry.IsKnownProperty(name.AsSpan()))
            {
                foreach (var item in VcardValue.EnumerateList(property.Value.AsSpan(), VcardValueFormat.Text))
                {
                    items.Add(item.ToString());
                }
            }
            else
            {
                items.Add(property.Value);
            }

            var elements = new List<XcardElement>();
            foreach (var item in items)
            {
                string elementName, xml;
                if (!TryConvertValue(valueType, item, out elementName, out xml)) return null;

                elements.Add(CreateTextElement(elementName, xml, property));
            }

            return elements;
        }

        /// <summary>
        /// Converts a value from the text form of a content line to the name and text of a value element.
        /// </summary>
        /// <returns><c>false</c> if the value has to be written in an unknown element: a TEXT or URI value that isn't valid, whose escaping would change, or a value xCard has no
        /// form for.  A value of another type that isn't valid is the text of its own element, which reads back the same.</returns>
        private static bool TryConvertValue(VcardValueType valueType, string text, out string elementName, out string xml)
        {
            var value = text.AsSpan();
            elementName = ValueElementNames[(int)valueType];
            xml = text;
            switch (valueType)
            {
                case VcardValueType.Text:
                    // ref. RFC 6351 §6: "Property value escaping is undone."
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? unescaped;
#else
                    string unescaped;
#endif
                    if (!VcardValue.TryParseText(value, VcardValueFormat.Text, out unescaped)) return false;

                    xml = unescaped;
                    return true;
                case VcardValueType.Uri:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? uri;
#else
                    string uri;
#endif
                    if (!VcardValue.TryGetUriText(text, out uri)) return false;

                    xml = uri;
                    return true;
                case VcardValueType.Boolean:
                    bool flag;
                    if (VcardValue.TryParseBoolean(value, VcardValueFormat.Text, out flag))
                    {
                        xml = FormatXmlBoolean(flag);
                    }

                    return true;
                case VcardValueType.Date:
                    // ref. RFC 6351 Appendix A §4.3.1: the pattern of value-date has no year alone, which vCard 4.0 has.
                    VcardDate date;
                    return !VcardDate.TryParse(value, VcardValueFormat.Text, out date) || !date.IsYearAlone;
                case VcardValueType.DateAndOrTime:
                    // ref. RFC 6351 Appendix A §4.3.4: value-date-and-or-time = value-date | value-date-time | value-time.  A value that isn't valid has no element.
                    VcardDateAndOrTime dateAndOrTime;
                    if (!VcardDateAndOrTime.TryParse(value, VcardValueFormat.Text, out dateAndOrTime)) return false;

                    switch (dateAndOrTime.Kind)
                    {
                        case VcardDateAndOrTimeKind.Date:
                            if (dateAndOrTime.Date.GetValueOrDefault().IsYearAlone) return false;

                            elementName = XcardNames.Date;
                            return true;
                        case VcardDateAndOrTimeKind.DateTime:
                            elementName = XcardNames.DateTime;
                            return true;
                        default:
                            elementName = XcardNames.Time;
                            xml = text.Substring(1);
                            return true;
                    }

                default:
                    // An INTEGER, FLOAT, TIME, DATE-TIME, TIMESTAMP and UTC-OFFSET are the same in both forms, and so is a LANGUAGE-TAG.
                    return true;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcardElement>? TryWriteStructuredValue(VcardContentLine property)
#else
        private static List<XcardElement> TryWriteStructuredValue(VcardContentLine property)
#endif
        {
            var name = property.Name;
            var value = property.Value;
            if (IsName(name, VcardPropertyNames.Gender)) return TryWriteGender(value, property);
            if (IsName(name, VcardPropertyNames.ClientPidMap)) return TryWriteClientPidMap(value, property);

            // ref. RFC 6351 Appendix A §6.2.2, §6.3.1 and §6.6.4: an element for each component, one for each value of a component of N or ADR, and an empty one for a component
            // with no value.  ORG's components are text elements.
            var isOrganization = IsName(name, VcardPropertyNames.Organization);
            var allowsLists = AllowsLists(name);
            int componentCount;
            if (!VcardText.TryCountComponents(value.AsSpan(), VcardVersion.V4_0, allowsLists, out componentCount)) return null;

            string[] componentNames;
            if (isOrganization)
            {
                componentNames = new string[componentCount];
                for (var index = 0; index < componentCount; index++)
                {
                    componentNames[index] = XcardNames.Text;
                }
            }
            else
            {
                // ref. RFC 6350 §6.2.2 and §6.3.1, and RFC 9554 §2: N has 5 or 7 components, and ADR 7 or 18; any other number has no elements that read back the same.
                componentNames = IsName(name, VcardPropertyNames.Name) ? XcardNames.NameComponents : XcardNames.AddressComponents;
                var requiredCount = IsName(name, VcardPropertyNames.Name) ? 5 : 7;
                if (componentCount != requiredCount && componentCount != componentNames.Length) return null;
            }

            var components = VcardComponentList.Parse(value.AsSpan(), VcardVersion.V4_0, allowsLists, componentCount);
            var elements = new List<XcardElement>();
            for (var index = 0; index < componentCount; index++)
            {
                var items = components[index];
                if (items.Count == 0)
                {
                    elements.Add(new XcardElement(componentNames[index]));
                    continue;
                }

                foreach (var item in items)
                {
                    elements.Add(CreateTextElement(componentNames[index], item, property));
                }
            }

            return elements;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcardElement>? TryWriteGender(string value, VcardContentLine property)
#else
        private static List<XcardElement> TryWriteGender(string value, VcardContentLine property)
#endif
        {
            // ref. RFC 6350 §6.2.7: GENDER-value = sex [";" text], where the text is the rest of the value and may hold a semicolon.  ref. RFC 6351 Appendix A §6.2.7: a sex
            // element, which may be empty, and an optional identity element.
            VcardSex sex;
            ReadOnlySpan<char> identity;
            if (!VcardGender.TryParseParts(value.AsSpan(), VcardValueFormat.Text, out sex, out identity)) return null;

            var delimiterIndex = value.IndexOf(';');
            // ref. RFC 6351 Appendix A §6.2.7: the sex is one of "M", "F", "O", "N" and "U" in upper case, which RFC 6350 lets be in either case.
            var sexText = (delimiterIndex < 0 ? value : value.Substring(0, delimiterIndex)).ToUpperInvariant();
            var elements = new List<XcardElement> { CreateTextElement(XcardNames.Sex, sexText, property) };
            if (delimiterIndex >= 0)
            {
                elements.Add(CreateTextElement(XcardNames.Identity, VcardText.Unescape(identity, VcardVersion.V4_0), property));
            }

            return elements;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static List<XcardElement>? TryWriteClientPidMap(string value, VcardContentLine property)
#else
        private static List<XcardElement> TryWriteClientPidMap(string value, VcardContentLine property)
#endif
        {
            // ref. RFC 6350 §6.7.7: CLIENTPIDMAP-value = 1*DIGIT ";" URI, where the URI is the rest of the value and may hold a semicolon.  ref. RFC 6351 Appendix A §6.7.7: a
            // sourceid element and a uri element.
            long sourceId;
            ReadOnlySpan<char> uriPart;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? uri;
#else
            string uri;
#endif
            if (!VcardClientPidMap.TryParseParts(value.AsSpan(), VcardValueFormat.Text, out sourceId, out uriPart) || !VcardValue.TryGetUriText(uriPart.ToString(), out uri)) return null;

            return new List<XcardElement>
            {
                CreateTextElement(XcardNames.SourceId, value.Substring(0, value.IndexOf(';')), property), CreateTextElement(XcardNames.Uri, uri, property)
            };
        }

        /// <summary>
        /// Creates an element holding text, checking that XML can hold the text.
        /// </summary>
        /// <exception cref="VcardFileException">The text holds a character that XML cannot hold.</exception>
        private static XcardElement CreateTextElement(string name, string text, VcardContentLine property)
        {
            if (!VcardValueFormatting.IsXmlText(text.AsSpan()))
                throw BuildWriteException(
                    Format("The {0} property holds a character that XML cannot hold, so it cannot be written in xCard.", property.Name.ToUpperInvariant()), property);

            var element = new XcardElement(name);
            element.AppendText(text);
            return element;
        }

        /// <summary>
        /// Gets whether a value of a DATE or DATE-AND-OR-TIME, or a list of them, has a date of a year alone, which is valid in vCard 4.0 and has no xCard form.
        /// </summary>
        private static bool HasYearAloneDate(VcardValueType valueType, string value)
        {
            if (valueType != VcardValueType.Date && valueType != VcardValueType.DateAndOrTime) return false;

            foreach (var item in VcardValue.EnumerateList(value.AsSpan(), VcardValueFormat.Text))
            {
                VcardDate date;
                if (VcardDate.TryParse(item, VcardValueFormat.Text, out date) && date.IsYearAlone) return true;
            }

            return false;
        }

        private static int CountParameters(VcardContentLine property, string name)
        {
            var count = 0;
            foreach (var parameter in property.ParameterArray)
            {
                if (IsName(parameter.Name, name))
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        /// <summary>
        /// Gets the value type a value element names.
        /// </summary>
        /// <returns><c>true</c> if the name is that of a value element, including <c>unknown</c>; otherwise <c>false</c>.</returns>
        private static bool TryGetValueType(string elementName, out VcardValueType valueType)
        {
            var index = elementName.Length == 0 ? -1 : Array.IndexOf(ValueElementNames, elementName);
            valueType = index < 0 ? VcardValueType.Unknown : (VcardValueType)index;
            return index >= 0;
        }

        /// <summary>
        /// Gets the value element RFC 6351 gives a parameter's values.
        /// </summary>
        /// <returns><c>false</c> if the parameter isn't recognised, and has <c>unknown</c> elements.</returns>
        private static bool TryGetParameterElementName(string parameterName, out string elementName)
        {
            elementName = XcardNames.Text;
            if (Contains(UriParameters, parameterName))
            {
                elementName = XcardNames.Uri;
            }
            else if (Contains(IntegerParameters, parameterName))
            {
                elementName = "integer";
            }
            else if (Contains(BooleanParameters, parameterName))
            {
                elementName = "boolean";
            }
            else if (Contains(TimestampParameters, parameterName))
            {
                elementName = "timestamp";
            }
            else if (Contains(LanguageTagParameters, parameterName))
            {
                elementName = "language-tag";
            }
            else if (!Contains(TextParameters, parameterName) && !IsName(parameterName, VcardParameterNames.TimeZone))
            {
                elementName = XcardNames.Unknown;
                return false;
            }

            return true;
        }

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
            return Contains(ListProperties, name);
        }

        private static bool IsListParameter(string name)
        {
            return Contains(ListParameters, name);
        }

        private static bool Contains(string[] names, string name)
        {
            foreach (var candidate in names)
            {
                if (IsName(name, candidate)) return true;
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

        private static string FormatBoolean(bool value)
        {
            var buffer = new char[5];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, VcardValue.TryFormatBoolean(value, buffer, out charsWritten, VcardValueFormat.Text), charsWritten);
        }

        private static string FormatXmlBoolean(bool value)
        {
            var buffer = new char[5];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, VcardValue.TryFormatBoolean(value, buffer, out charsWritten, VcardValueFormat.Xml), charsWritten);
        }

        private static void CheckNoText(XcardElement element, string propertyName)
        {
            if (element.HasNonWhitespaceText)
                throw BuildException(element, propertyName, Format("The {0} element cannot hold text; its content must be elements.", element.Name));
        }

        private static void CheckNoChildren(XcardElement element, string propertyName)
        {
            if (element.Children.Count != 0)
                throw BuildException(element.Children[0], propertyName, Format("The {0} element cannot hold elements; its content must be text.", element.Name));
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
        /// What was found while converting a property.
        /// </summary>
        private sealed class Conversion
        {
            /// <summary>
            /// Gets or sets whether a value isn't in the form xCard gives its type.
            /// </summary>
            internal bool IsValueNotInXcardForm { get; set; }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static VcardFileException BuildException(XcardElement element, string? propertyName, string message)
#else
        private static VcardFileException BuildException(XcardElement element, string propertyName, string message)
#endif
        {
            return BuildException(message, element.LineNumber, element.LinePosition, propertyName);
        }

        /// <summary>
        /// Builds the exception for a problem in the structure of an xCard document.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="lineNumber">The 1-based line of the XML document; or 0 if not known.</param>
        /// <param name="linePosition">The 1-based position on the line; or 0 if not known.</param>
        /// <param name="propertyName">The name of the property; or <c>null</c>.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardFileException BuildException(string message, long lineNumber, long linePosition, string? propertyName)
#else
        internal static VcardFileException BuildException(string message, long lineNumber, long linePosition, string propertyName)
#endif
        {
            var exception = new VcardFileException(message);
            exception.Data[VcardFileException.DataItemName.DiagnosticKind] = VcardDiagnosticKind.InvalidStructure;
            AddLocation(exception, lineNumber, linePosition);
            if (propertyName != null)
            {
                exception.Data[VcardFileException.DataItemName.PropertyName] = propertyName;
            }

            return exception;
        }

        /// <summary>
        /// Adds the line and position in an XML document to the data of an exception.
        /// </summary>
        internal static void AddLocation(VcardFileException exception, long lineNumber, long linePosition)
        {
            if (lineNumber != 0)
            {
                exception.Data[VcardFileException.DataItemName.LineNumber] = lineNumber;
            }

            if (linePosition != 0)
            {
                exception.Data[VcardFileException.DataItemName.LinePosition] = linePosition;
            }
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
