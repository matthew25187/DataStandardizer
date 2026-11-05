#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Checks the lines of a vCard 3.0 card, in the form of that version, against RFC 2426 with its errata, RFC 2739 and RFC 4770.
    /// </summary>
    /// <remarks>
    /// RFC 2426 §4 gives the parameters of each type, and the value of a type no RFC defines is not known, so it isn't checked.
    /// </remarks>
    internal static class Vcard30Conformance
    {
        internal const string BinaryValueName = "binary";
        internal const string DateTimeValueName = "date-time";
        internal const string DateValueName = "date";
        internal const string TextValueName = "text";
        internal const string UriValueName = "uri";

        // ref. RFC 2426 §4: text-param = ("VALUE" "=" "ptext").  Its "ptext" is the rule of a parameter value, and §3 says "text", so both are read.
        private const string PlainTextValueName = "ptext";

        // ref. RFC 2426 §4: source-param, as erratum 869 corrects it.
        private const string ContextParameterName = "CONTEXT";

        private const string XNamePrefix = "X-";

        // ref. RFC 1766 §2: Primary-tag and Subtag = 1*8ALPHA
        private const int MaxLanguageSubtagLength = 8;

        private static readonly string[] NoParameters = new string[0];
        private static readonly string[] TypeOnly = { VcardParameterNames.Type };
        private static readonly string[] LanguageOnly = { VcardParameterNames.Language };
        private static readonly string[] LanguageAndType = { VcardParameterNames.Language, VcardParameterNames.Type };
        private static readonly string[] MediaParameters = { VcardParameterNames.Type, VcardLegacyTokens.EncodingName };
        private static readonly string[] SourceParameters = { ContextParameterName };

        private static readonly string[] NoValueTypes = new string[0];
        private static readonly string[] TextValueTypes = { TextValueName, PlainTextValueName };
        private static readonly string[] DateValueTypes = { DateValueName, DateTimeValueName };
        private static readonly string[] MediaValueTypes = { BinaryValueName, UriValueName };

        private static readonly Dictionary<string, Rule> Rules = CreateRules();

        /// <summary>
        /// Checks the lines of a card.
        /// </summary>
        internal static void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
        {
            var hasName = false;
            var hasFormattedName = false;
            foreach (var line in lines)
            {
                if (IsStructuralName(line.Name))
                {
                    Add(found, VcardDiagnosticKind.InvalidStructure, Format("A {0} line can't be in the properties of a card; a writer writes it.", line.Name), line, card);
                    continue;
                }

                hasName |= string.Equals(line.Name, VcardPropertyNames.Name, StringComparison.OrdinalIgnoreCase);
                hasFormattedName |= string.Equals(line.Name, VcardPropertyNames.FormattedName, StringComparison.OrdinalIgnoreCase);

                var hasLegacySyntax = line.LegacySyntax != VcardLegacySyntax.None || line.GetParameter(VcardLegacyTokens.CharsetName) != null || HasLegacyEncoding(line);
                if (hasLegacySyntax)
                {
                    // ref. RFC 2426 §5: "Only the "B" encoding of [RFC 2047] is an allowed value for the ENCODING parameter", and the CHARSET parameter "has been eliminated".
                    Add(found, VcardDiagnosticKind.LegacySyntax, "The line has a form of vCard 2.1 that RFC 2426 §5 removed: a parameter with no name, CHARSET, or an ENCODING other than b.", line, card);
                }

                Rule rule;
                if (!Rules.TryGetValue(line.Name, out rule)) continue;

                CheckParameters(line, rule, card, found);
                CheckValue(line, card, options, depth, found);
            }

            // ref. RFC 2426 §5: "The VERSION, N and FN types MUST be specified in a vCard."
            if (!hasName)
            {
                found.Add(new VcardDiagnostic(VcardDiagnosticKind.MissingRequiredProperty, "RFC 2426 §5 requires an N type.", VcardPropertyNames.Name, null, card.LineNumber, card.CardIndex));
            }

            if (!hasFormattedName)
            {
                found.Add(new VcardDiagnostic(VcardDiagnosticKind.MissingRequiredProperty, "RFC 2426 §5 requires an FN type.", VcardPropertyNames.FormattedName, null, card.LineNumber, card.CardIndex));
            }
        }

        /// <summary>
        /// Checks a <c>VERSION</c> line, which a card doesn't hold.
        /// </summary>
        internal static void ValidateVersionLine(VcardContentLine line, VcardCard card, List<VcardDiagnostic> found)
        {
            // ref. RFC 2426 §4: for name="VERSION", param = "", "No parameters allowed".
            if (line.ParameterArray.Length != 0)
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, "RFC 2426 §4 allows no parameters on a VERSION line.", line, card);
            }
        }

        /// <summary>
        /// Gets whether a type of vCard 3.0 allows a parameter.  A type no RFC defines may have any.
        /// </summary>
        internal static bool IsParameterAllowed(string propertyName, string parameterName)
        {
            Rule rule;
            return !Rules.TryGetValue(propertyName, out rule) || rule.Allows(parameterName);
        }

        /// <summary>
        /// Gets whether a type of vCard 3.0 allows a <c>TYPE</c> parameter that can say <c>pref</c>.  The media types use it for a format instead.
        /// </summary>
        internal static bool AllowsPreference(string propertyName)
        {
            Rule rule;
            return Rules.TryGetValue(propertyName, out rule) && rule.Allows(VcardParameterNames.Type) && !rule.Allows(VcardLegacyTokens.EncodingName);
        }

        /// <summary>
        /// Gets whether text is a language tag of RFC 1766 §2, which is the edition RFC 2426 §4 cites: <c>Language-Tag = Primary-tag *( "-" Subtag )</c>, with each 1 to 8 letters.
        /// </summary>
        /// <remarks>
        /// This holds <c>i-klingon</c>, which RFC 5646 keeps only as a grandfathered tag, and <c>en-cockney</c>.  It doesn't hold <c>es-419</c>, whose digits came with RFC 4646.
        /// </remarks>
        internal static bool IsRfc1766Tag(string text)
        {
            // TODO: RFC 2426 cites RFC 1766, which has no digits, and producers of vCard 3.0 today write BCP 47 tags such as es-419.  Revisit if that matters, with an opt-in.
            var length = 0;
            foreach (var character in text)
            {
                if (character == '-')
                {
                    if (length == 0) return false;

                    length = 0;
                }
                else if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))
                {
                    if (++length > MaxLanguageSubtagLength) return false;
                }
                else
                {
                    return false;
                }
            }

            return length != 0;
        }

        /// <summary>
        /// Gets whether a line's <c>ENCODING</c> says its value is base64, with the <c>b</c> of RFC 2426 §4, or under <see cref="VcardOptions.AcceptLegacySyntax"/> the <c>BASE64</c> of vCard 2.1.
        /// </summary>
        internal static bool IsBinary(VcardContentLine line)
        {
            Communication.Mime.MimeTransferEncoding encoding;
            return line.TryGetTransferEncoding(out encoding) && encoding == Communication.Mime.MimeTransferEncoding.Base64;
        }

        private static bool HasLegacyEncoding(VcardContentLine line)
        {
            var parameter = line.GetParameter(VcardLegacyTokens.EncodingName);
            return parameter != null && !string.Equals(parameter.ValueArray[0], VcardLegacyTokens.Version30Base64, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, Rule> CreateRules()
        {
            var text = new Rule(LanguageOnly, TextValueTypes, true);
            var addressed = new Rule(LanguageAndType, TextValueTypes, true);
            var media = new Rule(MediaParameters, MediaValueTypes, false);
            var typed = new Rule(TypeOnly, NoValueTypes, false);
            var none = new Rule(NoParameters, NoValueTypes, false);
            var dated = new Rule(NoParameters, DateValueTypes, false);

            return new Dictionary<string, Rule>(StringComparer.OrdinalIgnoreCase)
            {
                // ref. RFC 2426 §4.
                { VcardPropertyNames.SourceName, none },
                { VcardPropertyNames.Profile, none },
                { VcardPropertyNames.Source, new Rule(SourceParameters, new[] { UriValueName }, true) },
                { VcardPropertyNames.FormattedName, text },
                { VcardPropertyNames.Name, text },
                { VcardPropertyNames.Nickname, text },
                { VcardPropertyNames.Photo, media },
                { VcardPropertyNames.Birthday, dated },
                { VcardPropertyNames.Address, addressed },
                { VcardPropertyNames.PostalLabel, addressed },
                { VcardPropertyNames.Telephone, typed },
                { VcardPropertyNames.Email, typed },
                { VcardPropertyNames.Mailer, text },

                // ref. RFC 2426 §4 has no parameter on TZ, and §3.4.1 says that it "can also be reset to a single text value", as its example does.
                { VcardPropertyNames.TimeZone, new Rule(NoParameters, new[] { TextValueName }, false) },
                { VcardPropertyNames.Geo, none },
                { VcardPropertyNames.Title, text },
                { VcardPropertyNames.Role, text },
                { VcardPropertyNames.Logo, media },

                // ref. RFC 2426 §3.5.4: "It can also be reset to either a single text or uri value."
                { VcardPropertyNames.Agent, new Rule(NoParameters, new[] { TextValueName, UriValueName }, false) },
                { VcardPropertyNames.Organization, text },
                { VcardPropertyNames.Categories, text },
                { VcardPropertyNames.Note, text },
                { VcardPropertyNames.ProductId, none },
                { VcardPropertyNames.Revision, dated },
                { VcardPropertyNames.SortString, text },
                { VcardPropertyNames.Sound, media },

                // ref. RFC 2426 §4, as erratum 870 corrects it.
                { VcardPropertyNames.Uid, typed },
                { VcardPropertyNames.Url, none },
                { VcardPropertyNames.Classification, none },

                // ref. RFC 2426 §3.7.2: "It can also be reset to a text value", and §4 as erratum 1549 corrects it.
                { VcardPropertyNames.Key, new Rule(MediaParameters, new[] { BinaryValueName, TextValueName }, false) },

                // ref. RFC 4770 §3.
                { VcardPropertyNames.InstantMessaging, typed },

                // ref. RFC 2739 §2.3, whose erratum 7914 writes the preference as TYPE=PREF.
                { VcardPropertyNames.FreeBusyUrl, typed },
                { VcardPropertyNames.CalendarAddressUri, typed },
                { VcardPropertyNames.CalendarAccessUri, typed },
                { VcardPropertyNames.CalendarUri, typed }
            };
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        private static void Add(List<VcardDiagnostic> found, VcardDiagnosticKind kind, string message, VcardContentLine line, VcardCard card)
        {
            found.Add(new VcardDiagnostic(kind, message, line.Name, line.Group, line.LineNumber, card.CardIndex));
        }

        private static bool IsStructuralName(string name)
        {
            return string.Equals(name, VcardPropertyNames.Begin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.End, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Version, StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckParameters(VcardContentLine line, Rule rule, VcardCard card, List<VcardDiagnostic> found)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in line.ParameterArray)
            {
                var isRepeated = !names.Add(parameter.Name);
                if (string.Equals(parameter.Name, VcardParameterNames.Value, StringComparison.OrdinalIgnoreCase))
                {
                    if (!rule.AllowsValueType(parameter.ValueArray[0]))
                    {
                        Add(found, VcardDiagnosticKind.InvalidValue, Format("RFC 2426 §4 doesn't allow the value type {0} on {1}.", parameter.ValueArray[0], line.Name), line, card);
                    }
                }
                else if (string.Equals(parameter.Name, VcardLegacyTokens.CharsetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (string.Equals(parameter.Name, VcardLegacyTokens.EncodingName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!HasLegacyEncoding(line) && !rule.Allows(parameter.Name))
                    {
                        Add(found, VcardDiagnosticKind.InvalidParameter, Format("RFC 2426 §4 doesn't allow the parameter {0} on {1}.", parameter.Name, line.Name), line, card);
                    }
                }
                else if (!rule.Allows(parameter.Name))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("RFC 2426 §4 doesn't allow the parameter {0} on {1}.", parameter.Name, line.Name), line, card);
                }

                var isSingle = !string.Equals(parameter.Name, VcardParameterNames.Type, StringComparison.OrdinalIgnoreCase) &&
                               !parameter.Name.StartsWith(XNamePrefix, StringComparison.OrdinalIgnoreCase);
                if (isSingle && (isRepeated || parameter.ValueArray.Length > 1))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("The parameter {0} can have only one value and appear only once.", parameter.Name), line, card);
                }

                if (string.Equals(parameter.Name, VcardParameterNames.Language, StringComparison.OrdinalIgnoreCase) && !IsRfc1766Tag(parameter.ValueArray[0]))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("\"{0}\" is not a language tag of RFC 1766, which RFC 2426 §4 cites.", parameter.ValueArray[0]), line, card);
                }
            }
        }

        private static string GetValueType(VcardContentLine line)
        {
            return VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Value);
        }

        private static bool IsValueType(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckValue(VcardContentLine line, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
        {
            bool isValid;
            try
            {
                isValid = IsValidValue(line, options, depth);
            }
            catch (VcardFileException)
            {
                isValid = false;
            }

            if (!isValid)
            {
                Add(found, VcardDiagnosticKind.InvalidValue, Format("The value of {0} is not valid in vCard 3.0.", line.Name), line, card);
            }
        }

        private static bool IsValidValue(VcardContentLine line, VcardOptions options, int depth)
        {
            var valueType = GetValueType(line);
            if (IsMediaProperty(line.Name)) return IsValidMedia(line, valueType);

            string text;
            if (!VcardConformance.TryGetValueText(line, out text)) return false;

            var value = text.AsSpan();
            VcardStructuredValueView view;
            switch (line.Name.ToUpperInvariant())
            {
                case VcardPropertyNames.Name:
                    return VcardName.TryParseView(value, VcardValueFormat.Text, VcardVersion.V3_0, out view);
                case VcardPropertyNames.Address:
                    return VcardAddress.TryParseView(value, VcardValueFormat.Text, VcardVersion.V3_0, out view);
                case VcardPropertyNames.Organization:
                    return VcardOrganization.TryParseView(value, VcardValueFormat.Text, VcardVersion.V3_0, out view);
                case VcardPropertyNames.Nickname:
                case VcardPropertyNames.Categories:
                    foreach (var item in VcardValue.EnumerateList(value, VcardValueFormat.Text))
                    {
                        if (!VcardText.IsValidText(item, VcardVersion.V3_0)) return false;
                    }

                    return true;
                case VcardPropertyNames.Birthday:
                    return IsValidDate(value, valueType);
                case VcardPropertyNames.Revision:
                    return IsValidRevision(value, valueType);
                case VcardPropertyNames.TimeZone:
                    VcardUtcOffset offset;

                    // ref. RFC 2426 §3.4.1: its example of a text value, "-05:00; EST; Raleigh/North America", says that it "has a single value, not a structure text value", so a semicolon needn't be escaped.
                    return valueType is null ? VcardUtcOffset.TryParse(value, VcardValueFormat.Text, VcardVersion.V3_0, out offset) : VcardText.IsValidText(value, VcardVersion.V4_0);
                case VcardPropertyNames.Geo:
                    VcardGeo geo;
                    return VcardGeo.TryParse(value, VcardValueFormat.Text, VcardVersion.V3_0, out geo);
                case VcardPropertyNames.Source:
                case VcardPropertyNames.Url:
                case VcardPropertyNames.InstantMessaging:
                case VcardPropertyNames.FreeBusyUrl:
                case VcardPropertyNames.CalendarAddressUri:
                case VcardPropertyNames.CalendarAccessUri:
                case VcardPropertyNames.CalendarUri:
                    return VcardValue.IsValidUriText(value);
                case VcardPropertyNames.Profile:
                    // ref. RFC 2426 §4: 'Value MUST be the case insensitive value "VCARD"'.
                    return string.Equals(text, VcardCardBuilder.CardValue, StringComparison.OrdinalIgnoreCase);
                case VcardPropertyNames.Classification:
                    return VcardConformance.IsToken(text);
                case VcardPropertyNames.Agent:
                    return IsValidAgent(text, valueType, options, depth);
                default:
                    return VcardText.IsValidText(value, VcardVersion.V3_0);
            }
        }

        private static bool IsValidDate(ReadOnlySpan<char> value, string valueType)
        {
            VcardDateAndOrTime date;
            if (!VcardDateAndOrTime.TryParse(value, VcardValueFormat.Text, VcardVersion.V3_0, out date)) return false;

            // ref. RFC 2426 §4: "Value MUST match value type".  With no VALUE, §3.1.5's examples have a date and a date-time.
            if (IsValueType(valueType, DateValueName)) return date.Kind == VcardDateAndOrTimeKind.Date;

            return !IsValueType(valueType, DateTimeValueName) || date.Kind == VcardDateAndOrTimeKind.DateTime;
        }

        private static bool IsValidRevision(ReadOnlySpan<char> value, string valueType)
        {
            VcardTimestamp timestamp;
            var isTimestamp = VcardTimestamp.TryParse(value, VcardValueFormat.Text, VcardVersion.V3_0, out timestamp);
            if (IsValueType(valueType, DateTimeValueName)) return isTimestamp;

            // ref. RFC 2426 §3.6.4: its example "REV:1997-11-15" has a date with no VALUE.
            VcardDate date;
            return (isTimestamp && valueType is null) || VcardDate.TryParse(value, VcardValueFormat.Text, VcardVersion.V3_0, out date);
        }

        private static bool IsValidAgent(string text, string valueType, VcardOptions options, int depth)
        {
            if (IsValueType(valueType, UriValueName)) return VcardValue.IsValidUriText(text.AsSpan());
            if (!VcardText.IsValidText(text.AsSpan(), VcardVersion.V3_0)) return false;
            if (valueType != null) return true;

            // ref. RFC 2426 §4: "Value MUST be a valid vCard object".
            VcardCard nested;
            return VcardNestedCards.TryRead(VcardText.Unescape(text.AsSpan(), VcardVersion.V3_0), options, depth, out nested);
        }

        private static bool IsMediaProperty(string name)
        {
            return string.Equals(name, VcardPropertyNames.Photo, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Logo, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Sound, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Key, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidMedia(VcardContentLine line, string valueType)
        {
            if (IsBinary(line))
            {
                // ref. RFC 2426 §4: "Value and parameter MUST match".
                if (valueType != null && !IsValueType(valueType, BinaryValueName)) return false;

                line.GetDecodedValue();
                return true;
            }

            if (IsValueType(valueType, UriValueName)) return VcardValue.IsValidUriText(line.Value.AsSpan());

            // ref. RFC 2426 §4: only a key has a value that isn't binary or a URI: key-txt-param = "TYPE" "=" keytype.
            return string.Equals(line.Name, VcardPropertyNames.Key, StringComparison.OrdinalIgnoreCase) && !IsValueType(valueType, BinaryValueName) &&
                   VcardText.IsValidText(line.Value.AsSpan(), VcardVersion.V3_0);
        }

        /// <summary>
        /// What RFC 2426 §4 allows on one type.
        /// </summary>
        private sealed class Rule
        {
            private readonly bool _allowsXNames;
            private readonly string[] _parameters;
            private readonly string[] _valueTypes;

            internal Rule(string[] parameters, string[] valueTypes, bool allowsXNames)
            {
                _parameters = parameters;
                _valueTypes = valueTypes;
                _allowsXNames = allowsXNames;
            }

            internal bool Allows(string parameterName)
            {
                if (string.Equals(parameterName, VcardParameterNames.Value, StringComparison.OrdinalIgnoreCase)) return _valueTypes.Length != 0;
                if (_allowsXNames && parameterName.StartsWith(XNamePrefix, StringComparison.OrdinalIgnoreCase)) return true;

                foreach (var parameter in _parameters)
                {
                    if (string.Equals(parameter, parameterName, StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }

            internal bool AllowsValueType(string valueType)
            {
                foreach (var allowed in _valueTypes)
                {
                    if (string.Equals(allowed, valueType, StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }
        }
    }
}
