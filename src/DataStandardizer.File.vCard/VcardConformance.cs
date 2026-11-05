#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using DataStandardizer.Communication.Mime;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Checks cards against the rules of RFC 6350 and its extensions that <see cref="VcardReader"/> and <see cref="VcardWriter"/> enforce.
    /// </summary>
    internal static class VcardConformance
    {
        // ref. RFC 6350 §5.8: "The only value specified by iCalendar is 'gregorian'".
        private const string GregorianCalendarScale = "gregorian";

        private const string GroupKind = "group";

        // ref. RFC 6350 §6.1.5: the namespace an XML property MUST NOT have.
        private const string Version40Namespace = "urn:ietf:params:xml:ns:vcard-4.0";

        // ref. RFC 6350 §6.4.1: type-param-tel, which "MUST NOT be used with a property other than TEL".
        private static readonly string[] TelephoneTypes = { "text", "voice", "fax", "cell", "video", "pager", "textphone" };

        // ref. RFC 6350 §6.6.6: related-type-value, which "MUST NOT be used with a property other than RELATED".
        // ref. RFC 8605 §2.1: 'At least one "mailto", "http", or "https" URI value MUST be provided.'
        private static readonly string[] ContactUriSchemes = { "mailto", "http", "https" };

        private static readonly string[] RelatedTypes =
        {
            "contact", "acquaintance", "friend", "met", "co-worker", "colleague", "co-resident", "neighbor", "child", "parent", "sibling", "spouse", "kin", "muse", "crush", "date",
            "sweetheart", "me", "agent", "emergency"
        };

        /// <summary>
        /// Indicates whether an option accepts a kind of problem.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <param name="options">The options.</param>
        /// <param name="isWriting"><c>true</c> for a writer, which never writes the forms of vCard 2.1 in a vCard 4.0 card.</param>
        internal static bool IsAccepted(VcardDiagnosticKind kind, VcardOptions options, bool isWriting)
        {
            switch (kind)
            {
                case VcardDiagnosticKind.MissingRequiredProperty:
                    return options.AllowMissingRequiredProperties;
                case VcardDiagnosticKind.CardinalityViolation:
                    return options.AllowCardinalityViolations;
                case VcardDiagnosticKind.InvalidValue:
                case VcardDiagnosticKind.InvalidParameter:
                    return options.AllowInvalidValues;
                case VcardDiagnosticKind.LegacySyntax:
                    return !isWriting && options.AcceptLegacySyntax;
                case VcardDiagnosticKind.Lossy:
                    return options.AllowLossyVersionConversion;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Builds the exception for a problem a reader or writer does not accept.
        /// </summary>
        internal static VcardFileException BuildException(VcardDiagnostic diagnostic)
        {
            var exception = new VcardFileException(diagnostic.Message);
            exception.Data.Add(VcardFileException.DataItemName.DiagnosticKind, diagnostic.Kind);
            if (diagnostic.CardIndex >= 0)
            {
                exception.Data.Add(VcardFileException.DataItemName.CardIndex, diagnostic.CardIndex);
            }

            if (diagnostic.LineNumber != 0)
            {
                exception.Data.Add(VcardFileException.DataItemName.LineNumber, diagnostic.LineNumber);
            }

            if (diagnostic.PropertyName != null)
            {
                exception.Data.Add(VcardFileException.DataItemName.PropertyName, diagnostic.PropertyName);
            }

            if (diagnostic.Group != null)
            {
                exception.Data.Add(VcardFileException.DataItemName.Group, diagnostic.Group);
            }

            return exception;
        }

        /// <summary>
        /// Gets whether RFC 6350 or one of its extensions defines a parameter.
        /// </summary>
        internal static bool IsDefinedParameter(string name)
        {
            return string.Equals(name, VcardParameterNames.Value, StringComparison.OrdinalIgnoreCase) || GetParameterFlag(name) != VcardParameterSet.None;
        }

        /// <summary>
        /// Gets whether a line has no <c>CALSCALE</c>, or one this package understands.  ref. RFC 6350 §5.8: an implementation "MUST ignore properties with a CALSCALE parameter value
        /// that it does not understand".
        /// </summary>
        internal static bool IsUnderstoodCalendarScale(VcardContentLine line)
        {
            var calendarScale = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.CalendarScale);
            return calendarScale is null || string.Equals(calendarScale, GregorianCalendarScale, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the value of a line, as it appears in a vCard 4.0 content line.  On a line with the <c>ENCODING</c> or <c>CHARSET</c> of vCard 2.1, which only
        /// <see cref="VcardOptions.AcceptLegacySyntax"/> lets a reader accept, that means decoding it.
        /// </summary>
        /// <returns><c>false</c> if the value can't be decoded.</returns>
        internal static bool TryGetValueText(VcardContentLine line, out string text)
        {
            text = line.Value;
            if (!HasEncodingParameter(line)) return true;

            try
            {
                text = line.GetDecodedText();
                return true;
            }
            catch (VcardFileException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks a card.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="diagnostics">Receives the problems found, in the order of the lines they were found on.</param>
        internal static void Validate(VcardCard card, List<VcardDiagnostic> diagnostics)
        {
            var found = new List<VcardDiagnostic>();
            var sourceIds = GetPidSourceIds(card);
            var altIds = new HashSet<string>(StringComparer.Ordinal);
            var instanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var isGroup = IsGroup(card);
            foreach (var line in card.Properties)
            {
                var info = VcardPropertyInfo.Find(line.Name);
                if (info != null && !info.IsDefinedIn(VcardVersion.V4_0))
                {
                    info = null;
                }

                if (IsStructuralName(line.Name))
                {
                    Add(found, VcardDiagnosticKind.InvalidStructure, Format("A {0} line can't be in the properties of a card; a writer writes it.", line.Name), line, card);
                    continue;
                }

                CheckLegacySyntax(line, card, found);
                CheckParameters(line, info, sourceIds, card, found);
                if (info != null)
                {
                    // ref. RFC 6350 §5.8: a property with a CALSCALE this package doesn't understand is ignored, so what its value means isn't known.
                    if (IsUnderstoodCalendarScale(line))
                    {
                        CheckValue(line, info, card, found);
                    }

                    CheckCardinality(line, info, altIds, instanceCounts, card, found);
                    if (!isGroup && string.Equals(line.Name, VcardPropertyNames.Member, StringComparison.OrdinalIgnoreCase))
                    {
                        Add(found, VcardDiagnosticKind.CardinalityViolation, "RFC 6350 §6.6.5 allows a MEMBER property only if the KIND property is \"group\".", line, card);
                    }
                }
            }

            CheckContactUris(card, found);

            int formattedNames;
            if (!instanceCounts.TryGetValue(VcardPropertyNames.FormattedName, out formattedNames))
            {
                found.Add(new VcardDiagnostic(VcardDiagnosticKind.MissingRequiredProperty, "RFC 6350 §6.2.1 requires at least one FN property.", VcardPropertyNames.FormattedName, null,
                    card.LineNumber, card.CardIndex));
            }

            // A stable sort, so problems on the same line, and in a card built in code, stay in the order they were found.
            diagnostics.AddRange(found.OrderBy(diagnostic => diagnostic.LineNumber == 0 ? long.MaxValue : diagnostic.LineNumber));
        }

        /// <summary>
        /// Checks a <c>VERSION</c> line, which a card doesn't hold, by the rules of RFC 6350 §6.7.9 for its parameters: <c>VALUE=text</c> and <c>any-param</c>.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="card">The card it is in.</param>
        /// <param name="found">Receives the problems found.</param>
        internal static void ValidateVersionLine(VcardContentLine line, VcardCard card, List<VcardDiagnostic> found)
        {
            var info = VcardPropertyInfo.Find(VcardPropertyNames.Version);
            CheckLegacySyntax(line, card, found);
            CheckParameters(line, info, new HashSet<long>(), card, found);
            CheckValue(line, info, card, found);
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

        private static bool IsGroup(VcardCard card)
        {
            foreach (var line in card.Properties)
            {
                if (string.Equals(line.Name, VcardPropertyNames.Kind, StringComparison.OrdinalIgnoreCase))
                {
                    string text;
                    return TryGetValueText(line, out text) && string.Equals(text, GroupKind, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static HashSet<long> GetPidSourceIds(VcardCard card)
        {
            var sourceIds = new HashSet<long>();
            foreach (var line in card.Properties)
            {
                VcardClientPidMap map;
                string text;
                if (string.Equals(line.Name, VcardPropertyNames.ClientPidMap, StringComparison.OrdinalIgnoreCase) && TryGetValueText(line, out text) &&
                    VcardClientPidMap.TryParse(text.AsSpan(), VcardValueFormat.Text, out map))
                {
                    sourceIds.Add(map.SourceId);
                }
            }

            return sourceIds;
        }

        internal static bool HasEncodingParameter(VcardContentLine line)
        {
            foreach (var parameter in line.ParameterArray)
            {
                if (string.Equals(parameter.Name, VcardLegacyTokens.EncodingName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter.Name, VcardLegacyTokens.CharsetName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckLegacySyntax(VcardContentLine line, VcardCard card, List<VcardDiagnostic> found)
        {
            // ref. RFC 6350 Appendix A: vCard 4.0 removed ENCODING and CHARSET, and §5 has no parameter without a name.
            if (line.LegacySyntax != VcardLegacySyntax.None || HasEncodingParameter(line))
            {
                Add(found, VcardDiagnosticKind.LegacySyntax, "The line has a form of vCard 2.1 or 3.0 that RFC 6350 removed: a parameter with no name, or ENCODING or CHARSET.", line, card);
            }
        }

        private static void CheckCardinality(VcardContentLine line, VcardPropertyInfo info, HashSet<string> altIds, Dictionary<string, int> instanceCounts, VcardCard card,
            List<VcardDiagnostic> found)
        {
            // ref. RFC 6350 §5.4: "Property instances having the same ALTID parameter value count as 1 toward cardinality."
            var altId = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.AlternativeId);
            if (altId != null && !altIds.Add(info.Name + "\0" + altId)) return;

            int count;
            instanceCounts.TryGetValue(info.Name, out count);
            count++;
            instanceCounts[info.Name] = count;
            if (info.Cardinality == VcardCardinality.AtMostOne && count > 1)
            {
                Add(found, VcardDiagnosticKind.CardinalityViolation, Format("vCard 4.0 allows at most one {0} property.", info.Name), line, card);
            }
        }

        private static void CheckParameters(VcardContentLine line, VcardPropertyInfo info, HashSet<long> sourceIds, VcardCard card, List<VcardDiagnostic> found)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in line.ParameterArray)
            {
                var isRepeated = !names.Add(parameter.Name);
                if (string.Equals(parameter.Name, VcardParameterNames.Value, StringComparison.OrdinalIgnoreCase))
                {
                    if (info != null && !info.AllowsValueParameter)
                    {
                        Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 4.0 doesn't allow a VALUE parameter on {0}.", info.Name), line, card);
                    }

                    CheckSingleValue(line, parameter, isRepeated, card, found);
                    continue;
                }

                var flag = GetParameterFlag(parameter.Name);
                if (info != null)
                {
                    if (flag == VcardParameterSet.None)
                    {
                        if (!info.AllowsOtherParameters && !IsLegacyParameterName(parameter.Name))
                        {
                            Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 4.0 doesn't allow the parameter {0} on {1}.", parameter.Name, info.Name), line, card);
                        }
                    }
                    else if ((info.Parameters & flag) == 0)
                    {
                        Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 4.0 doesn't allow the parameter {0} on {1}.", parameter.Name, info.Name), line, card);
                    }
                }

                switch (flag)
                {
                    case VcardParameterSet.Pid:
                        CheckPid(line, parameter, sourceIds, card, found);
                        break;
                    case VcardParameterSet.Type:
                        CheckTypes(line, parameter, info, card, found);
                        break;
                    case VcardParameterSet.None:
                        break;
                    default:
                        CheckSingleValue(line, parameter, isRepeated, card, found);
                        CheckParameterValue(line, parameter, flag, info, card, found);
                        break;
                }
            }
        }

        private static bool IsLegacyParameterName(string name)
        {
            return string.Equals(name, VcardLegacyTokens.EncodingName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardLegacyTokens.CharsetName, StringComparison.OrdinalIgnoreCase);
        }

        private static VcardParameterSet GetParameterFlag(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case VcardParameterNames.Language:
                    return VcardParameterSet.Language;
                case VcardParameterNames.Preference:
                    return VcardParameterSet.Preference;
                case VcardParameterNames.AlternativeId:
                    return VcardParameterSet.AlternativeId;
                case VcardParameterNames.Pid:
                    return VcardParameterSet.Pid;
                case VcardParameterNames.Type:
                    return VcardParameterSet.Type;
                case VcardParameterNames.MediaType:
                    return VcardParameterSet.MediaType;
                case VcardParameterNames.CalendarScale:
                    return VcardParameterSet.CalendarScale;
                case VcardParameterNames.SortAs:
                    return VcardParameterSet.SortAs;
                case VcardParameterNames.Geo:
                    return VcardParameterSet.Geo;
                case VcardParameterNames.TimeZone:
                    return VcardParameterSet.TimeZone;
                case VcardParameterNames.Label:
                    return VcardParameterSet.Label;
                case VcardParameterNames.Index:
                    return VcardParameterSet.Index;
                case VcardParameterNames.Level:
                    return VcardParameterSet.Level;
                case VcardParameterNames.CountryCode:
                    return VcardParameterSet.CountryCode;
                default:
                    return VcardParameterSet.None;
            }
        }

        private static void CheckSingleValue(VcardContentLine line, VcardContentLineParameter parameter, bool isRepeated, VcardCard card, List<VcardDiagnostic> found)
        {
            // SORT-AS holds a list.  ref. RFC 6350 §5.9.
            var isList = string.Equals(parameter.Name, VcardParameterNames.SortAs, StringComparison.OrdinalIgnoreCase);
            if (isRepeated || (!isList && parameter.ValueArray.Length > 1))
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, Format("The parameter {0} can have only one value and appear only once.", parameter.Name), line, card);
            }
        }

        private static void CheckPid(VcardContentLine line, VcardContentLineParameter parameter, HashSet<long> sourceIds, VcardCard card, List<VcardDiagnostic> found)
        {
            foreach (var value in parameter.ValueArray)
            {
                VcardPid pid;
                if (!VcardPid.TryParse(value.AsSpan(), VcardValueFormat.Text, out pid))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("\"{0}\" is not a PID value of RFC 6350 §5.5.", value), line, card);
                }
                else if (pid.SourceId.HasValue && !sourceIds.Contains(pid.SourceId.Value))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter,
                        Format("RFC 6350 §6.7.7 requires a CLIENTPIDMAP for each source identifier, and there is none for {0}.", pid.SourceId.Value), line, card);
                }
            }
        }

        private static void CheckTypes(VcardContentLine line, VcardContentLineParameter parameter, VcardPropertyInfo info, VcardCard card, List<VcardDiagnostic> found)
        {
            foreach (var value in parameter.ValueArray)
            {
                foreach (var type in value.Split(','))
                {
                    // ref. RFC 6350 §5.6: type-value is a token.
                    if (!IsToken(type))
                    {
                        Add(found, VcardDiagnosticKind.InvalidParameter, Format("\"{0}\" is not a TYPE value of RFC 6350 §5.6.", type), line, card);
                    }
                    else if (info != null && IsTypeOfAnotherProperty(type, info.Name))
                    {
                        Add(found, VcardDiagnosticKind.InvalidParameter, Format("RFC 6350 doesn't allow the type {0} on {1}.", type, info.Name), line, card);
                    }
                }
            }
        }

        private static bool IsTypeOfAnotherProperty(string type, string propertyName)
        {
            if (!string.Equals(propertyName, VcardPropertyNames.Telephone, StringComparison.Ordinal) && ContainsIgnoringCase(TelephoneTypes, type)) return true;

            return !string.Equals(propertyName, VcardPropertyNames.Related, StringComparison.Ordinal) && ContainsIgnoringCase(RelatedTypes, type);
        }

        private static bool ContainsIgnoringCase(string[] values, string value)
        {
            foreach (var candidate in values)
            {
                if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static void CheckParameterValue(VcardContentLine line, VcardContentLineParameter parameter, VcardParameterSet flag, VcardPropertyInfo info, VcardCard card,
            List<VcardDiagnostic> found)
        {
            var value = parameter.ValueArray[0];
            var isValid = true;
            switch (flag)
            {
                case VcardParameterSet.Language:
                    Bcp47LanguageTag language;
                    isValid = VcardValue.TryParseLanguageTag(value.AsSpan(), VcardValueFormat.Text, out language);
                    break;
                case VcardParameterSet.Preference:
                    int preference;
                    isValid = VcardPropertyParameterReader.TryParsePreference(value, out preference);
                    break;
                case VcardParameterSet.MediaType:
                    MimeMediaType mediaType;
                    isValid = MimeMediaType.TryParse(value.AsSpan(), out mediaType);
                    break;
                case VcardParameterSet.CalendarScale:
                    isValid = IsToken(value);
                    break;
                case VcardParameterSet.Geo:
                    isValid = VcardValue.IsUriText(value.AsSpan());
                    break;
                case VcardParameterSet.Index:
                    long index;
                    isValid = VcardPropertyParameterReader.TryParseIndex(value, out index);
                    break;
                case VcardParameterSet.Level:
                    VcardLevel level;
                    isValid = VcardPropertyParameterReader.TryParseLevel(value, line.Name, out level);
                    break;
                case VcardParameterSet.CountryCode:
                    Iso3166Part1Alpha2Country countryCode;
                    isValid = VcardPropertyParameterReader.TryParseCountryCode(value, out countryCode);
                    break;
            }

            if (!isValid)
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, Format("\"{0}\" is not a valid value of the parameter {1}.", value, parameter.Name), line, card);
            }

            if (info != null)
            {
                CheckParameterMatchesValueType(line, parameter, flag, info, card, found);
            }

            if (flag == VcardParameterSet.SortAs)
            {
                CheckSortAs(line, parameter, card, found);
            }
        }

        private static void CheckParameterMatchesValueType(VcardContentLine line, VcardContentLineParameter parameter, VcardParameterSet flag, VcardPropertyInfo info, VcardCard card,
            List<VcardDiagnostic> found)
        {
            var valueType = VcardValueTypeRegistry.GetValueType(line);
            var isKeyOrTelephoneOrRelated = info.Name == VcardPropertyNames.Telephone || info.Name == VcardPropertyNames.Key || info.Name == VcardPropertyNames.Related;
            var isDateOfBirthOrDeath = info.Name == VcardPropertyNames.Birthday || info.Name == VcardPropertyNames.DeathDate;

            // ref. RFC 6350 §6.4.1, §6.6.6 and §6.8.1: mediatype-param is only with a URI, and "Value and parameter MUST match".
            var isMismatch = flag == VcardParameterSet.MediaType && isKeyOrTelephoneOrRelated && valueType != VcardValueType.Uri;

            // ref. RFC 6350 §6.2.5 and §6.6.6, and RFC 6474 §2.3: language-param is only with text.
            isMismatch |= flag == VcardParameterSet.Language && (isDateOfBirthOrDeath || info.Name == VcardPropertyNames.Related) && valueType != VcardValueType.Text;

            // ref. RFC 6350 §6.2.5 and §6.2.6, and RFC 6474 §2.3: "calscale-param can only be present when BDAY-value is date-and-or-time and actually contains a date or date-time."
            if (flag == VcardParameterSet.CalendarScale && (isDateOfBirthOrDeath || info.Name == VcardPropertyNames.Anniversary))
            {
                VcardDateAndOrTime dateAndOrTime;
                isMismatch |= valueType != VcardValueType.DateAndOrTime ||
                              (VcardDateAndOrTime.TryParse(line.Value.AsSpan(), VcardValueFormat.Text, out dateAndOrTime) && dateAndOrTime.Kind == VcardDateAndOrTimeKind.Time);
            }

            if (isMismatch)
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 4.0 doesn't allow the parameter {0} on {1} with this type of value.", parameter.Name, info.Name), line, card);
            }
        }

        private static void CheckContactUris(VcardCard card, List<VcardDiagnostic> found)
        {
            var instances = card.GetProperties(VcardPropertyNames.ContactUri);
            if (instances.Count == 0) return;

            var hasRequiredScheme = false;
            var hasPreference = false;
            foreach (var line in instances)
            {
                string text;
                Uri uri;
                hasRequiredScheme |= TryGetValueText(line, out text) && VcardValue.TryParseUri(text.AsSpan(), VcardValueFormat.Text, out uri) && ContainsIgnoringCase(ContactUriSchemes, uri.Scheme);
                hasPreference |= VcardPropertyParameterReader.GetPreferenceOrLeast(line) <= VcardPropertyParameterReader.MaxPreference;
            }

            if (!hasRequiredScheme)
            {
                Add(found, VcardDiagnosticKind.MissingRequiredProperty, "RFC 8605 §2.1 requires at least one CONTACT-URI with a mailto, http or https URI.", instances[0], card);
            }

            // ref. RFC 8605 §2.1: "If multiple CONTACT-URI properties are used, the vCard PREF parameter MUST be used to describe the most preferred property".
            if (instances.Count > 1 && !hasPreference)
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, "RFC 8605 §2.1 requires a PREF parameter where a card has several CONTACT-URI properties.", instances[0], card);
            }
        }

        private static void CheckSortAs(VcardContentLine line, VcardContentLineParameter parameter, VcardCard card, List<VcardDiagnostic> found)
        {
            // ref. RFC 6350 §5.9: "as many or fewer elements as the corresponding property value has components".  Only N and ORG have it, and a parameter elsewhere is reported anyway.
            string text;
            if (!TryGetValueText(line, out text)) return;

            VcardStructuredValueView view;
            bool isParsed;
            if (string.Equals(line.Name, VcardPropertyNames.Name, StringComparison.OrdinalIgnoreCase))
            {
                isParsed = VcardName.TryParseView(text.AsSpan(), VcardValueFormat.Text, out view);
            }
            else if (string.Equals(line.Name, VcardPropertyNames.Organization, StringComparison.OrdinalIgnoreCase))
            {
                isParsed = VcardOrganization.TryParseView(text.AsSpan(), VcardValueFormat.Text, out view);
            }
            else
            {
                return;
            }

            if (isParsed && VcardPropertyParameterReader.GetListValues(line, parameter.Name).Count > view.ComponentCount)
            {
                Add(found, VcardDiagnosticKind.InvalidParameter, "RFC 6350 §5.9 allows SORT-AS no more elements than the value has components.", line, card);
            }
        }

        private static void CheckValue(VcardContentLine line, VcardPropertyInfo info, VcardCard card, List<VcardDiagnostic> found)
        {
            var valueType = VcardValueTypeRegistry.GetValueType(line);
            if (!VcardValueTypeRegistry.IsAllowedValueType(info.Name.AsSpan(), valueType))
            {
                var valueParameter = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Value);
                Add(found, VcardDiagnosticKind.InvalidValue, Format("vCard 4.0 doesn't allow the value type {0} on {1}.", valueParameter, info.Name), line, card);
                return;
            }

            string text;
            if (!TryGetValueText(line, out text) || !IsValidValue(text, info.Name, valueType))
            {
                Add(found, VcardDiagnosticKind.InvalidValue, Format("The value of {0} is not a valid {1}.", info.Name, DescribeValue(info.Name, valueType)), line, card);
            }
        }

        private static string DescribeValue(string name, VcardValueType valueType)
        {
            switch (name)
            {
                case VcardPropertyNames.Name:
                case VcardPropertyNames.Address:
                case VcardPropertyNames.Organization:
                case VcardPropertyNames.Gender:
                case VcardPropertyNames.ClientPidMap:
                    return "structured value";
                case VcardPropertyNames.Xml:
                    return "XML element in a namespace other than that of vCard 4.0";
                case VcardPropertyNames.Kind:
                    return "kind";
                default:
                    return VcardValueTypeRegistry.GetValueTypeName(valueType);
            }
        }

        private static bool IsValidValue(string text, string name, VcardValueType valueType)
        {
            var value = text.AsSpan();
            switch (valueType)
            {
                case VcardValueType.Text:
                    return IsValidTextValue(text, name);
                case VcardValueType.Uri:
                    Uri uri;
                    return VcardValue.TryParseUri(value, VcardValueFormat.Text, out uri);
                case VcardValueType.DateAndOrTime:
                    VcardDateAndOrTime dateAndOrTime;
                    return VcardDateAndOrTime.TryParse(value, VcardValueFormat.Text, out dateAndOrTime);
                case VcardValueType.Timestamp:
                    VcardTimestamp timestamp;
                    return VcardTimestamp.TryParse(value, VcardValueFormat.Text, out timestamp);
                case VcardValueType.UtcOffset:
                    VcardUtcOffset utcOffset;
                    return VcardUtcOffset.TryParse(value, VcardValueFormat.Text, out utcOffset);
                case VcardValueType.LanguageTag:
                    Bcp47LanguageTag languageTag;
                    return VcardValue.TryParseLanguageTag(value, VcardValueFormat.Text, out languageTag);
                default:
                    return false;
            }
        }

        private static bool IsValidTextValue(string text, string name)
        {
            var value = text.AsSpan();
            VcardStructuredValueView view;
            switch (name)
            {
                case VcardPropertyNames.Name:
                    return VcardName.TryParseView(value, VcardValueFormat.Text, out view);
                case VcardPropertyNames.Address:
                    return VcardAddress.TryParseView(value, VcardValueFormat.Text, out view);
                case VcardPropertyNames.Organization:
                    return VcardOrganization.TryParseView(value, VcardValueFormat.Text, out view);
                case VcardPropertyNames.Gender:
                    VcardGender gender;
                    return VcardGender.TryParse(value, VcardValueFormat.Text, out gender);
                case VcardPropertyNames.ClientPidMap:
                    VcardClientPidMap map;
                    return VcardClientPidMap.TryParse(value, VcardValueFormat.Text, out map);
                case VcardPropertyNames.Nickname:
                case VcardPropertyNames.Categories:
                    foreach (var item in VcardValue.EnumerateList(value, VcardValueFormat.Text))
                    {
                        if (!IsValidText(item)) return false;
                    }

                    return true;
                case VcardPropertyNames.Kind:
                    // ref. RFC 6350 §6.1.4: KIND-value is one of the values given, an iana-token or an x-name.
                    return IsToken(text);
                case VcardPropertyNames.Xml:
                    return IsValidText(value) && IsValidXmlProperty(text);
                default:
                    return IsValidText(value);
            }
        }

        private static bool IsValidText(ReadOnlySpan<char> value)
        {
            var buffer = new char[value.Length];
            int charsWritten;
            return VcardValue.TryParseText(value, VcardValueFormat.Text, VcardVersion.V4_0, buffer, out charsWritten);
        }

        private static bool IsValidXmlProperty(string text)
        {
            string unescaped;
            return VcardValue.TryParseText(text.AsSpan(), VcardValueFormat.Text, out unescaped) && IsValidXmlElement(unescaped);
        }

        /// <summary>
        /// Checks that text, with its escaping removed, is one XML element in an explicit namespace that isn't that of vCard 4.0.  ref. RFC 6350 §6.1.5.
        /// </summary>
        internal static bool IsValidXmlElement(string unescaped)
        {
            // The text comes from outside, so it is not allowed a DTD, and so no entity can expand.
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, DtdProcessing = DtdProcessing.Prohibit };
            try
            {
                using (var reader = XmlReader.Create(new StringReader(unescaped), settings))
                {
                    var elementCount = 0;
                    while (!reader.EOF)
                    {
                        reader.MoveToContent();
                        if (reader.EOF) break;

                        if (reader.NodeType != XmlNodeType.Element || ++elementCount > 1) return false;
                        if (reader.NamespaceURI.Length == 0 || string.Equals(reader.NamespaceURI, Version40Namespace, StringComparison.Ordinal)) return false;

                        reader.Skip();
                    }

                    return elementCount == 1;
                }
            }
            catch (XmlException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets whether text is an <c>iana-token</c> or <c>x-name</c> of RFC 6350 §3.3: one or more letters, digits or '-'.
        /// </summary>
        private static bool IsToken(string text)
        {
            if (text.Length == 0) return false;

            foreach (var character in text)
            {
                if (!((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || VcardValueFormatting.IsDigit(character) || character == '-')) return false;
            }

            return true;
        }
    }
}
